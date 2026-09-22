import { useQuery } from "@tanstack/react-query";
import {
  CircularProgress, Link, Paper, Table, TableBody, TableCell, TableContainer,
  TableHead, TableRow, Typography,
} from "@mui/material";
import { adminApi } from "../api/admin";

function AttachmentLink({ id, fileName }: { id: number; fileName: string }) {
  const handleClick = async (e: React.MouseEvent) => {
    e.preventDefault();
    const url = await adminApi.supportRequestAttachmentUrl(id);
    window.open(url, "_blank", "noopener");
  };
  return <Link href="#" onClick={handleClick}>{fileName}</Link>;
}

export function SupportRequestsPage() {
  const { data: requests, isLoading } = useQuery({ queryKey: ["support-requests"], queryFn: adminApi.supportRequests });

  if (isLoading || !requests) return <CircularProgress />;

  return (
    <>
      <Typography variant="h5" fontWeight={700} gutterBottom>Support requests</Typography>
      {requests.length === 0 ? (
        <Typography variant="body2" color="text.secondary">No support requests yet.</Typography>
      ) : (
        <TableContainer component={Paper}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Date</TableCell>
                <TableCell>Name</TableCell>
                <TableCell>Email</TableCell>
                <TableCell>Message</TableCell>
                <TableCell>Attachment</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {requests.map((r) => (
                <TableRow key={r.id}>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>{new Date(r.createdAt).toLocaleString()}</TableCell>
                  <TableCell>{r.name ?? "—"}</TableCell>
                  <TableCell>{r.email}</TableCell>
                  <TableCell sx={{ maxWidth: 420, whiteSpace: "pre-wrap" }}>{r.message}</TableCell>
                  <TableCell>
                    {r.hasAttachment
                      ? <AttachmentLink id={r.id} fileName={r.attachmentOriginalFileName ?? "attachment"} />
                      : "—"}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </>
  );
}
