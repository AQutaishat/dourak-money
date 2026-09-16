import { useState } from "react";
import { Paper, Stack, Typography, Button, IconButton } from "@mui/material";
import CloseIcon from "@mui/icons-material/Close";
import EmailOutlinedIcon from "@mui/icons-material/EmailOutlined";
import { useTranslation } from "react-i18next";
import { authApi } from "../api/auth";

/**
 * A light, non-blocking nudge for an unverified email — deliberately NOT a modal/dialog:
 * all actions stay allowed while unverified (per spec), this is encouragement, not a gate.
 * Sits fixed in the corner (inline-end side, so it mirrors correctly under RTL) so it never
 * competes with page content or steals focus. Dismissing hides it for this browser session
 * only — reloading or a fresh login shows it again as long as the email stays unverified.
 */
export function UnverifiedEmailBanner() {
  const { t } = useTranslation();
  const [dismissed, setDismissed] = useState(false);
  const [sent, setSent] = useState(false);
  const [sending, setSending] = useState(false);

  if (dismissed) return null;

  const handleResend = async () => {
    setSending(true);
    try {
      await authApi.sendVerification();
      setSent(true);
    } finally {
      setSending(false);
    }
  };

  return (
    <Paper
      elevation={3}
      sx={{
        position: "fixed",
        bottom: 16,
        insetInlineEnd: 16,
        zIndex: (theme) => theme.zIndex.snackbar,
        p: 2,
        maxWidth: 320,
        borderInlineStart: "4px solid",
        borderColor: "warning.main",
      }}
    >
      <Stack direction="row" spacing={1} alignItems="flex-start">
        <EmailOutlinedIcon color="warning" fontSize="small" sx={{ mt: 0.5 }} />
        <Stack spacing={1} sx={{ flex: 1 }}>
          <Typography variant="body2">
            {sent ? t("auth.verificationSent") : t("auth.unverifiedBannerText")}
          </Typography>
          {!sent && (
            <Button size="small" onClick={handleResend} disabled={sending} sx={{ alignSelf: "flex-start" }}>
              {t("auth.resendVerification")}
            </Button>
          )}
        </Stack>
        <IconButton size="small" onClick={() => setDismissed(true)} aria-label="close">
          <CloseIcon fontSize="small" />
        </IconButton>
      </Stack>
    </Paper>
  );
}
