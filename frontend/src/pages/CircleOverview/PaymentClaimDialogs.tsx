import { useEffect, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  Alert, Box, Button, Dialog, DialogActions, DialogContent, DialogTitle,
  Stack, TextField, Typography, Menu, MenuItem, Link as MuiLink,
} from "@mui/material";
import UploadFileIcon from "@mui/icons-material/UploadFile";
import { useTranslation } from "react-i18next";
import { paymentClaimsApi } from "../../api/circles";
import type { PaymentClaim } from "../../api/types";
import { ClaimStatusChip } from "./StatusChip";

export interface AlternateClaimMonth {
  cycleId: number;
  dueDate: string;
  outstanding: number;
}

const MAX_EVIDENCE_BYTES = 5 * 1024 * 1024;

/**
 * prompt02 §6b: a member reports their own payment with optional image/PDF evidence. This never
 * marks the contribution paid — it creates a claim the organizer must approve.
 */
export function SubmitPaymentClaimDialog({
  open, onClose, circleId, cycleId, outstanding, currency, otherMonths,
}: {
  open: boolean;
  onClose: () => void;
  circleId: number;
  cycleId: number;
  outstanding: number;
  currency: string;
  /** Other months (past unpaid or future not yet fully paid) this member can redirect the claim
   * to instead — same picker the organizer gets when recording a payment directly. */
  otherMonths?: AlternateClaimMonth[];
}) {
  const { t, i18n } = useTranslation();
  const queryClient = useQueryClient();

  const [amount, setAmount] = useState(String(outstanding));
  const [note, setNote] = useState("");
  const [evidence, setEvidence] = useState<File | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [targetCycleId, setTargetCycleId] = useState<number | null>(null);
  const [cyclePickerAnchor, setCyclePickerAnchor] = useState<HTMLElement | null>(null);

  // The current cycle may already be fully paid (nothing left to claim there) while an earlier
  // or later month still has an outstanding balance — default straight to the first such month
  // instead of opening on a dead-end "0 outstanding" current-cycle target.
  useEffect(() => {
    if (!open) return;
    if (outstanding > 0) {
      setTargetCycleId(null);
      setAmount(String(outstanding));
    } else if (otherMonths && otherMonths.length > 0) {
      setTargetCycleId(otherMonths[0].cycleId);
      setAmount(String(otherMonths[0].outstanding));
    } else {
      setTargetCycleId(null);
      setAmount("0");
    }
    setNote(""); setEvidence(null); setError(null);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, cycleId]);

  const targetMonth = targetCycleId != null ? otherMonths?.find((m) => m.cycleId === targetCycleId) : undefined;
  const effectiveCycleId = targetMonth ? targetMonth.cycleId : cycleId;
  const effectiveOutstanding = targetMonth ? targetMonth.outstanding : outstanding;
  const targetCycleLabel = targetMonth
    ? new Date(targetMonth.dueDate).toLocaleDateString(i18n.language, { month: "long", year: "numeric" })
    : t("circle.currentCycle");

  const submit = useMutation({
    mutationFn: () => paymentClaimsApi.submit(effectiveCycleId, { claimedAmount: Number(amount), note: note || undefined, evidence }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["dashboard", circleId] });
      queryClient.invalidateQueries({ queryKey: ["paymentClaims", circleId] });
      queryClient.invalidateQueries({ queryKey: ["myPaymentClaims"] });
      queryClient.invalidateQueries({ queryKey: ["monthsDetail", circleId] });
      setNote(""); setEvidence(null); setError(null); setTargetCycleId(null);
      onClose();
    },
    onError: (err: unknown) => {
      const title = (err as { response?: { data?: { title?: string } } })?.response?.data?.title;
      setError(title ?? t("common.error"));
    },
  });

  const pickFile = (file: File | null) => {
    if (file && file.size > MAX_EVIDENCE_BYTES) {
      setError(t("circle.claimEvidenceHint"));
      return;
    }
    setError(null);
    setEvidence(file);
  };

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>{t("circle.submitClaim")}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {error && <Alert severity="error">{error}</Alert>}
          <TextField
            label={`${t("circle.claimAmount")} (${currency})`}
            type="number"
            value={amount}
            onChange={(e) => { setAmount(e.target.value); setError(null); }}
            onFocus={(e) => (e.target as HTMLInputElement).select()}
            inputProps={{ min: 0.01, max: effectiveOutstanding, step: 0.01 }}
            error={Number(amount) > effectiveOutstanding}
            helperText={
              Number(amount) > effectiveOutstanding
                ? t("circle.recordPaymentExceedsOutstanding", { amount: effectiveOutstanding, currency })
                : undefined
            }
            fullWidth
          />
          {otherMonths && otherMonths.length > 0 && (
            <Box>
              <Typography variant="body2" component="span" color="text.secondary">
                {t("circle.recordPaymentOnLabel")}{" "}
              </Typography>
              <MuiLink component="button" type="button" variant="body2" onClick={(e) => setCyclePickerAnchor(e.currentTarget)}>
                {targetCycleLabel}
              </MuiLink>
              <Menu anchorEl={cyclePickerAnchor} open={!!cyclePickerAnchor} onClose={() => setCyclePickerAnchor(null)}>
                <MenuItem
                  selected={targetCycleId === null}
                  onClick={() => {
                    setTargetCycleId(null);
                    setAmount(String(outstanding));
                    setError(null);
                    setCyclePickerAnchor(null);
                  }}
                >
                  {t("circle.currentCycle")}
                </MenuItem>
                {otherMonths.map((m) => (
                  <MenuItem
                    key={m.cycleId}
                    selected={targetCycleId === m.cycleId}
                    onClick={() => {
                      setTargetCycleId(m.cycleId);
                      setAmount(String(m.outstanding));
                      setError(null);
                      setCyclePickerAnchor(null);
                    }}
                  >
                    {new Date(m.dueDate).toLocaleDateString(i18n.language, { month: "long", year: "numeric" })}
                  </MenuItem>
                ))}
              </Menu>
            </Box>
          )}
          <TextField label={t("circle.claimNote")} value={note} onChange={(e) => setNote(e.target.value)} fullWidth multiline rows={2} />

          <Box>
            <Button component="label" variant="outlined" startIcon={<UploadFileIcon />} fullWidth>
              {t("circle.claimEvidence")}
              <input
                hidden
                type="file"
                accept="image/*,application/pdf"
                onChange={(e) => pickFile(e.target.files?.[0] ?? null)}
              />
            </Button>
            <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 0.5 }}>
              {evidence ? evidence.name : t("circle.claimEvidenceHint")}
            </Typography>
          </Box>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t("common.cancel")}</Button>
        <Button
          variant="contained"
          disabled={submit.isPending || !(Number(amount) > 0) || Number(amount) > effectiveOutstanding}
          onClick={() => submit.mutate()}
        >
          {t("common.submit")}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

