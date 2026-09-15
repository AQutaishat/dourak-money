import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../auth/auth_state.dart';
import '../l10n/app_localizations.dart';
import '../state/locale_provider.dart';
import '../state/providers.dart';

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
        title: Text(title),
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
                child: Text(context.t('app.name'), style: Theme.of(context).textTheme.titleLarge),
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
            ],
          ),
        ),
      ),
      body: SafeArea(child: body),
      floatingActionButton: floatingActionButton,
    );
  }
}
