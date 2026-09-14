import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Box, Button, List, ListItem, ListItemText, Stack, IconButton, Typography, Alert, Chip,
} from "@mui/material";
import ArrowUpwardIcon from "@mui/icons-material/ArrowUpward";
import ArrowDownwardIcon from "@mui/icons-material/ArrowDownward";
import ShuffleIcon from "@mui/icons-material/Shuffle";
import { useTranslation } from "react-i18next";
import { circlesApi } from "../../api/circles";
import type { CircleDetail } from "../../api/types";

export function PayoutOrderTab({ circle, onActivated }: { circle: CircleDetail; onActivated: () => void }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { data: members } = useQuery({ queryKey: ["members", circle.id], queryFn: () => circlesApi.members(circle.id) });
  const { data: order, refetch } = useQuery({ queryKey: ["payoutOrder", circle.id], queryFn: () => circlesApi.payoutOrder(circle.id) });

  const activeMembers = members?.filter((m) => m.isActive) ?? [];
  const [localOrder, setLocalOrder] = useState<{ memberId: number; memberName: string }[]>([]);

  useEffect(() => {
    if (order && order.length > 0) {
      setLocalOrder(order.map((o) => ({ memberId: o.memberId, memberName: o.memberName })));
    } else {
      setLocalOrder(activeMembers.map((m) => ({ memberId: m.id, memberName: m.name })));
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [order, members]);

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["payoutOrder", circle.id] });
    queryClient.invalidateQueries({ queryKey: ["members", circle.id] });
    queryClient.invalidateQueries({ queryKey: ["circle", circle.id] });
  };

  const saveManualMutation = useMutation({
    mutationFn: () => circlesApi.setManualOrder(circle.id, localOrder.map((o) => o.memberId)),
    onSuccess: invalidate,
  });

  const drawMutation = useMutation({
    mutationFn: () => circlesApi.runDraw(circle.id),
    onSuccess: async () => { invalidate(); await refetch(); },
  });

  const confirmMutation = useMutation({
    mutationFn: () => circlesApi.confirmOrder(circle.id),
    onSuccess: invalidate,
  });

  const resetMutation = useMutation({
    mutationFn: () => circlesApi.resetOrder(circle.id),
    onSuccess: invalidate,
  });

  const activateMutation = useMutation({
    mutationFn: () => circlesApi.activate(circle.id),
    onSuccess: onActivated,
  });

  const move = (index: number, direction: -1 | 1) => {
    const next = [...localOrder];
    const target = index + direction;
    if (target < 0 || target >= next.length) return;
    [next[index], next[target]] = [next[target], next[index]];
    setLocalOrder(next);
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

  if (activeMembers.length === 0) {
    return <Alert severity="info">{t("circle.addMember")} — add members first.</Alert>;
  }

  const hasOrder = (order?.length ?? 0) > 0;

  return (
    <Box>
      <Typography variant="h6" gutterBottom>{t("circle.payoutOrder")}</Typography>

      {!circle.payoutOrderConfirmed && (
        <Stack direction="row" spacing={1} sx={{ mb: 2 }}>
          <Button startIcon={<ShuffleIcon />} variant="outlined" onClick={() => drawMutation.mutate()}>{t("circle.runDraw")}</Button>
          {hasOrder && <Button color="warning" onClick={() => resetMutation.mutate()}>{t("circle.resetOrder")}</Button>}
        </Stack>
      )}

      <List>
        {localOrder.map((entry, index) => (
          <ListItem
            key={entry.memberId}
            sx={{ bgcolor: "background.paper", mb: 1, borderRadius: 2 }}
            secondaryAction={
              !circle.payoutOrderConfirmed && (
                <Stack direction="row">
                  <IconButton size="small" onClick={() => move(index, -1)} disabled={index === 0}><ArrowUpwardIcon fontSize="small" /></IconButton>
                  <IconButton size="small" onClick={() => move(index, 1)} disabled={index === localOrder.length - 1}><ArrowDownwardIcon fontSize="small" /></IconButton>
                </Stack>
              )
            }
          >
            <Chip label={index + 1} size="small" sx={{ mr: 2 }} />
            <ListItemText primary={entry.memberName} />
          </ListItem>
        ))}
      </List>

      {!circle.payoutOrderConfirmed ? (
        <Stack direction="row" spacing={1} sx={{ mt: 2 }}>
          <Button variant="outlined" onClick={() => saveManualMutation.mutate()}>{t("common.save")}</Button>
          <Button
            variant="contained"
            onClick={async () => { await saveManualMutation.mutateAsync(); await confirmMutation.mutateAsync(); }}
          >
            {t("circle.confirmOrder")}
          </Button>
        </Stack>
      ) : (
        <Alert severity="success" sx={{ mt: 2 }}>
          {t("circle.lockOrder")}
          <Button sx={{ ml: 2 }} variant="contained" onClick={() => activateMutation.mutate()}>{t("circle.activate")}</Button>
        </Alert>
      )}
    </Box>
  );
}
