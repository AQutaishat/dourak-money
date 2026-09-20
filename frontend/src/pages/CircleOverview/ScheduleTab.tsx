import { useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Box, Stack, Typography, Table, TableHead, TableRow, TableCell, TableBody, Chip,
  Button, Dialog, DialogTitle, DialogContent, DialogActions, TextField, Link as MuiLink,
} from "@mui/material";
import AttachFileIcon from "@mui/icons-material/AttachFile";
import { useTranslation } from "react-i18next";
import { circlesApi, cyclesApi } from "../../api/circles";
import { isRtl } from "../../i18n";
import { shortDate } from "../../utils/date";
import { ClaimStatusChip } from "./StatusChip";
import { ClaimDetailDialog, MyClaimDialog } from "./PaymentClaimDialogs";
import type { PaymentRow } from "../../api/types";

/**
 * One month's full breakdown: a large month heading with the same collection/payout status
 * badges shown under "صاحب الدور" in the Current Cycle tab (once that month is current or in the
 * past) plus a "current month" badge, the collected/total amount underneath, and every member's
 * row — name, email, one line per payment row (an organizer-recorded installment, or a claim at
 * any stage — Pending/Rejected claims show their own line with a badge but don't count toward the
 * total paid), date(s) paid, and the running total paid. A month after the current one has no
 * payment data yet, so those cells render as "—".
 */
