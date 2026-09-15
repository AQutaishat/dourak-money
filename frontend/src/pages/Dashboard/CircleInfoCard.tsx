import { Card, CardContent, Chip, Stack, Typography } from "@mui/material";
import { Link as RouterLink } from "react-router-dom";
import { useTranslation } from "react-i18next";
import type { CircleStatus, CircleSummary } from "../../api/types";

/**
 * prompt02 §Dashboard: every circle card shows the organizer name and creation date, and an
 * active circle also shows its member count and the per-member contribution amount. The Active
 * badge is green rather than the old neutral gray.
 */
const STATUS_COLOR: Record<CircleStatus, "success" | "info" | "warning" | "default"> = {
  Active: "success",
  Draft: "info",
  Paused: "warning",
  Completed: "default",
  Cancelled: "default",
};

export function CircleInfoCard({ circle }: { circle: CircleSummary }) {
  const { t, i18n } = useTranslation();

  const created = new Date(circle.createdAt).toLocaleDateString(i18n.language, {
    year: "numeric", month: "short", day: "numeric",
  });

  return (
    <Card component={RouterLink} to={`/circles/${circle.id}`} sx={{ textDecoration: "none", display: "block", height: "100%" }}>
      <CardContent>
        <Stack direction="row" justifyContent="space-between" alignItems="center" spacing={1}>
          <Typography variant="subtitle1" fontWeight={600}>{circle.name}</Typography>
          <Chip size="small" color={STATUS_COLOR[circle.status]} label={t(`circle.${circle.status.toLowerCase()}`)} />
        </Stack>

        <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5 }}>
          {t("circle.memberCount")}: {circle.memberCount} · {t("circle.perMemberAmount")}: {circle.contributionAmount} {circle.currency}
        </Typography>

        <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 1 }}>
          {t("circle.organizer")}: {circle.organizerName}
        </Typography>
        <Typography variant="caption" color="text.secondary" display="block">
          {t("circle.createdAt")}: {created}
        </Typography>

        {!circle.isOrganizer && (
          <Chip size="small" variant="outlined" label={t("circle.viewOnly")} sx={{ mt: 1 }} />
        )}
      </CardContent>
    </Card>
  );
}