/**
 * A single claim's detail — opened from the "دفعة معلقة"/"دفعة مقبولة" badge next to a member's
 * row in the Current Cycle payments table (replaces the old organizer-wide inbox dialog).
 * Approve/reject only show while the claim is still Pending and the viewer can review it;
 * otherwise it's a read-only view of the same details (and evidence, if attached).
 */
export function ClaimDetailDialog({
  open, onClose, claim, canReview,
}: {
  open: boolean;
  onClose: () => void;
  claim: PaymentClaim | undefined;
  canReview: boolean;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [rejecting, setRejecting] = useState(false);
  const [reason, setReason] = useState("");

  const review = useMutation({
    mutationFn: ({ approve, rejectionReason }: { approve: boolean; rejectionReason?: string }) =>
      paymentClaimsApi.review(claim!.id, approve, rejectionReason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["paymentClaims", claim!.circleId] });
      queryClient.invalidateQueries({ queryKey: ["dashboard", claim!.circleId] });
      // The Monthly Cycles tab renders this same claim's badge from its own query — without this,
      // approving/rejecting from there left the badge showing the stale pre-review status.
      queryClient.invalidateQueries({ queryKey: ["monthsDetail", claim!.circleId] });
      setRejecting(false);
      setReason("");
      onClose();
    },
  });

  if (!claim) return null;

  const canAct = canReview && claim.status === "Pending";

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{t("circle.paymentClaims")}</DialogTitle>
      <DialogContent>
        <ClaimRow claim={claim}>
          {canAct && (
            rejecting ? (
              <Stack spacing={1} sx={{ mt: 1 }}>
                <TextField
                  size="small" label={t("circle.rejectReason")} value={reason}
                  onChange={(e) => setReason(e.target.value)} fullWidth multiline rows={2}
                />
                <Stack direction="row" spacing={1}>
                  <Button size="small" onClick={() => { setRejecting(false); setReason(""); }}>{t("common.cancel")}</Button>
                  <Button
                    size="small" color="error" variant="contained" disabled={review.isPending}
                    onClick={() => review.mutate({ approve: false, rejectionReason: reason || undefined })}
                  >
                    {t("circle.reject")}
                  </Button>
                </Stack>
              </Stack>
            ) : (
              <Stack direction="row" spacing={1} sx={{ mt: 1 }}>
                <Button
                  size="small" variant="contained" color="success" disabled={review.isPending}
                  onClick={() => review.mutate({ approve: true })}
                >
                  {t("circle.approve")}
                </Button>
                <Button size="small" color="error" onClick={() => setRejecting(true)}>{t("circle.reject")}</Button>
              </Stack>
            )
          )}
        </ClaimRow>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t("common.close")}</Button>
      </DialogActions>
    </Dialog>
  );
}

