import { useQuery } from "@tanstack/react-query";
import { useNavigate, useParams } from "react-router-dom";
import { Box, Typography, Table, TableHead, TableRow, TableCell, TableBody, Chip, Button, Stack } from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import CloseIcon from "@mui/icons-material/Close";
import { useTranslation } from "react-i18next";
import { circlesApi } from "../../api/circles";
import { ContributionStatusChip } from "./StatusChip";

export function MemberHistoryPage() {
  const { circleId, memberId } = useParams();
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const cId = Number(circleId);
  const mId = Number(memberId);

  const { data: history } = useQuery({ queryKey: ["memberHistory", cId, mId], queryFn: () => circlesApi.memberHistory(cId, mId) });

  // prompt02 §Active circles: both Back and Close return to the Members tab inside Circle
  // Details, not to the circle's default tab.
  const backToMembers = () => navigate(`/circles/${cId}?tab=members`);

  if (!history) return null;

  return (
    <Box>
      <Button onClick={backToMembers} startIcon={<ArrowBackIcon />} sx={{ mb: 2 }}>{t("common.back")}</Button>

      {/* Heading says what this page is, not just the bare member name. */}
      <Typography variant="h5" fontWeight={700} gutterBottom>
        {t("circle.memberHistoryTitle", { name: history.memberName })}
      </Typography>
      {history.payoutPosition && (
        <Typography color="text.secondary" gutterBottom>{t("circle.payoutOrder")}: #{history.payoutPosition}</Typography>
      )}

      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>#</TableCell>
            <TableCell>{t("circle.startDate")}</TableCell>
            <TableCell>{t("circle.contributionAmount")}</TableCell>
            <TableCell>{t("circle.status")}</TableCell>
            <TableCell>{t("circle.payoutStatus")}</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {history.entries.map((e) => (
            <TableRow key={e.cycleId}>
              <TableCell>{e.sequenceNumber}</TableCell>
              <TableCell>{new Date(e.dueDate).toLocaleDateString(i18n.language, { month: "long", year: "numeric" })}</TableCell>
              <TableCell>{e.paidAmount} / {e.expectedAmount}</TableCell>
              {/* Same colored badges as the Current Cycle payments table. */}
              <TableCell><ContributionStatusChip status={e.status} /></TableCell>
              <TableCell>
                {e.isRecipientThisCycle
                  ? <Chip size="small" color={e.payoutStatusIfRecipient === "Paid" ? "success" : "default"} label={t(`circle.${e.payoutStatusIfRecipient === "Paid" ? "paid" : "pending"}`)} />
                  : <Stack component="span">—</Stack>}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>

      <Stack direction="row" justifyContent="flex-end" sx={{ mt: 3 }}>
        <Button variant="outlined" startIcon={<CloseIcon />} onClick={backToMembers}>{t("common.close")}</Button>
      </Stack>
    </Box>
  );
}
