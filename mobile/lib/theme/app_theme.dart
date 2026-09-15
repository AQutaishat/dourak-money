import 'package:flutter/material.dart';

/// Mirrors frontend/src/theme/theme.ts: a single calm accent color, generous
/// whitespace, no dense banking-style visuals (BRD "not a complicated banking-style
/// dashboard"). Material 3 stands in for MUI; the palette/shape values are ported.
ThemeData buildDourakTheme(TextDirection direction) {
  const primary = Color(0xFF1F8A70); // calm green — money/trust without looking like a bank
  const secondary = Color(0xFFF2A541);
  const background = Color(0xFFFAF9F6);

  final colorScheme = ColorScheme.fromSeed(
    seedColor: primary,
    secondary: secondary,
    brightness: Brightness.light,
  );

  return ThemeData(
    useMaterial3: true,
    colorScheme: colorScheme,
    scaffoldBackgroundColor: background,
    // No bundled font asset (would need a real Tajawal .ttf added to pubspec.yaml's
    // `fonts:` section) — naming it here is harmless (Flutter falls back to the
    // platform default when a family isn't registered) and documents the intent to
    // match the web app's Arabic font choice once an asset is added.
    fontFamily: direction == TextDirection.rtl ? 'Tajawal' : null,
    visualDensity: VisualDensity.standard,
    cardTheme: const CardThemeData(
      elevation: 1,
      shadowColor: Color(0x14000000),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.all(Radius.circular(12))),
      margin: EdgeInsets.zero,
    ),
    inputDecorationTheme: const InputDecorationTheme(
      border: OutlineInputBorder(borderRadius: BorderRadius.all(Radius.circular(10))),
    ),
    elevatedButtonTheme: ElevatedButtonThemeData(
      style: ElevatedButton.styleFrom(
        elevation: 0,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
      ),
    ),
    appBarTheme: AppBarTheme(
      backgroundColor: background,
      foregroundColor: colorScheme.onSurface,
      elevation: 0,
    ),
  );
}
