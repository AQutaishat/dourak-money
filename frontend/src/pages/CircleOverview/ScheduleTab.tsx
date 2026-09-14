import { useQuery } from "@tanstack/react-query";
import { Table, TableHead, TableRow, TableCell, TableBody, Chip, Typography } from "@mui/material";
import { useTranslation } from "react-i18next";
import { circlesApi } from "../../api/circles";

export function ScheduleTab({ circleId, currency }: { circleId: number; currency: string }) {
  const { t, i18n } = useTranslation();
  const { data: schedule } = useQuery({ queryKey: ["schedule", circleId], queryFn: () => circlesApi.schedule(circleId) });

  if (!schedule || schedule.length === 0) {
    return <Typography color="text.secondary">{t("circle.schedule")} — {t("circle.activate")}</Typography>;
  }

  return (
    <Table size="small">
      <TableHead>
        <TableRow>
          <TableCell>#</TableCell>
          <TableCell>{t("circle.startDate")}</TableCell>
          <TableCell>{t("circle.recipient")}</TableCell>
          <TableCell>{t("circle.expectedPool")}</TableCell>
          <TableCell>{t("circle.status")}</TableCell>
          <TableCell>{t("circle.payoutStatus")}</TableCell>
        </TableRow>
      </TableHead>
      <TableBody>
        {schedule.map((c) => (
          <TableRow key={c.cycleId}>
            <TableCell>{c.sequenceNumber}</TableCell>
            <TableCell>{new Date(c.dueDate).toLocaleDateString(i18n.language, { month: "long", year: "numeric" })}</TableCell>
            <TableCell>{c.recipientName}</TableCell>
            <TableCell>{c.expectedPoolAmount} {currency}</TableCell>
            <TableCell><Chip size="small" label={t(`circle.${c.status === "Completed" ? "completed" : "pending"}`)} /></TableCell>
            <TableCell>
              <Chip size="small" color={c.payoutStatus === "Paid" ? "success" : "default"} label={t(`circle.${c.payoutStatus === "Paid" ? "paid" : "pending"}`)} />
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}
