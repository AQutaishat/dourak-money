import { Box, Stack, Tooltip, Typography } from "@mui/material";
import { useTranslation } from "react-i18next";
import type { ScheduleCycle } from "../../api/types";

const DOT_SIZE = 14;
const CURRENT_RING_SIZE = 26;
const LINE_THICKNESS = 6;
// Fixed vertical position of the line — every dot (including the current month's ring) is
// centered on it, so the line always visually passes through the middle of each marker.
const LINE_Y = 16;
const DOT_AREA_HEIGHT = LINE_Y + CURRENT_RING_SIZE / 2;

/**
 * Fill color reflects the circle's actual state that month — except a month after the current
 * one hasn't started yet, so it's always gray regardless of its (meaningless, still-zero) data.
 */
function stateFor(cycle: ScheduleCycle, isFuture: boolean) {
  if (isFuture) return { color: "grey.400", statusKey: "timelineNotStarted" };

  const fullyCollected = cycle.collectedAmount >= cycle.expectedPoolAmount;
  const fullyPaidOut = cycle.payoutStatus === "Paid";
  if (fullyCollected && fullyPaidOut) return { color: "success.main", statusKey: "timelineDoneAndPaid" };
  if (fullyCollected) return { color: "info.main", statusKey: "timelineCollectedNotPaid" };
  return { color: "warning.main", statusKey: "timelineUnderCollection" };
}

/**
 * A month-by-month progress line under the circle header: one solid dot per cycle on a thick
 * connecting line, colored by that month's actual collection/payout state (never a placeholder
 * "future" color). The line itself is green up to the current month and gray beyond it. The
 * current month is marked by a gray ring around its dot, centered on the line like every other
 * dot, so the line always passes through the middle of every marker. All dot/ring positions are
 * absolute within a fixed-height area so the line's vertical position never drifts.
 */
export function CircleTimeline({ schedule, currentCycleId }: { schedule: ScheduleCycle[]; currentCycleId?: number }) {
  const { t, i18n } = useTranslation();

  if (schedule.length === 0) return null;

  const currentIndex = schedule.findIndex((c) => c.cycleId === currentCycleId);
  const gapsBeforeCurrent = currentIndex === -1 ? schedule.length - 1 : currentIndex;
  const gapsAfterCurrent = schedule.length - 1 - gapsBeforeCurrent;

  return (
    <Box
      sx={{
        overflowX: "auto", width: "100%",
        // Keep the row scrollable on narrow screens without showing a visible scrollbar.
        scrollbarWidth: "none", msOverflowStyle: "none",
        "&::-webkit-scrollbar": { display: "none" },
      }}
    >
      <Box sx={{ position: "relative", display: "flex", minWidth: schedule.length * 90 }}>
        {schedule.length > 1 && (
          <Box sx={{ position: "absolute", left: `${50 / schedule.length}%`, right: `${50 / schedule.length}%`, top: LINE_Y - LINE_THICKNESS / 2, height: LINE_THICKNESS, display: "flex", borderRadius: LINE_THICKNESS / 2, overflow: "hidden" }}>
            <Box sx={{ flex: gapsBeforeCurrent || 0.0001, bgcolor: "success.main" }} />
            <Box sx={{ flex: gapsAfterCurrent || 0.0001, bgcolor: "grey.400" }} />
          </Box>
        )}
        {schedule.map((cycle, i) => {
          const isCurrent = cycle.cycleId === currentCycleId;
          const isFuture = currentIndex !== -1 && i > currentIndex;
          const { color, statusKey } = stateFor(cycle, isFuture);
          const monthName = new Date(cycle.dueDate).toLocaleDateString(i18n.language, { month: "long" });
          const statusLabel = t(`circle.${statusKey}`);

          return (
            <Box key={cycle.cycleId} sx={{ position: "relative", zIndex: 1, flex: 1, display: "flex", flexDirection: "column", alignItems: "center" }}>
              <Box sx={{ position: "relative", height: DOT_AREA_HEIGHT, width: "100%" }}>
                <Tooltip title={`${monthName} — ${statusLabel}`}>
                  <Box
                    sx={{
                      position: "absolute", left: "50%", transform: "translateX(-50%)",
                      top: isCurrent ? LINE_Y - CURRENT_RING_SIZE / 2 : LINE_Y - DOT_SIZE / 2,
                      width: isCurrent ? CURRENT_RING_SIZE : DOT_SIZE,
                      height: isCurrent ? CURRENT_RING_SIZE : DOT_SIZE,
                      borderRadius: "50%",
                      display: "flex", alignItems: "center", justifyContent: "center",
                      border: isCurrent ? "2px solid" : "none",
                      borderColor: isCurrent ? "grey.500" : "transparent",
                      cursor: "default",
                    }}
                  >
                    <Box sx={{ width: DOT_SIZE, height: DOT_SIZE, borderRadius: "50%", bgcolor: color }} />
                  </Box>
                </Tooltip>
              </Box>
              <Stack direction="row" alignItems="baseline">
                <Typography variant="caption" fontWeight={600} sx={{ whiteSpace: "nowrap", mr: 0.5 }}>{monthName}</Typography>
                <Typography variant="caption" color="text.secondary" sx={{ whiteSpace: "nowrap" }}>
                  ({cycle.recipientName})
                </Typography>
              </Stack>
              <Typography variant="caption" component="div" sx={{ textAlign: "center", lineHeight: 1.2, px: 0.5 }}>
                <Box component="span" sx={{ color }}>{statusLabel}</Box>
                {statusKey === "timelineUnderCollection" && (
                  // Isolated LTR so the number/slash sequence never gets bidi-reordered inside
                  // the surrounding Arabic text (a recurring class of bug in this app).
                  <Box component="span" sx={{ color: "text.secondary" }} style={{ unicodeBidi: "isolate", direction: "ltr" }}>
                    {" "}{cycle.collectedAmount}/{cycle.expectedPoolAmount}
                  </Box>
                )}
              </Typography>
            </Box>
          );
        })}
      </Box>
    </Box>
  );
}
