import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Box, Button, List, ListItem, ListItemText, TextField, Stack, IconButton,
  Dialog, DialogTitle, DialogContent, DialogActions, Chip, Typography,
} from "@mui/material";
import DeleteIcon from "@mui/icons-material/PersonOff";
import HistoryIcon from "@mui/icons-material/History";
import { useTranslation } from "react-i18next";
import { Link as RouterLink } from "react-router-dom";
import { circlesApi } from "../../api/circles";
import type { CircleDetail } from "../../api/types";

export function MembersTab({ circle }: { circle: CircleDetail }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { data: members } = useQuery({ queryKey: ["members", circle.id], queryFn: () => circlesApi.members(circle.id) });

  const [open, setOpen] = useState(false);
  const [name, setName] = useState("");
  const [phone, setPhone] = useState("");
  const [email, setEmail] = useState("");

  const isDraft = circle.status === "Draft";

  const addMutation = useMutation({
    mutationFn: () => circlesApi.addMember(circle.id, { name, phone: phone || undefined, email: email || undefined }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["members", circle.id] });
      setOpen(false);
      setName(""); setPhone(""); setEmail("");
    },
  });

  const deactivateMutation = useMutation({
    mutationFn: (memberId: number) => circlesApi.deactivateMember(circle.id, memberId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["members", circle.id] }),
  });

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
        <Typography variant="h6">{t("circle.members")}</Typography>
        {isDraft && <Button variant="contained" size="small" onClick={() => setOpen(true)}>{t("circle.addMember")}</Button>}
      </Stack>

      <List>
        {members?.map((m) => (
          <ListItem
            key={m.id}
            secondaryAction={
              <Stack direction="row" spacing={1}>
                {m.payoutPosition && <Chip size="small" label={`#${m.payoutPosition}`} />}
                {!circle.payoutOrderConfirmed || circle.status !== "Draft" ? null : null}
                <IconButton component={RouterLink} to={`/circles/${circle.id}/members/${m.id}/history`} size="small">
                  <HistoryIcon fontSize="small" />
                </IconButton>
                {isDraft && m.isActive && (
                  <IconButton size="small" onClick={() => deactivateMutation.mutate(m.id)} title={t("circle.deactivate")}>
                    <DeleteIcon fontSize="small" />
                  </IconButton>
                )}
              </Stack>
            }
          >
            <ListItemText
              primary={m.name}
              secondary={[m.phone, m.email].filter(Boolean).join(" · ") || undefined}
              sx={{ opacity: m.isActive ? 1 : 0.5, textDecoration: m.isActive ? "none" : "line-through" }}
            />
          </ListItem>
        ))}
      </List>

      <Dialog open={open} onClose={() => setOpen(false)} fullWidth maxWidth="xs">
        <DialogTitle>{t("circle.addMember")}</DialogTitle>
        <DialogContent>
          <Stack spacing={2} sx={{ mt: 1 }}>
            <TextField label={t("circle.memberName")} value={name} onChange={(e) => setName(e.target.value)} autoFocus fullWidth required />
            <TextField label={t("circle.phone")} value={phone} onChange={(e) => setPhone(e.target.value)} fullWidth />
            <TextField label={t("circle.email")} value={email} onChange={(e) => setEmail(e.target.value)} fullWidth />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpen(false)}>{t("common.cancel")}</Button>
          <Button variant="contained" disabled={!name || addMutation.isPending} onClick={() => addMutation.mutate()}>{t("common.save")}</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
