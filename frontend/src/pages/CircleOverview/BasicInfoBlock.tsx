import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Alert, Box, Button, Grid, IconButton, Paper, Stack, TextField, Typography } from "@mui/material";
import EditIcon from "@mui/icons-material/Edit";
import ExpandLessIcon from "@mui/icons-material/ExpandLess";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import { useTranslation } from "react-i18next";
import { circlesApi } from "../../api/circles";
import type { CircleDetail } from "../../api/types";
import { buildDateFromMonthAndDay } from "../../utils/date";

/**
 * Single shared box rendered once in CircleOverviewPage, above the tabs and below the timeline —
 * not tab-specific content, so it stays visible regardless of which tab is open. Same fields,
 * same order, same code path for every circle status. The only thing that differs is the Edit
 * button, which only appears while the circle is a Draft (name and creation date live in the page
 * header instead, not in this box).
 */
export function BasicInfoBlock({ circle }: { circle: CircleDetail }) {
  const { t, i18n } = useTranslation();
  const queryClient = useQueryClient();

  const canEdit = circle.status === "Draft" && circle.isOrganizer;
  const [editing, setEditing] = useState(false);
  const [collapsed, setCollapsed] = useState(false);
  const [name, setName] = useState(circle.name);
  const [description, setDescription] = useState(circle.description ?? "");
  // The month/year and the collection day are edited as two separate fields, then combined into
  // the one `startDate` the backend expects.
  const [startMonth, setStartMonth] = useState(circle.startDate.slice(0, 7));
  const [collectionDay, setCollectionDay] = useState(new Date(circle.startDate).getDate());
  const [contributionAmount, setContributionAmount] = useState<number>(circle.contributionAmount);
  const [error, setError] = useState<string | null>(null);

  const save = useMutation({
    mutationFn: () => circlesApi.updateBasicInfo(circle.id, {
      name: name.trim(),
      description: description.trim() || undefined,
      startDate: buildDateFromMonthAndDay(startMonth, collectionDay),
      contributionAmount,
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
    setStartMonth(circle.startDate.slice(0, 7));
    setCollectionDay(new Date(circle.startDate).getDate());
    setContributionAmount(circle.contributionAmount);
    setError(null);
    setEditing(true);
  };

  const monthYear = (iso?: string | null) =>
    iso ? new Date(iso).toLocaleDateString(i18n.language, { month: "long", year: "numeric" }) : "—";

  if (editing) {
    return (
      <Paper variant="outlined" sx={{ p: 3 }}>
        {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
        <Stack spacing={2}>
          <TextField label={t("circle.name")} value={name} onChange={(e) => setName(e.target.value)} fullWidth required />
          <TextField label={t("circle.description")} value={description} onChange={(e) => setDescription(e.target.value)} fullWidth multiline minRows={2} />
          <Stack direction="row" spacing={2}>
            <TextField
              label={t("circle.startDate")}
              type="month"
              value={startMonth}
              onChange={(e) => setStartMonth(e.target.value)}
              fullWidth
              slotProps={{ inputLabel: { shrink: true } }}
            />
            <TextField
              label={t("circle.collectionDay")}
              type="number"
              value={collectionDay}
              onChange={(e) => setCollectionDay(Number(e.target.value))}
              fullWidth
              inputProps={{ min: 1, max: 31, step: 1 }}
            />
          </Stack>
          <TextField
            label={t("circle.contributionAmount")}
            type="number"
            value={contributionAmount}
            onChange={(e) => setContributionAmount(Number(e.target.value))}
            onFocus={(e) => (e.target as HTMLInputElement).select()}
            fullWidth
            required
            inputProps={{ min: 0.01, step: 0.01 }}
          />
          <Stack direction="row" spacing={1}>
            <Button variant="contained" disabled={!name.trim() || contributionAmount <= 0 || save.isPending} onClick={() => save.mutate()}>
              {t("common.save")}
            </Button>
            <Button onClick={() => setEditing(false)}>{t("common.cancel")}</Button>
          </Stack>
        </Stack>
      </Paper>
    );
  }

  const field = (label: string, value: string) => (
    <Box>
      <Typography variant="caption" color="text.secondary">{label}</Typography>
      <Typography variant="body2" fontWeight={500}>{value}</Typography>
    </Box>
  );

  // Identical field set/order for a draft circle's own tab and an active circle's merged box —
  // no circle-name row (shown in the page header) and no creation-date row (also in the header).
  return (
    <Paper variant="outlined" sx={{ p: 2, mb: 2 }}>
      <Stack direction="row" justifyContent="space-between" alignItems="center">
        {/* flex-start (not flex-end) so this naturally lands on the right in Arabic and the left
            in English, following the page's own reading direction rather than fighting it. */}
        {canEdit ? (
          <Button size="small" startIcon={<EditIcon />} onClick={startEditing}>
            {t("common.edit")}
          </Button>
        ) : <Box />}
        <IconButton size="small" onClick={() => setCollapsed((c) => !c)}>
          {collapsed ? <ExpandMoreIcon /> : <ExpandLessIcon />}
        </IconButton>
      </Stack>
      {!collapsed && (
        <Grid container spacing={1}>
          <Grid item xs={12}>{field(t("circle.description"), circle.description || "—")}</Grid>
          <Grid item xs={12} sm={6}>{field(t("circle.startDate"), monthYear(circle.startDate))}</Grid>
          <Grid item xs={12} sm={6}>{field(t("circle.lastPaymentMonth"), monthYear(circle.lastPaymentMonth))}</Grid>
          {/* Collection day sits directly under the start date; the other column of this row is
              deliberately left empty. */}
          <Grid item xs={12} sm={6}>{field(t("circle.collectionDay"), String(new Date(circle.startDate).getDate()))}</Grid>
          <Grid item xs={12} sm={6} />
          <Grid item xs={12} sm={6}>{field(t("circle.contributionAmount"), `${circle.contributionAmount} ${circle.currency}`)}</Grid>
          <Grid item xs={12} sm={6}>{field(t("circle.totalMonthlyAmount"), `${circle.totalMonthlyAmount} ${circle.currency}`)}</Grid>
          <Grid item xs={12} sm={6}>{field(t("circle.organizer"), circle.organizerName)}</Grid>
          <Grid item xs={12} sm={6}>{field(t("circle.memberCount"), String(circle.memberCount))}</Grid>
        </Grid>
      )}
    </Paper>
  );
}
