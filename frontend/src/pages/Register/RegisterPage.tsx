import { useState } from "react";
import { Box, Button, Paper, TextField, Typography, Alert, Stack, Link as MuiLink, MenuItem } from "@mui/material";
import { Link as RouterLink, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { useAuth } from "../../auth/AuthContext";

export function RegisterPage() {
  const { t, i18n } = useTranslation();
  const { register } = useAuth();
  const navigate = useNavigate();
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [language, setLanguage] = useState(i18n.language.startsWith("ar") ? "ar" : "en");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      await register(name, email, password, language);
      navigate("/");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Registration failed");
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box sx={{ minHeight: "100vh", display: "flex", alignItems: "center", justifyContent: "center", bgcolor: "background.default" }}>
      <Paper sx={{ p: 4, width: 380 }}>
        <Typography variant="h5" fontWeight={700} color="primary" gutterBottom>{t("auth.register")}</Typography>
        <Box component="form" onSubmit={handleSubmit} sx={{ mt: 2 }}>
          <Stack spacing={2}>
            {error && <Alert severity="error">{error}</Alert>}
            <TextField label={t("auth.name")} value={name} onChange={(e) => setName(e.target.value)} required fullWidth />
            <TextField label={t("auth.email")} type="email" value={email} onChange={(e) => setEmail(e.target.value)} required fullWidth />
            <TextField label={t("auth.password")} type="password" value={password} onChange={(e) => setPassword(e.target.value)} required fullWidth helperText="8+ characters" />
            <TextField select label={t("auth.preferredLanguage")} value={language} onChange={(e) => setLanguage(e.target.value)} fullWidth>
              <MenuItem value="ar">العربية</MenuItem>
              <MenuItem value="en">English</MenuItem>
            </TextField>
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
