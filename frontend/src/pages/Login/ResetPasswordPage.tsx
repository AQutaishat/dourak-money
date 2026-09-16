import { useState } from "react";
import { Box, Button, Paper, TextField, Typography, Alert, Stack, Link as MuiLink } from "@mui/material";
import { Link as RouterLink, useNavigate, useSearchParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import axios from "axios";
import { authApi } from "../../api/auth";
import { useValidatedField } from "../../components/ValidatedTextField";
import dourakLogo from "../../assets/dourak-logo.png";

/** Landed on from the link in the password-reset email — userId/token come from the URL. */
export function ResetPasswordPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const userId = params.get("userId") ?? "";
  const token = params.get("token") ?? "";

  const password = useValidatedField("", ["required", "password"]);
  const [error, setError] = useState<string | null>(null);
  const [succeeded, setSucceeded] = useState(false);
  const [loading, setLoading] = useState(false);

  const missingLinkParams = !userId || !token;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    if (!password.validateNow()) return;

    setLoading(true);
    try {
      await authApi.resetPassword({ userId, token, newPassword: password.value });
      setSucceeded(true);
    } catch (err) {
      const message = axios.isAxiosError(err) ? err.response?.data?.errors?.join(" ") : undefined;
      setError(message || t("auth.resetPasswordInvalidLink"));
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box sx={{ minHeight: "100vh", display: "flex", alignItems: "center", justifyContent: "center", bgcolor: "background.default", p: 2 }}>
      <Paper sx={{ p: 4, width: 360, maxWidth: "100%" }}>
        <Box sx={{ display: "flex", justifyContent: "center", mb: 2 }}>
          <Box component="img" src={dourakLogo} alt="" width={64} height={64} sx={{ borderRadius: 2 }} />
        </Box>
        {succeeded ? (
          <Stack spacing={2} alignItems="center" textAlign="center">
            <Alert severity="success" sx={{ width: "100%" }}>{t("auth.resetPasswordSuccess")}</Alert>
            <Button variant="contained" onClick={() => navigate("/login")}>{t("auth.backToLogin")}</Button>
          </Stack>
        ) : missingLinkParams ? (
          <Stack spacing={2} alignItems="center" textAlign="center">
            <Alert severity="error" sx={{ width: "100%" }}>{t("auth.resetPasswordInvalidLink")}</Alert>
            <MuiLink component={RouterLink} to="/forgot-password">{t("auth.forgotPassword")}</MuiLink>
          </Stack>
        ) : (
          <Box component="form" onSubmit={handleSubmit} noValidate>
            <Typography variant="h6" fontWeight={700} align="center" gutterBottom>{t("auth.resetPasswordTitle")}</Typography>
            <Stack spacing={2} sx={{ mt: 2 }}>
              {error && <Alert severity="error">{error}</Alert>}
              <TextField
                label={t("auth.newPassword")}
                type="password"
                fullWidth
                autoComplete="new-password"
                autoFocus
                {...password.fieldProps}
                helperText={password.fieldProps.helperText ?? t("auth.passwordRules")}
              />
              <Button type="submit" variant="contained" size="large" disabled={loading}>{t("auth.resetPasswordCta")}</Button>
            </Stack>
          </Box>
        )}
      </Paper>
    </Box>
  );
}
