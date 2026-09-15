import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Alert, Box, Button, Grid, Paper, Stack, TextField, Typography } from "@mui/material";
import EditIcon from "@mui/icons-material/Edit";
import { useTranslation } from "react-i18next";
import { circlesApi } from "../../api/circles";
import type { CircleDetail } from "../../api/types";

/**
 * prompt02 §Draft circles: the draft view's first tab. §Active circles: the same read-only block
 * merged into the top of the Current Cycle tab (the preferred direction in the spec) rather than
 * living in a separate tab.
 *
 * prompt03 §1: while the circle is still a draft, the organizer can edit name, description and
 * start date directly from this tab. Contribution amount stays non-editable throughout, and once
 * activated the whole block goes back to read-only (structure is locked, same as before).
 */
export function BasicInfoBlock({ circle, dense = false }: { circle: CircleDetail; dense?: boolean }) {
  const { t, i18n } = useTranslation();
  const queryClient = useQueryClient();

  const canEdit = circle.status === "Draft" && circle.isOrganizer && !dense;
  const [editing, setEditing] = useState(false);
  const [name, setName] = useState(circle.name);
  const [description, setDescription] = useState(circle.description ?? "");
  const [startDate, setStartDate] = useState(circle.startDate.slice(0, 10));
  const [error, setError] = useState<string | null>(null);

  const save = useMutation({
    mutationFn: () => circlesApi.updateBasicInfo(circle.id, {
      name: name.trim(),
      description: description.trim() || undefined,
      startDate,
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["circle", circle.id] });
      setEditing(false);
      setError(null);
    },
    onError: (err: unknown) => {
      const title = (err as { response?: { data?: { title?: string } } })?.response?.data?.title;
      setError(title ?? t("common.error"));
    },
  });

  const startEditing = () => {
    setName(circle.name);
    setDescription(circle.description ?? "");
    setStartDate(circle.startDate.slice(0, 10));
    setError(null);
    setEditing(true);
  };

  const monthYear = (iso?: string | null) =>
    iso ? new Date(iso).toLocaleDateString(i18n.language, { month: "long", year: "numeric" }) : "—";
  const fullDate = (iso: string) =>
    new Date(iso).toLocaleDateString(i18n.language, { year: "numeric", month: "short", day: "numeric" });

  if (editing) {
    return (
      <Paper variant="outlined" sx={{ p: 3 }}>
        {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
        <Stack spacing={2}>
          <TextField label={t("circle.name")} value={name} onChange={(e) => setName(e.target.value)} fullWidth required />
          <TextField label={t("circle.description")} value={description} onChange={(e) => setDescription(e.target.value)} fullWidth multiline minRows={2} />
          <TextField
            label={t("circle.startDate")}
            type="date"
            value={startDate}
            onChange={(e) => setStartDate(e.target.value)}
            fullWidth
            slotProps={{ inputLabel: { shrink: true } }}
          />
          <Stack direction="row" spacing={1}>
            <Button variant="contained" disabled={!name.trim() || save.isPending} onClick={() => save.mutate()}>
              {t("common.save")}
            </Button>
            <Button onClick={() => setEditing(false)}>{t("common.cancel")}</Button>
          </Stack>
        </Stack>
      </Paper>
    );
  }

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
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: dense ? 1 : 0 }}>
        {dense && <Typography variant="subtitle2" color="text.secondary" gutterBottom>{t("circle.basicInfo")}</Typography>}
        {canEdit && (
          <Button size="small" startIcon={<EditIcon />} onClick={startEditing} sx={{ ms: "auto" }}>
            {t("common.edit")}
          </Button>
        )}
      </Stack>
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
