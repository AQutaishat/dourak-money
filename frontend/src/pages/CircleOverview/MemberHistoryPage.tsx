import { useQuery } from "@tanstack/react-query";
import { useParams, Link as RouterLink } from "react-router-dom";
import { Box, Typography, Table, TableHead, TableRow, TableCell, TableBody, Chip, Button, Stack } from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { useTranslation } from "react-i18next";
import { circlesApi } from "../../api/circles";

export function MemberHistoryPage() {
  const { circleId, memberId } = useParams();
  const { t, i18n } = useTranslation();
  const cId = Number(circleId);
  const mId = Number(memberId);

  const { data: history } = useQuery({ queryKey: ["memberHistory", cId, mId], queryFn: () => circlesApi.memberHistory(cId, mId) });

  if (!history) return null;

  return (
    <Box>
      <Button component={RouterLink} to={`/circles/${cId}`} startIcon={<ArrowBackIcon />} sx={{ mb: 2 }}>{t("common.back")}</Button>
      <Typography variant="h5" fontWeight={700} gutterBottom>{history.memberName}</Typography>
      {history.payoutPosition && <Typography color="text.secondary" gutterBottom>#{history.payoutPosition}</Typography>}

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
              <TableCell>{t(`circle.${e.status === "PartiallyPaid" ? "partiallyPaid" : e.status.toLowerCase()}`)}</TableCell>
              <TableCell>
                {e.isRecipientThisCycle
                  ? <Chip size="small" color={e.payoutStatusIfRecipient === "Paid" ? "success" : "default"} label={t(`circle.${e.payoutStatusIfRecipient === "Paid" ? "paid" : "pending"}`)} />
                  : <Stack component="span">—</Stack>}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </Box>
  );
}
