import { useState } from "react";
import { Box, Button, MenuItem, Paper, Stack, TextField, Typography, Alert } from "@mui/material";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { circlesApi } from "../../api/circles";
import { SelectOnFocusTextField, useValidatedField } from "../../components/ValidatedTextField";

// prompt02 §Create Circle: JOD added to the supported currencies.
const CURRENCIES = ["SAR", "JOD", "USD", "EGP", "AED", "KWD", "QAR", "MAD"];

export function CreateCirclePage() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  const name = useValidatedField("", ["required"]);
  const [description, setDescription] = useState("");
  const [currency, setCurrency] = useState("JOD");
  const [contributionAmount, setContributionAmount] = useState("0");
  const [startDate, setStartDate] = useState(new Date().toISOString().slice(0, 10));
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    if (!name.validateNow()) return;

    setSubmitting(true);
    try {
      const id = await circlesApi.create({
        name: name.value.trim(),
        description: description || undefined,
        currency,
        // Frequency is fixed to Monthly server-side; no selector is shown (prompt02 §Create Circle).
        contributionAmount: Number(contributionAmount),
        startDate,
      });
      navigate(`/circles/${id}`);
    } catch {
      setError("Could not create the circle. Please check the fields and try again.");
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <Paper sx={{ p: 4, maxWidth: 560, mx: "auto" }}>
      <Typography variant="h5" fontWeight={700} gutterBottom>{t("circle.createCircle")}</Typography>
      <Box component="form" onSubmit={handleSubmit} noValidate sx={{ mt: 2 }}>
        <Stack spacing={2}>
          {error && <Alert severity="error">{error}</Alert>}
          <TextField label={t("circle.name")} fullWidth {...name.fieldProps} />
          <TextField label={t("circle.description")} value={description} onChange={(e) => setDescription(e.target.value)} fullWidth multiline rows={2} />
          <Stack direction="row" spacing={2}>
            <TextField select label={t("circle.currency")} value={currency} onChange={(e) => setCurrency(e.target.value)} sx={{ width: 160 }}>
              {CURRENCIES.map((c) => <MenuItem key={c} value={c}>{c}</MenuItem>)}
            </TextField>
            <SelectOnFocusTextField
              label={t("circle.contributionAmount")} type="number" value={contributionAmount}
              onChange={(e) => setContributionAmount(e.target.value)} required fullWidth
              inputProps={{ min: 0.01, step: 0.01 }}
            />
          </Stack>
          <TextField label={t("circle.startDate")} type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} required fullWidth InputLabelProps={{ shrink: true }} />

          <Stack direction="row" spacing={1} justifyContent="flex-end">
            <Button onClick={() => navigate(-1)}>{t("common.cancel")}</Button>
            <Button type="submit" variant="contained" size="large" disabled={submitting || Number(contributionAmount) <= 0}>
              {t("common.next")}
            </Button>
          </Stack>
        </Stack>
      </Box>
    </Paper>
  );
}
