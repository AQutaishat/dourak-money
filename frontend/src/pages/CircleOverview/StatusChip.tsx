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

export function ClaimStatusChip({ status }: { status: string }) {
  const { t } = useTranslation();
  return <Chip size="small" color={CLAIM_COLOR[status] ?? "default"} label={t(`circle.claim${status}`)} />;
}
