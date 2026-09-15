import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Alert, Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, Divider,
  Stack, TextField, Typography,
} from "@mui/material";
import UploadFileIcon from "@mui/icons-material/UploadFile";
import { useTranslation } from "react-i18next";
import { circlesApi, paymentClaimsApi } from "../../api/circles";
import type { PaymentClaim } from "../../api/types";
import { ClaimStatusChip } from "./StatusChip";

const MAX_EVIDENCE_BYTES = 5 * 1024 * 1024;

/**
 * prompt02 §6b: a member reports their own payment with optional image/PDF evidence. This never
 * marks the contribution paid — it creates a claim the organizer must approve.
 */
export function SubmitPaymentClaimDialog({
  open, onClose, circleId, cycleId, outstanding, currency,
}: {
  open: boolean;
  onClose: () => void;
  circleId: number;
  cycleId: number;
  outstanding: number;
  currency: string;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();

  const [amount, setAmount] = useState(String(outstanding));
  const [note, setNote] = useState("");
  const [evidence, setEvidence] = useState<File | null>(null);
  const [error, setError] = useState<string | null>(null);

  const submit = useMutation({
    mutationFn: () => paymentClaimsApi.submit(cycleId, { claimedAmount: Number(amount), note: note || undefined, evidence }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["dashboard", circleId] });
      queryClient.invalidateQueries({ queryKey: ["paymentClaims", circleId] });
      setNote(""); setEvidence(null); setError(null);
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
            onChange={(e) => setAmount(e.target.value)}
            onFocus={(e) => (e.target as HTMLInputElement).select()}
            inputProps={{ min: 0.01, max: outstanding, step: 0.01 }}
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
              {evidence ? evidence.name : t("circle.claimEvidenceHint")}
            </Typography>
          </Box>
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{t("common.cancel")}</Button>
        <Button
          variant="contained"
          disabled={submit.isPending || !(Number(amount) > 0) || Number(amount) > outstanding}
          onClick={() => submit.mutate()}
        >
          {t("common.submit")}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

/**
 * prompt02 §6b: the organizer's review inbox for one circle. Approving turns the claim into a
 * recorded contribution; rejecting leaves it unpaid and lets the member see why.
 */
export function ReviewPaymentClaimsDialog({
  open, onClose, circleId,
}: {
  open: boolean;
  onClose: () => void;
  circleId: number;
}) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { data: claims } = useQuery({
    queryKey: ["paymentClaims", circleId],
    queryFn: () => circlesApi.paymentClaims(circleId),
    enabled: open,
  });

  const [rejectingId, setRejectingId] = useState<number | null>(null);
  const [reason, setReason] = useState("");

  const review = useMutation({
    mutationFn: ({ claimId, approve, rejectionReason }: { claimId: number; approve: boolean; rejectionReason?: string }) =>
      paymentClaimsApi.review(claimId, approve, rejectionReason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["paymentClaims", circleId] });
      queryClient.invalidateQueries({ queryKey: ["dashboard", circleId] });
      setRejectingId(null);
      setReason("");
    },
  });

  const pending = claims?.filter((c) => c.status === "Pending") ?? [];
  const reviewed = claims?.filter((c) => c.status !== "Pending") ?? [];

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{t("circle.paymentClaims")}</DialogTitle>
      <DialogContent>
        {pending.length === 0 && <Alert severity="info" sx={{ mb: 2 }}>{t("circle.noPendingClaims")}</Alert>}

        <Stack spacing={2}>
          {pending.map((claim) => (
            <ClaimRow key={claim.id} claim={claim}>
              {rejectingId === claim.id ? (
                <Stack spacing={1} sx={{ mt: 1 }}>
                  <TextField
                    size="small" label={t("circle.rejectReason")} value={reason}
                    onChange={(e) => setReason(e.target.value)} fullWidth multiline rows={2}
                  />
                  <Stack direction="row" spacing={1}>
                    <Button size="small" onClick={() => { setRejectingId(null); setReason(""); }}>{t("common.cancel")}</Button>
                    <Button
                      size="small" color="error" variant="contained" disabled={review.isPending}
                      onClick={() => review.mutate({ claimId: claim.id, approve: false, rejectionReason: reason || undefined })}
                    >
                      {t("circle.reject")}
                    </Button>
                  </Stack>
                </Stack>
              ) : (
                <Stack direction="row" spacing={1} sx={{ mt: 1 }}>
                  <Button
                    size="small" variant="contained" color="success" disabled={review.isPending}
                    onClick={() => review.mutate({ claimId: claim.id, approve: true })}
                  >
                    {t("circle.approve")}
                  </Button>
                  <Button size="small" color="error" onClick={() => setRejectingId(claim.id)}>{t("circle.reject")}</Button>
                </Stack>
              )}
            </ClaimRow>
          ))}

          {reviewed.length > 0 && (
            <>
              <Divider />
              {reviewed.map((claim) => <ClaimRow key={claim.id} claim={claim} />)}
            </>
          )}
        </Stack>
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
