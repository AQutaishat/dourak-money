import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../api/models.dart';
import '../../l10n/app_localizations.dart';
import '../../state/providers.dart';
import '../../utils/format.dart';

const double _dotSize = 14;
const double _ringSize = 26;
const double _lineThickness = 6;
const double _cellWidth = 104;

/// One month's visual state on the timeline — the mobile twin of CircleTimeline.tsx's
/// `stateFor`. The fill color always reflects what actually happened that month, except a month
/// *after* the current one hasn't started yet, so it's always gray regardless of its
/// (meaningless, still-zero) data.
({Color color, String statusKey}) _stateFor(ScheduleCycle cycle, bool isFuture) {
  if (isFuture) return (color: Colors.grey.shade400, statusKey: 'timelineNotStarted');
  final paidOut = cycle.payoutStatus == 'Paid';
  if (cycle.fullyCollected && paidOut) return (color: Colors.green, statusKey: 'timelineDoneAndPaid');
  if (cycle.fullyCollected) return (color: Colors.blue, statusKey: 'timelineCollectedNotPaid');
  return (color: Colors.orange, statusKey: 'timelineUnderCollection');
}

/// A month-by-month progress line shown under the circle header (mirrors
/// `frontend/src/pages/CircleOverview/CircleTimeline.tsx`): one dot per cycle on a thick
/// connecting line that is "done"-colored up to the current month and gray beyond it, with the
/// current month's dot wearing a gray ring so it reads as a "you are here" marker. Under each
/// dot: the month name, its recipient, the status spelled out in the status's own color, and —
/// while that month is still under collection — its collected/expected figure.
///
/// The row scrolls horizontally (a phone can't fit 12 months at once) and each cell is tappable,
/// popping the same "{month} — {recipient} — {status}" summary the web shows on hover.
class CircleTimeline extends ConsumerWidget {
  const CircleTimeline({super.key, required this.circleId});

  final int circleId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final schedule = ref.watch(scheduleProvider(circleId)).valueOrNull ?? const <ScheduleCycle>[];
    final dashboard = ref.watch(dashboardProvider(circleId)).valueOrNull;
    if (schedule.isEmpty) return const SizedBox.shrink();

    final currentIndex = dashboard == null ? -1 : schedule.indexWhere((c) => c.cycleId == dashboard.cycleId);
    const doneColor = Colors.green;
    final pendingColor = Colors.grey.shade400;

    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          for (var i = 0; i < schedule.length; i++)
            _TimelineCell(
              cycle: schedule[i],
              isCurrent: i == currentIndex,
              // With no current cycle at all (a fully Completed circle) nothing is in the future.
              isFuture: currentIndex != -1 && i > currentIndex,
              // The segment joining month i-1 to month i counts as "done" once i is at or before
              // the current month — same two-toned line the web draws as one continuous bar.
              leadingSegment: i == 0 ? null : (currentIndex != -1 && i <= currentIndex ? doneColor : pendingColor),
              trailingSegment:
                  i == schedule.length - 1 ? null : (currentIndex != -1 && i < currentIndex ? doneColor : pendingColor),
            ),
        ],
      ),
    );
  }
}

class _TimelineCell extends StatelessWidget {
  const _TimelineCell({
    required this.cycle,
    required this.isCurrent,
    required this.isFuture,
    required this.leadingSegment,
    required this.trailingSegment,
  });

  final ScheduleCycle cycle;
  final bool isCurrent;
  final bool isFuture;

  /// Null on the first/last cell, where the line has nothing to connect to on that side.
  final Color? leadingSegment;
  final Color? trailingSegment;

  @override
  Widget build(BuildContext context) {
    final locale = Localizations.localeOf(context).toString();
    final monthName = DateFormat.MMMM(locale).format(DateTime.parse(cycle.dueDate));
    final state = _stateFor(cycle, isFuture);
    final statusLabel = context.t('circle.${state.statusKey}');

    return SizedBox(
      width: _cellWidth,
      child: Tooltip(
        triggerMode: TooltipTriggerMode.tap,
        message: '$monthName — ${cycle.recipientName} — $statusLabel',
        child: Column(
          children: [
            SizedBox(
              height: _ringSize,
              child: Stack(
                alignment: Alignment.center,
                children: [
                  // The connecting line. Laid out as two logical halves inside a Row so it
                  // mirrors correctly in RTL without any hardcoded left/right.
                  Row(children: [
                    Expanded(child: Container(height: _lineThickness, color: leadingSegment)),
                    Expanded(child: Container(height: _lineThickness, color: trailingSegment)),
                  ]),
                  Container(
                    width: isCurrent ? _ringSize : _dotSize,
                    height: isCurrent ? _ringSize : _dotSize,
                    alignment: Alignment.center,
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      color: Theme.of(context).scaffoldBackgroundColor,
                      border: isCurrent ? Border.all(color: Colors.grey.shade600, width: 2) : null,
                    ),
                    child: Container(
                      width: _dotSize,
                      height: _dotSize,
                      decoration: BoxDecoration(shape: BoxShape.circle, color: state.color),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 4),
            Text(
              monthName,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: Theme.of(context).textTheme.bodySmall?.copyWith(fontWeight: FontWeight.w600),
            ),
            Text(
              '(${cycle.recipientName})',
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: Theme.of(context).textTheme.bodySmall?.copyWith(color: Colors.grey),
            ),
            Text(
              statusLabel,
              textAlign: TextAlign.center,
              style: Theme.of(context).textTheme.bodySmall?.copyWith(color: state.color, height: 1.2),
            ),
            // Only the still-collecting case needs the running figure — once a month is fully
            // collected the status label alone says everything.
            if (state.statusKey == 'timelineUnderCollection')
              Text(
                ltrIsolate('${formatAmount(cycle.collectedAmount)}/${formatAmount(cycle.expectedPoolAmount)}'),
                style: Theme.of(context).textTheme.bodySmall?.copyWith(color: Colors.grey),
              ),
          ],
        ),
      ),
    );
  }
}
