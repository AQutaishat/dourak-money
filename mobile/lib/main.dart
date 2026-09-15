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
import 'screens/login/login_screen.dart';
import 'screens/my_circles/my_circles_screen.dart';
import 'screens/profile/profile_screen.dart';
import 'screens/register/register_screen.dart';
import 'state/locale_provider.dart';
import 'state/providers.dart';
import 'theme/app_theme.dart';

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

class DourakApp extends ConsumerWidget {
  const DourakApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final locale = ref.watch(localeProvider);
    final router = ref.watch(_routerProvider);
    final direction = locale.languageCode == 'ar' ? TextDirection.rtl : TextDirection.ltr;

    return Directionality(
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
    );
  }
}
