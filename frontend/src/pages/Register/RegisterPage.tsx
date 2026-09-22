import { useEffect, useRef, useState } from "react";
import { Box, Button, Paper, TextField, Typography, Alert, Stack, Link as MuiLink, Divider } from "@mui/material";
import { Link as RouterLink, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { useAuth } from "../../auth/AuthContext";
import { useValidatedField } from "../../components/ValidatedTextField";
import { authApi } from "../../api/auth";

// Same Google Identity Services ambient shape as LoginPage.tsx — see its comment for why
// there's no official types package used here.
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

/**
 * prompt02 §7 / §Register screen: only email and password. Name and phone are set later by the
 * user from their profile page. Errors use the shared on-blur pattern instead of a popup.
 *
 * The Google button here uses the same googleLogin as the login page (find-or-create by Google
 * account), so it doubles as "sign up with Google" without a separate registration call.
 */
export function RegisterPage() {
  const { t, i18n } = useTranslation();
  const { register, googleLogin } = useAuth();
  const navigate = useNavigate();
  const googleButtonRef = useRef<HTMLDivElement>(null);
  const [googleClientId, setGoogleClientId] = useState<string | null | undefined>(undefined);

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

  useEffect(() => {
    authApi.config()
      .then((config) => setGoogleClientId(config.googleSignInEnabled ? config.googleClientId : null))
      .catch(() => setGoogleClientId(null)); // config lookup failing must never block plain registration
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
              setError(err instanceof Error ? err.message : t("common.error"));
            }
          },
        });
        window.google.accounts.id.renderButton(googleButtonRef.current, {
          type: "standard", theme: "outline", size: "large", width: 296,
          text: "signup_with", locale: i18n.language.startsWith("ar") ? "ar" : "en",
        });
      })
      .catch(() => setGoogleClientId(null)); // script failed to load — fail quiet, not a registration error

    return () => { cancelled = true; };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [googleClientId, i18n.language]);

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
            {/* Absent entirely (no divider, no reserved space, no script fetched) until the
                backend confirms Google sign-in is actually configured and enabled — mirrors
                LoginPage.tsx. */}
            {googleClientId && (
              <>
                <Divider>{t("auth.orContinueWith")}</Divider>
                <Box ref={googleButtonRef} sx={{ display: "flex", justifyContent: "center" }} />
              </>
            )}
            <Typography variant="body2">
              {t("auth.haveAccount")} <MuiLink component={RouterLink} to="/login">{t("auth.login")}</MuiLink>
            </Typography>
          </Stack>
        </Box>
      </Paper>
    </Box>
  );
}
