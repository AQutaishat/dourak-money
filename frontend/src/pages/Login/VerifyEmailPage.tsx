import { useEffect, useState } from "react";
import { Box, Paper, Typography, Alert, Stack, Button, CircularProgress } from "@mui/material";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { authApi } from "../../api/auth";
import { useAuth } from "../../auth/AuthContext";
import dourakLogo from "../../assets/dourak-logo.png";

/** Landed on from the link in the verification email — userId/token come from the URL. */
export function VerifyEmailPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { isAuthenticated, refreshProfile } = useAuth();
  const [params] = useSearchParams();
  const [status, setStatus] = useState<"loading" | "success" | "error">("loading");

  useEffect(() => {
    const userId = params.get("userId");
    const token = params.get("token");
    if (!userId || !token) {
      setStatus("error");
      return;
    }
    authApi.verifyEmail({ userId, token })
      .then(async () => {
        setStatus("success");
        // If the signed-in account is the one just verified, drop the unverified banner/badge
        // immediately instead of waiting for the next natural profile refresh.
        if (isAuthenticated) await refreshProfile();
      })
      .catch(() => setStatus("error"));
    // Runs once on mount — re-running on every refreshProfile identity change would re-verify pointlessly.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [params]);

  return (
    <Box sx={{ minHeight: "100vh", display: "flex", alignItems: "center", justifyContent: "center", bgcolor: "background.default", p: 2 }}>
      <Paper sx={{ p: 4, width: 360, maxWidth: "100%" }}>
        <Box sx={{ display: "flex", justifyContent: "center", mb: 2 }}>
          <Box component="img" src={dourakLogo} alt="" width={64} height={64} sx={{ borderRadius: 2 }} />
        </Box>
        <Stack spacing={2} alignItems="center" textAlign="center">
          {status === "loading" && (
            <>
              <CircularProgress size={32} />
              <Typography variant="body2">{t("auth.verifyEmailTitle")}</Typography>
            </>
          )}
          {status === "success" && (
            <>
              <Alert severity="success" sx={{ width: "100%" }}>{t("auth.verifyEmailSuccess")}</Alert>
              <Button variant="contained" onClick={() => navigate(isAuthenticated ? "/" : "/login")}>
                {isAuthenticated ? t("nav.dashboard") : t("auth.backToLogin")}
              </Button>
            </>
          )}
          {status === "error" && (
            <>
              <Alert severity="error" sx={{ width: "100%" }}>{t("auth.verifyEmailFailed")}</Alert>
              <Button variant="outlined" onClick={() => navigate(isAuthenticated ? "/" : "/login")}>
                {isAuthenticated ? t("nav.dashboard") : t("auth.backToLogin")}
              </Button>
            </>
          )}
        </Stack>
      </Paper>
    </Box>
  );
}
