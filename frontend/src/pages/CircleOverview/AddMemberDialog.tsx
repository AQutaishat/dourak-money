import { useEffect, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  Alert, Autocomplete, Box, Button, CircularProgress, Dialog, DialogActions, DialogContent,
  DialogTitle, Stack, TextField, Typography,
} from "@mui/material";
import WhatsAppIcon from "@mui/icons-material/WhatsApp";
import { useTranslation } from "react-i18next";
import { usersApi } from "../../api/auth";
import { circlesApi } from "../../api/circles";
import type { UserSearchResult } from "../../api/types";
import { buildInviteToRegisterText, currentAppUrl, shareToWhatsApp } from "../../utils/whatsapp";

/**
 * prompt02 §2 + the WhatsApp-invite-with-token flow, in one dialog:
 *  - one textbox with autocomplete-style live search over registered users (name/email/phone),
 *  - plus an "invite via WhatsApp" button, right next to Add, for someone who isn't on Dourak
 *    yet. This one DOES create a member row (Pending, no UserId yet, carrying a one-time
 *    invite token) so they show up in the members table right away as "لم يقبل بعد" — once the
 *    invitee registers/logs in and opens the link, their account is linked to that exact row
 *    (`InvitePage` + `PendingInvitationsSection`) and they see the invitation to accept/decline.
 */
export function AddMemberDialog({
  open, onClose, circleId, circleName, organizerName,
}: {
  open: boolean;
  onClose: () => void;
  circleId: number;
  circleName: string;
  organizerName: string;
}) {
  const { t, i18n } = useTranslation();
  const queryClient = useQueryClient();
  const isArabic = i18n.language.startsWith("ar");

  const [term, setTerm] = useState("");
  const [debounced, setDebounced] = useState("");
  const [options, setOptions] = useState<UserSearchResult[]>([]);
  const [searching, setSearching] = useState(false);
  const [selected, setSelected] = useState<UserSearchResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [whatsAppNameOpen, setWhatsAppNameOpen] = useState(false);
  const [whatsAppName, setWhatsAppName] = useState("");

  // Debounced so every keystroke doesn't hit the API while the organizer is still typing.
  useEffect(() => {
    const id = setTimeout(() => setDebounced(term.trim()), 300);
    return () => clearTimeout(id);
  }, [term]);

  useEffect(() => {
    let cancelled = false;
    if (debounced.length < 2) {
      setOptions([]);
      return;
    }
    setSearching(true);
    usersApi.search(debounced)
      .then((results) => { if (!cancelled) setOptions(results); })
      .catch(() => { if (!cancelled) setOptions([]); })
      .finally(() => { if (!cancelled) setSearching(false); });
    return () => { cancelled = true; };
  }, [debounced]);

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["members", circleId] });
    queryClient.invalidateQueries({ queryKey: ["circle", circleId] });
  };

  const reset = () => {
    setTerm(""); setDebounced(""); setSelected(null); setOptions([]); setError(null);
  };

  const addUser = useMutation({
    mutationFn: (userId: string) => circlesApi.addUserMember(circleId, userId),
    onSuccess: () => { invalidate(); reset(); onClose(); },
    onError: (err: unknown) => setError(extractMessage(err, t("common.error"))),
  });

  const inviteUnregistered = useMutation({
    mutationFn: (name: string) => circlesApi.inviteUnregisteredMember(circleId, name),
    onSuccess: ({ token }) => {
      shareToWhatsApp(
        buildInviteToRegisterText({
          personName: whatsAppName.trim(),
          circleName,
          organizerName,
          appUrl: `${currentAppUrl()}/invite/${token}`,
          isArabic,
        }),
        null,
      );
      invalidate();
      setWhatsAppNameOpen(false);
      setWhatsAppName("");
      reset();
      onClose();
    },
    onError: (err: unknown) => setError(extractMessage(err, t("common.error"))),
  });

  return (
    <Dialog open={open} onClose={() => { reset(); onClose(); }} fullWidth maxWidth="sm">
      <DialogTitle>{t("circle.addMember")}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {error && <Alert severity="error">{error}</Alert>}

          <Box>
            <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 1 }}>
              <Typography variant="subtitle2">{t("circle.addExistingUser")}</Typography>
              <Button variant="outlined" size="small" startIcon={<WhatsAppIcon />} onClick={() => setWhatsAppNameOpen(true)}>
                {t("circle.inviteByWhatsApp")}
              </Button>
            </Stack>
            <Autocomplete
              options={options}
              value={selected}
              onChange={(_, value) => setSelected(value)}
              inputValue={term}
              onInputChange={(_, value) => setTerm(value)}
              getOptionLabel={(option) => option.displayLabel}
              isOptionEqualToValue={(a, b) => a.userId === b.userId}
              loading={searching}
              // The API already filters; don't let MUI re-filter and hide phone/email matches.
              filterOptions={(x) => x}
              noOptionsText={debounced.length < 2 ? t("circle.searchUsersHint") : t("circle.noUsersFound")}
              renderOption={(props, option) => (
                <li {...props} key={option.userId}>
                  <Box>
                    <Typography variant="body2">{option.displayLabel}</Typography>
                    <Typography variant="caption" color="text.secondary">
                      {[option.email, option.phone].filter(Boolean).join(" · ")}
                    </Typography>
                  </Box>
                </li>
              )}
              renderInput={(params) => (
                <TextField
                  {...params}
                  label={t("circle.searchUsers")}
                  helperText={t("circle.searchUsersHint")}
                  slotProps={{
                    input: {
                      ...params.InputProps,
                      endAdornment: (
                        <>
                          {searching ? <CircularProgress size={18} /> : null}
                          {params.InputProps.endAdornment}
                        </>
                      ),
                    },
                  }}
                />
              )}
            />
            <Button
              variant="contained"
              sx={{ mt: 1 }}
              disabled={!selected || addUser.isPending}
              onClick={() => selected && addUser.mutate(selected.userId)}
            >
              {t("common.add")}
            </Button>
          </Box>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={() => { reset(); onClose(); }}>{t("common.close")}</Button>
      </DialogActions>

      <Dialog open={whatsAppNameOpen} onClose={() => setWhatsAppNameOpen(false)} fullWidth maxWidth="xs">
        <DialogTitle>{t("circle.inviteByWhatsApp")}</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            {t("circle.inviteUnregisteredNameHint")}
          </Typography>
          <TextField
            autoFocus
            fullWidth
            label={t("circle.memberName")}
            value={whatsAppName}
            onChange={(e) => setWhatsAppName(e.target.value)}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setWhatsAppNameOpen(false)}>{t("common.cancel")}</Button>
          <Button
            variant="contained"
            disabled={!whatsAppName.trim() || inviteUnregistered.isPending}
            onClick={() => inviteUnregistered.mutate(whatsAppName.trim())}
          >
            {t("circle.inviteByWhatsApp")}
          </Button>
        </DialogActions>
      </Dialog>
    </Dialog>
  );
}

function extractMessage(err: unknown, fallback: string) {
  const title = (err as { response?: { data?: { title?: string } } })?.response?.data?.title;
  return title ?? fallback;
}
