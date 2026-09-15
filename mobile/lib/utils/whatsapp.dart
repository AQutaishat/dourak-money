import 'package:url_launcher/url_launcher.dart';

/// Mirrors frontend/src/utils/whatsapp.ts — same message templates, same wa.me
/// scheme. `url_launcher` opens the WhatsApp app / browser fallback in place of
/// `window.open`.
Future<void> shareToWhatsApp(String text, [String? phone]) async {
  final normalizedPhone = phone?.replaceAll(RegExp(r'[^\d]'), '') ?? '';
  final base = normalizedPhone.isNotEmpty ? 'https://wa.me/$normalizedPhone' : 'https://wa.me/';
  final uri = Uri.parse('$base?text=${Uri.encodeComponent(text)}');
  await launchUrl(uri, mode: LaunchMode.externalApplication);
}

String buildCurrentCycleShareText({
  required String circleName,
  required String monthLabel,
  required int paid,
  required int total,
  required double collected,
  required double expected,
  required String currency,
  required String recipientName,
  required bool isArabic,
}) {
  return isArabic
      ? '$circleName — $monthLabel\nتم الدفع: $paid من $total\nالمبلغ المحصل: $collected / $expected $currency\nصاحب الدور هذا الشهر: $recipientName'
      : "$circleName — $monthLabel\nPaid: $paid / $total\nCollected: $collected / $expected $currency\nThis month's recipient: $recipientName";
}

String buildPaymentReminderText({
  required String memberName,
  required String circleName,
  required String monthLabel,
  required double outstanding,
  required String currency,
  required bool isArabic,
}) {
  return isArabic
      ? 'مرحبًا $memberName،\nتذكير بقسط جمعية "$circleName" لشهر $monthLabel.\nالمبلغ المستحق: $outstanding $currency\nشكرًا لك.'
      : 'Hi $memberName,\nA reminder about your contribution to "$circleName" for $monthLabel.\nAmount due: $outstanding $currency\nThank you.';
}

/// prompt03 §2: a pure share action, no backend record created either side.
String buildInviteToRegisterText({
  String? personName,
  required String circleName,
  required String organizerName,
  required String appUrl,
  required bool isArabic,
}) {
  final greeting = personName != null
      ? (isArabic ? 'مرحبًا $personName،\n' : 'Hi $personName,\n')
      : '';
  return isArabic
      ? '$greetingدعاك $organizerName للانضمام إلى جمعية "$circleName" على تطبيق دورك.\nسجّل حسابك من هنا ثم ستصلك الدعوة داخل التطبيق:\n$appUrl'
      : '$greeting$organizerName invited you to join the savings circle "$circleName" on Dourak.\nCreate your account here and you\'ll find the invitation waiting inside the app:\n$appUrl';
}

/// The web app used `window.location.origin`; the mobile app has no such origin, so it
/// links to the production web app instead (kept as a constant — see docs/progress.md).
const String dourakAppUrl = 'https://dourak.app';
