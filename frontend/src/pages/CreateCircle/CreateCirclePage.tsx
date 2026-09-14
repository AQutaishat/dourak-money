import { useState } from "react";
import { Box, Button, Checkbox, FormControlLabel, MenuItem, Paper, Stack, TextField, Typography, Alert } from "@mui/material";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { circlesApi } from "../../api/circles";

const CURRENCIES = ["SAR", "USD", "EGP", "AED", "KWD", "QAR", "MAD"];

export function CreateCirclePage() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [currency, setCurrency] = useState("SAR");
  const [contributionAmount, setContributionAmount] = useState<number>(0);
  const [startDate, setStartDate] = useState(new Date().toISOString().slice(0, 10));
  const [organizerIsMember, setOrganizerIsMember] = useState(false);
  const [organizerMemberName, setOrganizerMemberName] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      const id = await circlesApi.create({
        name, description: description || undefined, currency, contributionAmount, startDate,
        organizerIsMember, organizerMemberName: organizerIsMember ? organizerMemberName : undefined,
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
      <Box component="form" onSubmit={handleSubmit} sx={{ mt: 2 }}>
        <Stack spacing={2}>
          {error && <Alert severity="error">{error}</Alert>}
          <TextField label={t("circle.name")} value={name} onChange={(e) => setName(e.target.value)} required fullWidth />
          <TextField label={t("circle.description")} value={description} onChange={(e) => setDescription(e.target.value)} fullWidth multiline rows={2} />
          <Stack direction="row" spacing={2}>
            <TextField select label={t("circle.currency")} value={currency} onChange={(e) => setCurrency(e.target.value)} sx={{ width: 160 }}>
              {CURRENCIES.map((c) => <MenuItem key={c} value={c}>{c}</MenuItem>)}
            </TextField>
            <TextField
              label={t("circle.contributionAmount")} type="number" value={contributionAmount}
              onChange={(e) => setContributionAmount(Number(e.target.value))} required fullWidth
              inputProps={{ min: 0.01, step: 0.01 }}
            />
          </Stack>
          <TextField label={t("circle.startDate")} type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} required fullWidth InputLabelProps={{ shrink: true }} />
          <TextField label={t("circle.frequency")} value={t("circle.monthly")} disabled fullWidth />

          <FormControlLabel
            control={<Checkbox checked={organizerIsMember} onChange={(e) => setOrganizerIsMember(e.target.checked)} />}
            label={t("circle.organizerIsMember")}
          />
          {organizerIsMember && (
            <TextField label={t("circle.organizerMemberName")} value={organizerMemberName} onChange={(e) => setOrganizerMemberName(e.target.value)} fullWidth />
          )}

          <Button type="submit" variant="contained" size="large" disabled={submitting}>{t("common.next")}</Button>
        </Stack>
      </Box>
    </Paper>
  );
}
