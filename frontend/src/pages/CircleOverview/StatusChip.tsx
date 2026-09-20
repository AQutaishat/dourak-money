import { Chip } from "@mui/material";
import { useTranslation } from "react-i18next";

/**
 * prompt02 §Active circles: the member-history table must use the *same* colored status badges
 * as the Current Cycle payments table. Defining them once here is what keeps the visual
 * language identical across both tables instead of two copies drifting apart.
 */
const STATUS_COLOR: Record<string, "default" | "success" | "warning" | "error"> = {
  Paid: "success",
  PartiallyPaid: "warning",
  Unpaid: "default",
  Late: "error",
};

export function ContributionStatusChip({ status }: { status: string }) {
  const { t } = useTranslation();
  const key = status === "PartiallyPaid" ? "partiallyPaid" : status.toLowerCase();
  return <Chip size="small" color={STATUS_COLOR[status] ?? "default"} label={t(`circle.${key}`)} />;
}

const INVITATION_COLOR: Record<string, "default" | "success" | "warning" | "error"> = {
  Accepted: "success",
  Pending: "warning",
  Declined: "error",
  NotInvited: "default",
};

/** prompt02 §5: Accepted / Declined / Pending shown next to each member. */
export function InvitationStatusChip({ status }: { status: string }) {
  const { t } = useTranslation();
  const key = `invitation${status}`;
  return <Chip size="small" variant="outlined" color={INVITATION_COLOR[status] ?? "default"} label={t(`circle.${key}`)} />;
}

const CLAIM_COLOR: Record<string, "default" | "success" | "warning" | "error"> = {
  Pending: "warning",
  Approved: "success",
  Rejected: "error",
};

/**
 * The single, canonical claim-status badge — same color, same wording, same component, used
 * everywhere a payment claim's status is shown (the Current Cycle tab, for both the organizer's
 * and a member's own row, and the Monthly Cycles tab). Previously there were two near-duplicate
 * components with slightly different wording ("دفعة مقبولة" vs "تمت الموافقة" for the same
 * Approved state) that had drifted apart across screens — keeping exactly one avoids that.
 */
export function ClaimStatusChip({ status, onClick }: { status: string; onClick?: () => void }) {
  const { t } = useTranslation();
  const label = status === "Pending" ? t("circle.pendingPaymentBadge")
    : status === "Approved" ? t("circle.approvedPaymentBadge")
    : t(`circle.claim${status}`);
  return (
    <Chip
      size="small" color={CLAIM_COLOR[status] ?? "default"} label={label}
      clickable={!!onClick} onClick={onClick}
    />
  );
}
