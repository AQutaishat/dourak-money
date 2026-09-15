import { useQuery } from "@tanstack/react-query";
import { Box, Typography, Grid, Button, Stack } from "@mui/material";
import { Link as RouterLink } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { circlesApi } from "../../api/circles";
import { CurrentCycleSummaryCard } from "../CircleOverview/CurrentCycleSummaryCard";
import { PendingInvitationsSection } from "./PendingInvitationsSection";
import { CircleInfoCard } from "./CircleInfoCard";

export function DashboardPage() {
  const { t } = useTranslation();
  const { data: circles, isLoading } = useQuery({ queryKey: ["circles"], queryFn: circlesApi.list });

  const activeCircles = circles?.filter((c) => c.status === "Active") ?? [];

  if (isLoading) return <Typography>{t("common.loading")}</Typography>;

  if (!circles || circles.length === 0) {
    return (
      <Stack spacing={4}>
        {/* Someone invited to their first circle has no circles of their own yet — the
            invitations section must still show (prompt02 §4). */}
        <PendingInvitationsSection />
        <Box textAlign="center" py={6}>
          <Typography variant="h6" gutterBottom>{t("circle.myCircles")}</Typography>
          <Typography color="text.secondary" gutterBottom>{t("app.tagline")}</Typography>
          <Button component={RouterLink} to="/circles/new" variant="contained" sx={{ mt: 2 }}>
            {t("circle.createCircle")}
          </Button>
        </Box>
      </Stack>
    );
  }

  return (
    <Stack spacing={3}>
      <Stack direction="row" justifyContent="space-between" alignItems="center">
        <Typography variant="h5" fontWeight={700}>{t("nav.dashboard")}</Typography>
        <Button component={RouterLink} to="/circles/new" variant="contained">{t("circle.createCircle")}</Button>
      </Stack>

      <PendingInvitationsSection />

      <Grid container spacing={2}>
        {activeCircles.map((circle) => (
          <Grid item xs={12} md={6} key={circle.id}>
            <CurrentCycleSummaryCard
              circleId={circle.id}
              circleName={circle.name}
              currency={circle.currency}
              memberCount={circle.memberCount}
              organizerName={circle.organizerName}
            />
          </Grid>
        ))}
      </Grid>

      <Typography variant="h6" sx={{ mt: 2 }}>{t("circle.myCircles")}</Typography>
      <Grid container spacing={2}>
        {circles.map((circle) => (
          <Grid item xs={12} sm={6} md={4} key={circle.id}>
            <CircleInfoCard circle={circle} />
          </Grid>
        ))}
      </Grid>
    </Stack>
  );
}
