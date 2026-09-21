import 'dart:async';

import 'package:app_links/app_links.dart';
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/date_symbol_data_local.dart';

import 'l10n/app_localizations.dart';
import 'screens/circle_overview/circle_overview_screen.dart';
import 'screens/circle_overview/member_history_screen.dart';
import 'screens/create_circle/create_circle_screen.dart';
import 'screens/dashboard/dashboard_screen.dart';
import 'screens/invite/invite_screen.dart';
import 'screens/login/login_screen.dart';
import 'screens/my_circles/my_circles_screen.dart';
import 'screens/profile/profile_screen.dart';
import 'screens/register/register_screen.dart';
import 'state/locale_provider.dart';
import 'state/providers.dart';
import 'theme/app_theme.dart';
import 'utils/invite_token.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  // Needed so DateFormat.yMMMM('ar')/('en') can render month/year labels — mirrors
  // the date formatting used throughout CurrentCycleTab.tsx / HistoryTab.tsx etc.
  await initializeDateFormatting('ar');
  await initializeDateFormatting('en');
  runApp(const ProviderScope(child: DourakApp()));
}

/// go_router with an auth guard mirroring the protected-route wrapper in
/// frontend/src/app — signed-out users are redirected to /login for every screen
/// except /login and /register.
final _routerProvider = Provider<GoRouter>((ref) {
  return GoRouter(
    initialLocation: '/',
    refreshListenable: _AuthListenable(ref),
    redirect: (context, state) {
      final isAuthenticated = ref.read(authControllerProvider).isAuthenticated;
      final goingToAuth = state.matchedLocation == '/login' || state.matchedLocation == '/register';
      // /invite/:token is reachable signed out — it only stashes the token and then sends
      // the person to /login itself (mirrors the web's InvitePage).
      if (state.matchedLocation.startsWith('/invite/')) return null;
      if (!isAuthenticated && !goingToAuth) return '/login';
      if (isAuthenticated && goingToAuth) return '/';
      return null;
    },
    routes: [
      GoRoute(path: '/login', builder: (context, state) => const LoginScreen()),
      GoRoute(path: '/register', builder: (context, state) => const RegisterScreen()),
      GoRoute(path: '/', builder: (context, state) => const DashboardScreen()),
      GoRoute(path: '/circles', builder: (context, state) => const MyCirclesScreen()),
      GoRoute(path: '/circles/new', builder: (context, state) => const CreateCircleScreen()),
      GoRoute(path: '/profile', builder: (context, state) => const ProfileScreen()),
      GoRoute(
        path: '/invite/:token',
        builder: (context, state) => InviteScreen(token: state.pathParameters['token']!),
      ),
      GoRoute(
        path: '/circles/:circleId',
        builder: (context, state) {
          final id = int.parse(state.pathParameters['circleId']!);
          final tab = state.uri.queryParameters['tab'];
          return CircleOverviewScreen(circleId: id, initialTab: tab);
        },
      ),
      GoRoute(
        path: '/circles/:circleId/members/:memberId/history',
        builder: (context, state) {
          final circleId = int.parse(state.pathParameters['circleId']!);
          final memberId = int.parse(state.pathParameters['memberId']!);
          return MemberHistoryScreen(circleId: circleId, memberId: memberId);
        },
      ),
    ],
  );
});

class _AuthListenable extends ChangeNotifier {
  _AuthListenable(this.ref) {
    ref.listen(authControllerProvider, (_, __) => notifyListeners());
  }
  final Ref ref;
}

/// Listens for incoming `https://dourak.money/invite/{token}` App Links and the
/// `dourak://invite/{token}` fallback scheme, in both the cold-start ("the link launched
/// the app") and warm ("the app was already running") cases, and routes them to the
/// in-app `/invite/:token` handler. Anything that isn't an invite link is ignored, so a
/// stray link can never dead-end the app.
class _DeepLinkListener extends ConsumerStatefulWidget {
  const _DeepLinkListener({required this.child});
  final Widget child;

  @override
  ConsumerState<_DeepLinkListener> createState() => _DeepLinkListenerState();
}

class _DeepLinkListenerState extends ConsumerState<_DeepLinkListener> {
  final _appLinks = AppLinks();
  StreamSubscription<Uri>? _sub;

  @override
  void initState() {
    super.initState();
    _sub = _appLinks.uriLinkStream.listen(_handle, onError: (_) {});
    // The stream replays the launch intent on most platforms, but not every one — asking
    // explicitly covers the cold-start case either way (handling the same link twice is
    // harmless: it stashes the same token and navigates to the same route).
    // Deferred one frame so the router is attached before the first navigation.
    WidgetsBinding.instance.addPostFrameCallback((_) {
      _appLinks.getInitialLink().then((uri) {
        if (uri != null && mounted) _handle(uri);
      }).catchError((_) {});
    });
  }

  void _handle(Uri uri) {
    final token = inviteTokenFromUri(uri);
    if (token == null || token.isEmpty) return;
    ref.read(_routerProvider).go('/invite/$token');
  }

  @override
  void dispose() {
    _sub?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => widget.child;
}

class DourakApp extends ConsumerWidget {
  const DourakApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final locale = ref.watch(localeProvider);
    final direction = locale.languageCode == 'ar' ? TextDirection.rtl : TextDirection.ltr;

    // The stored-token check (AuthController._bootstrap) is async, so its result isn't
    // known on the very first frame. Hold the whole app on a blank splash until it
    // resolves, instead of letting GoRouter's redirect run against a not-yet-known
    // isAuthenticated value — the fix for "the app makes me log in again every time"
    // (it wasn't losing the token, it was racing the read of it).
    final isBootstrapping = ref.watch(authControllerProvider.select((a) => a.isBootstrapping));
    if (isBootstrapping) {
      return Directionality(
        textDirection: direction,
        child: MaterialApp(
          debugShowCheckedModeBanner: false,
          theme: buildDourakTheme(direction),
          home: const Scaffold(body: Center(child: CircularProgressIndicator())),
        ),
      );
    }

    final router = ref.watch(_routerProvider);
    return _DeepLinkListener(
      child: Directionality(
        textDirection: direction,
        child: MaterialApp.router(
          title: 'Dourak',
          debugShowCheckedModeBanner: false,
          theme: buildDourakTheme(direction),
          locale: locale,
          supportedLocales: AppLocalizations.supportedLocales,
          localizationsDelegates: const [
            AppLocalizations.delegate,
            GlobalMaterialLocalizations.delegate,
            GlobalWidgetsLocalizations.delegate,
            GlobalCupertinoLocalizations.delegate,
          ],
          routerConfig: router,
          builder: (context, child) => Directionality(textDirection: direction, child: child!),
        ),
      ),
    );
  }
}
