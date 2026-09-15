import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

/// App language state — mirrors i18next's `changeLanguage`, defaulting to Arabic
/// (RTL) exactly like the web app's default locale.
final localeProvider = StateProvider<Locale>((ref) => const Locale('ar'));
