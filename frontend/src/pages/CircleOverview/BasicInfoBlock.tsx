import { Box, Grid, Paper, Typography } from "@mui/material";
import { useTranslation } from "react-i18next";
import type { CircleDetail } from "../../api/types";

/**
 * prompt02 §Draft circles: the draft view's first tab. §Active circles: the same read-only block
 * merged into the top of the Current Cycle tab (the preferred direction in the spec) rather than
 * living in a separate tab.
 */
export function BasicInfoBlock({ circle, dense = false }: { circle: CircleDetail; dense?: boolean }) {
  const { t, i18n } = useTranslation();

  const monthYear = (iso?: string | null) =>
    iso ? new Date(iso).toLocaleDateString(i18n.language, { month: "long", year: "numeric" }) : "—";
  const fullDate = (iso: string) =>
    new Date(iso).toLocaleDateString(i18n.language, { year: "numeric", month: "short", day: "numeric" });

  const rows: [string, string][] = [
    [t("circle.name"), circle.name],
    [t("circle.description"), circle.description || "—"],
    [t("circle.startDate"), fullDate(circle.startDate)],
    [t("circle.contributionAmount"), `${circle.contributionAmount} ${circle.currency}`],
    // Computed: members count × contribution amount.
    [t("circle.totalMonthlyAmount"), `${circle.totalMonthlyAmount} ${circle.currency}`],
    // Computed: start date + (member count − 1) months.
    [t("circle.lastPaymentMonth"), monthYear(circle.lastPaymentMonth)],
    [t("circle.memberCount"), String(circle.memberCount)],
    [t("circle.organizer"), circle.organizerName],
    [t("circle.createdAt"), fullDate(circle.createdAt)],
  ];

  return (
    <Paper variant="outlined" sx={{ p: dense ? 2 : 3, mb: dense ? 2 : 0 }}>
      {dense && <Typography variant="subtitle2" color="text.secondary" gutterBottom>{t("circle.basicInfo")}</Typography>}
      <Grid container spacing={dense ? 1 : 2}>
        {rows.map(([label, value]) => (
          <Grid item xs={12} sm={6} key={label}>
            <Box>
              <Typography variant="caption" color="text.secondary">{label}</Typography>
              <Typography variant={dense ? "body2" : "body1"} fontWeight={500}>{value}</Typography>
            </Box>
          </Grid>
        ))}
      </Grid>
    </Paper>
  );
}
