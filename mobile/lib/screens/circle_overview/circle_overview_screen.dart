import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
// `TextDirection` is hidden because intl exports its own, which would shadow the Flutter one
// used by this screen's left/right-locked bottom action row.
import 'package:intl/intl.dart' hide TextDirection;

import '../../api/models.dart';
import '../../l10n/app_localizations.dart';
import '../../state/providers.dart';
import '../../utils/format.dart';
import '../../widgets/status_chips.dart';
import 'activate_circle_button.dart';
import 'basic_info_tab.dart';
import 'circle_timeline.dart';
import 'current_cycle_tab.dart';
import 'history_tab.dart';
import 'members_tab.dart';
import 'payout_order_tab.dart';
import 'schedule_tab.dart';

const _draftTabKeys = ['basicInfo', 'members', 'payoutOrder'];
const _activeTabKeys = ['currentCycle', 'schedule', 'members', 'history'];

/// Mirrors CircleOverviewPage.tsx: tab container + header actions (Actions menu for
/// pause/resume/cancel, delete-with-confirm on drafts, Close). Deep-links `?tab=members`
/// so MemberHistoryScreen's Back/Close can land on the Members tab (prompt02 §Active circles).
class CircleOverviewScreen extends ConsumerStatefulWidget {
  const CircleOverviewScreen({super.key, required this.circleId, this.initialTab});
  final int circleId;
  final String? initialTab;

  @override
  ConsumerState<CircleOverviewScreen> createState() => _CircleOverviewScreenState();
}

class _CircleOverviewScreenState extends ConsumerState<CircleOverviewScreen> {
  @override
  Widget build(BuildContext context) {
    final circleAsync = ref.watch(circleDetailProvider(widget.circleId));

    return circleAsync.when(
      loading: () => Scaffold(appBar: AppBar(), body: Center(child: Text(context.t('common.loading')))),
      error: (e, _) => Scaffold(appBar: AppBar(), body: Center(child: Text(context.t('common.error')))),
      data: (circle) {
        final tabKeys = circle.isDraft ? _draftTabKeys : _activeTabKeys;
        final initialIndex = widget.initialTab != null ? tabKeys.indexOf(widget.initialTab!) : -1;

        return DefaultTabController(
          length: tabKeys.length,
          initialIndex: initialIndex >= 0 ? initialIndex : 0,
          child: Scaffold(
            appBar: AppBar(
              title: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(circle.name, style: const TextStyle(fontSize: 16)),
                  Row(children: [
                    Chip(
                      label: Text(context.t('circle.${circle.status.toLowerCase()}'), style: const TextStyle(color: Colors.white, fontSize: 10)),
                      backgroundColor: circleStatusColor(circle.status),
                      visualDensity: VisualDensity.compact,
                      padding: EdgeInsets.zero,
                    ),
                    const SizedBox(width: 6),
                    if (!circle.isOrganizer)
                      Chip(label: Text(context.t('circle.viewOnly'), style: const TextStyle(fontSize: 10)), visualDensity: VisualDensity.compact, padding: EdgeInsets.zero),
                  ]),
                ],
              ),
              actions: [
                if (circle.isOrganizer && circle.isDraft)
                  IconButton(
                    tooltip: context.t('circle.deleteCircle'),
                    icon: const Icon(Icons.delete_outline),
                    onPressed: circle.canDelete ? () => _confirmDelete(context, ref) : null,
                  ),
                if (circle.isOrganizer && (circle.isActive || circle.status == 'Paused'))
                  PopupMenuButton<String>(
                    onSelected: (action) => _runAction(ref, action),
                    itemBuilder: (context) => [
                      if (circle.isActive) PopupMenuItem(value: 'pause', child: Text(context.t('circle.pause'))),
                      if (circle.status == 'Paused') PopupMenuItem(value: 'resume', child: Text(context.t('circle.resume'))),
                      PopupMenuItem(value: 'cancel', child: Text(context.t('circle.cancel'))),
                    ],
                  ),
              ],
              bottom: TabBar(
                isScrollable: true,
                tabs: tabKeys.map((k) => Tab(text: context.t('circle.$k'))).toList(),
              ),
            ),
            body: SafeArea(
              child: Column(
                children: [
                  // The web renders this line directly under the circle name; on mobile the name
                  // lives in the AppBar, so it sits at the very top of the body instead.
                  _CircleHeaderLine(circle: circle),
                  // The mobile equivalent of the web's "timeline under the circle name": it sits
                  // directly below the header/tab bar, shared by every tab, and only once the
                  // circle has an actual schedule (a Draft one has no cycles yet).
                  if (!circle.isDraft) CircleTimeline(circleId: circle.id),
                  Expanded(
                    child: TabBarView(
                      children: circle.isDraft
                          ? [
                              SingleChildScrollView(padding: const EdgeInsets.all(16), child: BasicInfoTab(circle: circle)),
                              MembersTab(circle: circle),
                              PayoutOrderTab(circle: circle),
                            ]
                          : [
                              CurrentCycleTab(circle: circle),
                              ScheduleTab(circle: circle),
                              MembersTab(circle: circle),
                              HistoryTab(circleId: circle.id, currency: circle.currency),
                            ],
                    ),
                  ),
                  if (circle.isDraft)
                    Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                      // Physical order is locked left-to-right so each button's visual side
                      // stays consistent between English and Arabic, independent of the app's
                      // current text direction: Arabic -> Close left, Activate right.
                      // English -> Close right, Activate left.
                      child: Directionality(
                        textDirection: TextDirection.ltr,
                        child: Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: Directionality.of(context) == TextDirection.rtl
                              ? [_closeButton(context), if (circle.isOrganizer) ActivateCircleButton(circleId: circle.id)]
                              : [if (circle.isOrganizer) ActivateCircleButton(circleId: circle.id), _closeButton(context)],
                        ),
                      ),
                    ),
                  if (!circle.isDraft)
                    Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.end,
                        children: [_closeButton(context)],
                      ),
                    ),
                ],
              ),
            ),
          ),
        );
      },
    );
  }

  Widget _closeButton(BuildContext context) => OutlinedButton.icon(
        icon: const Icon(Icons.close),
        label: Text(context.t('common.close')),
        onPressed: () => context.go('/circles'),
      );

  Future<void> _runAction(WidgetRef ref, String action) async {
    final api = ref.read(circlesApiProvider);
    switch (action) {
      case 'pause':
        await api.pause(widget.circleId);
        break;
      case 'resume':
        await api.resume(widget.circleId);
        break;
      case 'cancel':
        await api.cancel(widget.circleId);
        break;
    }
    ref.read(refreshTickProvider.notifier).state++;
  }

  Future<void> _confirmDelete(BuildContext context, WidgetRef ref) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(context.t('circle.deleteCircle')),
        content: Text(context.t('circle.deleteCircleConfirm')),
        actions: [
          TextButton(onPressed: () => Navigator.of(context).pop(false), child: Text(context.t('common.cancel'))),
          FilledButton(
            style: FilledButton.styleFrom(backgroundColor: Colors.red),
            onPressed: () => Navigator.of(context).pop(true),
            child: Text(context.t('common.delete')),
          ),
        ],
      ),
    );
    if (confirmed != true) return;
    try {
      await ref.read(circlesApiProvider).remove(widget.circleId);
      ref.read(refreshTickProvider.notifier).state++;
      if (context.mounted) context.go('/circles');
    } catch (_) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(context.t('circle.deleteCircleBlocked'))));
      }
    }
  }
}

