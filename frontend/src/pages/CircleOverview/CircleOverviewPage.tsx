import { useState } from "react";
import { useQuery, useQueryClient, useMutation } from "@tanstack/react-query";
import { useParams } from "react-router-dom";
import { Box, Tabs, Tab, Typography, Chip, Stack, Button, Menu, MenuItem } from "@mui/material";
import MoreVertIcon from "@mui/icons-material/MoreVert";
import { useTranslation } from "react-i18next";
import { circlesApi } from "../../api/circles";
import { MembersTab } from "./MembersTab";
import { PayoutOrderTab } from "./PayoutOrderTab";
import { ScheduleTab } from "./ScheduleTab";
import { CurrentCycleTab } from "./CurrentCycleTab";
import { HistoryTab } from "./HistoryTab";

export function CircleOverviewPage() {
  const { circleId } = useParams();
  const id = Number(circleId);
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [tab, setTab] = useState(0);
  const [menuAnchor, setMenuAnchor] = useState<null | HTMLElement>(null);

  const { data: circle } = useQuery({ queryKey: ["circle", id], queryFn: () => circlesApi.detail(id) });

  const invalidateCircle = () => queryClient.invalidateQueries({ queryKey: ["circle", id] });
  const pauseMutation = useMutation({ mutationFn: () => circlesApi.pause(id), onSuccess: invalidateCircle });
  const resumeMutation = useMutation({ mutationFn: () => circlesApi.resume(id), onSuccess: invalidateCircle });
  const cancelMutation = useMutation({ mutationFn: () => circlesApi.cancel(id), onSuccess: invalidateCircle });

  if (!circle) return null;

  const isDraft = circle.status === "Draft";
  const isActive = circle.status === "Active";

  const tabs = isDraft
    ? [t("circle.members"), t("circle.payoutOrder")]
    : [t("circle.currentCycle"), t("circle.schedule"), t("circle.members"), t("circle.history")];

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="flex-start" sx={{ mb: 1 }}>
        <Box>
          <Typography variant="h5" fontWeight={700}>{circle.name}</Typography>
          <Stack direction="row" spacing={1} alignItems="center" sx={{ mt: 0.5 }}>
            <Chip size="small" label={t(`circle.${circle.status.toLowerCase()}`)} />
            <Typography variant="body2" color="text.secondary">
              {circle.contributionAmount} {circle.currency} · {circle.memberCount} {t("circle.members")}
            </Typography>
          </Stack>
        </Box>
        {(isActive || circle.status === "Paused") && (
          <>
            <Button onClick={(e) => setMenuAnchor(e.currentTarget)} endIcon={<MoreVertIcon />}>{t("common.confirm")}</Button>
            <Menu anchorEl={menuAnchor} open={!!menuAnchor} onClose={() => setMenuAnchor(null)}>
              {isActive && <MenuItem onClick={() => { pauseMutation.mutate(); setMenuAnchor(null); }}>{t("circle.pause")}</MenuItem>}
              {circle.status === "Paused" && <MenuItem onClick={() => { resumeMutation.mutate(); setMenuAnchor(null); }}>{t("circle.resume")}</MenuItem>}
              <MenuItem onClick={() => { cancelMutation.mutate(); setMenuAnchor(null); }}>{t("circle.cancel")}</MenuItem>
            </Menu>
          </>
        )}
      </Stack>

      <Tabs value={tab} onChange={(_, v) => setTab(v)} sx={{ mb: 3, borderBottom: "1px solid #eee" }}>
        {tabs.map((label) => <Tab key={label} label={label} />)}
      </Tabs>

      {isDraft && tab === 0 && <MembersTab circle={circle} />}
      {isDraft && tab === 1 && <PayoutOrderTab circle={circle} onActivated={invalidateCircle} />}

      {!isDraft && tab === 0 && <CurrentCycleTab circleId={id} circleName={circle.name} currency={circle.currency} />}
      {!isDraft && tab === 1 && <ScheduleTab circleId={id} currency={circle.currency} />}
      {!isDraft && tab === 2 && <MembersTab circle={circle} />}
      {!isDraft && tab === 3 && <HistoryTab circleId={id} currency={circle.currency} />}
    </Box>
  );
}
