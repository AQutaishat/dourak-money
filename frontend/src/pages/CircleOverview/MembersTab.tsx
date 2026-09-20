import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Alert, Box, Button, Chip, Dialog, DialogActions, DialogContent, DialogContentText, DialogTitle,
  IconButton, List, ListItem, Stack, Tooltip, Typography,
} from "@mui/material";
import DeleteIcon from "@mui/icons-material/DeleteOutline";
import HistoryIcon from "@mui/icons-material/History";
import PersonAddIcon from "@mui/icons-material/PersonAdd";
import RefreshIcon from "@mui/icons-material/Replay";
import { useTranslation } from "react-i18next";
import { Link as RouterLink } from "react-router-dom";
import { circlesApi } from "../../api/circles";
import type { CircleDetail } from "../../api/types";
import { isRtl } from "../../i18n";
import { AddMemberDialog } from "./AddMemberDialog";
import { InvitationStatusChip } from "./StatusChip";

export function MembersTab({ circle }: { circle: CircleDetail }) {
  const { t, i18n } = useTranslation();
  const rtl = isRtl(i18n.language);
  const queryClient = useQueryClient();
  const { data: members } = useQuery({ queryKey: ["members", circle.id], queryFn: () => circlesApi.members(circle.id) });

  const [open, setOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [removeTarget, setRemoveTarget] = useState<{ id: number; name: string } | null>(null);

  const isDraft = circle.status === "Draft";
  // Members get the same view but no controls (prompt02 "Member visibility").
  const canManage = circle.isOrganizer;
  const alreadyAMember = !!circle.myMemberId;

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["members", circle.id] });
    queryClient.invalidateQueries({ queryKey: ["circle", circle.id] });
  };

  const reinviteMutation = useMutation({
    mutationFn: (memberId: number) => circlesApi.reinviteMember(circle.id, memberId),
    onSuccess: invalidate,
  });

  const addSelfMutation = useMutation({
    mutationFn: () => circlesApi.addSelfAsMember(circle.id),
    onSuccess: invalidate,
    onError: () => setError(t("common.error")),
  });

  // prompt03 §1: while the circle is still a draft, a member row can be fully removed —
  // regardless of invitation status — not just deactivated/soft-excluded.
  const removeMutation = useMutation({
    mutationFn: (memberId: number) => circlesApi.removeMember(circle.id, memberId),
    onSuccess: () => { invalidate(); setRemoveTarget(null); },
    onError: () => setError(t("common.error")),
  });

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="center" flexWrap="wrap" gap={1} sx={{ mb: 2 }}>
        <Typography variant="h6">{t("circle.members")}</Typography>
        {canManage && isDraft && (
          <Stack direction="row" spacing={1}>
            {/* prompt02 §Draft circles: quick self-add shortcut, hidden once you're already in. */}
            {!alreadyAMember && (
              <Tooltip title={t("circle.addSelfAsMember")}>
                <Button size="small" startIcon={<PersonAddIcon />} onClick={() => addSelfMutation.mutate()}>
                  {t("circle.addSelfAsMember")}
                </Button>
              </Tooltip>
            )}
            <Button variant="contained" size="small" onClick={() => setOpen(true)}>{t("circle.addMember")}</Button>
          </Stack>
        )}
      </Stack>

      {error && <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>{error}</Alert>}
      {!canManage && <Alert severity="info" sx={{ mb: 2 }}>{t("circle.viewOnlyNote")}</Alert>}

      <List>
        {members?.map((m) => {
          // Declined members are shown struck through exactly like deactivated ones (§5).
          const excluded = !m.isParticipating;
          return (
            <ListItem
              key={m.id}
              secondaryAction={
                <Stack direction="row" spacing={1} alignItems="center">
                  {/* No badge once accepted — the invitation is a non-event at that point. */}
                  {m.invitationStatus !== "Accepted" && <InvitationStatusChip status={m.invitationStatus} />}

                  {/* History only makes sense once the circle has actually started collecting. */}
                  {circle.status === "Active" && (
                    <Tooltip title={t("circle.viewHistory")}>
                      <IconButton component={RouterLink} to={`/circles/${circle.id}/members/${m.id}/history`} size="small">
                        <HistoryIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  )}

                  {canManage && isDraft && m.invitationStatus === "Declined" && (
                    <Tooltip title={t("circle.reinvite")}>
                      <IconButton size="small" onClick={() => reinviteMutation.mutate(m.id)}>
                        <RefreshIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  )}

                  {/* prompt03 §1: outright removal, pre-activation only, regardless of
                      invitation status (pending/accepted/declined/not-invited all qualify). */}
                  {canManage && isDraft && (
                    <Tooltip title={t("circle.removeMember")}>
                      <IconButton size="small" color="error" onClick={() => setRemoveTarget({ id: m.id, name: m.name })}>
                        <DeleteIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  )}
                </Stack>
              }
            >
              <Stack
                direction="row"
                spacing={2}
                alignItems="center"
                sx={{ opacity: excluded ? 0.5 : 1, textDecoration: excluded ? "line-through" : "none", paddingInlineEnd: "20px", flex: 1 }}
              >
                <Box sx={{ width: 40, flexShrink: 0 }}>
                  {m.payoutPosition && <Chip size="small" label={`#${m.payoutPosition}`} />}
                </Box>
                <Typography sx={{ flex: 1, minWidth: 0 }} noWrap>{m.name || m.email || ""}</Typography>
                <Typography sx={{ flex: 1, minWidth: 0 }} color="text.secondary" noWrap style={{ textAlign: rtl ? "right" : "left" }}>
                  {m.name ? (m.email ?? "") : ""}
                </Typography>
              </Stack>
            </ListItem>
          );
        })}
      </List>

      <AddMemberDialog
        open={open}
        onClose={() => setOpen(false)}
        circleId={circle.id}
        circleName={circle.name}
        organizerName={circle.organizerName}
      />

      <Dialog open={!!removeTarget} onClose={() => setRemoveTarget(null)} fullWidth maxWidth="xs">
        <DialogTitle>{t("circle.removeMember")}</DialogTitle>
        <DialogContent>
          <DialogContentText>
            {t("circle.removeMemberConfirm", { name: removeTarget?.name ?? "" })}
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setRemoveTarget(null)}>{t("common.cancel")}</Button>
          <Button
            color="error"
            variant="contained"
            disabled={removeMutation.isPending}
            onClick={() => removeTarget && removeMutation.mutate(removeTarget.id)}
          >
            {t("common.delete")}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
