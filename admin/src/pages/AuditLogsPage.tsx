import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  CircularProgress, MenuItem, Paper, Stack, Table, TableBody, TableCell,
  TableContainer, TableHead, TablePagination, TableRow, TextField, Typography,
} from "@mui/material";
import { adminApi } from "../api/admin";

/** Admin audit trail — see AuditLoggingBehavior/IAuditLogger on the backend for what gets logged. */
export function AuditLogsPage() {
  const [page, setPage] = useState(0); // MUI TablePagination is 0-based
  const [pageSize, setPageSize] = useState(50);
  const [userId, setUserId] = useState("");
  const [action, setAction] = useState("");
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");

  const { data: actions } = useQuery({ queryKey: ["audit-log-actions"], queryFn: adminApi.auditLogActions });

  const filters = {
    page: page + 1,
    pageSize,
    userId: userId || undefined,
    action: action || undefined,
    dateFrom: dateFrom ? new Date(dateFrom).toISOString() : undefined,
    // End-of-day so the "to" date filter is inclusive.
    dateTo: dateTo ? new Date(`${dateTo}T23:59:59.999`).toISOString() : undefined,
  };

  const { data, isLoading } = useQuery({
    queryKey: ["audit-logs", filters],
    queryFn: () => adminApi.auditLogs(filters),
  });

  const resetToFirstPage = () => setPage(0);

  return (
    <>
      <Typography variant="h5" fontWeight={700} gutterBottom>Audit trail</Typography>

      <Paper sx={{ p: 2, mb: 2 }}>
        <Stack direction="row" spacing={2} flexWrap="wrap" useFlexGap>
          <TextField
            label="User ID"
            size="small"
            value={userId}
            onChange={(e) => { setUserId(e.target.value); resetToFirstPage(); }}
            sx={{ minWidth: 220 }}
          />
          <TextField
            label="Action"
            size="small"
            select
            value={action}
            onChange={(e) => { setAction(e.target.value); resetToFirstPage(); }}
            sx={{ minWidth: 220 }}
          >
            <MenuItem value="">All actions</MenuItem>
            {(actions ?? []).map((a) => (
              <MenuItem key={a} value={a}>{a}</MenuItem>
            ))}
          </TextField>
          <TextField
            label="From"
            type="date"
            size="small"
            value={dateFrom}
            onChange={(e) => { setDateFrom(e.target.value); resetToFirstPage(); }}
            slotProps={{ inputLabel: { shrink: true } }}
          />
          <TextField
            label="To"
            type="date"
            size="small"
            value={dateTo}
            onChange={(e) => { setDateTo(e.target.value); resetToFirstPage(); }}
            slotProps={{ inputLabel: { shrink: true } }}
          />
        </Stack>
      </Paper>

      {isLoading || !data ? (
        <CircularProgress />
      ) : (
        <TableContainer component={Paper}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Date/Time</TableCell>
                <TableCell>User</TableCell>
                <TableCell>Action</TableCell>
                <TableCell>Details</TableCell>
                <TableCell>IP</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {data.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={5}>
                    <Typography variant="body2" color="text.secondary">No matching audit log entries.</Typography>
                  </TableCell>
                </TableRow>
              )}
              {data.items.map((log) => (
                <TableRow key={log.id}>
                  <TableCell sx={{ whiteSpace: "nowrap" }}>{new Date(log.createdAt).toLocaleString()}</TableCell>
                  <TableCell>{log.userDisplayName ?? log.userId ?? "—"}</TableCell>
                  <TableCell>{log.action}</TableCell>
                  <TableCell sx={{ maxWidth: 420, whiteSpace: "pre-wrap" }}>{log.details ?? "—"}</TableCell>
                  <TableCell>{log.ipAddress ?? "—"}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
          <TablePagination
            component="div"
            count={data.totalCount}
            page={page}
            onPageChange={(_, newPage) => setPage(newPage)}
            rowsPerPage={pageSize}
            onRowsPerPageChange={(e) => { setPageSize(parseInt(e.target.value, 10)); setPage(0); }}
            rowsPerPageOptions={[25, 50, 100]}
          />
        </TableContainer>
      )}
    </>
  );
}
