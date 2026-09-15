import 'package:flutter/material.dart';

import 'strings.dart';

/// Lightweight i18n mirroring `frontend/src/i18n/index.ts` (i18next) without pulling
/// in an ARB/codegen pipeline. `t()` looks up a dotted key ("circle.recordPayment")
/// and substitutes `{placeholder}` tokens, the same convention the web app uses with
/// i18next's `{{placeholder}}` interpolation.
class AppLocalizations {
  AppLocalizations(this.locale);

  final Locale locale;

  static AppLocalizations of(BuildContext context) {
    return Localizations.of<AppLocalizations>(context, AppLocalizations)!;
  }

  static const supportedLocales = [Locale('ar'), Locale('en')];

  Map<String, String> get _table => locale.languageCode == 'en' ? enStrings : arStrings;

  bool get isRtl => locale.languageCode != 'en';

  String t(String key, [Map<String, String>? params]) {
    var value = _table[key] ?? key;
    params?.forEach((k, v) {
      value = value.replaceAll('{$k}', v);
    });
    return value;
  }

  static const LocalizationsDelegate<AppLocalizations> delegate = _AppLocalizationsDelegate();
}

class _AppLocalizationsDelegate extends LocalizationsDelegate<AppLocalizations> {
  const _AppLocalizationsDelegate();

  @override
  bool isSupported(Locale locale) => ['ar', 'en'].contains(locale.languageCode);

  @override
  Future<AppLocalizations> load(Locale locale) async => AppLocalizations(locale);

  @override
  bool shouldReload(_AppLocalizationsDelegate old) => false;
}

/// Shorthand used throughout the screens: `context.t('circle.members')`.
extension AppLocalizationsX on BuildContext {
  String t(String key, [Map<String, String>? params]) => AppLocalizations.of(this).t(key, params);
}
