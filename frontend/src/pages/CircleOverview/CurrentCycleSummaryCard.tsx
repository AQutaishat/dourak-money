import { useQuery } from "@tanstack/react-query";
import { Box, Card, CardContent, Typography, Stack, LinearProgress, Chip, Button, Tooltip } from "@mui/material";
import WhatsAppIcon from "@mui/icons-material/WhatsApp";
import { Link as RouterLink } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { circlesApi } from "../../api/circles";
import { buildCurrentCycleShareText, shareToWhatsApp } from "../../utils/whatsapp";
import { monthOrdinalWord } from "../../utils/monthOrdinal";
import { CircleTimeline } from "./CircleTimeline";

export function CurrentCycleSummaryCard({
  circleId, circleName, currency, memberCount, organizerName, contributionAmount, createdAt,
}: {
  circleId: number;
  circleName: string;
  currency: string;
  /** prompt03 §3: the active-with-progress card variant was missing these (Phase 2 only added
      them to the general/draft grid cards below). */
  memberCount: number;
  organizerName: string;
  contributionAmount: number;
  createdAt: string;
}) {
  const { t, i18n } = useTranslation();
  const { data: dashboard } = useQuery({ queryKey: ["dashboard", circleId], queryFn: () => circlesApi.dashboard(circleId) });
  const { data: schedule } = useQuery({ queryKey: ["schedule", circleId], queryFn: () => circlesApi.schedule(circleId) });

  if (!dashboard) return null;

  const progress = dashboard.expected > 0 ? (dashboard.collected / dashboard.expected) * 100 : 0;
  const monthLabel = new Date(dashboard.dueDate).toLocaleDateString(i18n.language, { month: "long", year: "numeric" });
  const created = new Date(createdAt);
  const createdDdMmYyyy = `${String(created.getDate()).padStart(2, "0")}/${String(created.getMonth() + 1).padStart(2, "0")}/${created.getFullYear()}`;
  const fullyCollected = dashboard.collected >= dashboard.expected;
  const isArabic = i18n.language.startsWith("ar");
  const dueMonthName = new Date(dashboard.dueDate).toLocaleDateString(i18n.language, { month: "long" });

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
          <Tooltip
            title={
              <>
                {t("circle.createdAt")}{" "}
                <span style={{ unicodeBidi: "isolate", direction: "ltr" }}>{createdDdMmYyyy}</span>
              </>
            }
          >
            <Typography variant="caption" color="text.secondary">
              {created.toLocaleDateString(i18n.language, { year: "numeric", month: "long" })}
            </Typography>
          </Tooltip>
        </Stack>

        <Typography variant="caption" color="text.secondary" display="block">
          {t("circle.organizer")}: {organizerName}. {t("circle.members")}: {memberCount}. {t("circle.installmentLabel")}: {contributionAmount} {currency}.
        </Typography>

        {schedule && schedule.length > 0 && (
          <Box sx={{ my: 1 }}>
            <CircleTimeline schedule={schedule} currentCycleId={dashboard.cycleId} />
          </Box>
        )}

        <Stack direction="row" spacing={1} alignItems="baseline" sx={{ mt: 2, mb: 0.5 }}>
          <Typography variant="subtitle2" fontWeight={700}>{t("circle.currentCycle")}</Typography>
          {schedule && schedule.length > 0 && (
            <Typography variant="body2" color="text.secondary">
              {t("circle.monthOrdinalDetail", {
                month: dueMonthName, ordinal: monthOrdinalWord(dashboard.sequenceNumber, isArabic), total: schedule.length,
              })}
            </Typography>
          )}
        </Stack>
        <Typography variant="body2" sx={{ mb: 1 }}>
          {t("circle.recipient")}: <strong>{dashboard.recipientName}</strong>
        </Typography>
        <Stack direction="row" spacing={1} flexWrap="wrap" sx={{ mb: 1 }}>
          <Chip size="small" color="success" label={`${t("circle.paid")}: ${dashboard.membersPaid}/${dashboard.membersTotal}`} />
          {dashboard.membersLate > 0 && <Chip size="small" color="error" label={`${t("circle.late")}: ${dashboard.membersLate}`} />}
          <Chip
            size="small"
            color={fullyCollected ? "success" : "warning"}
            label={t(fullyCollected ? "circle.collectionDone" : "circle.collectionUnderway")}
          />
          {(fullyCollected || dashboard.payoutStatus === "Paid") && (
            <Chip
              size="small"
              color={dashboard.payoutStatus === "Paid" ? "success" : "warning"}
              label={t(dashboard.payoutStatus === "Paid" ? "circle.payoutPaidBadge" : "circle.payoutPendingBadge")}
            />
          )}
        </Stack>

        <LinearProgress variant="determinate" value={Math.min(progress, 100)} sx={{ height: 8, borderRadius: 4, mb: 1 }} />
        <Typography variant="body2" color="text.secondary">
          {dashboard.collected} / {dashboard.expected} {currency} · {t("circle.outstanding")}: {dashboard.outstanding} {currency}
        </Typography>

        <Stack direction="row" justifyContent="flex-end" sx={{ mt: 1 }}>
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
