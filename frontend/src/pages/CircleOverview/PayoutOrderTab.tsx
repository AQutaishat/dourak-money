import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Box, Button, List, ListItem, ListItemText, Stack, IconButton, Typography, Alert, Chip, Tooltip,
} from "@mui/material";
import ArrowUpwardIcon from "@mui/icons-material/ArrowUpward";
import ArrowDownwardIcon from "@mui/icons-material/ArrowDownward";
import ShuffleIcon from "@mui/icons-material/Shuffle";
import { useTranslation } from "react-i18next";
import { circlesApi } from "../../api/circles";
import type { CircleDetail } from "../../api/types";

export function PayoutOrderTab({ circle }: { circle: CircleDetail }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { data: members } = useQuery({ queryKey: ["members", circle.id], queryFn: () => circlesApi.members(circle.id) });
  const { data: order, refetch } = useQuery({ queryKey: ["payoutOrder", circle.id], queryFn: () => circlesApi.payoutOrder(circle.id) });

  // Declined and not-yet-accepted members are excluded from the order (prompt02 §5).
  const participatingMembers = members?.filter((m) => m.isParticipating) ?? [];
  const [localOrder, setLocalOrder] = useState<{ memberId: number; memberName: string }[]>([]);
  // The "reset to original order" button only makes sense once the organizer has actually
  // touched the order (arrows or a draw) — not for the default order members get automatically
  // as they're added.
  const [reordered, setReordered] = useState(false);
  // Snapshot of the order right before the first arrow move / draw, so "Reset to original
  // order" can restore exactly that — not just wipe the order back to empty.
  const [originalOrder, setOriginalOrder] = useState<{ memberId: number; memberName: string }[] | null>(null);

  useEffect(() => {
    if (order && order.length > 0) {
      setLocalOrder(order.map((o) => ({ memberId: o.memberId, memberName: o.memberName })));
    } else {
      setLocalOrder(participatingMembers.map((m) => ({ memberId: m.id, memberName: m.name })));
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [order, members]);

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["payoutOrder", circle.id] });
    queryClient.invalidateQueries({ queryKey: ["members", circle.id] });
    queryClient.invalidateQueries({ queryKey: ["circle", circle.id] });
  };

  const moveMutation = useMutation({
    mutationFn: ({ memberId, direction }: { memberId: number; direction: -1 | 1 }) =>
      circlesApi.movePayoutPosition(circle.id, memberId, direction),
    onSuccess: invalidate,
  });

  const drawMutation = useMutation({
    mutationFn: () => circlesApi.runDraw(circle.id),
    onSuccess: async () => { invalidate(); await refetch(); },
  });

  const resetMutation = useMutation({
    mutationFn: () => circlesApi.setManualOrder(circle.id, (originalOrder ?? []).map((o) => o.memberId)),
    onSuccess: () => { invalidate(); setReordered(false); setOriginalOrder(null); },
  });

  const captureOriginalOrderIfNeeded = () => {
    if (!reordered) setOriginalOrder(localOrder);
  };

  const move = (index: number, direction: -1 | 1) => {
    const next = [...localOrder];
    const target = index + direction;
    if (target < 0 || target >= next.length) return;
    captureOriginalOrderIfNeeded();
    [next[index], next[target]] = [next[target], next[index]];
    // Optimistic local reorder for instant feedback, persisted immediately server-side —
    // there is no separate Save step while the circle is still a Draft.
    setLocalOrder(next);
    setReordered(true);
    moveMutation.mutate({ memberId: next[target].memberId, direction });
  };

  if (circle.status !== "Draft") {
    return (
      <Box>
        <Typography variant="h6" gutterBottom>{t("circle.payoutOrder")}</Typography>
        <List>
          {order?.map((o) => (
            <ListItem key={o.memberId}><ListItemText primary={`${o.position}. ${o.memberName}`} /></ListItem>
          ))}
        </List>
      </Box>
    );
  }

  if (participatingMembers.length === 0) {
    return <Alert severity="info">{t("circle.addMember")}</Alert>;
  }

  const canManage = circle.isOrganizer;

  return (
    <Box>
      <Typography variant="h6" gutterBottom>{t("circle.payoutOrder")}</Typography>

      {canManage && (
        <Stack direction="row" spacing={1} sx={{ mb: 2 }}>
          <Button
            startIcon={<ShuffleIcon />}
            variant="outlined"
            onClick={() => { captureOriginalOrderIfNeeded(); setReordered(true); drawMutation.mutate(); }}
          >
            {t("circle.runDraw")}
          </Button>
          {reordered && <Button color="warning" onClick={() => resetMutation.mutate()}>{t("circle.resetOrder")}</Button>}
        </Stack>
      )}

      <List>
        {localOrder.map((entry, index) => (
          <ListItem
            key={entry.memberId}
            sx={{ bgcolor: "background.paper", mb: 1, borderRadius: 2 }}
            secondaryAction={
              canManage && (
                <Stack direction="row">
                  <Tooltip title={t("circle.moveUp")}>
                    <span>
                      <IconButton size="small" onClick={() => move(index, -1)} disabled={index === 0}><ArrowUpwardIcon fontSize="small" /></IconButton>
                    </span>
                  </Tooltip>
                  <Tooltip title={t("circle.moveDown")}>
                    <span>
                      <IconButton size="small" onClick={() => move(index, 1)} disabled={index === localOrder.length - 1}><ArrowDownwardIcon fontSize="small" /></IconButton>
                    </span>
                  </Tooltip>
                </Stack>
              )
            }
          >
            <Chip label={index + 1} size="small" sx={{ marginInlineEnd: 2 }} />
            <ListItemText primary={entry.memberName} />
          </ListItem>
        ))}
      </List>
    </Box>
  );
}
