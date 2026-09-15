import { useQuery } from "@tanstack/react-query";
import { Box, Grid, Stack, Typography, Button } from "@mui/material";
import { Link as RouterLink } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { circlesApi } from "../../api/circles";
import { CircleInfoCard } from "../Dashboard/CircleInfoCard";

export function MyCirclesPage() {
  const { t } = useTranslation();
  const { data: circles, isLoading } = useQuery({ queryKey: ["circles"], queryFn: circlesApi.list });

  if (isLoading) return <Typography>{t("common.loading")}</Typography>;

  return (
    <Stack spacing={3}>
      <Stack direction="row" justifyContent="space-between" alignItems="center">
        <Typography variant="h5" fontWeight={700}>{t("circle.myCircles")}</Typography>
        <Button component={RouterLink} to="/circles/new" variant="contained">{t("circle.createCircle")}</Button>
      </Stack>

      {(!circles || circles.length === 0) && (
        <Box textAlign="center" py={6}>
          <Typography color="text.secondary">{t("app.tagline")}</Typography>
        </Box>
      )}

      <Grid container spacing={2}>
        {circles?.map((circle) => (
          <Grid item xs={12} sm={6} md={4} key={circle.id}>
            <CircleInfoCard circle={circle} />
          </Grid>
        ))}
      </Grid>
    </Stack>
  );
}
