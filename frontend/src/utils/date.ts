/**
 * Combines a "YYYY-MM" month value with a day-of-month into a full "YYYY-MM-DD" date string,
 * clamping the day to the last valid day of that month (e.g. day 31 in February becomes the 28th
 * or 29th) instead of letting it silently roll over into the next month.
 */
export function buildDateFromMonthAndDay(monthValue: string, day: number): string {
  const [year, month] = monthValue.split("-").map(Number);
  const daysInMonth = new Date(year, month, 0).getDate();
  const clampedDay = Math.min(Math.max(1, day), daysInMonth);
  return `${year}-${String(month).padStart(2, "0")}-${String(clampedDay).padStart(2, "0")}`;
}

/** yy/mm/dd — two-digit year, numeric month, day, for compact table cells and payment lines. */
export function shortDate(iso: string): string {
  const d = new Date(iso);
  const dd = String(d.getDate()).padStart(2, "0");
  const mm = String(d.getMonth() + 1).padStart(2, "0");
  const yy = String(d.getFullYear()).slice(-2);
  return `${yy}/${mm}/${dd}`;
}
