import { useEffect, useState } from "react";
import { useQuery, useQueryClient, useMutation } from "@tanstack/react-query";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import {
  Alert, Box, Tabs, Tab, Typography, Chip, Stack, Button, Menu, MenuItem, IconButton,
  Dialog, DialogTitle, DialogContent, DialogContentText, DialogActions, Tooltip,
} from "@mui/material";
import MoreVertIcon from "@mui/icons-material/MoreVert";
import CloseIcon from "@mui/icons-material/Close";
import { useTranslation } from "react-i18next";
import { isRtl } from "../../i18n";
import { circlesApi } from "../../api/circles";
import { MembersTab } from "./MembersTab";
import { PayoutOrderTab } from "./PayoutOrderTab";
import { ScheduleTab } from "./ScheduleTab";
import { CurrentCycleTab } from "./CurrentCycleTab";
import { HistoryTab } from "./HistoryTab";
import { BasicInfoBlock } from "./BasicInfoBlock";
import { ActivateCircleButton } from "./ActivateCircleButton";
import { CircleTimeline } from "./CircleTimeline";

/** Basic info now lives in a shared box above the tabs (not its own tab) for every status. */
const DRAFT_TABS = ["members", "payoutOrder"] as const;
const ACTIVE_TABS = ["currentCycle", "schedule", "members", "history"] as const;

/** Mirrors CircleInfoCard's status-color mapping so the badge is consistent everywhere. */
const STATUS_COLOR: Record<string, "success" | "info" | "warning" | "default"> = {
  Active: "success",
  Draft: "info",
  Paused: "warning",
  Completed: "default",
  Cancelled: "default",
};

