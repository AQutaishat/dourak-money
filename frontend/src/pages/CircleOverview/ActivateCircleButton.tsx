import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  Alert, Button, Dialog, DialogActions, DialogContent, DialogContentText, DialogTitle,
} from "@mui/material";
import PlayArrowIcon from "@mui/icons-material/PlayArrow";
import { useTranslation } from "react-i18next";
import { circlesApi } from "../../api/circles";

/**
 * prompt02 §Draft circles: the Activate action lives inside the payout-order tab *and* is always
 * visible beneath all tabs. Sharing one component keeps the two placements — and especially the
 * member-order confirmation message — identical rather than two dialogs that could drift.
 */
export function ActivateCircleButton({
  circleId, disabled, fullWidth, onActivated,
}: {
  circleId: number;
  disabled?: boolean;
  fullWidth?: boolean;
  onActivated: () => void;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [open, setOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const activate = useMutation({
    mutationFn: () => circlesApi.activate(circleId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["circle", circleId] });
      queryClient.invalidateQueries({ queryKey: ["circles"] });
      setOpen(false);
      onActivated();
    },
    onError: (err: unknown) => {
      const title = (err as { response?: { data?: { title?: string } } })?.response?.data?.title;
      setError(title ?? t("common.error"));
    },
  });

  return (
    <>
      <Button
        variant="contained"
        size="large"
        fullWidth={fullWidth}
        startIcon={<PlayArrowIcon />}
        disabled={disabled}
        onClick={() => { setError(null); setOpen(true); }}
      >
        {t("circle.activate")}
      </Button>

      <Dialog open={open} onClose={() => setOpen(false)} fullWidth maxWidth="xs">
        <DialogTitle>{t("circle.activateConfirmTitle")}</DialogTitle>
        <DialogContent>
          {/* Specifically asks about the member order, not a generic "are you sure". */}
          <DialogContentText>{t("circle.activateConfirmMessage")}</DialogContentText>
          {error && <Alert severity="error" sx={{ mt: 2 }}>{error}</Alert>}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>{t("common.cancel")}</Button>
          <Button variant="contained" disabled={activate.isPending} onClick={() => activate.mutate()}>
            {t("common.confirm")}
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