/** Shared row rendering, including the token-authenticated evidence download. */
export function ClaimRow({ claim, children }: { claim: PaymentClaim; children?: React.ReactNode }) {
  const { t, i18n } = useTranslation();

  const openEvidence = async () => {
    // The evidence endpoint requires the bearer token, so it can't be a plain <a href>.
    const url = await paymentClaimsApi.evidenceUrl(claim.id);
    window.open(url, "_blank", "noopener,noreferrer");
  };

  return (
    <Box sx={{ p: 1.5, border: "1px solid", borderColor: "divider", borderRadius: 2 }}>
      <Stack direction="row" justifyContent="space-between" alignItems="center" spacing={1}>
        <Typography variant="subtitle2">
          {t("circle.claimSubmittedBy", { name: claim.memberName })}
        </Typography>
        <ClaimStatusChip status={claim.status} />
      </Stack>
      <Typography variant="body2" color="text.secondary">
        {claim.claimedAmount} / {claim.expectedAmount} {claim.currency} ·{" "}
        {new Date(claim.dueDate).toLocaleDateString(i18n.language, { month: "long", year: "numeric" })}
      </Typography>
      {claim.note && <Typography variant="body2" sx={{ mt: 0.5 }}>{claim.note}</Typography>}
      {claim.status === "Rejected" && claim.rejectionReason && (
        <Alert severity="warning" sx={{ mt: 1 }}>
          {t("circle.claimRejectedReason", { reason: claim.rejectionReason })}
        </Alert>
      )}
      {claim.hasEvidence && (
        <Button size="small" sx={{ mt: 0.5 }} onClick={openEvidence}>{t("circle.viewEvidence")}</Button>
      )}
      {children}
    </Box>
  );
}

/**
 * The submitting member's own view of their still-Pending claim — opened from the clickable
 * "بانتظار الموافقة" chip in the payments table. Unlike the read-only ClaimDetailDialog, this
 * lets them correct the amount/note/evidence, or withdraw ("unsend") the claim outright.
 */
