import { useEffect, useRef, useState } from "react";
import {
  Box, Button, Paper, TextField, Typography, Alert, Stack, Link as MuiLink,
  Select, MenuItem, IconButton, Divider,
} from "@mui/material";
import VisibilityIcon from "@mui/icons-material/Visibility";
import VisibilityOffIcon from "@mui/icons-material/VisibilityOff";
import { Link as RouterLink, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { AuthError, useAuth } from "../../auth/AuthContext";
import { useValidatedField } from "../../components/ValidatedTextField";
import { authApi } from "../../api/auth";
import dourakLogo from "../../assets/dourak-logo.png";

// Google Identity Services' JS SDK attaches itself to `window.google` — no official types
// package for this bit (the credential-response button flow), so a minimal ambient shape is
// declared here rather than pulling in a whole @types package for three methods.
declare global {
  interface Window {
    google?: {
      accounts: {
        id: {
          initialize: (config: { client_id: string; callback: (response: { credential: string }) => void }) => void;
          renderButton: (parent: HTMLElement, options: Record<string, unknown>) => void;
        };
      };
    };
  }
}

const GOOGLE_SCRIPT_ID = "google-identity-services";

function loadGoogleScript(): Promise<void> {
  return new Promise((resolve, reject) => {
    if (window.google?.accounts?.id) { resolve(); return; }
    const existing = document.getElementById(GOOGLE_SCRIPT_ID);
    if (existing) { existing.addEventListener("load", () => resolve()); return; }
    const script = document.createElement("script");
    script.id = GOOGLE_SCRIPT_ID;
    script.src = "https://accounts.google.com/gsi/client";
    script.async = true;
    script.defer = true;
    script.onload = () => resolve();
    script.onerror = () => reject(new Error("Failed to load Google Identity Services"));
    document.head.appendChild(script);
  });
}

export function LoginPage() {
  const { t, i18n } = useTranslation();
  const { login, googleLogin } = useAuth();
  const navigate = useNavigate();
  const googleButtonRef = useRef<HTMLDivElement>(null);
  // undefined = still checking; null = checked, not available. Never renders the button (or
  // loads Google's script at all) until the backend has actually confirmed it's configured —
  // see GoogleAuthOptions.IsUsable server-side.
  const [googleClientId, setGoogleClientId] = useState<string | null | undefined>(undefined);

  // Common on-blur validation pattern (prompt02 §Login screen).
  const email = useValidatedField("", ["required", "email"]);
  const password = useValidatedField("", ["required"]);

  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [showPassword, setShowPassword] = useState(false);

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

  useEffect(() => {
    authApi.config()
      .then((config) => setGoogleClientId(config.googleSignInEnabled ? config.googleClientId : null))
      .catch(() => setGoogleClientId(null)); // config lookup failing must never block plain email/password login
  }, []);

  useEffect(() => {
    if (!googleClientId || !googleButtonRef.current) return;
    let cancelled = false;

    loadGoogleScript()
      .then(() => {
        if (cancelled || !window.google || !googleButtonRef.current) return;
        window.google.accounts.id.initialize({
          client_id: googleClientId,
          callback: async (response) => {
            setError(null);
            try {
              await googleLogin(response.credential);
              navigate("/");
            } catch (err) {
              setError(err instanceof Error ? err.message : t("auth.invalidCredentials"));
            }
          },
        });
        window.google.accounts.id.renderButton(googleButtonRef.current, {
          type: "standard", theme: "outline", size: "large", width: 296,
          text: "continue_with", locale: i18n.language.startsWith("ar") ? "ar" : "en",
        });
      })
      .catch(() => setGoogleClientId(null)); // script failed to load — fail quiet, not a login error

    return () => { cancelled = true; };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [googleClientId, i18n.language]);

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
            <TextField
              label={t("auth.password")}
              type={showPassword ? "text" : "password"}
              fullWidth
              autoComplete="current-password"
              {...password.fieldProps}
              slotProps={{
                input: {
                  endAdornment: (
                    <IconButton size="small" onClick={() => setShowPassword((v) => !v)} edge="end" tabIndex={-1}>
                      {showPassword ? <VisibilityOffIcon fontSize="small" /> : <VisibilityIcon fontSize="small" />}
                    </IconButton>
                  ),
                },
              }}
            />
            <MuiLink component={RouterLink} to="/forgot-password" variant="body2" sx={{ alignSelf: "flex-end" }}>
              {t("auth.forgotPassword")}
            </MuiLink>
            <Button type="submit" variant="contained" size="large" disabled={loading}>{t("auth.loginCta")}</Button>
            {/* Absent entirely (no divider, no reserved space, no script fetched) until the
                backend confirms Google sign-in is actually configured and enabled — see
                GoogleAuthOptions on the backend. */}
            {googleClientId && (
              <>
                <Divider>{t("auth.orContinueWith")}</Divider>
                <Box ref={googleButtonRef} sx={{ display: "flex", justifyContent: "center" }} />
              </>
            )}
            <Stack direction="row" justifyContent="space-between" alignItems="center">
              <Typography variant="body2">
                {t("auth.noAccount")} <MuiLink component={RouterLink} to="/register">{t("auth.register")}</MuiLink>
              </Typography>
              <Select
                size="small"
                value={i18n.language.startsWith("ar") ? "ar" : "en"}
                onChange={(e) => i18n.changeLanguage(e.target.value)}
              >
                <MenuItem value="ar">العربية</MenuItem>
                <MenuItem value="en">English</MenuItem>
              </Select>
            </Stack>
          </Stack>
        </Box>
        <Typography variant="caption" color="text.secondary" align="center" sx={{ display: "block", mt: 2 }}>
          <MuiLink
            href={i18n.language.startsWith("ar") ? "/privacy/ar.html" : "/privacy/en.html"}
            target="_blank"
            rel="noopener"
          >
            {t("auth.privacyPolicy")}
          </MuiLink>
        </Typography>
      </Paper>
    </Box>
  );
}
