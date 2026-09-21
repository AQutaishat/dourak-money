import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Box, Grid, Card, CardContent, Typography, Table, TableContainer, TableHead, TableRow, TableCell, TableBody,
  Button, Dialog, DialogTitle, DialogContent, DialogActions, TextField, Stack, Tooltip, Chip, LinearProgress,
  Menu, MenuItem, Link as MuiLink,
} from "@mui/material";
import WhatsAppIcon from "@mui/icons-material/WhatsApp";
import NotificationsActiveIcon from "@mui/icons-material/NotificationsActive";
import { useTranslation } from "react-i18next";
import { isRtl } from "../../i18n";
import { shortDate } from "../../utils/date";
import { monthOrdinalWord } from "../../utils/monthOrdinal";
import { circlesApi, cyclesApi, paymentClaimsApi } from "../../api/circles";
import type { CircleDetail, CurrentCycleMemberRow } from "../../api/types";
import {
  buildCurrentCycleShareText, buildPaymentReminderText, shareToWhatsApp,
} from "../../utils/whatsapp";
import { ContributionStatusChip, ClaimStatusChip } from "./StatusChip";
import { ClaimDetailDialog, MyClaimDialog, SubmitPaymentClaimDialog } from "./PaymentClaimDialogs";
import { ConfirmPayoutButton } from "./ConfirmPayoutButton";

