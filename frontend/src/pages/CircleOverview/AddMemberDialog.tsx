import { useEffect, useState } from "react";
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
 * prompt02 §2 + prompt03 §2 in one flow:
 *  - one textbox with autocomplete-style live search over registered users (name/email/phone),
 *  - plus a single "invite via WhatsApp" button for someone who isn't on Dourak yet. Per
 *    prompt03 §2 this is a pure share action — no name/phone fields, no member record created.
 *    Once that person registers, the organizer adds them the normal way, above.
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

  /**
   * prompt03 §2: sending the WhatsApp invite is a pure share action — it must not create any
   * member/invitation record at all. No name/phone fields, no backend call whatsoever. Once
   * the invited person registers on their own, the organizer adds them the normal way, above,
   * via the user-search flow; there is no link tracked between this button and that later signup.
   */
  const inviteByWhatsApp = () => {
    shareToWhatsApp(
      buildInviteToRegisterText({
        personName: null,
        circleName,
        organizerName,
        appUrl: currentAppUrl(),
        isArabic,
      }),
      null,
    );
  };

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
            <Typography variant="caption" color="text.secondary" display="block" sx={{ mb: 1 }}>
              {t("circle.inviteSentNote")}
            </Typography>
            <Button variant="outlined" startIcon={<WhatsAppIcon />} onClick={inviteByWhatsApp}>
              {t("circle.inviteByWhatsApp")}
            </Button>
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
