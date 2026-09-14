import { useQuery } from "@tanstack/react-query";
import { Table, TableHead, TableRow, TableCell, TableBody, Typography, Chip } from "@mui/material";
import { useTranslation } from "react-i18next";
import { circlesApi } from "../../api/circles";

export function HistoryTab({ circleId, currency }: { circleId: number; currency: string }) {
  const { t, i18n } = useTranslation();
  const { data: history } = useQuery({ queryKey: ["history", circleId], queryFn: () => circlesApi.history(circleId) });

  if (!history || history.length === 0) {
    return <Typography color="text.secondary">{t("circle.history")} — no completed cycles yet.</Typography>;
  }

  return (
    <Table size="small">
      <TableHead>
        <TableRow>
          <TableCell>#</TableCell>
          <TableCell>{t("circle.startDate")}</TableCell>
          <TableCell>{t("circle.recipient")}</TableCell>
          <TableCell>{t("circle.collected")}</TableCell>
          <TableCell>{t("circle.unpaid")}</TableCell>
          <TableCell>{t("circle.late")}</TableCell>
          <TableCell>{t("circle.payoutStatus")}</TableCell>
        </TableRow>
      </TableHead>
      <TableBody>
        {history.map((c) => (
          <TableRow key={c.cycleId}>
            <TableCell>{c.sequenceNumber}</TableCell>
            <TableCell>{new Date(c.dueDate).toLocaleDateString(i18n.language, { month: "long", year: "numeric" })}</TableCell>
            <TableCell>{c.recipientName}</TableCell>
            <TableCell>{c.collected} / {c.expectedPool} {currency}</TableCell>
            <TableCell>{c.unpaidMembers.join(", ") || "—"}</TableCell>
            <TableCell>{c.lateMembers.join(", ") || "—"}</TableCell>
            <TableCell><Chip size="small" color={c.payoutStatus === "Paid" ? "success" : "default"} label={t(`circle.${c.payoutStatus === "Paid" ? "paid" : "pending"}`)} /></TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}
