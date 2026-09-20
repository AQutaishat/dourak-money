const MONTH_ORDINAL_AR = ["الأول", "الثاني", "الثالث", "الرابع", "الخامس", "السادس", "السابع", "الثامن", "التاسع", "العاشر", "الحادي عشر", "الثاني عشر"];
const MONTH_ORDINAL_EN = ["First", "Second", "Third", "Fourth", "Fifth", "Sixth", "Seventh", "Eighth", "Ninth", "Tenth", "Eleventh", "Twelfth"];

/** "الأول" / "First" for month n (1-based) — falls back to the plain number past the 12th. */
export function monthOrdinalWord(n: number, isArabic: boolean): string {
  const words = isArabic ? MONTH_ORDINAL_AR : MONTH_ORDINAL_EN;
  return n - 1 < words.length ? words[n - 1] : String(n);
}
