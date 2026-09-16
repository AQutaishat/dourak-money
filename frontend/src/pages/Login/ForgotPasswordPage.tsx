import { useState } from "react";
import { Box, Button, Paper, TextField, Typography, Alert, Stack, Link as MuiLink } from "@mui/material";
import { Link as RouterLink } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { authApi } from "../../api/auth";
import { useValidatedField } from "../../components/ValidatedTextField";
import dourakLogo from "../../assets/dourak-logo.png";

/**
 * The backend always responds 204 here regardless of whether the email is registered (see
 * AuthController.ForgotPassword) — this page must show the same "check your email" success
 * state either way, never a different message for "email not found", or it'd leak which
 * emails have accounts.
 */
export function ForgotPasswordPage() {
  const { t } = useTranslation();
  const email = useValidatedField("", ["required", "email"]);
  const [submitted, setSubmitted] = useState(false);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!email.validateNow()) return;
    setLoading(true);
    try {
      await authApi.forgotPassword({ email: email.value.trim() });
    } finally {
      // Always show the success state, even on a network error — retrying costs nothing and
      // a different error state here would be more confusing than helpful.
      setLoading(false);
      setSubmitted(true);
    }
  };

  return (
    <Box sx={{ minHeight: "100vh", display: "flex", alignItems: "center", justifyContent: "center", bgcolor: "background.default", p: 2 }}>
      <Paper sx={{ p: 4, width: 360, maxWidth: "100%" }}>
        <Box sx={{ display: "flex", justifyContent: "center", mb: 2 }}>
          <Box component="img" src={dourakLogo} alt="" width={64} height={64} sx={{ borderRadius: 2 }} />
        </Box>
        {submitted ? (
          <Stack spacing={2} alignItems="center" textAlign="center">
            <Typography variant="h6" fontWeight={700}>{t("auth.resetLinkSentTitle")}</Typography>
            <Alert severity="success" sx={{ width: "100%" }}>{t("auth.resetLinkSentHint")}</Alert>
            <MuiLink component={RouterLink} to="/login">{t("auth.backToLogin")}</MuiLink>
          </Stack>
        ) : (
          <Box component="form" onSubmit={handleSubmit} noValidate>
            <Typography variant="h6" fontWeight={700} align="center" gutterBottom>{t("auth.forgotPasswordTitle")}</Typography>
            <Typography variant="body2" color="text.secondary" align="center" gutterBottom>{t("auth.forgotPasswordHint")}</Typography>
            <Stack spacing={2} sx={{ mt: 2 }}>
              <TextField label={t("auth.email")} type="email" fullWidth autoComplete="email" autoFocus {...email.fieldProps} />
              <Button type="submit" variant="contained" size="large" disabled={loading}>{t("auth.sendResetLink")}</Button>
              <MuiLink component={RouterLink} to="/login" textAlign="center">{t("auth.backToLogin")}</MuiLink>
            </Stack>
          </Box>
        )}
      </Paper>
    </Box>
  );
}
