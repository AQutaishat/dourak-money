import { useEffect, useState } from "react";
import { useQuery, useQueryClient, useMutation } from "@tanstack/react-query";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import {
  Alert, Box, Tabs, Tab, Typography, Chip, Stack, Button, Menu, MenuItem, Paper,
  Dialog, DialogTitle, DialogContent, DialogContentText, DialogActions, Tooltip,
} from "@mui/material";
import MoreVertIcon from "@mui/icons-material/MoreVert";
import DeleteIcon from "@mui/icons-material/DeleteOutline";
import CloseIcon from "@mui/icons-material/Close";
import { useTranslation } from "react-i18next";
import { circlesApi } from "../../api/circles";
import { MembersTab } from "./MembersTab";
import { PayoutOrderTab } from "./PayoutOrderTab";
import { ScheduleTab } from "./ScheduleTab";
import { CurrentCycleTab } from "./CurrentCycleTab";
import { HistoryTab } from "./HistoryTab";
import { BasicInfoBlock } from "./BasicInfoBlock";
import { ActivateCircleButton } from "./ActivateCircleButton";

/** Draft tab order: Basic Info first (prompt02 §Draft circles). */
const DRAFT_TABS = ["basicInfo", "members", "payoutOrder"] as const;
const ACTIVE_TABS = ["currentCycle", "schedule", "members", "history"] as const;

