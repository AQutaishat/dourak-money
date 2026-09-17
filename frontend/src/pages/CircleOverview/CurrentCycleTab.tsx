import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Badge, Box, Grid, Card, CardContent, Typography, Table, TableHead, TableRow, TableCell, TableBody,
  Button, Dialog, DialogTitle, DialogContent, DialogActions, TextField, Stack, Alert, Tooltip,
} from "@mui/material";
import WhatsAppIcon from "@mui/icons-material/WhatsApp";
import NotificationsActiveIcon from "@mui/icons-material/NotificationsActive";
import { useTranslation } from "react-i18next";
import { circlesApi, cyclesApi } from "../../api/circles";
import type { CircleDetail, CurrentCycleMemberRow } from "../../api/types";
import {
  buildCurrentCycleShareText, buildPaymentReminderText, shareToWhatsApp,
} from "../../utils/whatsapp";
import { BasicInfoBlock } from "./BasicInfoBlock";
import { ContributionStatusChip, ClaimStatusChip } from "./StatusChip";
import { ReviewPaymentClaimsDialog, SubmitPaymentClaimDialog } from "./PaymentClaimDialogs";

export function CurrentCycleTab({ circle }: { circle: CircleDetail }) {
  const { t, i18n } = useTranslation();
  const circleId = circle.id;
  const currency = circle.currency;
  const queryClient = useQueryClient();
  const { data: dashboard } = useQuery({ queryKey: ["dashboard", circleId], queryFn: () => circlesApi.dashboard(circleId) });
  const { data: members } = useQuery({ queryKey: ["members", circleId], queryFn: () => circlesApi.members(circleId) });

  const [paymentDialogMember, setPaymentDialogMember] = useState<CurrentCycleMemberRow | null>(null);
  const [amount, setAmount] = useState<number>(0);
  const [payoutDialogOpen, setPayoutDialogOpen] = useState(false);
  const [payoutAmount, setPayoutAmount] = useState<number>(0);
  const [claimDialogOpen, setClaimDialogOpen] = useState(false);
  const [reviewDialogOpen, setReviewDialogOpen] = useState(false);

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["dashboard", circleId] });
    queryClient.invalidateQueries({ queryKey: ["schedule", circleId] });
    queryClient.invalidateQueries({ queryKey: ["history", circleId] });
  };

  const recordContribution = useMutation({
    mutationFn: () => cyclesApi.recordContribution(dashboard!.cycleId, { memberId: paymentDialogMember!.memberId, paidAmount: amount }),
    onSuccess: () => { invalidate(); setPaymentDialogMember(null); },
  });

  const recordPayout = useMutation({
    mutationFn: () => cyclesApi.recordPayout(dashboard!.cycleId, { actualAmount: payoutAmount }),
    onSuccess: () => { invalidate(); setPayoutDialogOpen(false); },
  });

  if (!dashboard) {
    return (
      <Box>
        <BasicInfoBlock circle={circle} dense />
        <Alert severity="info">{t("circle.activate")}</Alert>
      </Box>
    );
  }

  const monthLabel = new Date(dashboard.dueDate).toLocaleDateString(i18n.language, { month: "long", year: "numeric" });
  const isArabic = i18n.language.startsWith("ar");
  const canManage = circle.isOrganizer;

  // The member row belonging to the signed-in user, if they are in this circle themselves.
  const myRow = circle.myMemberId ? dashboard.members.find((m) => m.memberId === circle.myMemberId) : undefined;
  const myOutstanding = myRow ? myRow.expectedAmount - myRow.paidAmount : 0;

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

  return (
    <Box>
      {/* Basic info merged into the top of Current Cycle as a read-only block, rather than
          living in its own tab (prompt02 §Active circles). */}
      <BasicInfoBlock circle={circle} dense />

      {/* Prominent recipient line above the summary cards. prompt03 §5: "Share to WhatsApp"
          moves up here, well clear of the record-payment/claim controls below, so the two
          don't crowd each other. justifyContent="space-between" mirrors correctly for RTL. */}
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }} flexWrap="wrap" gap={1}>
        <Stack direction="row" spacing={1} alignItems="baseline" flexWrap="wrap">
          <Typography variant="h6" color="text.secondary">{t("circle.currentRecipientLabel")}:</Typography>
          <Typography variant="h5" fontWeight={700} color="primary">{dashboard.recipientName}</Typography>
        </Stack>
        <Button size="small" startIcon={<WhatsAppIcon />} onClick={handleShareStatus}>{t("circle.shareStatus")}</Button>
      </Stack>

      <Grid container spacing={2} sx={{ mb: 2 }}>
        <Grid item xs={6} sm={3}><StatCard label={t("circle.paid")} value={`${dashboard.membersPaid}/${dashboard.membersTotal}`} /></Grid>
        <Grid item xs={6} sm={3}><StatCard label={t("circle.collected")} value={`${dashboard.collected} ${currency}`} /></Grid>
        <Grid item xs={6} sm={3}><StatCard label={t("circle.outstanding")} value={`${dashboard.outstanding} ${currency}`} /></Grid>
        <Grid item xs={6} sm={3}><StatCard label={t("circle.nextRecipient")} value={dashboard.nextRecipientName ?? "—"} /></Grid>
      </Grid>

      <Stack direction="row" spacing={1} sx={{ mb: 2 }} flexWrap="wrap" gap={1}>
        {/* Organizer's payment-claim inbox, badged with the pending count. */}
        {canManage && (
          <Badge badgeContent={dashboard.pendingClaimCount} color="warning">
            <Button size="small" onClick={() => setReviewDialogOpen(true)}>{t("circle.paymentClaims")}</Button>
          </Badge>
        )}

        {/* prompt03 §5: member self-report only — must never appear to the organizer, even
            when the organizer is also a participating member of their own circle. */}
        {!canManage && myRow && myOutstanding > 0 && !myRow.hasPendingClaim && (
          <Button size="small" variant="outlined" onClick={() => setClaimDialogOpen(true)}>{t("circle.iPaid")}</Button>
        )}
        {!canManage && myRow?.myClaimStatus && <ClaimStatusChip status={myRow.myClaimStatus} />}
      </Stack>

      <Typography variant="caption" color="text.secondary" sx={{ display: "block", mb: 0.5 }}>
        {t("circle.gracePeriodHint")}
      </Typography>

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
            return (
              <TableRow key={m.memberId}>
                <TableCell>{m.memberName}</TableCell>
                <TableCell>{m.paidAmount} / {m.expectedAmount} {currency}</TableCell>
                <TableCell>
                  <Stack direction="row" spacing={0.5} alignItems="center">
                    <ContributionStatusChip status={m.status} />
                    {/* Claim state is only ever sent for the organizer and for your own row. */}
                    {m.myClaimStatus && <ClaimStatusChip status={m.myClaimStatus} />}
                  </Stack>
                </TableCell>
                <TableCell>
                  <Stack direction="row" spacing={0.5} justifyContent="flex-end">
                    {canManage && isUnpaid && (
                      <Tooltip title={t("circle.reminderSentVia")}>
                        <Button size="small" startIcon={<NotificationsActiveIcon />} onClick={() => handleSendReminder(m)}>
                          {t("circle.sendReminder")}
                        </Button>
                      </Tooltip>
                    )}
                    {canManage && (
                      <Button size="small" onClick={() => { setPaymentDialogMember(m); setAmount(m.expectedAmount); }}>
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

      {canManage && (
        <Stack direction="row" justifyContent="flex-end" sx={{ mt: 2 }}>
          {dashboard.payoutStatus === "Pending" && (
            <Button variant="contained" onClick={() => { setPayoutAmount(dashboard.collected); setPayoutDialogOpen(true); }}>
              {t("circle.confirmPayout")}
            </Button>
          )}
        </Stack>
      )}

      <Dialog open={!!paymentDialogMember} onClose={() => setPaymentDialogMember(null)} fullWidth maxWidth="xs">
        <DialogTitle>{t("circle.recordPayment")} — {paymentDialogMember?.memberName}</DialogTitle>
        <DialogContent>
          <TextField
            label={t("circle.contributionAmount")} type="number" fullWidth sx={{ mt: 1 }}
            value={amount} onChange={(e) => setAmount(Number(e.target.value))}
            onFocus={(e) => (e.target as HTMLInputElement).select()}
            inputProps={{ min: 0, max: paymentDialogMember?.expectedAmount, step: 0.01 }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setPaymentDialogMember(null)}>{t("common.cancel")}</Button>
          <Button variant="contained" onClick={() => recordContribution.mutate()}>{t("common.save")}</Button>
        </DialogActions>
      </Dialog>

      <Dialog open={payoutDialogOpen} onClose={() => setPayoutDialogOpen(false)} fullWidth maxWidth="xs">
        <DialogTitle>{t("circle.confirmPayout")} — {dashboard.recipientName}</DialogTitle>
        <DialogContent>
          {dashboard.membersUnpaid + dashboard.membersLate > 0 && (
            <Alert severity="warning" sx={{ mb: 2 }}>{t("circle.unpaid")}: {dashboard.membersUnpaid + dashboard.membersLate}</Alert>
          )}
          <TextField
            label={t("circle.expectedPool")} type="number" fullWidth
            value={payoutAmount} onChange={(e) => setPayoutAmount(Number(e.target.value))}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setPayoutDialogOpen(false)}>{t("common.cancel")}</Button>
          <Button variant="contained" onClick={() => recordPayout.mutate()}>{t("common.confirm")}</Button>
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
        />
      )}

      {canManage && (
        <ReviewPaymentClaimsDialog open={reviewDialogOpen} onClose={() => setReviewDialogOpen(false)} circleId={circleId} />
      )}
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
