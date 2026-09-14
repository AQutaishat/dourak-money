import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Box, Grid, Card, CardContent, Typography, Table, TableHead, TableRow, TableCell, TableBody,
  Chip, Button, Dialog, DialogTitle, DialogContent, DialogActions, TextField, Stack, Alert,
} from "@mui/material";
import WhatsAppIcon from "@mui/icons-material/WhatsApp";
import { useTranslation } from "react-i18next";
import { circlesApi, cyclesApi } from "../../api/circles";
import type { CurrentCycleMemberRow } from "../../api/types";
import { buildCurrentCycleShareText, buildUnpaidShareText, shareToWhatsApp } from "../../utils/whatsapp";

const statusColor: Record<string, "default" | "success" | "warning" | "error"> = {
  Paid: "success", PartiallyPaid: "warning", Unpaid: "default", Late: "error",
};

export function CurrentCycleTab({ circleId, circleName, currency }: { circleId: number; circleName: string; currency: string }) {
  const { t, i18n } = useTranslation();
  const queryClient = useQueryClient();
  const { data: dashboard } = useQuery({ queryKey: ["dashboard", circleId], queryFn: () => circlesApi.dashboard(circleId) });

  const [paymentDialogMember, setPaymentDialogMember] = useState<CurrentCycleMemberRow | null>(null);
  const [amount, setAmount] = useState<number>(0);
  const [payoutDialogOpen, setPayoutDialogOpen] = useState(false);
  const [payoutAmount, setPayoutAmount] = useState<number>(0);

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
    return <Alert severity="info">{t("circle.activate")} — no active cycle yet.</Alert>;
  }

  const monthLabel = new Date(dashboard.dueDate).toLocaleDateString(i18n.language, { month: "long", year: "numeric" });
  const isArabic = i18n.language.startsWith("ar");

  const handleShareStatus = () => shareToWhatsApp(buildCurrentCycleShareText({
    circleName, monthLabel, paid: dashboard.membersPaid, total: dashboard.membersTotal,
    collected: dashboard.collected, expected: dashboard.expected, currency,
    recipientName: dashboard.recipientName, isArabic,
  }));

  const handleShareUnpaid = () => shareToWhatsApp(buildUnpaidShareText(
    dashboard.members.filter((m) => m.status !== "Paid").map((m) => m.memberName), isArabic,
  ));

  return (
    <Box>
      <Grid container spacing={2} sx={{ mb: 2 }}>
        <Grid item xs={6} sm={3}><StatCard label={t("circle.paid")} value={`${dashboard.membersPaid}/${dashboard.membersTotal}`} /></Grid>
        <Grid item xs={6} sm={3}><StatCard label={t("circle.collected")} value={`${dashboard.collected} ${currency}`} /></Grid>
        <Grid item xs={6} sm={3}><StatCard label={t("circle.outstanding")} value={`${dashboard.outstanding} ${currency}`} /></Grid>
        <Grid item xs={6} sm={3}><StatCard label={t("circle.recipient")} value={dashboard.recipientName} /></Grid>
      </Grid>

      <Stack direction="row" spacing={1} sx={{ mb: 2 }}>
        <Button size="small" startIcon={<WhatsAppIcon />} onClick={handleShareStatus}>{t("circle.shareStatus")}</Button>
        <Button size="small" startIcon={<WhatsAppIcon />} onClick={handleShareUnpaid}>{t("circle.unpaid")}</Button>
      </Stack>

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
          {dashboard.members.map((m) => (
            <TableRow key={m.memberId}>
              <TableCell>{m.memberName}</TableCell>
              <TableCell>{m.paidAmount} / {m.expectedAmount} {currency}</TableCell>
              <TableCell><Chip size="small" color={statusColor[m.status]} label={t(`circle.${m.status === "PartiallyPaid" ? "partiallyPaid" : m.status.toLowerCase()}`)} /></TableCell>
              <TableCell>
                <Button size="small" onClick={() => { setPaymentDialogMember(m); setAmount(m.expectedAmount); }}>
                  {t("circle.recordPayment")}
                </Button>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>

      <Stack direction="row" justifyContent="flex-end" sx={{ mt: 2 }}>
        {dashboard.payoutStatus === "Pending" && (
          <Button variant="contained" onClick={() => { setPayoutAmount(dashboard.collected); setPayoutDialogOpen(true); }}>
            {t("circle.confirmPayout")}
          </Button>
        )}
      </Stack>

      <Dialog open={!!paymentDialogMember} onClose={() => setPaymentDialogMember(null)} fullWidth maxWidth="xs">
        <DialogTitle>{t("circle.recordPayment")} — {paymentDialogMember?.memberName}</DialogTitle>
        <DialogContent>
          <TextField
            label={t("circle.contributionAmount")} type="number" fullWidth sx={{ mt: 1 }}
            value={amount} onChange={(e) => setAmount(Number(e.target.value))}
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
            <Alert severity="warning" sx={{ mb: 2 }}>Not all members have paid yet.</Alert>
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
