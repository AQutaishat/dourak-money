import { useEffect, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle, TextField, Stack, Typography,
} from "@mui/material";
import AttachFileIcon from "@mui/icons-material/AttachFile";
import { useTranslation } from "react-i18next";
import { circlesApi, cyclesApi } from "../../api/circles";

/**
 * Rendered inside the Current Cycle tab (only while that tab is open) — fetches its own
 * current-cycle dashboard and renders nothing once the payout has been fully paid. Like a
 * member's own contribution, the payout can be paid in more than one installment: this stays
 * visible and re-openable after a partial payment, capped each time at what's still outstanding.
 */
export function ConfirmPayoutButton({ circleId }: { circleId: number }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { data: dashboard } = useQuery({ queryKey: ["dashboard", circleId], queryFn: () => circlesApi.dashboard(circleId) });

  const [open, setOpen] = useState(false);
  const [payoutAmount, setPayoutAmount] = useState(0);
  const [evidence, setEvidence] = useState<File | null>(null);
  const [error, setError] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const outstanding = dashboard ? dashboard.payoutExpectedAmount - dashboard.payoutActualAmount : 0;

  useEffect(() => {
    if (dashboard) setPayoutAmount(dashboard.payoutExpectedAmount - dashboard.payoutActualAmount);
  }, [dashboard]);

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["dashboard", circleId] });
    queryClient.invalidateQueries({ queryKey: ["schedule", circleId] });
    queryClient.invalidateQueries({ queryKey: ["history", circleId] });
    queryClient.invalidateQueries({ queryKey: ["monthsDetail", circleId] });
  };

  const recordPayout = useMutation({
    mutationFn: () => cyclesApi.recordPayout(dashboard!.cycleId, { actualAmount: payoutAmount, evidence }),
    onSuccess: () => { invalidate(); setOpen(false); setEvidence(null); setError(null); },
    onError: (err: unknown) => {
      const title = (err as { response?: { data?: { title?: string } } })?.response?.data?.title;
      setError(title ?? t("common.error"));
    },
  });

  if (!dashboard || dashboard.payoutStatus !== "Pending") return null;

  return (
    <>
      <Button variant="contained" size="large" onClick={() => { setOpen(true); setError(null); }}>
        {t("circle.confirmPayout")}
      </Button>

      <Dialog open={open} onClose={() => setOpen(false)} fullWidth maxWidth="xs">
        <DialogTitle>{t("circle.confirmPayout")} — {dashboard.recipientName}</DialogTitle>
        <DialogContent>
          {dashboard.membersUnpaid + dashboard.membersLate > 0 && (
            <Alert severity="warning" sx={{ mb: 2 }}>{t("circle.unpaid")}: {dashboard.membersUnpaid + dashboard.membersLate}</Alert>
          )}
          <TextField
            label={t("circle.expectedPool")} type="number" fullWidth
            value={payoutAmount}
            onChange={(e) => { setPayoutAmount(Number(e.target.value)); setError(null); }}
            inputProps={{ min: 0, max: outstanding, step: 0.01 }}
            error={!!error || payoutAmount > outstanding}
            helperText={error ?? (payoutAmount > outstanding ? t("circle.recordPaymentExceedsOutstanding", { amount: outstanding, currency: "" }) : undefined)}
          />
          <Stack direction="row" spacing={1} alignItems="center" sx={{ mt: 2 }}>
            <Button size="small" startIcon={<AttachFileIcon />} onClick={() => fileInputRef.current?.click()}>
              {t("circle.claimEvidence")}
            </Button>
            {evidence && <Typography variant="body2" color="text.secondary" noWrap>{evidence.name}</Typography>}
            <input
              ref={fileInputRef} type="file" hidden accept="image/*,application/pdf"
              onChange={(e) => setEvidence(e.target.files?.[0] ?? null)}
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>{t("common.cancel")}</Button>
          <Button
            variant="contained"
            disabled={recordPayout.isPending || payoutAmount <= 0 || payoutAmount > outstanding}
            onClick={() => recordPayout.mutate()}
          >
            {t("common.confirm")}
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}
