import { useEffect, useMemo, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  Alert, Autocomplete, Box, Button, CircularProgress, Dialog, DialogActions, DialogContent,
  DialogTitle, Divider, Stack, TextField, Typography,
} from "@mui/material";
import WhatsAppIcon from "@mui/icons-material/WhatsApp";
import { useTranslation } from "react-i18next";
import { usersApi } from "../../api/auth";
import { circlesApi } from "../../api/circles";
import type { UserSearchResult } from "../../api/types";
import { buildInviteToRegisterText, currentAppUrl, shareToWhatsApp } from "../../utils/whatsapp";

/**
 * prompt02 §2 + §3 in one flow:
 *  - one textbox with autocomplete-style live search over registered users (name/email/phone),
 *  - plus an "invite someone who isn't on Dourak yet" option that shares the app URL on WhatsApp.
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

  // Invite-an-unregistered-person sub-form.
  const [inviteName, setInviteName] = useState("");
  const [invitePhone, setInvitePhone] = useState("");

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
    setTerm(""); setDebounced(""); setSelected(null); setOptions([]);
    setInviteName(""); setInvitePhone(""); setError(null);
  };

  const addUser = useMutation({
    mutationFn: (userId: string) => circlesApi.addUserMember(circleId, userId),
    onSuccess: () => { invalidate(); reset(); onClose(); },
    onError: (err: unknown) => setError(extractMessage(err, t("common.error"))),
  });

  /**
   * The unregistered person is also stored as a plain Phase 1 member record, so the organizer can
   * keep tracking them in this circle straight away; once they register, the organizer can add
   * their account properly. This is the "lightweight invite-to-register" the spec asked for —
   * no deep-link/token system.
   */
  const inviteUnregistered = useMutation({
    mutationFn: () => circlesApi.addMember(circleId, {
      name: inviteName.trim() || invitePhone.trim(),
      phone: invitePhone.trim() || undefined,
    }),
    onSuccess: () => {
      shareToWhatsApp(
        buildInviteToRegisterText({
          personName: inviteName.trim() || null,
          circleName,
          organizerName,
          appUrl: currentAppUrl(),
          isArabic,
        }),
        invitePhone,
      );
      invalidate();
      reset();
      onClose();
    },
    onError: (err: unknown) => setError(extractMessage(err, t("common.error"))),
  });

  const canInvite = useMemo(() => invitePhone.trim().length >= 6, [invitePhone]);

  return (
    <Dialog open={open} onClose={() => { reset(); onClose(); }} fullWidth maxWidth="sm">
      <DialogTitle>{t("circle.addMember")}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {error && <Alert severity="error">{error}</Alert>}

          <Box>
            <Typography variant="subtitle2" gutterBottom>{t("circle.addExistingUser")}</Typography>
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

          <Divider />

          <Box>
            <Typography variant="subtitle2" gutterBottom>{t("circle.inviteUnregistered")}</Typography>
            <Stack spacing={2}>
              <TextField label={t("circle.invitePersonName")} value={inviteName} onChange={(e) => setInviteName(e.target.value)} fullWidth />
              <TextField
                label={t("circle.invitePersonPhone")}
                value={invitePhone}
                onChange={(e) => setInvitePhone(e.target.value)}
                fullWidth
                required
                helperText={t("circle.inviteSentNote")}
              />
              <Button
                variant="outlined"
                startIcon={<WhatsAppIcon />}
                disabled={!canInvite || inviteUnregistered.isPending}
                onClick={() => inviteUnregistered.mutate()}
              >
                {t("circle.inviteByWhatsApp")}
              </Button>
            </Stack>
          </Box>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={() => { reset(); onClose(); }}>{t("common.close")}</Button>
      </DialogActions>
    </Dialog>
  );
}

function extractMessage(err: unknown, fallback: string) {
  const title = (err as { response?: { data?: { title?: string } } })?.response?.data?.title;
  return title ?? fallback;
}
