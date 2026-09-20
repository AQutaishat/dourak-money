import 'package:intl/intl.dart';

/// Mirrors the web app's shared `frontend/src/utils/date.ts` + amount formatting, extracted here
/// so the Monthly Cycles tab, the Current Cycle tab and the timeline all format identically
/// instead of keeping drifting private copies.

/// Drops the pointless trailing ".0" Dart prints for whole-number doubles, so amounts read
/// "50" / "12.5" the way the web app shows them.
String formatAmount(double value) => value == value.roundToDouble() ? value.toStringAsFixed(0) : value.toString();

/// yy/MM/dd — the same compact format `shortDate` uses on the web, wrapped in LTR isolate
/// characters so the numeric sequence can't get bidi-reordered inside surrounding Arabic text.
String shortDate(String iso) => ltrIsolate(DateFormat('yy/MM/dd').format(DateTime.parse(iso).toLocal()));

const _monthOrdinalAr = [
  'الأول', 'الثاني', 'الثالث', 'الرابع', 'الخامس', 'السادس',
  'السابع', 'الثامن', 'التاسع', 'العاشر', 'الحادي عشر', 'الثاني عشر',
];
const _monthOrdinalEn = [
  'First', 'Second', 'Third', 'Fourth', 'Fifth', 'Sixth',
  'Seventh', 'Eighth', 'Ninth', 'Tenth', 'Eleventh', 'Twelfth',
];

/// "الأول" / "First" for month [n] (1-based) — falls back to the plain number past the 12th.
/// The mobile twin of the web's shared `utils/monthOrdinal.ts`.
String monthOrdinalWord(int n, bool isArabic) {
  final words = isArabic ? _monthOrdinalAr : _monthOrdinalEn;
  return n - 1 >= 0 && n - 1 < words.length ? words[n - 1] : '$n';
}

/// Wraps a numeric/latin run in Unicode isolate marks so it keeps its own direction inside an
/// Arabic sentence — the fix for a recurring class of bidi-reordering bug in this app.
String ltrIsolate(String text) {
  final lri = String.fromCharCode(0x2066); // LTR isolate
  final pdi = String.fromCharCode(0x2069); // pop directional isolate
  return '$lri$text$pdi';
}
