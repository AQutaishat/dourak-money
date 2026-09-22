import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_sign_in/google_sign_in.dart';

import '../../auth/auth_state.dart';
import '../../l10n/app_localizations.dart';
import '../../state/providers.dart';
import '../../widgets/google_logo.dart';
import '../../widgets/validated_text_field.dart';

/// Phase 2 §7: registration only asks for email + password; name/phone come later
/// from the profile screen (mirrors RegisterPage.tsx).
///
/// The Google button here uses the same googleLogin as the login screen (find-or-create by
/// Google account), so it doubles as "sign up with Google" without a separate registration call.
class RegisterScreen extends ConsumerStatefulWidget {
  const RegisterScreen({super.key});

  @override
  ConsumerState<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends ConsumerState<RegisterScreen> {
  late final email = ValidatedController(['required', 'email']);
  late final password = ValidatedController(['required', 'password']);
  String? error;
  bool loading = false;

  String? googleClientId;
  bool googleLoading = false;

  @override
  void initState() {
    super.initState();
    ref.read(authApiProvider).config().then((config) {
      if (mounted) setState(() => googleClientId = config.googleSignInEnabled ? config.googleClientId : null);
    }).catchError((_) {
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
      await ref.read(authControllerProvider.notifier).register(email.text.text.trim(), password.text.text);
      if (mounted) context.go('/');
    } catch (err) {
      debugPrint('Registration failed: $err'); // visible via `adb logcat` / `flutter logs`
      final message = err.toString();
      setState(() {
        error = RegExp('already exists|already registered', caseSensitive: false).hasMatch(message)
            ? context.t('auth.emailAlreadyRegistered')
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
            constraints: const BoxConstraints(maxWidth: 380),
            child: Card(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(context.t('auth.register'),
                        style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                              fontWeight: FontWeight.bold,
                              color: Theme.of(context).colorScheme.primary,
                            )),
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
                      obscureText: true,
                      helperText: context.t('auth.passwordRules'),
                    ),
                    const SizedBox(height: 20),
                    SizedBox(
                      width: double.infinity,
                      child: FilledButton(
                        onPressed: loading ? null : _submit,
                        child: Padding(
                          padding: const EdgeInsets.symmetric(vertical: 12),
                          child: Text(context.t('auth.registerCta')),
                        ),
                      ),
                    ),
                    // Absent entirely until the backend confirms Google sign-in is configured
                    // and enabled — mirrors login_screen.dart.
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
                              : const GoogleLogo(size: 18),
                          label: Padding(
                            padding: const EdgeInsets.symmetric(vertical: 8),
                            child: Text(context.t('auth.continueWithGoogle')),
                          ),
                        ),
                      ),
                    ],
                    const SizedBox(height: 12),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Text(context.t('auth.haveAccount')),
                        TextButton(onPressed: () => context.go('/login'), child: Text(context.t('auth.login'))),
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
