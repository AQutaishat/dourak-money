/** Opens WhatsApp's share intent with pre-filled text (BRD §6.22). */
export function shareToWhatsApp(text: string, phone?: string | null) {
  // With a phone number the message opens in that person's chat; without one WhatsApp asks
  // the user to pick a contact — which is what the generic "share status" action wants.
  const normalizedPhone = phone ? phone.replace(/[^\d]/g, "") : "";
  const base = normalizedPhone ? `https://wa.me/${normalizedPhone}` : "https://wa.me/";
  window.open(`${base}?text=${encodeURIComponent(text)}`, "_blank", "noopener,noreferrer");
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

/**
 * prompt02 §Active circles: a per-row reminder aimed at one specific unpaid member, replacing
 * the old "Unpaid" bulk button.
 */
export function buildPaymentReminderText(params: {
  memberName: string;
  circleName: string;
  monthLabel: string;
  outstanding: number;
  currency: string;
  isArabic: boolean;
}) {
  const { memberName, circleName, monthLabel, outstanding, currency, isArabic } = params;
  return isArabic
    ? `مرحبًا ${memberName}،\nتذكير بقسط جمعية "${circleName}" لشهر ${monthLabel}.\nالمبلغ المستحق: ${outstanding} ${currency}\nشكرًا لك.`
    : `Hi ${memberName},\nA reminder about your contribution to "${circleName}" for ${monthLabel}.\nAmount due: ${outstanding} ${currency}\nThank you.`;
}

/**
 * prompt02 §3: invite someone who isn't a Dourak user yet by sending them the current
 * application URL over WhatsApp. Deliberately the lightweight version — the link is just the
 * app's address, not a signed invitation token.
 */
export function buildInviteToRegisterText(params: {
  personName?: string | null;
  circleName: string;
  organizerName: string;
  appUrl: string;
  isArabic: boolean;
}) {
  const { personName, circleName, organizerName, appUrl, isArabic } = params;
  const greeting = personName ? (isArabic ? `مرحبًا ${personName}،\n` : `Hi ${personName},\n`) : "";
  return isArabic
    ? `${greeting}دعاك ${organizerName} للانضمام إلى جمعية "${circleName}" على تطبيق دورك.\nسجّل حسابك من هنا ثم ستصلك الدعوة داخل التطبيق:\n${appUrl}`
    : `${greeting}${organizerName} invited you to join the savings circle "${circleName}" on Dourak.\nCreate your account here and you'll find the invitation waiting inside the app:\n${appUrl}`;
}

/** The address of the running app — what an invitee should open to register. */
export function currentAppUrl() {
  return window.location.origin;
}