export function CurrentCycleTab({ circle }: { circle: CircleDetail }) {
  const { t, i18n } = useTranslation();
  const rtl = isRtl(i18n.language);
  const circleId = circle.id;
  const currency = circle.currency;
  const canManage = circle.isOrganizer;
  const queryClient = useQueryClient();
  const { data: dashboard } = useQuery({ queryKey: ["dashboard", circleId], queryFn: () => circlesApi.dashboard(circleId) });
  const { data: members } = useQuery({ queryKey: ["members", circleId], queryFn: () => circlesApi.members(circleId) });
  // Reused so the "الدورة الحالية" picker (in both the organizer's record-payment dialog and a
  // member's own submit-claim dialog) can redirect to any other month not fully paid yet — same
  // cache entry as the Monthly Cycles tab. Amounts are visible to every viewer already (only
  // claim status is privacy-masked), so this is safe to fetch for members too.
  const { data: monthsDetail } = useQuery({
    queryKey: ["monthsDetail", circleId],
    queryFn: () => circlesApi.monthsDetail(circleId),
  });
  // Only the organizer reviews claims, so this list is only fetched for them.
  const { data: claims } = useQuery({
    queryKey: ["paymentClaims", circleId],
    queryFn: () => circlesApi.paymentClaims(circleId),
    enabled: canManage,
  });
  // A non-organizer needs their own claim's id to open the edit/withdraw dialog.
  const { data: myClaims } = useQuery({
    queryKey: ["myPaymentClaims"],
    queryFn: paymentClaimsApi.mine,
    enabled: !canManage,
  });

  const [paymentDialogMember, setPaymentDialogMember] = useState<CurrentCycleMemberRow | null>(null);
  const [amount, setAmount] = useState<number>(0);
  const [recordPaymentError, setRecordPaymentError] = useState<string | null>(null);
  // null = the current cycle (the default target). Set to redirect the payment being recorded
  // to a different month entirely — any earlier or later cycle this same member hasn't fully
  // paid yet — via the "الدورة الحالية" picker under the amount field.
  const [targetCycleId, setTargetCycleId] = useState<number | null>(null);
  const [cyclePickerAnchor, setCyclePickerAnchor] = useState<HTMLElement | null>(null);
  const [claimDialogOpen, setClaimDialogOpen] = useState(false);
  const [selectedClaimId, setSelectedClaimId] = useState<number | null>(null);
  const [myClaimDialogOpen, setMyClaimDialogOpen] = useState(false);
  const [myClaimDetailOpen, setMyClaimDetailOpen] = useState(false);

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["dashboard", circleId] });
    queryClient.invalidateQueries({ queryKey: ["schedule", circleId] });
    queryClient.invalidateQueries({ queryKey: ["history", circleId] });
  };

  const effectiveCycleId = targetCycleId ?? dashboard?.cycleId;

  const recordContribution = useMutation({
    // `amount` here is the *new* installment being added on top of whatever the member already
    // paid — the backend adds it to the existing PaidAmount rather than replacing it.
    mutationFn: () => cyclesApi.recordContribution(effectiveCycleId!, { memberId: paymentDialogMember!.memberId, paidAmount: amount }),
    onSuccess: () => {
      invalidate();
      queryClient.invalidateQueries({ queryKey: ["monthsDetail", circleId] });
      setPaymentDialogMember(null);
      setRecordPaymentError(null);
      setTargetCycleId(null);
    },
    onError: (err: unknown) => {
      const title = (err as { response?: { data?: { title?: string } } })?.response?.data?.title;
      setRecordPaymentError(title ?? t("common.error"));
    },
  });

  // Other months (past or future) this same member hasn't fully paid yet, offered in the
  // "الدورة الحالية" picker as alternate targets for the payment about to be recorded.
  const otherUnpaidMonths = (monthsDetail ?? [])
    .filter((month) => month.cycleId !== dashboard?.cycleId)
    .map((month) => ({
      cycleId: month.cycleId,
      dueDate: month.dueDate,
      memberRow: month.members.find((m) => m.memberId === paymentDialogMember?.memberId),
    }))
    .filter((entry) => entry.memberRow && entry.memberRow.paidAmount < entry.memberRow.expectedAmount);

  const targetMonth = targetCycleId != null ? otherUnpaidMonths.find((m) => m.cycleId === targetCycleId) : undefined;
  const paymentOutstanding = targetMonth?.memberRow
    ? targetMonth.memberRow.expectedAmount - targetMonth.memberRow.paidAmount
    : paymentDialogMember ? paymentDialogMember.expectedAmount - paymentDialogMember.paidAmount : 0;
  const targetCycleLabel = targetMonth
    ? new Date(targetMonth.dueDate).toLocaleDateString(i18n.language, { month: "long", year: "numeric" })
    : t("circle.currentCycle");

  // No cycle yet to report on — the shared basic-info box above the tabs already covers this
  // case, so there's nothing left to render here.
  if (!dashboard) return null;

  const monthLabel = new Date(dashboard.dueDate).toLocaleDateString(i18n.language, { month: "long", year: "numeric" });
  const isArabic = i18n.language.startsWith("ar");

  // The member row belonging to the signed-in user, if they are in this circle themselves.
  const myRow = circle.myMemberId ? dashboard.members.find((m) => m.memberId === circle.myMemberId) : undefined;
  const myOutstanding = myRow ? myRow.expectedAmount - myRow.paidAmount : 0;
  const myClaim = myClaims?.find((c) => c.cycleId === dashboard.cycleId);

  // Other months (past unpaid or future not yet fully paid) this member can redirect their own
  // claim to — same picker the organizer gets when recording a payment directly.
  const myOtherUnpaidMonths = (monthsDetail ?? [])
    .filter((month) => month.cycleId !== dashboard.cycleId)
    .map((month) => ({
      cycleId: month.cycleId,
      dueDate: month.dueDate,
      memberRow: circle.myMemberId ? month.members.find((m) => m.memberId === circle.myMemberId) : undefined,
    }))
    .filter((entry): entry is { cycleId: number; dueDate: string; memberRow: NonNullable<typeof entry.memberRow> } =>
      !!entry.memberRow && entry.memberRow.paidAmount < entry.memberRow.expectedAmount)
    .map((entry) => ({
      cycleId: entry.cycleId,
      dueDate: entry.dueDate,
      outstanding: entry.memberRow.expectedAmount - entry.memberRow.paidAmount,
    }));

  const phoneFor = (memberId: number) => members?.find((m) => m.id === memberId)?.phone ?? null;

  const handleShareStatus = () => shareToWhatsApp(buildCurrentCycleShareText({
    circleName: circle.name, monthLabel, paid: dashboard.membersPaid, total: dashboard.membersTotal,
    collected: dashboard.collected, expected: dashboard.expected, currency,
    recipientName: dashboard.recipientName, isArabic,
  }));

  // prompt02 §Active circles: a per-row reminder replaces the old bulk "Unpaid" button.
  const handleSendReminder = (row: CurrentCycleMemberRow) => shareToWhatsApp(
    buildPaymentReminderText({
      memberName: row.memberName, circleName: circle.name, monthLabel,
      outstanding: row.expectedAmount - row.paidAmount, currency, isArabic,
    }),
    phoneFor(row.memberId),
  );

  const monthOrdinal = (n: number, dueDate: string) => {
    const monthName = new Date(dueDate).toLocaleDateString(i18n.language, { month: "long" });
    return `${monthOrdinalWord(n, isArabic)} (${monthName})`;
  };

  const selectedClaim = claims?.find((c) => c.id === selectedClaimId);

  return (
    <Box>
      {/* Recipient line is the first line of the tab, immediately above everything else
          (prompt03: "صاحب الدور: [name]"). */}
      <Stack spacing={1} sx={{ mb: 2 }}>
        <Stack direction="row" alignItems="center" spacing={1} flexWrap="wrap">
          <Typography variant="h6" color="text.secondary">{t("circle.currentRecipientLabel")}:</Typography>
          <Typography variant="h5" fontWeight={700} color="primary">{dashboard.recipientName}</Typography>
        </Stack>
        {/* Collection/payout status badges, and the collected/expected amounts (moved here from
            the page header), sit on their own line under the recipient's name. */}
        <Stack direction="row" spacing={1} flexWrap="wrap" alignItems="center">
          <Chip
            size="small"
            color={dashboard.collected >= dashboard.expected ? "success" : "warning"}
            label={t(dashboard.collected >= dashboard.expected ? "circle.collectionDone" : "circle.collectionUnderway")}
          />
          {(dashboard.collected >= dashboard.expected || dashboard.payoutStatus === "Paid") && (
            <Chip
              size="small"
              color={dashboard.payoutStatus === "Paid" ? "success" : "warning"}
              label={t(dashboard.payoutStatus === "Paid" ? "circle.payoutPaidBadge" : "circle.payoutPendingBadge")}
            />
          )}
          <Typography variant="body2" color="text.secondary">
            {t("circle.headerAmounts")} : {dashboard.collected} / {dashboard.expected}
          </Typography>
        </Stack>
        {/* Same collection-progress bar shown on this circle's card on the home page — same
            styling, same underlying collected/expected ratio, kept as one visual language. */}
        <LinearProgress
          variant="determinate"
          value={Math.min(dashboard.expected > 0 ? (dashboard.collected / dashboard.expected) * 100 : 0, 100)}
          sx={{ height: 8, borderRadius: 4 }}
        />
      </Stack>

      {/* Any payout installments already paid to this cycle's recipient — same format as the
          Monthly Cycles tab's payout line, shown here right under the timeline. */}
      {dashboard.payoutRows.length > 0 && (
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          {dashboard.payoutRows.map((r, idx) => (
            <span key={r.payoutPaymentId}>
              {idx > 0 && "، "}
              {idx === 0
                ? t("circle.payoutPaidLine", { amount: r.amount, recipientName: dashboard.recipientName, date: shortDate(r.date) })
                : t("circle.payoutExtraPaymentLine", { amount: r.amount, date: shortDate(r.date) })}
              {r.hasEvidence && (
                <>
                  {" "}
                  <MuiLink
                    component="button" type="button" variant="body2"
                    onClick={async () => window.open(await cyclesApi.payoutEvidenceUrl(r.payoutPaymentId), "_blank", "noopener,noreferrer")}
                  >
                    {t("circle.attachmentLink")}
                  </MuiLink>
                </>
              )}
            </span>
          ))}
        </Typography>
      )}

      {/* Only shown here — while viewing this tab — instead of in the page's shared bottom
          action row, so it disappears on every other tab. */}
      {canManage && (
        <Box sx={{ mb: 2 }}>
          <ConfirmPayoutButton circleId={circleId} />
        </Box>
      )}

      <Grid container spacing={2} sx={{ mb: 2 }}>
        {/* Which month of the circle this cycle is — first card, instead of the next
            recipient's name. */}
        <Grid item xs={6} sm={3}><StatCard label={t("circle.monthLabel")} value={monthOrdinal(dashboard.sequenceNumber, dashboard.dueDate)} /></Grid>
        <Grid item xs={6} sm={3}><StatCard label={t("circle.paid")} value={`${dashboard.membersPaid}/${dashboard.membersTotal}`} /></Grid>
        <Grid item xs={6} sm={3}><StatCard label={t("circle.collected")} value={`${dashboard.collected} ${currency}`} /></Grid>
        <Grid item xs={6} sm={3}><StatCard label={t("circle.outstanding")} value={`${dashboard.outstanding} ${currency}`} /></Grid>
      </Grid>

      {/* prompt03 §5: member self-report only — must never appear to the organizer, even when
          the organizer is also a participating member of their own circle. The claim's own
          status now shows only inside the table (last badge in the Status column), not here.
          Shown whenever there's anything left to claim anywhere — the current cycle, or any
          other month (past or future) — not just when the current cycle itself is outstanding;
          a duplicate pending claim on whichever month is actually picked is still caught
          server-side, so it isn't re-checked per month here. */}
      {!canManage && myRow && (myOutstanding > 0 || myOtherUnpaidMonths.length > 0) && (
        <Stack direction="row" sx={{ mb: 2 }}>
          <Button size="small" variant="outlined" onClick={() => setClaimDialogOpen(true)}>{t("circle.iPaid")}</Button>
        </Stack>
      )}

      <Typography variant="h6" fontWeight={600} sx={{ mt: 3, mb: 1 }}>
        {t("circle.membersPayments")}
      </Typography>

      <TableContainer sx={{ maxWidth: "100%" }}>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>{t("circle.memberName")}</TableCell>
            <TableCell>{t("circle.contributionAmount")}</TableCell>
            <TableCell>{t("circle.status")}</TableCell>
            <TableCell />
          </TableRow>
        </TableHead>
        <TableBody>
          {dashboard.members.map((m) => {
            const isUnpaid = m.status !== "Paid";
            const claim = canManage ? claims?.find((c) => c.memberId === m.memberId && c.cycleId === dashboard.cycleId) : undefined;
            const isMyRow = m.memberId === circle.myMemberId;
            return (
              <TableRow key={m.memberId}>
                <TableCell>
                  <Stack direction="row" spacing={0.5} alignItems="center">
                    <span>{m.memberName}</span>
                    {m.paidInAdvance && <Chip size="small" color="info" label={t("circle.paidInAdvance")} />}
                  </Stack>
                </TableCell>
                <TableCell>{m.paidAmount} / {m.expectedAmount} {currency}</TableCell>
                <TableCell>
                  <Stack direction="row" spacing={0.5} alignItems="center">
                    <ContributionStatusChip status={m.status} />
                    {/* Organizer: clickable badge, last in the Status column — opens the
                        claim's detail dialog, with approve/reject while still Pending. */}
                    {canManage && m.myClaimStatus && claim && (
                      <ClaimStatusChip status={m.myClaimStatus} onClick={() => setSelectedClaimId(claim.id)} />
                    )}
                    {/* Non-organizer: only ever shown for their own row (privacy rule). While
                        still Pending it opens the editable/withdraw dialog; once Approved or
                        Rejected it opens the same read-only detail view the organizer sees
                        (including the rejection reason, if any). */}
                    {!canManage && m.myClaimStatus && (
                      <ClaimStatusChip
                        status={m.myClaimStatus}
                        onClick={isMyRow && myClaim
                          ? () => (m.myClaimStatus === "Pending" ? setMyClaimDialogOpen(true) : setMyClaimDetailOpen(true))
                          : undefined}
                      />
                    )}
                  </Stack>
                </TableCell>
                <TableCell>
                  <Stack direction="row" spacing={0.5} justifyContent="flex-end" alignItems="center">
                    {canManage && isUnpaid && (
                      <Tooltip title={t("circle.reminderSentVia")}>
                        <Button size="small" startIcon={<NotificationsActiveIcon />} onClick={() => handleSendReminder(m)}>
                          {t("circle.sendReminder")}
                        </Button>
                      </Tooltip>
                    )}
                    {/* Once a member is fully paid there's nothing left to record. */}
                    {canManage && m.status !== "Paid" && (
                      <Button
                        size="small"
                        onClick={() => { setPaymentDialogMember(m); setAmount(m.expectedAmount - m.paidAmount); setRecordPaymentError(null); setTargetCycleId(null); }}
                      >
                        {t("circle.recordPayment")}
                      </Button>
                    )}
                  </Stack>
                </TableCell>
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
      </TableContainer>

      {/* Grace-period explanation and the WhatsApp share button share one line below the table.
          Physical order is locked left-to-right via a native `style` prop (the RTL emotion cache
          rewrites an sx-based `direction: ltr` back to `rtl`, see note on the header line above),
          then `order` swaps which side each item lands on per language: share button on the right
          and the hint on the left in Arabic, the mirror image in English. */}
      <Stack
        direction="row" justifyContent="space-between" alignItems="center" flexWrap="wrap" gap={1}
        sx={{ mt: 1 }} style={{ direction: "ltr" }}
      >
        <Typography variant="caption" color="text.secondary" sx={{ order: rtl ? 0 : 1 }}>
          {t("circle.gracePeriodHint")}
        </Typography>
        <Button
          size="small" startIcon={<WhatsAppIcon />} onClick={handleShareStatus}
          sx={{ order: rtl ? 1 : 0 }} style={{ direction: rtl ? "rtl" : "ltr" }}
        >
          {t("circle.shareStatus")}
        </Button>
      </Stack>

      <Dialog
        open={!!paymentDialogMember}
        onClose={() => { setPaymentDialogMember(null); setTargetCycleId(null); }}
        fullWidth maxWidth="xs"
      >
        <DialogTitle>{t("circle.recordPayment")} — {paymentDialogMember?.memberName}</DialogTitle>
        <DialogContent>
          <TextField
            label={t("circle.contributionAmount")} type="number" fullWidth sx={{ mt: 1 }}
            value={amount}
            onChange={(e) => { setAmount(Number(e.target.value)); setRecordPaymentError(null); }}
            onFocus={(e) => (e.target as HTMLInputElement).select()}
            inputProps={{ min: 0, max: paymentOutstanding, step: 0.01 }}
            // Guarded on `paymentDialogMember` still being set: once it's cleared (save succeeded,
            // or Cancel was clicked) `paymentOutstanding` falls back to 0, which would otherwise
            // flip this to an error state for the brief moment the dialog is fading out.
            error={!!recordPaymentError || (!!paymentDialogMember && amount > paymentOutstanding)}
            helperText={
              recordPaymentError
              ?? (paymentDialogMember && amount > paymentOutstanding
                ? t("circle.recordPaymentExceedsOutstanding", { amount: paymentOutstanding, currency })
                : undefined)
            }
          />
          {/* Lets the organizer redirect this exact payment to any other month this member
              hasn't fully paid yet — past or future — instead of only ever the current one. */}
          {otherUnpaidMonths.length > 0 && (
            <Box sx={{ mt: 1 }}>
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
                    setAmount(paymentDialogMember ? paymentDialogMember.expectedAmount - paymentDialogMember.paidAmount : 0);
                    setRecordPaymentError(null);
                    setCyclePickerAnchor(null);
                  }}
                >
                  {t("circle.currentCycle")}
                </MenuItem>
                {otherUnpaidMonths.map((m) => (
                  <MenuItem
                    key={m.cycleId}
                    selected={targetCycleId === m.cycleId}
                    onClick={() => {
                      setTargetCycleId(m.cycleId);
                      setAmount(m.memberRow!.expectedAmount - m.memberRow!.paidAmount);
                      setRecordPaymentError(null);
                      setCyclePickerAnchor(null);
                    }}
                  >
                    {new Date(m.dueDate).toLocaleDateString(i18n.language, { month: "long", year: "numeric" })}
                  </MenuItem>
                ))}
              </Menu>
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => { setPaymentDialogMember(null); setTargetCycleId(null); }}>{t("common.cancel")}</Button>
          <Button
            variant="contained"
            disabled={recordContribution.isPending || amount <= 0 || amount > paymentOutstanding}
            onClick={() => recordContribution.mutate()}
          >
            {t("common.save")}
          </Button>
        </DialogActions>
      </Dialog>

      {!canManage && myRow && (
        <SubmitPaymentClaimDialog
          open={claimDialogOpen}
          onClose={() => setClaimDialogOpen(false)}
          circleId={circleId}
          cycleId={dashboard.cycleId}
          outstanding={myOutstanding}
          currency={currency}
          otherMonths={myOtherUnpaidMonths}
        />
      )}

      {!canManage && myRow && (
        <MyClaimDialog
          open={myClaimDialogOpen}
          onClose={() => setMyClaimDialogOpen(false)}
          claim={myClaim}
          outstanding={myOutstanding}
          currency={currency}
        />
      )}

      {!canManage && (
        <ClaimDetailDialog
          open={myClaimDetailOpen}
          onClose={() => setMyClaimDetailOpen(false)}
          claim={myClaim}
          canReview={false}
        />
      )}

      <ClaimDetailDialog
        open={!!selectedClaimId}
        onClose={() => setSelectedClaimId(null)}
        claim={selectedClaim}
        canReview={canManage}
      />
    </Box>
  );
}

function StatCard({ label, value }: { label: string; value: string }) {
  return (
    <Card variant="outlined">
      <CardContent>
        <Typography variant="caption" color="text.secondary">{label}</Typography>
        <Typography variant="h6" fontWeight={700}>{value}</Typography>
      </CardContent>
    </Card>
  );
}
