import 'package:flutter/material.dart';

import '../l10n/app_localizations.dart';

/// Mirrors frontend/src/components/ValidatedTextField.tsx's `useValidatedField`: an
/// on-blur red-border + inline-error pattern, shared by login/register/create-circle.
class ValidatedController {
  ValidatedController(this.rules, {String initial = ''}) : text = TextEditingController(text: initial);

  final List<String> rules; // 'required' | 'email' | 'password'
  final TextEditingController text;
  final ValueNotifier<String?> error = ValueNotifier(null);

  bool _validate(BuildContext context) {
    final value = text.text.trim();
    for (final rule in rules) {
      switch (rule) {
        case 'required':
          if (value.isEmpty) {
            error.value = context.t('validation.required');
            return false;
          }
          break;
        case 'email':
          final emailRegex = RegExp(r'^[^\s@]+@[^\s@]+\.[^\s@]+$');
          if (!emailRegex.hasMatch(value)) {
            error.value = context.t('validation.email');
            return false;
          }
          break;
        case 'password':
          final passwordRegex = RegExp(r'^(?=.*[A-Za-z])(?=.*\d).{8,}$');
          if (!passwordRegex.hasMatch(value)) {
            error.value = context.t('validation.password');
            return false;
          }
          break;
      }
    }
    error.value = null;
    return true;
  }

  /// Called on submit; also used as the field's onFieldSubmitted/onEditingComplete
  /// equivalent to the web app's on-blur validation.
  bool validateNow(BuildContext context) => _validate(context);

  void dispose() {
    text.dispose();
    error.dispose();
  }
}

class ValidatedTextField extends StatelessWidget {
  const ValidatedTextField({
    super.key,
    required this.controller,
    required this.label,
    this.obscureText = false,
    this.keyboardType,
    this.helperText,
    this.suffixIcon,
  });

  final ValidatedController controller;
  final String label;
  final bool obscureText;
  final TextInputType? keyboardType;
  final String? helperText;

  /// Optional trailing control — used for the login screen's password-visibility toggle.
  final Widget? suffixIcon;

  @override
  Widget build(BuildContext context) {
    return ValueListenableBuilder<String?>(
      valueListenable: controller.error,
      builder: (context, error, _) {
        return TextField(
          controller: controller.text,
          obscureText: obscureText,
          keyboardType: keyboardType,
          decoration: InputDecoration(
            labelText: label,
            errorText: error,
            helperText: error == null ? helperText : null,
            suffixIcon: suffixIcon,
          ),
          onTapOutside: (_) => controller.validateNow(context),
          onEditingComplete: () => controller.validateNow(context),
        );
      },
    );
  }
}
