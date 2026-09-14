import { useQuery } from "@tanstack/react-query";
import { Box, Card, CardContent, Typography, Grid, Chip, Button, Stack } from "@mui/material";
import { Link as RouterLink } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { circlesApi } from "../../api/circles";
import { CurrentCycleSummaryCard } from "../CircleOverview/CurrentCycleSummaryCard";

export function DashboardPage() {
  const { t } = useTranslation();
  const { data: circles, isLoading } = useQuery({ queryKey: ["circles"], queryFn: circlesApi.list });

  const activeCircles = circles?.filter((c) => c.status === "Active") ?? [];

  if (isLoading) return <Typography>{t("common.loading")}</Typography>;

  if (!circles || circles.length === 0) {
    return (
      <Box textAlign="center" py={8}>
        <Typography variant="h6" gutterBottom>{t("circle.myCircles")}</Typography>
        <Typography color="text.secondary" gutterBottom>{t("app.tagline")}</Typography>
        <Button component={RouterLink} to="/circles/new" variant="contained" sx={{ mt: 2 }}>
          {t("circle.createCircle")}
        </Button>
      </Box>
    );
  }

  return (
    <Stack spacing={3}>
      <Stack direction="row" justifyContent="space-between" alignItems="center">
        <Typography variant="h5" fontWeight={700}>{t("nav.dashboard")}</Typography>
        <Button component={RouterLink} to="/circles/new" variant="contained">{t("circle.createCircle")}</Button>
      </Stack>

      {activeCircles.length === 0 && (
        <Typography color="text.secondary">{t("circle.draft")} / {t("circle.active")} —</Typography>
      )}

      <Grid container spacing={2}>
        {activeCircles.map((circle) => (
          <Grid item xs={12} md={6} key={circle.id}>
            <CurrentCycleSummaryCard circleId={circle.id} circleName={circle.name} currency={circle.currency} />
          </Grid>
        ))}
      </Grid>

      <Typography variant="h6" sx={{ mt: 2 }}>{t("circle.myCircles")}</Typography>
      <Grid container spacing={2}>
        {circles.map((circle) => (
          <Grid item xs={12} sm={6} md={4} key={circle.id}>
            <Card component={RouterLink} to={`/circles/${circle.id}`} sx={{ textDecoration: "none", display: "block" }}>
              <CardContent>
                <Stack direction="row" justifyContent="space-between" alignItems="center">
                  <Typography variant="subtitle1" fontWeight={600}>{circle.name}</Typography>
                  <Chip size="small" label={t(`circle.${circle.status.toLowerCase()}`)} />
                </Stack>
                <Typography variant="body2" color="text.secondary">
                  {circle.memberCount} · {circle.contributionAmount} {circle.currency}
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>
    </Stack>
  );
}