export function CircleOverviewPage() {
  const { circleId } = useParams();
  const id = Number(circleId);
  const { t, i18n } = useTranslation();
  const rtl = isRtl(i18n.language);
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [searchParams, setSearchParams] = useSearchParams();
  const [tab, setTab] = useState(0);
  const [menuAnchor, setMenuAnchor] = useState<null | HTMLElement>(null);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const { data: circle } = useQuery({ queryKey: ["circle", id], queryFn: () => circlesApi.detail(id) });
  // Same query key CurrentCycleTab uses for its own dashboard fetch, so this just reuses that
  // cache entry rather than firing a second network request — only needed once there's a cycle.
  const { data: dashboard } = useQuery({
    queryKey: ["dashboard", id],
    queryFn: () => circlesApi.dashboard(id),
    enabled: !!circle && circle.status !== "Draft",
  });
  // Same query key ScheduleTab uses, so this reuses that cache entry — needed here for the
  // month-by-month timeline dots under the header.
  const { data: schedule } = useQuery({
    queryKey: ["schedule", id],
    queryFn: () => circlesApi.schedule(id),
    enabled: !!circle && circle.status !== "Draft",
  });

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
      {/* Single shared header for every circle status — draft and active are not two
          separate pages/branches, only the status-driven bits (badge color, the
          pause/resume/cancel menu, the delete button) differ. */}
      <Stack direction="row" justifyContent="space-between" alignItems="flex-start" flexWrap="wrap" gap={1} sx={{ mb: 1 }}>
        {/* Full width so the organizer/date line below can stretch edge-to-edge — otherwise this
            Box only shrink-wraps its content and the date's "left" is relative to the name row's
            width, not the actual left edge of the page. */}
        <Box sx={{ width: "100%" }}>
          {/* Name, status badge, view-only badge and the actions menu trigger all share one row.
              The three-dot trigger is always shown to the organizer regardless of status — only
              its menu content differs (pause/resume/cancel while Active/Paused, Delete while
              Draft/anything else). There is no separate Delete button any more. */}
          <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap">
            <Typography variant="h5" fontWeight={700}>{circle.name}</Typography>
            <Chip size="small" color={STATUS_COLOR[circle.status]} label={t(`circle.${circle.status.toLowerCase()}`)} />
            {!canManage && <Chip size="small" variant="outlined" label={t("circle.viewOnly")} />}
            {canManage && (
              <>
                <IconButton size="small" onClick={(e) => setMenuAnchor(e.currentTarget)}>
                  <MoreVertIcon fontSize="small" />
                </IconButton>
                <Menu anchorEl={menuAnchor} open={!!menuAnchor} onClose={() => setMenuAnchor(null)}>
                  {isActive && <MenuItem onClick={() => { pauseMutation.mutate(); setMenuAnchor(null); }}>{t("circle.pause")}</MenuItem>}
                  {circle.status === "Paused" && <MenuItem onClick={() => { resumeMutation.mutate(); setMenuAnchor(null); }}>{t("circle.resume")}</MenuItem>}
                  {(isActive || circle.status === "Paused") && (
                    <MenuItem onClick={() => { cancelMutation.mutate(); setMenuAnchor(null); }}>{t("circle.cancel")}</MenuItem>
                  )}
                  {!isActive && circle.status !== "Paused" && (
                    <MenuItem disabled={!circle.canDelete} onClick={() => { setDeleteOpen(true); setMenuAnchor(null); }}>
                      {t("circle.deleteCircle")}
                    </MenuItem>
                  )}
                </Menu>
              </>
            )}
          </Stack>

          {/* Organizer + member count (mirrors the home-page circle card) and the creation date
              share one line. Physical order is locked left-to-right via a native `style` prop —
              not `sx` — because the RTL emotion cache (stylis-plugin-rtl -> cssjanus) rewrites any
              `direction: ltr` found in generated CSS back to `rtl`, silently undoing an sx-based
              lock in Arabic. Inline `style` bypasses that pipeline entirely. */}
          <Stack
            direction="row" justifyContent="space-between" alignItems="center" flexWrap="wrap" gap={1}
            sx={{ mt: 0.5 }} style={{ direction: "ltr" }}
          >
            <Typography variant="body2" color="text.secondary" sx={{ order: rtl ? 1 : 0 }}>
              {t("circle.organizer")} : {circle.organizerName} . {t("circle.members")}: {circle.memberCount}
              {dashboard && (
                <>
                  . {t("circle.installmentLabel")}: {circle.contributionAmount}
                  . {t("circle.currentRecipientLabel")}: {dashboard.recipientName}
                </>
              )}
            </Typography>
            <Tooltip
              title={
                <>
                  {t("circle.createdAt")}{" "}
                  {/* Isolated LTR so the day/month/year sequence can't get bidi-reordered inside
                      the surrounding Arabic tooltip text (recurring class of bug in this app). */}
                  <span style={{ unicodeBidi: "isolate", direction: "ltr" }}>
                    {(() => {
                      const d = new Date(circle.createdAt);
                      const dd = String(d.getDate()).padStart(2, "0");
                      const mm = String(d.getMonth() + 1).padStart(2, "0");
                      return `${dd}/${mm}/${d.getFullYear()}`;
                    })()}
                  </span>
                </>
              }
            >
              <Typography variant="caption" color="text.secondary" sx={{ order: rtl ? 0 : 1 }}>
                {new Date(circle.createdAt).toLocaleDateString(i18n.language, { year: "numeric", month: "long" })}
              </Typography>
            </Tooltip>
          </Stack>

          {/* Month-by-month progress line, on its own line under the name/organizer/amounts text
              — extra top margin so it reads as a distinct section rather than crowding the text
              above it. Only meaningful once there's a schedule (not for a Draft circle). */}
          {schedule && schedule.length > 0 && (
            <Box sx={{ mt: 3 }}>
              <CircleTimeline schedule={schedule} currentCycleId={dashboard?.cycleId} />
            </Box>
          )}
        </Box>

        {/* Close no longer lives at the top for any status — it only appears in the shared
            bottom action row below. */}
      </Stack>

      {error && <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError(null)}>{error}</Alert>}

      {/* The basic-info box sits once here, above every tab and below the timeline — shared by
          every circle status, rather than living inside a per-status tab. */}
      <BasicInfoBlock circle={circle} />

      {/* Traditional tab look — top corners only, a small gap between them (not touching, not the
          wide pill-like spacing from before either) — with a light tint and a bolder selected
          state so the section names read more prominently than a plain underline. */}
      <Tabs
        value={tab}
        onChange={(_, v) => changeTab(v)}
        variant="scrollable"
        allowScrollButtonsMobile
        sx={{
          mb: 3,
          borderBottom: "1px solid #eee",
          "& .MuiTabs-flexContainer": { gap: 0.5 },
          "& .MuiTab-root": {
            textTransform: "none",
            fontWeight: 600,
            borderTopLeftRadius: 8,
            borderTopRightRadius: 8,
            minHeight: 44,
            bgcolor: "action.hover",
            transition: "background-color 0.15s, color 0.15s",
          },
          "& .MuiTab-root.Mui-selected": {
            bgcolor: "primary.main",
            color: (theme) => theme.palette.primary.contrastText,
          },
        }}
      >
        {tabKeys.map((key) => <Tab key={key} label={t(`circle.${key}`)} />)}
      </Tabs>

      {isDraft && tab === 0 && <MembersTab circle={circle} />}
      {isDraft && tab === 1 && <PayoutOrderTab circle={circle} />}

      {!isDraft && tab === 0 && <CurrentCycleTab circle={circle} />}
      {!isDraft && tab === 1 && <ScheduleTab circleId={id} canManage={canManage} currency={circle.currency} myMemberId={circle.myMemberId} />}
      {!isDraft && tab === 2 && <MembersTab circle={circle} />}
      {!isDraft && tab === 3 && <HistoryTab circleId={id} currency={circle.currency} />}

      {/* Shared bottom action row for every circle status — Close always on the right, Activate
          (Draft only) always on the left, in both English and Arabic. Confirm-payout no longer
          lives here — it moved inside the Current Cycle tab so it only shows while that tab is
          open. Physical order is locked left-to-right via a native `style` prop (see note above
          on why `sx` doesn't survive the RTL cache). */}
      <Stack direction="row" justifyContent="space-between" sx={{ mt: 4 }} style={{ direction: "ltr" }}>
        <Button
          variant="outlined"
          startIcon={<CloseIcon />}
          onClick={() => navigate("/circles")}
          sx={{ order: 1 }}
          style={{ direction: rtl ? "rtl" : "ltr" }}
        >
          {t("common.close")}
        </Button>
        {/* Always rendered (even empty) so Close stays pinned to the right — a lone flex child
            under justifyContent="space-between" would otherwise drift to the start instead of
            staying opposite this slot. */}
        <Box sx={{ order: 0 }}>
          {canManage && isDraft && <ActivateCircleButton circleId={id} onActivated={invalidateCircle} />}
        </Box>
      </Stack>

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
