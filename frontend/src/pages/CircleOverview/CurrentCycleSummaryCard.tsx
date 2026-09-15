import { useQuery } from "@tanstack/react-query";
import { Card, CardContent, Typography, Stack, LinearProgress, Chip, Button } from "@mui/material";
import WhatsAppIcon from "@mui/icons-material/WhatsApp";
import { Link as RouterLink } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { circlesApi } from "../../api/circles";
import { buildCurrentCycleShareText, shareToWhatsApp } from "../../utils/whatsapp";

export function CurrentCycleSummaryCard({
  circleId, circleName, currency, memberCount, organizerName,
}: {
  circleId: number;
  circleName: string;
  currency: string;
  /** prompt03 §3: the active-with-progress card variant was missing these (Phase 2 only added
      them to the general/draft grid cards below). */
  memberCount: number;
  organizerName: string;
}) {
  const { t, i18n } = useTranslation();
  const { data: dashboard } = useQuery({ queryKey: ["dashboard", circleId], queryFn: () => circlesApi.dashboard(circleId) });

  if (!dashboard) return null;

  const progress = dashboard.expected > 0 ? (dashboard.collected / dashboard.expected) * 100 : 0;
  const monthLabel = new Date(dashboard.dueDate).toLocaleDateString(i18n.language, { month: "long", year: "numeric" });

  const handleShare = () => {
    shareToWhatsApp(buildCurrentCycleShareText({
      circleName, monthLabel, paid: dashboard.membersPaid, total: dashboard.membersTotal,
      collected: dashboard.collected, expected: dashboard.expected, currency,
      recipientName: dashboard.recipientName, isArabic: i18n.language.startsWith("ar"),
    }));
  };

  return (
    <Card component={RouterLink} to={`/circles/${circleId}`} sx={{ textDecoration: "none", display: "block" }}>
      <CardContent>
        <Stack direction="row" justifyContent="space-between" alignItems="baseline">
          <Typography variant="subtitle1" fontWeight={700}>{circleName}</Typography>
          <Typography variant="caption" color="text.secondary">{monthLabel}</Typography>
        </Stack>

        <Typography variant="caption" color="text.secondary" display="block">
          {t("circle.organizer")}: {organizerName} · {memberCount} {t("circle.members")}
        </Typography>

        <Stack direction="row" spacing={1} sx={{ my: 1 }}>
          <Chip size="small" color="success" label={`${t("circle.paid")}: ${dashboard.membersPaid}/${dashboard.membersTotal}`} />
          {dashboard.membersLate > 0 && <Chip size="small" color="error" label={`${t("circle.late")}: ${dashboard.membersLate}`} />}
        </Stack>

        <LinearProgress variant="determinate" value={Math.min(progress, 100)} sx={{ height: 8, borderRadius: 4, mb: 1 }} />
        <Typography variant="body2" color="text.secondary">
          {dashboard.collected} / {dashboard.expected} {currency} · {t("circle.outstanding")}: {dashboard.outstanding} {currency}
        </Typography>

        <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mt: 1 }}>
          <Typography variant="body2">
            {t("circle.recipient")}: <strong>{dashboard.recipientName}</strong>
          </Typography>
          <Button
            size="small"
            startIcon={<WhatsAppIcon />}
            onClick={(e) => { e.preventDefault(); handleShare(); }}
          >
            {t("circle.shareStatus")}
          </Button>
        </Stack>
      </CardContent>
    </Card>
  );
}
