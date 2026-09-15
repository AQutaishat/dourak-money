import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Alert, Box, Button, MenuItem, Paper, Stack, TextField, Typography } from "@mui/material";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import axios from "axios";
import { authApi } from "../../api/auth";
import { useAuth } from "../../auth/AuthContext";

/**
 * prompt02 §8: name and phone are editable and optional; email is read-only here.
 * Uniqueness (normalized name/phone) is enforced by the API — its message is shown inline.
 */
export function ProfilePage() {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const { refreshProfile } = useAuth();

  const { data: profile, isLoading, refetch } = useQuery({ queryKey: ["profile"], queryFn: authApi.profile });

  const [name, setName] = useState("");
  const [phone, setPhone] = useState("");
  const [language, setLanguage] = useState("ar");
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!profile) return;
    setName(profile.name ?? "");
    setPhone(profile.phone ?? "");
    setLanguage(profile.preferredLanguage === "en" ? "en" : "ar");
  }, [profile]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSaved(false);
    setSaving(true);
    try {
      await authApi.updateProfile({
        name: name.trim() || null,
        phone: phone.trim() || null,
        preferredLanguage: language,
      });
      setSaved(true);
      await Promise.all([refetch(), refreshProfile()]);
      if (language !== i18n.language.slice(0, 2)) void i18n.changeLanguage(language);
    } catch (err) {
      const serverErrors: string[] | undefined = axios.isAxiosError(err) ? err.response?.data?.errors : undefined;
      setError(serverErrors?.length ? serverErrors.join(" ") : t("auth.profileSaveFailed"));
    } finally {
      setSaving(false);
    }
  };

  if (isLoading) return <Typography>{t("common.loading")}</Typography>;

  return (
    <Paper sx={{ p: 4, maxWidth: 520, mx: "auto" }}>
      <Typography variant="h5" fontWeight={700} gutterBottom>{t("auth.profileTitle")}</Typography>
      <Typography variant="body2" color="text.secondary" gutterBottom>{t("auth.profileHint")}</Typography>

      <Box component="form" onSubmit={handleSubmit} noValidate sx={{ mt: 3 }}>
        <Stack spacing={2}>
          {error && <Alert severity="error">{error}</Alert>}
          {saved && <Alert severity="success">{t("auth.profileSaved")}</Alert>}

          <TextField
            label={t("auth.email")}
            value={profile?.email ?? ""}
            fullWidth
            disabled
            helperText={t("auth.emailReadOnly")}
          />
          <TextField label={t("auth.name")} value={name} onChange={(e) => setName(e.target.value)} fullWidth />
          <TextField label={t("auth.phone")} value={phone} onChange={(e) => setPhone(e.target.value)} fullWidth />
          <TextField select label={t("auth.preferredLanguage")} value={language} onChange={(e) => setLanguage(e.target.value)} fullWidth>
            <MenuItem value="ar">العربية</MenuItem>
            <MenuItem value="en">English</MenuItem>
          </TextField>

          <Stack direction="row" spacing={1} justifyContent="flex-end">
            <Button onClick={() => navigate(-1)}>{t("common.close")}</Button>
            <Button type="submit" variant="contained" disabled={saving}>{t("common.save")}</Button>
          </Stack>
        </Stack>
      </Box>
    </Paper>
  );
}
