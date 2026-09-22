import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Alert, Box, Button, CircularProgress, FormControlLabel, Paper, Stack, Switch,
  TextField, Typography,
} from "@mui/material";
import { adminApi } from "../api/admin";

/**
 * One field per known key in AppSettingKeys.All (backend/src/Dourak.Application/Admin/AppSettingKeys.cs)
 * — add a row here (and to the backend whitelist) to make a new setting editable. The whole form
 * saves in one PUT rather than a request per field.
 */
export function SettingsPage() {
  const queryClient = useQueryClient();
  const { data: settings, isLoading } = useQuery({ queryKey: ["settings"], queryFn: adminApi.settings });

  const [maintenanceMode, setMaintenanceMode] = useState(false);
  const [announcementMessage, setAnnouncementMessage] = useState("");
  const [minSupportedAppVersion, setMinSupportedAppVersion] = useState("");
  const [supportEmail, setSupportEmail] = useState("");
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    if (!settings) return;
    const get = (key: string) => settings.find((s) => s.key === key)?.value ?? "";
    setMaintenanceMode(get("maintenanceMode") === "true");
    setAnnouncementMessage(get("announcementMessage"));
    setMinSupportedAppVersion(get("minSupportedAppVersion"));
    setSupportEmail(get("supportEmail"));
  }, [settings]);

  const save = useMutation({
    mutationFn: () => adminApi.updateSettings({
      maintenanceMode: String(maintenanceMode),
      announcementMessage: announcementMessage.trim() || null,
      minSupportedAppVersion: minSupportedAppVersion.trim() || null,
      supportEmail: supportEmail.trim() || null,
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["settings"] });
      setSaved(true);
      setTimeout(() => setSaved(false), 3000);
    },
  });

  if (isLoading || !settings) return <CircularProgress />;

  return (
    <>
      <Typography variant="h5" fontWeight={700} gutterBottom>Settings</Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        These are served back to the web and mobile apps via <code>GET /api/auth/config</code>,
        which both call once at startup.
      </Typography>
      <Paper sx={{ p: 3, maxWidth: 560 }}>
        <Stack spacing={3}>
          {saved && <Alert severity="success">Saved.</Alert>}

          <Box>
            <FormControlLabel
              control={<Switch checked={maintenanceMode} onChange={(e) => setMaintenanceMode(e.target.checked)} />}
              label="Maintenance mode"
            />
            <Typography variant="caption" color="text.secondary" display="block">
              Apps can show a maintenance banner/block when this is on — not yet enforced by
              either client, this only flips the flag they'd read.
            </Typography>
          </Box>

          <TextField
            label="Announcement message"
            value={announcementMessage}
            onChange={(e) => setAnnouncementMessage(e.target.value)}
            multiline
            minRows={2}
            fullWidth
            helperText="Shown app-wide when non-empty, e.g. a scheduled-maintenance notice. Leave blank to hide it."
          />

          <TextField
            label="Minimum supported app version"
            value={minSupportedAppVersion}
            onChange={(e) => setMinSupportedAppVersion(e.target.value)}
            fullWidth
            placeholder="e.g. 1.0.5"
            helperText="Mobile can use this to prompt a forced update once it checks its own version against this value — not yet enforced client-side."
          />

          <TextField
            label="Support email"
            value={supportEmail}
            onChange={(e) => setSupportEmail(e.target.value)}
            fullWidth
            type="email"
            helperText="Shown to users as the contact address, in place of a hardcoded one."
          />

          <Button variant="contained" onClick={() => save.mutate()} disabled={save.isPending} sx={{ alignSelf: "flex-start" }}>
            Save
          </Button>
        </Stack>
      </Paper>
    </>
  );
}