export function MyClaimDialog({
  open, onClose, claim, outstanding, currency,
}: {
  open: boolean;
  onClose: () => void;
  claim: PaymentClaim | undefined;
  outstanding: number;
  currency: string;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const [amount, setAmount] = useState("");
  const [note, setNote] = useState("");
  const [evidence, setEvidence] = useState<File | null>(null);
  const [removeEvidence, setRemoveEvidence] = useState(false);
  const [confirmingWithdraw, setConfirmingWithdraw] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Deliberately keyed on `open`/`claim?.id`, not the `claim` object itself: react-query hands
  // back a fresh object on every background refetch (e.g. the window-focus refetch that fires
  // right as the native file picker closes), and resetting on that would silently wipe out a
  // file the user had just picked, or their in-progress edits, before they got to Save.
  useEffect(() => {
    if (open && claim) {
      setAmount(String(claim.claimedAmount));
      setNote(claim.note ?? "");
      setEvidence(null);
      setRemoveEvidence(false);
      setConfirmingWithdraw(false);
      setError(null);
    }
  }, [open, claim?.id]);

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["myPaymentClaims"] });
    queryClient.invalidateQueries({ queryKey: ["dashboard", claim?.circleId] });
    queryClient.invalidateQueries({ queryKey: ["monthsDetail", claim?.circleId] });
  };

  const update = useMutation({
    mutationFn: () => paymentClaimsApi.update(claim!.id, {
      claimedAmount: Number(amount), note: note || undefined, removeEvidence, evidence,
    }),
    onSuccess: () => { invalidate(); onClose(); },
    onError: (err: unknown) => {
      const title = (err as { response?: { data?: { title?: string } } })?.response?.data?.title;
      setError(title ?? t("common.error"));
    },
  });

  const withdraw = useMutation({
    mutationFn: () => paymentClaimsApi.withdraw(claim!.id),
    onSuccess: () => { invalidate(); onClose(); },
    onError: (err: unknown) => {
      const title = (err as { response?: { data?: { title?: string } } })?.response?.data?.title;
      setError(title ?? t("common.error"));
      setConfirmingWithdraw(false);
    },
  });

  if (!claim) return null;

  const pickFile = (file: File | null) => {
    if (file && file.size > MAX_EVIDENCE_BYTES) {
      setError(t("circle.claimEvidenceHint"));
      return;
    }
    setError(null);
    setEvidence(file);
    setRemoveEvidence(false);
  };

  const evidenceLabel = evidence ? evidence.name
    : claim.hasEvidence && !removeEvidence ? claim.evidenceFileName ?? t("circle.viewEvidence")
    : t("circle.claimEvidenceHint");

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="xs">
      <DialogTitle>{t("circle.submitClaim")}</DialogTitle>
      <DialogContent>
        <Stack spacing={2} sx={{ mt: 1 }}>
          {error && <Alert severity="error">{error}</Alert>}
          <TextField
            label={`${t("circle.claimAmount")} (${currency})`}
            type="number"
            value={amount}
            onChange={(e) => { setAmount(e.target.value); setError(null); }}
            onFocus={(e) => (e.target as HTMLInputElement).select()}
            inputProps={{ min: 0.01, max: outstanding, step: 0.01 }}
            error={Number(amount) > outstanding}
            helperText={Number(amount) > outstanding ? t("circle.recordPaymentExceedsOutstanding", { amount: outstanding, currency }) : undefined}
            fullWidth
          />
          <TextField label={t("circle.claimNote")} value={note} onChange={(e) => setNote(e.target.value)} fullWidth multiline rows={2} />

          <Box>
            <Button component="label" variant="outlined" startIcon={<UploadFileIcon />} fullWidth>
              {t("circle.claimEvidence")}
              <input
                hidden
                type="file"
                accept="image/*,application/pdf"
                onChange={(e) => pickFile(e.target.files?.[0] ?? null)}
              />
            </Button>
            <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 0.5 }}>
              {evidenceLabel}
            </Typography>
            {claim.hasEvidence && !evidence && !removeEvidence && (
              <Button size="small" color="error" onClick={() => setRemoveEvidence(true)}>{t("common.delete")}</Button>
            )}
          </Box>

          {confirmingWithdraw ? (
            <Alert severity="warning">
              {t("circle.withdrawClaimConfirm")}
              <Stack direction="row" spacing={1} sx={{ mt: 1 }}>
                <Button size="small" onClick={() => setConfirmingWithdraw(false)}>{t("common.cancel")}</Button>
                <Button size="small" color="error" variant="contained" disabled={withdraw.isPending} onClick={() => withdraw.mutate()}>
                  {t("circle.withdrawClaim")}
                </Button>
              </Stack>
            </Alert>
          ) : (
            <Button size="small" color="error" onClick={() => setConfirmingWithdraw(true)}>{t("circle.withdrawClaim")}</Button>
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t("common.cancel")}</Button>
        <Button
          variant="contained"
          disabled={update.isPending || !(Number(amount) > 0) || Number(amount) > outstanding}
          onClick={() => update.mutate()}
        >
          {t("common.save")}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