export function CircleOverviewPage() {
  const { circleId } = useParams();
  const id = Number(circleId);
  const { t } = useTranslation();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [searchParams, setSearchParams] = useSearchParams();
  const [tab, setTab] = useState(0);
  const [menuAnchor, setMenuAnchor] = useState<null | HTMLElement>(null);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const { data: circle } = useQuery({ queryKey: ["circle", id], queryFn: () => circlesApi.detail(id) });

  /**
   * prompt02 §Active circles: Back and Close on the member-history view must return to the
   * *Members* tab, not the circle's default tab — so the tab is addressable via ?tab=members.
   */
  const requestedTab = searchParams.get("tab");
  useEffect(() => {
    if (!circle || !requestedTab) return;
    const tabs = circle.status === "Draft" ? DRAFT_TABS : ACTIVE_TABS;
    const index = tabs.indexOf(requestedTab as never);
    if (index >= 0) setTab(index);
  }, [circle, requestedTab]);

  const invalidateCircle = () => queryClient.invalidateQueries({ queryKey: ["circle", id] });
  const pauseMutation = useMutation({ mutationFn: () => circlesApi.pause(id), onSuccess: invalidateCircle });
  const resumeMutation = useMutation({ mutationFn: () => circlesApi.resume(id), onSuccess: invalidateCircle });
  const cancelMutation = useMutation({ mutationFn: () => circlesApi.cancel(id), onSuccess: invalidateCircle });

  const deleteMutation = useMutation({
    mutationFn: () => circlesApi.remove(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["circles"] });
      navigate("/circles");
    },
    onError: () => setError(t("circle.deleteCircleBlocked")),
  });

  if (!circle) return null;

  const isDraft = circle.status === "Draft";
  const isActive = circle.status === "Active";
  const canManage = circle.isOrganizer;
  const tabKeys = isDraft ? DRAFT_TABS : ACTIVE_TABS;

  const changeTab = (index: number) => {
    setTab(index);
    // Keep the URL in step so a tab survives a refresh and back-navigation.
    setSearchParams({ tab: tabKeys[index] }, { replace: true });
  };

  return (
    <Box>
      <Stack direction="row" justifyContent="space-between" alignItems="flex-start" flexWrap="wrap" gap={1} sx={{ mb: 1 }}>
        <Box>
          <Typography variant="h5" fontWeight={700}>{circle.name}</Typography>
          <Stack direction="row" spacing={1} alignItems="center" sx={{ mt: 0.5 }} flexWrap="wrap">
            <Chip
              size="small"
              color={isActive ? "success" : isDraft ? "info" : "default"}
              label={t(`circle.${circle.status.toLowerCase()}`)}
            />
            {!canManage && <Chip size="small" variant="outlined" label={t("circle.viewOnly")} />}
            <Typography variant="body2" color="text.secondary">
              {circle.contributionAmount} {circle.currency} · {circle.memberCount} {t("circle.members")}
            </Typography>
          </Stack>
        </Box>

        <Stack direction="row" spacing={1} alignItems="center">
          {canManage && isDraft && (
            <Tooltip title={t("circle.deleteCircle")}>
              <span>
                <Button color="error" startIcon={<DeleteIcon />} disabled={!circle.canDelete} onClick={() => setDeleteOpen(true)}>
                  {t("circle.deleteCircle")}
                </Button>
              </span>
            </Tooltip>
          )}
          {canManage && (isActive || circle.status === "Paused") && (
            <>
              {/* Renamed from "Confirm" — this menu pauses/resumes/cancels (prompt02 §Active circles). */}
              <Button onClick={(e) => setMenuAnchor(e.currentTarget)} endIcon={<MoreVertIcon />}>{t("circle.actions")}</Button>
              <Menu anchorEl={menuAnchor} open={!!menuAnchor} onClose={() => setMenuAnchor(null)}>
                {isActive && <MenuItem onClick={() => { pauseMutation.mutate(); setMenuAnchor(null); }}>{t("circle.pause")}</MenuItem>}
                {circle.status === "Paused" && <MenuItem onClick={() => { resumeMutation.mutate(); setMenuAnchor(null); }}>{t("circle.resume")}</MenuItem>}
                <MenuItem onClick={() => { cancelMutation.mutate(); setMenuAnchor(null); }}>{t("circle.cancel")}</MenuItem>
              </Menu>
            </>
          )}
          {/* prompt03 §1: on a draft circle the close button moves to its own row at the
              bottom of the page so it can't be confused with / mis-clicked next to Activate.
              Active circles keep it here (unchanged from Phase 2). */}
          {!isDraft && (
            <Button startIcon={<CloseIcon />} onClick={() => navigate("/circles")}>{t("common.close")}</Button>
          )}
        </Stack>
      </Stack>

      {error && <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>{error}</Alert>}

      <Tabs value={tab} onChange={(_, v) => changeTab(v)} variant="scrollable" allowScrollButtonsMobile sx={{ mb: 3, borderBottom: "1px solid #eee" }}>
        {tabKeys.map((key) => <Tab key={key} label={t(`circle.${key}`)} />)}
      </Tabs>

      {isDraft && tab === 0 && <BasicInfoBlock circle={circle} />}
      {isDraft && tab === 1 && <MembersTab circle={circle} />}
      {isDraft && tab === 2 && <PayoutOrderTab circle={circle} />}

      {!isDraft && tab === 0 && <CurrentCycleTab circle={circle} />}
      {!isDraft && tab === 1 && <ScheduleTab circleId={id} currency={circle.currency} />}
      {!isDraft && tab === 2 && <MembersTab circle={circle} />}
      {!isDraft && tab === 3 && <HistoryTab circleId={id} currency={circle.currency} />}

      {/* Persistent activate action beneath all tabs on a draft circle (prompt02 §Draft circles).
          prompt03 §1: normal-sized button aligned to one side, not a full-width block. */}
      {isDraft && canManage && (
        <Paper elevation={2} sx={{ position: "sticky", bottom: 0, mt: 4, p: 2, zIndex: 2 }}>
          <Stack direction="row" justifyContent="flex-end">
            <ActivateCircleButton circleId={id} onActivated={invalidateCircle} />
          </Stack>
        </Paper>
      )}

      {/* prompt03 §1: Close lives on its own row at the bottom, separate from Activate above,
          so the two actions can't be confused or mis-clicked. */}
      {isDraft && (
        <Stack direction="row" justifyContent="flex-end" sx={{ mt: 2 }}>
          <Button variant="outlined" startIcon={<CloseIcon />} onClick={() => navigate("/circles")}>{t("common.close")}</Button>
        </Stack>
      )}

      {/* Close button at the bottom of the active circle details page. */}
      {!isDraft && (
        <Stack direction="row" justifyContent="flex-end" sx={{ mt: 4 }}>
          <Button variant="outlined" startIcon={<CloseIcon />} onClick={() => navigate("/circles")}>{t("common.close")}</Button>
        </Stack>
      )}

      <Dialog open={deleteOpen} onClose={() => setDeleteOpen(false)} fullWidth maxWidth="xs">
        <DialogTitle>{t("circle.deleteCircle")}</DialogTitle>
        <DialogContent>
          <DialogContentText>{t("circle.deleteCircleConfirm")}</DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteOpen(false)}>{t("common.cancel")}</Button>
          <Button color="error" variant="contained" disabled={deleteMutation.isPending} onClick={() => deleteMutation.mutate()}>
            {t("common.delete")}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
