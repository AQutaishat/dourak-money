import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_sign_in/google_sign_in.dart';

import '../../auth/auth_state.dart';
import '../../l10n/app_localizations.dart';
import '../../state/locale_provider.dart';
import '../../state/providers.dart';
import '../../widgets/validated_text_field.dart';

class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  late final email = ValidatedController(['required', 'email']);
  late final password = ValidatedController(['required']);
  String? error;
  bool loading = false;
  bool showPassword = false;

  // Null while still checking, or once checked and unavailable — the button never renders
  // (and GoogleSignIn is never even instantiated) until the backend confirms it's usable,
  // same gate as the web login page's AuthConfig check.
  String? googleClientId;
  bool googleLoading = false;

  @override
  void initState() {
    super.initState();
    ref.read(authApiProvider).config().then((config) {
      if (mounted) setState(() => googleClientId = config.googleSignInEnabled ? config.googleClientId : null);
    }).catchError((_) {
      // A failed config lookup must never block plain email/password login.
      if (mounted) setState(() => googleClientId = null);
    });
  }

  @override
  void dispose() {
    email.dispose();
    password.dispose();
    super.dispose();
  }

  Future<void> _submitGoogle() async {
    final clientId = googleClientId;
    if (clientId == null) return;
    setState(() { error = null; googleLoading = true; });
    try {
      final googleSignIn = GoogleSignIn(scopes: const ['email'], serverClientId: clientId);
      final account = await googleSignIn.signIn();
      if (account == null) { setState(() => googleLoading = false); return; } // user cancelled
      final auth = await account.authentication;
      final idToken = auth.idToken;
      if (idToken == null) {
        throw AuthException('Google did not return an ID token.', 'server');
      }
      await ref.read(authControllerProvider.notifier).googleLogin(idToken);
      if (mounted) context.go('/');
    } catch (err) {
      debugPrint('Google sign-in failed: $err');
      setState(() => error = context.t('common.error'));
    } finally {
      if (mounted) setState(() => googleLoading = false);
    }
  }

  Future<void> _submit() async {
    setState(() => error = null);
    final emailOk = email.validateNow(context);
    final passwordOk = password.validateNow(context);
    if (!emailOk || !passwordOk) return;

    setState(() => loading = true);
    try {
      await ref.read(authControllerProvider.notifier).login(email.text.text.trim(), password.text.text);
      if (mounted) context.go('/');
    } catch (err) {
      // Printed so `adb logcat` / `flutter logs` shows the real cause (network error,
      // server error, etc.) — the on-screen message stays generic/localized on purpose.
      debugPrint('Login failed: $err');
      // No cleared fields, message says plainly the credentials are wrong (mirrors LoginPage.tsx).
      // A non-credentials failure (network/server error) must not be mislabeled as a
      // bad password — show the real error so a connectivity problem is visible.
      setState(() {
        error = err is AuthException && err.kind == 'invalid-credentials'
            ? context.t('auth.invalidCredentials')
            : context.t('common.error');
      });
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 360),
            child: Card(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.center,
                  children: [
                    ClipRRect(
                      borderRadius: BorderRadius.circular(12),
                      child: Image.asset('assets/images/dourak_logo.png', width: 64, height: 64),
                    ),
                    const SizedBox(height: 12),
                    Text(context.t('app.name'),
                        textAlign: TextAlign.center,
                        style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                              fontWeight: FontWeight.bold,
                              color: Theme.of(context).colorScheme.primary,
                            )),
                    const SizedBox(height: 4),
                    Text(context.t('app.tagline'), textAlign: TextAlign.center, style: Theme.of(context).textTheme.bodyMedium),
                    const SizedBox(height: 20),
                    if (error != null) ...[
                      Container(
                        width: double.infinity,
                        padding: const EdgeInsets.all(12),
                        decoration: BoxDecoration(color: Colors.red.shade50, borderRadius: BorderRadius.circular(8)),
                        child: Text(error!, style: TextStyle(color: Colors.red.shade800)),
                      ),
                      const SizedBox(height: 16),
                    ],
                    ValidatedTextField(controller: email, label: context.t('auth.email'), keyboardType: TextInputType.emailAddress),
                    const SizedBox(height: 16),
                    ValidatedTextField(
                      controller: password,
                      label: context.t('auth.password'),
                      obscureText: !showPassword,
                      suffixIcon: IconButton(
                        icon: Icon(showPassword ? Icons.visibility_off : Icons.visibility),
                        onPressed: () => setState(() => showPassword = !showPassword),
                      ),
                    ),
                    const SizedBox(height: 20),
                    SizedBox(
                      width: double.infinity,
                      child: FilledButton(
                        onPressed: loading ? null : _submit,
                        child: Padding(
                          padding: const EdgeInsets.symmetric(vertical: 12),
                          child: Text(context.t('auth.loginCta')),
                        ),
                      ),
                    ),
                    // Absent entirely (no divider, nothing fetched) until the backend confirms
                    // Google sign-in is actually configured and enabled.
                    if (googleClientId != null) ...[
                      const SizedBox(height: 16),
                      Row(children: [
                        const Expanded(child: Divider()),
                        Padding(
                          padding: const EdgeInsets.symmetric(horizontal: 8),
                          child: Text(context.t('auth.orContinueWith'), style: Theme.of(context).textTheme.bodySmall),
                        ),
                        const Expanded(child: Divider()),
                      ]),
                      const SizedBox(height: 12),
                      SizedBox(
                        width: double.infinity,
                        child: OutlinedButton.icon(
                          onPressed: googleLoading ? null : _submitGoogle,
                          icon: googleLoading
                              ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2))
                              : const Icon(Icons.login),
                          label: Padding(
                            padding: const EdgeInsets.symmetric(vertical: 8),
                            child: Text('Google'),
                          ),
                        ),
                      ),
                    ],
                    const SizedBox(height: 12),
                    // "No account? Create one" on one side and the language picker on the other
                    // — the web's final placement for the selector (it used to float alone above
                    // the logo). space-between puts the sign-up text first in reading order.
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Flexible(
                          child: Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              Flexible(child: Text(context.t('auth.noAccount'), overflow: TextOverflow.ellipsis)),
                              TextButton(onPressed: () => context.go('/register'), child: Text(context.t('auth.register'))),
                            ],
                          ),
                        ),
                        DropdownButton<String>(
                          value: ref.watch(localeProvider).languageCode,
                          underline: const SizedBox.shrink(),
                          icon: const Icon(Icons.translate, size: 18),
                          onChanged: (code) {
                            if (code != null) ref.read(localeProvider.notifier).state = Locale(code);
                          },
                          items: const [
                            DropdownMenuItem(value: 'ar', child: Text('العربية')),
                            DropdownMenuItem(value: 'en', child: Text('English')),
                          ],
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
