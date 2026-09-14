/** Opens WhatsApp's share intent with pre-filled text (BRD §6.22). */
export function shareToWhatsApp(text: string) {
  const url = `https://wa.me/?text=${encodeURIComponent(text)}`;
  window.open(url, "_blank", "noopener,noreferrer");
}

export function buildCurrentCycleShareText(params: {
  circleName: string;
  monthLabel: string;
  paid: number;
  total: number;
  collected: number;
  expected: number;
  currency: string;
  recipientName: string;
  isArabic: boolean;
}) {
  const { circleName, monthLabel, paid, total, collected, expected, currency, recipientName, isArabic } = params;
  return isArabic
    ? `${circleName} — ${monthLabel}\nتم الدفع: ${paid} من ${total}\nالمبلغ المحصل: ${collected} / ${expected} ${currency}\nصاحب الدور هذا الشهر: ${recipientName}`
    : `${circleName} — ${monthLabel}\nPaid: ${paid} / ${total}\nCollected: ${collected} / ${expected} ${currency}\nThis month's recipient: ${recipientName}`;
}

export function buildUnpaidShareText(unpaidNames: string[], isArabic: boolean) {
  const header = isArabic ? "لم يتم الدفع بعد من:" : "Not yet paid:";
  return `${header}\n${unpaidNames.join("\n")}`;
}
