import { useRef, useState } from "react";
import { Box, Button, Paper, TextField, Typography, Alert, Stack, Link as MuiLink } from "@mui/material";
import AttachFileIcon from "@mui/icons-material/AttachFile";
import { Link as RouterLink } from "react-router-dom";
import { useTranslation } from "react-i18next";
import axios from "axios";
import { supportApi } from "../../api/support";
import { useValidatedField } from "../../components/ValidatedTextField";
import dourakLogo from "../../assets/dourak-logo.png";

const MAX_ATTACHMENT_BYTES = 5 * 1024 * 1024;

/** Public — no sign-in required, so someone locked out of their account can still reach us. */
export function SupportPage() {
  const { t } = useTranslation();
  const email = useValidatedField("", ["required", "email"]);
  const [name, setName] = useState("");
  const [message, setMessage] = useState("");
  const [attachment, setAttachment] = useState<File | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [submitted, setSubmitted] = useState(false);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0] ?? null;
    setError(null);
    if (file && file.size > MAX_ATTACHMENT_BYTES) {
      setError(t("support.attachmentTooLarge"));
      e.target.value = "";
      setAttachment(null);
      return;
    }
    setAttachment(file);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    const emailOk = email.validateNow();
    if (!emailOk || !message.trim()) {
      if (!message.trim()) setError(t("support.messageRequired"));
      return;
    }

    setLoading(true);
    try {
      await supportApi.submit({ name: name.trim() || undefined, email: email.value.trim(), message: message.trim(), attachment });
      setSubmitted(true);
    } catch (err) {
      setError(axios.isAxiosError(err) ? err.response?.data?.title ?? t("support.submitError") : t("support.submitError"));
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box sx={{ minHeight: "100vh", display: "flex", alignItems: "center", justifyContent: "center", bgcolor: "background.default", p: 2 }}>
      <Paper sx={{ p: 4, width: 420, maxWidth: "100%" }}>
        <Box sx={{ display: "flex", justifyContent: "center", mb: 2 }}>
          <Box component="img" src={dourakLogo} alt="" width={64} height={64} sx={{ borderRadius: 2 }} />
        </Box>
        {submitted ? (
          <Stack spacing={2} alignItems="center" textAlign="center">
            <Typography variant="h6" fontWeight={700}>{t("support.sentTitle")}</Typography>
            <Alert severity="success" sx={{ width: "100%" }}>{t("support.sentHint")}</Alert>
            <MuiLink component={RouterLink} to="/login">{t("auth.backToLogin")}</MuiLink>
          </Stack>
        ) : (
          <Box component="form" onSubmit={handleSubmit} noValidate>
            <Typography variant="h6" fontWeight={700} align="center" gutterBottom>{t("support.title")}</Typography>
            <Typography variant="body2" color="text.secondary" align="center" gutterBottom>{t("support.hint")}</Typography>
            <Stack spacing={2} sx={{ mt: 2 }}>
              {error && <Alert severity="error">{error}</Alert>}
              <TextField label={t("support.name")} fullWidth value={name} onChange={(e) => setName(e.target.value)} />
              <TextField label={t("auth.email")} type="email" fullWidth autoComplete="email" required {...email.fieldProps} />
              <TextField
                label={t("support.message")}
                fullWidth
                required
                multiline
                minRows={4}
                value={message}
                onChange={(e) => setMessage(e.target.value)}
              />
              <Box>
                <Button
                  variant="outlined"
                  startIcon={<AttachFileIcon />}
                  onClick={() => fileInputRef.current?.click()}
                  fullWidth
                >
                  {attachment ? attachment.name : t("support.attach")}
                </Button>
                <input
                  ref={fileInputRef}
                  type="file"
                  accept="image/*,application/pdf"
                  hidden
                  onChange={handleFileChange}
                />
              </Box>
              <Button type="submit" variant="contained" size="large" disabled={loading}>{t("support.submit")}</Button>
              <MuiLink component={RouterLink} to="/login" textAlign="center">{t("auth.backToLogin")}</MuiLink>
            </Stack>
          </Box>
        )}
      </Paper>
    </Box>
  );
}
