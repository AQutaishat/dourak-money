import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../api/api_client.dart';
import '../../l10n/app_localizations.dart';
import '../../state/locale_provider.dart';
import '../../state/providers.dart';

/// Mirrors ProfilePage.tsx: name/phone editable and optional, email read-only,
/// preferred language select (also switches the app's own locale on save).
class ProfileScreen extends ConsumerStatefulWidget {
  const ProfileScreen({super.key});

  @override
  ConsumerState<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends ConsumerState<ProfileScreen> {
  final name = TextEditingController();
  final phone = TextEditingController();
  String language = 'ar';
  String? email;
  String? error;
  bool saved = false;
  bool saving = false;
  bool loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final profile = await ref.read(authApiProvider).profile();
      setState(() {
        name.text = profile.name ?? '';
        phone.text = profile.phone ?? '';
        language = profile.preferredLanguage == 'en' ? 'en' : 'ar';
        email = profile.email;
        loading = false;
      });
    } catch (_) {
      setState(() => loading = false);
    }
  }

  @override
  void dispose() {
    name.dispose();
    phone.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    setState(() {
      error = null;
      saved = false;
      saving = true;
    });
    try {
      await ref.read(authApiProvider).updateProfile(
            name: name.text.trim().isEmpty ? null : name.text.trim(),
            phone: phone.text.trim().isEmpty ? null : phone.text.trim(),
            preferredLanguage: language,
          );
      setState(() => saved = true);
      await ref.read(authControllerProvider.notifier).refreshProfile();
      ref.read(localeProvider.notifier).state = Locale(language);
    } catch (err) {
      setState(() => error = extractErrorMessage(err, context.t('auth.profileSaveFailed')));
    } finally {
      if (mounted) setState(() => saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (loading) {
      return Scaffold(appBar: AppBar(title: Text(context.t('auth.profileTitle'))), body: Center(child: Text(context.t('common.loading'))));
    }
    return Scaffold(
      appBar: AppBar(title: Text(context.t('auth.profileTitle'))),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text(context.t('auth.profileHint'), style: Theme.of(context).textTheme.bodySmall),
              const SizedBox(height: 16),
              if (error != null) ...[
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(color: Colors.red.shade50, borderRadius: BorderRadius.circular(8)),
                  child: Text(error!, style: TextStyle(color: Colors.red.shade800)),
                ),
                const SizedBox(height: 12),
              ],
              if (saved) ...[
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(color: Colors.green.shade50, borderRadius: BorderRadius.circular(8)),
                  child: Text(context.t('auth.profileSaved'), style: TextStyle(color: Colors.green.shade800)),
                ),
                const SizedBox(height: 12),
              ],
              TextField(
                enabled: false,
                controller: TextEditingController(text: email ?? ''),
                decoration: InputDecoration(labelText: context.t('auth.email'), helperText: context.t('auth.emailReadOnly')),
              ),
              const SizedBox(height: 16),
              TextField(controller: name, decoration: InputDecoration(labelText: context.t('auth.name'))),
              const SizedBox(height: 16),
              TextField(controller: phone, decoration: InputDecoration(labelText: context.t('auth.phone'))),
              const SizedBox(height: 16),
              DropdownButtonFormField<String>(
                value: language,
                decoration: InputDecoration(labelText: context.t('auth.preferredLanguage')),
                items: const [
                  DropdownMenuItem(value: 'ar', child: Text('العربية')),
                  DropdownMenuItem(value: 'en', child: Text('English')),
                ],
                onChanged: (v) => setState(() => language = v ?? language),
              ),
              const SizedBox(height: 24),
              Row(
                mainAxisAlignment: MainAxisAlignment.end,
                children: [
                  TextButton(onPressed: () => Navigator.of(context).maybePop(), child: Text(context.t('common.close'))),
                  const SizedBox(width: 8),
                  FilledButton(onPressed: saving ? null : _submit, child: Text(context.t('common.save'))),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}
