import { useState } from "react";
import { Box, Button, Paper, TextField, Typography, Alert, Stack, Link as MuiLink } from "@mui/material";
import { Link as RouterLink, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { AuthError, useAuth } from "../../auth/AuthContext";
import { useValidatedField } from "../../components/ValidatedTextField";
import dourakLogo from "../../assets/dourak-logo.png";

export function LoginPage() {
  const { t } = useTranslation();
  const { login } = useAuth();
  const navigate = useNavigate();

  // Common on-blur validation pattern (prompt02 §Login screen).
  const email = useValidatedField("", ["required", "email"]);
  const password = useValidatedField("", ["required"]);

  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    // Validate both fields before hitting the API so the user sees which one is wrong.
    const emailOk = email.validateNow();
    const passwordOk = password.validateNow();
    if (!emailOk || !passwordOk) return;

    setLoading(true);
    try {
      await login(email.value.trim(), password.value);
      navigate("/");
    } catch (err) {
      // No page reload and no cleared fields: whatever the user typed stays put, and the
      // message says plainly that the credentials are wrong (prompt02 §Login screen).
      setError(err instanceof AuthError && err.kind === "invalid-credentials"
        ? t("auth.invalidCredentials")
        : (err instanceof Error ? err.message : t("auth.invalidCredentials")));
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
        <Typography variant="h5" fontWeight={700} color="primary" gutterBottom align="center">{t("app.name")}</Typography>
        <Typography variant="body2" color="text.secondary" gutterBottom align="center">{t("app.tagline")}</Typography>
        <Box component="form" onSubmit={handleSubmit} noValidate sx={{ mt: 2 }}>
          <Stack spacing={2}>
            {/* Non-dismissing: it stays until the next submit attempt. */}
            {error && <Alert severity="error">{error}</Alert>}
            <TextField label={t("auth.email")} type="email" fullWidth autoComplete="email" {...email.fieldProps} />
            <TextField label={t("auth.password")} type="password" fullWidth autoComplete="current-password" {...password.fieldProps} />
            <Button type="submit" variant="contained" size="large" disabled={loading}>{t("auth.loginCta")}</Button>
            <Typography variant="body2">
              {t("auth.noAccount")} <MuiLink component={RouterLink} to="/register">{t("auth.register")}</MuiLink>
            </Typography>
          </Stack>
        </Box>
      </Paper>
    </Box>
  );
}
