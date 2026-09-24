import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:url_launcher/url_launcher.dart';

import '../l10n/app_localizations.dart';
import '../state/locale_provider.dart';
import '../state/providers.dart';
import '../utils/whatsapp.dart' show dourakAppUrl;

/// A shared top bar + drawer for the authenticated screens, mirroring the top nav
/// in frontend/src/layouts: app name, dashboard/my-circles links, a language
/// switcher, and an account menu (profile + logout) — see AuthContext.tsx §8.
class AppScaffold extends ConsumerWidget {
  const AppScaffold({super.key, required this.title, required this.body, this.floatingActionButton, this.actions});

  final String title;
  final Widget body;
  final Widget? floatingActionButton;
  final List<Widget>? actions;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final auth = ref.watch(authControllerProvider);
    final locale = ref.watch(localeProvider);

    return Scaffold(
      appBar: AppBar(
        // No custom `leading` here on purpose — setting one suppresses Flutter's automatic
        // drawer-toggle (hamburger) button, which made the drawer below (My Circles, User
        // Guide, Support) unreachable with no visible way to open it. The logo moves into
        // the title row instead so it's still shown, without hiding the menu button.
        title: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            ClipRRect(
              borderRadius: BorderRadius.circular(6),
              child: Image.asset('assets/images/dourak_logo.png', width: 28, height: 28),
            ),
            const SizedBox(width: 10),
            Flexible(child: Text(title, overflow: TextOverflow.ellipsis)),
          ],
        ),
        actions: [
          ...?actions,
          IconButton(
            tooltip: 'AR / EN',
            icon: const Icon(Icons.translate),
            onPressed: () {
              final next = locale.languageCode == 'ar' ? const Locale('en') : const Locale('ar');
              ref.read(localeProvider.notifier).state = next;
            },
          ),
          PopupMenuButton<String>(
            icon: const Icon(Icons.account_circle),
            onSelected: (value) async {
              if (value == 'profile') {
                context.push('/profile');
              } else if (value == 'logout') {
                await ref.read(authControllerProvider.notifier).logout();
                if (context.mounted) context.go('/login');
              }
            },
            itemBuilder: (context) => [
              PopupMenuItem(
                enabled: false,
                child: Text(auth.displayLabel, style: const TextStyle(fontWeight: FontWeight.bold)),
              ),
              const PopupMenuDivider(),
              PopupMenuItem(value: 'profile', child: Text(context.t('nav.myProfile'))),
              PopupMenuItem(value: 'logout', child: Text(context.t('nav.logout'))),
            ],
          ),
        ],
      ),
      drawer: Drawer(
        child: SafeArea(
          child: ListView(
            children: [
              DrawerHeader(
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.center,
                  children: [
                    ClipRRect(
                      borderRadius: BorderRadius.circular(8),
                      child: Image.asset('assets/images/dourak_logo.png', width: 40, height: 40),
                    ),
                    const SizedBox(width: 12),
                    Text(context.t('app.name'), style: Theme.of(context).textTheme.titleLarge),
                  ],
                ),
              ),
              ListTile(
                leading: const Icon(Icons.home_outlined),
                title: Text(context.t('nav.dashboard')),
                onTap: () {
                  Navigator.pop(context);
                  context.go('/');
                },
              ),
              ListTile(
                leading: const Icon(Icons.groups_outlined),
                title: Text(context.t('nav.myCircles')),
                onTap: () {
                  Navigator.pop(context);
                  context.go('/circles');
                },
              ),
              ListTile(
                leading: const Icon(Icons.person_outline),
                title: Text(context.t('nav.myProfile')),
                onTap: () {
                  Navigator.pop(context);
                  context.push('/profile');
                },
              ),
              const Divider(),
              // Mirrors the web nav's "User Guide" button (AppLayout.tsx) — but opens the
              // web app's illustrated static guide in the device browser rather than
              // building a native duplicate of it (per product direction), same as invite
              // links already fall back to the web address rather than in-app content.
              ListTile(
                leading: const Icon(Icons.help_outline),
                title: Text(context.t('nav.userGuide')),
                onTap: () {
                  Navigator.pop(context);
                  final path = locale.languageCode == 'ar' ? 'ar.html' : 'en.html';
                  launchUrl(Uri.parse('$dourakAppUrl/help/$path'), mode: LaunchMode.externalApplication);
                },
              ),
              // Opens the web app's support form (SupportPage.tsx) in the device browser rather
              // than duplicating it natively — same product direction as the User Guide link above.
              ListTile(
                leading: const Icon(Icons.support_agent_outlined),
                title: Text(context.t('nav.support')),
                onTap: () {
                  Navigator.pop(context);
                  launchUrl(Uri.parse('$dourakAppUrl/support'), mode: LaunchMode.externalApplication);
                },
              ),
            ],
          ),
        ),
      ),
      body: SafeArea(child: body),
      floatingActionButton: floatingActionButton,
    );
  }
}
