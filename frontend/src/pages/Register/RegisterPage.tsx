import { useState } from "react";
import { Box, Button, Paper, TextField, Typography, Alert, Stack, Link as MuiLink } from "@mui/material";
import { Link as RouterLink, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { useAuth } from "../../auth/AuthContext";
import { useValidatedField } from "../../components/ValidatedTextField";

/**
 * prompt02 §7 / §Register screen: only email and password. Name and phone are set later by the
 * user from their profile page. Errors use the shared on-blur pattern instead of a popup.
 */
export function RegisterPage() {
  const { t } = useTranslation();
  const { register } = useAuth();
  const navigate = useNavigate();

  const email = useValidatedField("", ["required", "email"]);
  const password = useValidatedField("", ["required", "password"]);

  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    const emailOk = email.validateNow();
    const passwordOk = password.validateNow();
    if (!emailOk || !passwordOk) return;

    setLoading(true);
    try {
      await register(email.value.trim(), password.value);
      navigate("/");
    } catch (err) {
      const message = err instanceof Error ? err.message : "";
      // The API says specifically when the email is taken — show that, not a generic failure.
      setError(/already exists|already registered/i.test(message)
        ? t("auth.emailAlreadyRegistered")
        : (message || t("common.error")));
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box sx={{ minHeight: "100vh", display: "flex", alignItems: "center", justifyContent: "center", bgcolor: "background.default", p: 2 }}>
      <Paper sx={{ p: 4, width: 380, maxWidth: "100%" }}>
        <Typography variant="h5" fontWeight={700} color="primary" gutterBottom>{t("auth.register")}</Typography>
        <Box component="form" onSubmit={handleSubmit} noValidate sx={{ mt: 2 }}>
          <Stack spacing={2}>
            {error && <Alert severity="error">{error}</Alert>}
            <TextField label={t("auth.email")} type="email" fullWidth autoComplete="email" {...email.fieldProps} />
            <TextField
              label={t("auth.password")}
              type="password"
              fullWidth
              autoComplete="new-password"
              {...password.fieldProps}
              // The rules are visible up front, not revealed only on failure.
              helperText={password.fieldProps.helperText ?? t("auth.passwordRules")}
            />
            <Button type="submit" variant="contained" size="large" disabled={loading}>{t("auth.registerCta")}</Button>
            <Typography variant="body2">
              {t("auth.haveAccount")} <MuiLink component={RouterLink} to="/login">{t("auth.login")}</MuiLink>
            </Typography>
          </Stack>
        </Box>
      </Paper>
    </Box>
  );
}
