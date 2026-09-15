import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../l10n/app_localizations.dart';
import '../../state/providers.dart';
import '../../widgets/validated_text_field.dart';

/// Phase 2 §7: registration only asks for email + password; name/phone come later
/// from the profile screen (mirrors RegisterPage.tsx).
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

  @override
  void dispose() {
    email.dispose();
    password.dispose();
    super.dispose();
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