/// The line the web shows directly under the circle name:
/// "{organizer} : {name} . {members}: {count} . {installment}: {amount} . {دور}: {recipient}",
/// with the creation date (month + year only) at the far end. Tapping/long-pressing the date
/// reveals the full dd/mm/yyyy in a tooltip, bidi-isolated so the day/month/year sequence can't
/// be visually reordered inside the surrounding Arabic text.
class _CircleHeaderLine extends ConsumerWidget {
  const _CircleHeaderLine({required this.circle});
  final CircleDetail circle;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final locale = Localizations.localeOf(context).toString();
    final dashboard = circle.isDraft ? null : ref.watch(dashboardProvider(circle.id)).valueOrNull;
    final created = DateTime.parse(circle.createdAt).toLocal();
    final createdMonth = DateFormat.yMMMM(locale).format(created);
    final createdFull = ltrIsolate(DateFormat('dd/MM/yyyy').format(created));

    final buffer = StringBuffer()
      ..write('${context.t('circle.organizer')} : ${circle.organizerName}')
      ..write(' . ${context.t('circle.members')}: ${circle.memberCount}');
    if (dashboard != null) {
      buffer
        ..write(' . ${context.t('circle.installmentLabel')}: ${formatAmount(circle.contributionAmount)} ${circle.currency}')
        ..write(' . ${context.t('circle.currentRecipientLabel')}: ${dashboard.recipientName}');
    }

    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(child: Text(buffer.toString(), style: Theme.of(context).textTheme.bodySmall)),
          const SizedBox(width: 8),
          Tooltip(
            triggerMode: TooltipTriggerMode.tap,
            message: '${context.t('circle.createdAt')} $createdFull',
            child: Row(mainAxisSize: MainAxisSize.min, children: [
              Text(createdMonth, style: Theme.of(context).textTheme.bodySmall?.copyWith(color: Colors.grey)),
              const SizedBox(width: 2),
              Icon(Icons.info_outline, size: 14, color: Colors.grey.shade500),
            ]),
          ),
        ],
      ),
    );
  }
}
