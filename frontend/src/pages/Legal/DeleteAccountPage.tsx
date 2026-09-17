import { Box, Paper, Typography, Stack, Link as MuiLink } from "@mui/material";
import dourakLogo from "../../assets/dourak-logo.png";

/**
 * Public, unauthenticated page required by the Google Play "Data safety" section: it must be
 * reachable without signing in, name the app, explain the deletion request steps, and state
 * what is deleted vs retained. There is no self-service delete-account flow yet (docs/mobile-gap.md
 * style tracking isn't relevant here since this is web+policy, not a mobile parity gap) — deletion
 * is handled manually by support until that flow is built.
 */
export function DeleteAccountPage() {
  return (
    <Box sx={{ minHeight: "100vh", display: "flex", justifyContent: "center", bgcolor: "background.default", p: 2, py: 6 }}>
      <Paper sx={{ p: 4, width: 640, maxWidth: "100%" }}>
        <Box sx={{ display: "flex", justifyContent: "center", mb: 2 }}>
          <Box component="img" src={dourakLogo} alt="" width={64} height={64} sx={{ borderRadius: 2 }} />
        </Box>
        <Typography variant="h5" fontWeight={700} align="center" gutterBottom>
          Delete your Dourak account
        </Typography>
        <Stack spacing={3} sx={{ mt: 3 }}>
          <Box>
            <Typography variant="h6" fontWeight={600} gutterBottom>How to request deletion</Typography>
            <Typography variant="body1" component="ol" sx={{ pl: 2.5, m: 0 }}>
              <li>Send an email to <MuiLink href="mailto:anass.shaddad@gmail.com">anass.shaddad@gmail.com</MuiLink> from the email address registered on your Dourak account.</li>
              <li>Use the subject line "Delete my account" and include the email address on your account if different from the sending address.</li>
              <li>We will verify the request and confirm by email once your account has been deleted, normally within 7 business days.</li>
            </Typography>
          </Box>
          <Box>
            <Typography variant="h6" fontWeight={600} gutterBottom>What gets deleted</Typography>
            <Typography variant="body1" component="ul" sx={{ pl: 2.5, m: 0 }}>
              <li>Your name, email address, phone number, and password.</li>
              <li>Your profile and login credentials, permanently, within 30 days of confirming the request.</li>
            </Typography>
          </Box>
          <Box>
            <Typography variant="h6" fontWeight={600} gutterBottom>What may be retained</Typography>
            <Typography variant="body1" component="ul" sx={{ pl: 2.5, m: 0 }}>
              <li>
                Contribution and payout records tied to savings circles you shared with other members are retained for
                up to 12 months so those circles' shared payment history stays accurate for the other participants,
                then anonymized (your name is replaced with "Former member").
              </li>
              <li>Records we are legally required to keep for accounting or fraud-prevention purposes, for as long as the law requires.</li>
            </Typography>
          </Box>
        </Stack>
      </Paper>
    </Box>
  );
}
