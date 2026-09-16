import { useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Alert, Box, Button, Chip, CircularProgress, Collapse, Dialog, DialogActions, DialogContent,
  DialogTitle, IconButton, List, ListItem, ListItemText, Paper, Snackbar, Stack, Table, TableBody,
  TableCell, TableContainer, TableHead, TableRow, TextField, Typography,
} from "@mui/material";
import KeyboardArrowDownIcon from "@mui/icons-material/KeyboardArrowDown";
import KeyboardArrowUpIcon from "@mui/icons-material/KeyboardArrowUp";
import axios from "axios";
import { adminApi } from "../api/admin";
import type { AdminUser } from "../api/types";

/** Row detail: which circles this user organizes / belongs to (spec: "see all circles created
 * or the user is member of"). Collapsible so the main table stays scannable. */
function UserRow({ user, onChanged }: { user: AdminUser; onChanged: (msg: string) => void }) {
  const [expanded, setExpanded] = useState(false);
  const [resetOpen, setResetOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [newPassword, setNewPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const handleResetPassword = async () => {
    setError(null);
    setBusy(true);
    try {
      await adminApi.resetPassword(user.userId, newPassword);
      setResetOpen(false);
      setNewPassword("");
      onChanged(`Password reset for ${user.displayLabel}.`);
    } catch (err) {
      setError(axios.isAxiosError(err) ? err.response?.data?.errors?.join(" ") ?? "Failed to reset password." : "Failed to reset password.");
    } finally {
      setBusy(false);
    }
  };

  const handleToggleActive = async () => {
    setBusy(true);
    try {
      if (user.isActive) await adminApi.deactivate(user.userId);
      else await adminApi.activate(user.userId);
      onChanged(user.isActive ? `${user.displayLabel} deactivated.` : `${user.displayLabel} activated.`);
    } finally {
      setBusy(false);
    }
  };

  const handleDelete = async () => {
    setError(null);
    setBusy(true);
    try {
      await adminApi.deleteUser(user.userId);
      setDeleteOpen(false);
      onChanged(`${user.displayLabel} deleted.`);
    } catch (err) {
      setError(axios.isAxiosError(err) ? err.response?.data?.errors?.join(" ") ?? "Failed to delete user." : "Failed to delete user.");
    } finally {
      setBusy(false);
    }
  };

  return (
    <>
      <TableRow>
        <TableCell>
          <IconButton size="small" onClick={() => setExpanded(!expanded)}>
            {expanded ? <KeyboardArrowUpIcon /> : <KeyboardArrowDownIcon />}
          </IconButton>
        </TableCell>
        <TableCell>{user.displayLabel}</TableCell>
        <TableCell>{user.email}</TableCell>
        <TableCell>{user.phone ?? "—"}</TableCell>
        <TableCell>
          <Chip
            size="small"
            label={user.emailConfirmed ? "Verified" : "Unverified"}
            sx={{
              color: user.emailConfirmed ? "success.dark" : "warning.dark",
              bgcolor: user.emailConfirmed ? "success.light" : "warning.light",
              fontWeight: 600,
            }}
          />
        </TableCell>
        <TableCell>
          <Chip
            size="small"
            label={user.isActive ? "Active" : "Deactivated"}
            sx={{
              color: user.isActive ? "success.dark" : "text.secondary",
              bgcolor: user.isActive ? "success.light" : "grey.300",
              fontWeight: 600,
            }}
          />
        </TableCell>
        <TableCell align="right">
          <Stack direction="row" spacing={1} justifyContent="flex-end">
            <Button size="small" onClick={() => setResetOpen(true)} disabled={busy}>Reset password</Button>
            <Button size="small" onClick={handleToggleActive} disabled={busy}>
              {user.isActive ? "Deactivate" : "Activate"}
            </Button>
            <Button size="small" color="error" onClick={() => setDeleteOpen(true)} disabled={busy}>Delete</Button>
          </Stack>
        </TableCell>
      </TableRow>
      <TableRow>
        <TableCell colSpan={7} sx={{ py: 0, borderBottom: expanded ? undefined : "none" }}>
          <Collapse in={expanded} unmountOnExit>
            <Box sx={{ py: 2, display: "flex", gap: 4 }}>
              <Box sx={{ minWidth: 220 }}>
                <Typography variant="subtitle2" gutterBottom>Circles organized ({user.organizedCircles.length})</Typography>
                {user.organizedCircles.length === 0 ? (
                  <Typography variant="body2" color="text.secondary">None</Typography>
                ) : (
                  <List dense disablePadding>
                    {user.organizedCircles.map((c) => (
                      <ListItem key={c.circleId} disableGutters>
                        <ListItemText primary={c.name} secondary={c.status} />
                      </ListItem>
                    ))}
                  </List>
                )}
              </Box>
              <Box sx={{ minWidth: 220 }}>
                <Typography variant="subtitle2" gutterBottom>Member of ({user.memberCircles.length})</Typography>
                {user.memberCircles.length === 0 ? (
                  <Typography variant="body2" color="text.secondary">None</Typography>
                ) : (
                  <List dense disablePadding>
                    {user.memberCircles.map((c) => (
                      <ListItem key={c.circleId} disableGutters>
                        <ListItemText primary={c.name} secondary={c.status} />
                      </ListItem>
                    ))}
                  </List>
                )}
              </Box>
            </Box>
          </Collapse>
        </TableCell>
      </TableRow>

      <Dialog open={resetOpen} onClose={() => setResetOpen(false)} maxWidth="xs" fullWidth>
        <DialogTitle>Reset password for {user.displayLabel}</DialogTitle>
        <DialogContent>
          {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
          <TextField
            autoFocus
            label="New password"
            type="text"
            fullWidth
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            helperText="At least 8 characters, including a letter and a digit."
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setResetOpen(false)}>Cancel</Button>
          <Button variant="contained" onClick={handleResetPassword} disabled={busy || newPassword.length < 8}>Reset</Button>
        </DialogActions>
      </Dialog>

      <Dialog open={deleteOpen} onClose={() => setDeleteOpen(false)} maxWidth="xs" fullWidth>
        <DialogTitle>Delete {user.displayLabel}?</DialogTitle>
        <DialogContent>
          {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
          <Typography variant="body2">
            This permanently deletes the account. If they organize any circle, deletion is
            refused until that circle is reassigned or removed. Memberships elsewhere are
            detached, not deleted.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteOpen(false)}>Cancel</Button>
          <Button variant="contained" color="error" onClick={handleDelete} disabled={busy}>Delete</Button>
        </DialogActions>
      </Dialog>
    </>
  );
}

export function UsersPage() {
  const queryClient = useQueryClient();
  const { data: users, isLoading } = useQuery({ queryKey: ["admin-users"], queryFn: adminApi.users });
  const [toast, setToast] = useState<string | null>(null);

  const handleChanged = (msg: string) => {
    setToast(msg);
    void queryClient.invalidateQueries({ queryKey: ["admin-users"] });
    void queryClient.invalidateQueries({ queryKey: ["admin-stats"] });
  };

  if (isLoading || !users) return <CircularProgress />;

  return (
    <>
      <Typography variant="h5" fontWeight={700} gutterBottom>Users</Typography>
      <TableContainer component={Paper}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell />
              <TableCell>Name</TableCell>
              <TableCell>Email</TableCell>
              <TableCell>Phone</TableCell>
              <TableCell>Email status</TableCell>
              <TableCell>Account status</TableCell>
              <TableCell align="right">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {users.map((u) => (
              <UserRow key={u.userId} user={u} onChanged={handleChanged} />
            ))}
          </TableBody>
        </Table>
      </TableContainer>
      <Snackbar open={!!toast} autoHideDuration={4000} onClose={() => setToast(null)} message={toast} />
    </>
  );
}