export function ScheduleTab({
  circleId, canManage, currency, myMemberId,
}: {
  circleId: number; canManage: boolean; currency: string; myMemberId?: number | null;
}) {
  const { t, i18n } = useTranslation();
  const rtl = isRtl(i18n.language);
  const queryClient = useQueryClient();
  const { data: months } = useQuery({ queryKey: ["monthsDetail", circleId], queryFn: () => circlesApi.monthsDetail(circleId) });
  const { data: dashboard } = useQuery({ queryKey: ["dashboard", circleId], queryFn: () => circlesApi.dashboard(circleId) });
  // A member only ever gets their own claims back from this endpoint (server-side privacy rule),
  // so this is safe to fetch and render for every viewer, not just the organizer.
  const { data: claims } = useQuery({ queryKey: ["paymentClaims", circleId], queryFn: () => circlesApi.paymentClaims(circleId) });
  const [selectedClaimId, setSelectedClaimId] = useState<number | null>(null);
  const selectedClaim = claims?.find((c) => c.id === selectedClaimId);
  // A member's own still-Pending claim opens the editable/withdraw dialog instead of the
  // read-only one, matching the Current Cycle tab's behavior for the same claim.
  const [editingClaimId, setEditingClaimId] = useState<number | null>(null);
  const editingClaim = claims?.find((c) => c.id === editingClaimId);
  const editingMonth = months?.find((mo) => mo.cycleId === editingClaim?.cycleId);
  const editingMemberRow = editingMonth?.members.find((mm) => mm.memberId === editingClaim?.memberId);
  const editingOutstanding = editingMemberRow ? editingMemberRow.expectedAmount - editingMemberRow.paidAmount : 0;

  // Record-payment dialog target: any member, in any month (past/current/future) that isn't
  // fully paid yet — a payment can be recorded ahead of schedule for a future month too, and it
  // only ever counts toward that specific month's contribution.
  const [paymentTarget, setPaymentTarget] = useState<{ cycleId: number; memberId: number; memberName: string; outstanding: number } | null>(null);
  const [amount, setAmount] = useState(0);
  const [recordPaymentError, setRecordPaymentError] = useState<string | null>(null);

  const recordContribution = useMutation({
    mutationFn: () => cyclesApi.recordContribution(paymentTarget!.cycleId, { memberId: paymentTarget!.memberId, paidAmount: amount }),
    onSuccess: () => {
      invalidateAll();
      setPaymentTarget(null);
      setRecordPaymentError(null);
    },
    onError: (err: unknown) => {
      const title = (err as { response?: { data?: { title?: string } } })?.response?.data?.title;
      setRecordPaymentError(title ?? t("common.error"));
    },
  });

  // Confirm-payout dialog target: like the record-payment one above, this works against any
  // month (past/current/future) whose payout isn't fully paid yet, and can carry an evidence file.
  const [payoutTarget, setPayoutTarget] = useState<{ cycleId: number; recipientName: string; outstanding: number } | null>(null);
  const [payoutAmount, setPayoutAmount] = useState(0);
  const [payoutEvidence, setPayoutEvidence] = useState<File | null>(null);
  const [payoutError, setPayoutError] = useState<string | null>(null);
  const payoutFileInputRef = useRef<HTMLInputElement>(null);

  const invalidateAll = () => {
    queryClient.invalidateQueries({ queryKey: ["monthsDetail", circleId] });
    queryClient.invalidateQueries({ queryKey: ["dashboard", circleId] });
    queryClient.invalidateQueries({ queryKey: ["schedule", circleId] });
    queryClient.invalidateQueries({ queryKey: ["history", circleId] });
  };

  const recordPayout = useMutation({
    mutationFn: () => cyclesApi.recordPayout(payoutTarget!.cycleId, { actualAmount: payoutAmount, evidence: payoutEvidence }),
    onSuccess: () => {
      invalidateAll();
      setPayoutTarget(null);
      setPayoutEvidence(null);
      setPayoutError(null);
    },
    onError: (err: unknown) => {
      const title = (err as { response?: { data?: { title?: string } } })?.response?.data?.title;
      setPayoutError(title ?? t("common.error"));
    },
  });

  const openPayoutEvidence = async (payoutPaymentId: number) => {
    const url = await cyclesApi.payoutEvidenceUrl(payoutPaymentId);
    window.open(url, "_blank", "noopener,noreferrer");
  };

  if (!months || months.length === 0) {
    return <Typography color="text.secondary">{t("circle.schedule")} — {t("circle.activate")}</Typography>;
  }

  const currentIndex = dashboard ? months.findIndex((m) => m.cycleId === dashboard.cycleId) : -1;

  return (
    <Box>
      {months.map((month, i) => {
        const isCurrent = i === currentIndex;
        // With no current cycle at all (the circle is fully Completed), nothing is future.
        const isFuture = currentIndex !== -1 && i > currentIndex;
        const fullyCollected = month.collectedAmount >= month.expectedPoolAmount;
        const monthName = new Date(month.dueDate).toLocaleDateString(i18n.language, { month: "long", year: "numeric" });

        const underCollection = !isFuture && !fullyCollected;

        return (
          <Box key={month.cycleId} sx={{ mb: 8 }}>
            <Stack direction="row" alignItems="center" spacing={1} flexWrap="wrap" sx={{ mb: 2 }}>
              <Typography variant="h5" fontWeight={700}>{monthName}</Typography>
              {isCurrent && <Chip size="small" color="primary" label={t("circle.currentMonthBadge")} />}
              {!isFuture && (
                <>
                  <Chip
                    size="small"
                    color={fullyCollected ? "success" : "warning"}
                    label={t(fullyCollected ? "circle.collectionDone" : "circle.collectionUnderway")}
                  />
                  <Chip
                    size="small"
                    color={month.payoutStatus === "Paid" ? "success" : "warning"}
                    label={t(month.payoutStatus === "Paid" ? "circle.payoutPaidBadge" : "circle.payoutPendingBadge")}
                  />
                </>
              )}
            </Stack>
            {/* While still under collection, the collected/total figure comes first (right under
                the month name), then the recipient line, then the payout line — once it's fully
                collected the amounts no longer matter, so only the recipient/payout lines show.
                Shifted slightly left of the month heading above — in Arabic the text is right-
                aligned, so a plain marginLeft does nothing; it needs marginRight to actually push
                it away from the right edge. Native style, not sx, so the RTL emotion cache
                doesn't mirror the offset back. */}
            <Box style={rtl ? { marginRight: "24px" } : { marginLeft: "-8px" }}>
              {underCollection && (
                <Typography variant="body2" color="text.secondary" sx={{ mb: 0.25 }}>
                  {t("circle.collectedAmountsLabel")} : {month.collectedAmount} / {month.expectedPoolAmount}
                </Typography>
              )}
              <Typography variant="body2" sx={{ mb: 0.25 }}>
                {t("circle.currentRecipientLabel")}: {month.recipientName}
              </Typography>
              {month.payoutRows.length > 0 && (
                <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
                  {month.payoutRows.map((r, idx) => (
                    <span key={r.payoutPaymentId}>
                      {idx > 0 && "، "}
                      {idx === 0
                        ? t("circle.payoutPaidLine", { amount: r.amount, recipientName: month.recipientName, date: shortDate(r.date) })
                        : t("circle.payoutExtraPaymentLine", { amount: r.amount, date: shortDate(r.date) })}
                      {r.hasEvidence && (
                        <>
                          {" "}
                          <MuiLink component="button" type="button" variant="body2" onClick={() => openPayoutEvidence(r.payoutPaymentId)}>
                            {t("circle.attachmentLink")}
                          </MuiLink>
                        </>
                      )}
                    </span>
                  ))}
                </Typography>
              )}
              {canManage && month.payoutExpectedAmount - month.payoutActualAmount > 0 && (
                <Button
                  size="small"
                  variant="outlined"
                  sx={{ mb: 1 }}
                  onClick={() => {
                    const outstanding = month.payoutExpectedAmount - month.payoutActualAmount;
                    setPayoutTarget({ cycleId: month.cycleId, recipientName: month.recipientName, outstanding });
                    setPayoutAmount(outstanding);
                    setPayoutEvidence(null);
                    setPayoutError(null);
                  }}
                >
                  {t(isFuture ? "circle.confirmPayoutAdvance" : isCurrent ? "circle.confirmPayout" : "circle.confirmPayoutOld")}
                </Button>
              )}
            </Box>

            <Table size="small" sx={{ mt: 3 }}>
              <TableHead>
                <TableRow>
                  <TableCell>{t("circle.memberName")}</TableCell>
                  <TableCell>{t("circle.email")}</TableCell>
                  <TableCell>{t("circle.amountPaidColumn")}</TableCell>
                  <TableCell>{t("circle.paymentDateColumn")}</TableCell>
                  <TableCell>{t("circle.totalPaidColumn")}</TableCell>
                  {canManage && <TableCell />}
                </TableRow>
              </TableHead>
              <TableBody>
                {month.members.map((m) => {
                  // A contribution paid before per-installment tracking existed has no
                  // `paymentRows` history — fall back to its single running PaidAmount/PaidAt
                  // pair so it still shows up instead of rendering as if nothing was ever paid.
                  const rows: PaymentRow[] = m.paymentRows.length > 0
                    ? m.paymentRows
                    : m.paidAmount > 0 && m.paidAt ? [{ amount: m.paidAmount, date: m.paidAt }] : [];
                  const outstanding = m.expectedAmount - m.paidAmount;

                  return (
                    <TableRow key={m.memberId}>
                      <TableCell>{m.memberName}</TableCell>
                      <TableCell>{m.email || "—"}</TableCell>
                      <TableCell>
                        {rows.length > 0 ? (
                          <Stack spacing={0.25}>
                            {rows.map((r, idx) => (
                              <Stack key={idx} direction="row" spacing={0.5} alignItems="center">
                                <Typography variant="body2">{r.amount}</Typography>
                                {r.claimStatus && r.claimId != null && (
                                  <ClaimStatusChip
                                    status={r.claimStatus}
                                    onClick={() => {
                                      const isMine = myMemberId != null && m.memberId === myMemberId;
                                      if (isMine && r.claimStatus === "Pending") setEditingClaimId(r.claimId!);
                                      else setSelectedClaimId(r.claimId!);
                                    }}
                                  />
                                )}
                              </Stack>
                            ))}
                          </Stack>
                        ) : "—"}
                      </TableCell>
                      <TableCell>
                        {rows.length > 0 ? (
                          <Stack spacing={0.25}>
                            {rows.map((r, idx) => <Typography key={idx} variant="body2">{shortDate(r.date)}</Typography>)}
                          </Stack>
                        ) : "—"}
                      </TableCell>
                      <TableCell>{m.paidAmount}</TableCell>
                      {canManage && (
                        <TableCell>
                          {/* Works for any month — past, current, or a future one not due yet —
                              as long as this specific month's contribution isn't fully paid;
                              a payment recorded here only ever counts toward this one month. */}
                          {outstanding > 0 && (
                            <Button
                              size="small"
                              onClick={() => {
                                setPaymentTarget({ cycleId: month.cycleId, memberId: m.memberId, memberName: m.memberName, outstanding });
                                setAmount(outstanding);
                                setRecordPaymentError(null);
                              }}
                            >
                              {t(isFuture ? "circle.recordPaymentAdvance" : isCurrent ? "circle.recordPayment" : "circle.recordPaymentOld")}
                            </Button>
                          )}
                        </TableCell>
                      )}
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </Box>
        );
      })}

      <ClaimDetailDialog
        open={!!selectedClaimId}
        onClose={() => setSelectedClaimId(null)}
        claim={selectedClaim}
        canReview={canManage}
      />

      <MyClaimDialog
        open={!!editingClaimId}
        onClose={() => setEditingClaimId(null)}
        claim={editingClaim}
        outstanding={editingOutstanding}
        currency={currency}
      />

      <Dialog open={!!paymentTarget} onClose={() => setPaymentTarget(null)} fullWidth maxWidth="xs">
        <DialogTitle>{t("circle.recordPayment")} — {paymentTarget?.memberName}</DialogTitle>
        <DialogContent>
          <TextField
            label={t("circle.contributionAmount")} type="number" fullWidth sx={{ mt: 1 }}
            value={amount}
            onChange={(e) => { setAmount(Number(e.target.value)); setRecordPaymentError(null); }}
            onFocus={(e) => (e.target as HTMLInputElement).select()}
            inputProps={{ min: 0, max: paymentTarget?.outstanding ?? 0, step: 0.01 }}
            error={!!recordPaymentError || (!!paymentTarget && amount > paymentTarget.outstanding)}
            helperText={
              recordPaymentError
              ?? (paymentTarget && amount > paymentTarget.outstanding
                ? t("circle.recordPaymentExceedsOutstanding", { amount: paymentTarget.outstanding, currency })
                : undefined)
            }
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setPaymentTarget(null)}>{t("common.cancel")}</Button>
          <Button
            variant="contained"
            disabled={recordContribution.isPending || amount <= 0 || (!!paymentTarget && amount > paymentTarget.outstanding)}
            onClick={() => recordContribution.mutate()}
          >
            {t("common.save")}
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={!!payoutTarget} onClose={() => setPayoutTarget(null)} fullWidth maxWidth="xs">
        <DialogTitle>{t("circle.confirmPayout")} — {payoutTarget?.recipientName}</DialogTitle>
        <DialogContent>
          <TextField
            label={t("circle.expectedPool")} type="number" fullWidth sx={{ mt: 1 }}
            value={payoutAmount}
            onChange={(e) => { setPayoutAmount(Number(e.target.value)); setPayoutError(null); }}
            onFocus={(e) => (e.target as HTMLInputElement).select()}
            inputProps={{ min: 0, max: payoutTarget?.outstanding ?? 0, step: 0.01 }}
            error={!!payoutError || (!!payoutTarget && payoutAmount > payoutTarget.outstanding)}
            helperText={
              payoutError
              ?? (payoutTarget && payoutAmount > payoutTarget.outstanding
                ? t("circle.recordPaymentExceedsOutstanding", { amount: payoutTarget.outstanding, currency })
                : undefined)
            }
          />
          <Stack direction="row" spacing={1} alignItems="center" sx={{ mt: 2 }}>
            <Button size="small" startIcon={<AttachFileIcon />} onClick={() => payoutFileInputRef.current?.click()}>
              {t("circle.claimEvidence")}
            </Button>
            {payoutEvidence && <Typography variant="body2" color="text.secondary" noWrap>{payoutEvidence.name}</Typography>}
            <input
              ref={payoutFileInputRef} type="file" hidden accept="image/*,application/pdf"
              onChange={(e) => setPayoutEvidence(e.target.files?.[0] ?? null)}
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setPayoutTarget(null)}>{t("common.cancel")}</Button>
          <Button
            variant="contained"
            disabled={recordPayout.isPending || payoutAmount <= 0 || (!!payoutTarget && payoutAmount > payoutTarget.outstanding)}
            onClick={() => recordPayout.mutate()}
          >
            {t("common.confirm")}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
