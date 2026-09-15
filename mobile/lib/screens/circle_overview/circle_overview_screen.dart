import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../l10n/app_localizations.dart';
import '../../state/providers.dart';
import '../../widgets/status_chips.dart';
import 'activate_circle_button.dart';
import 'basic_info_tab.dart';
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
                              ScheduleTab(circleId: circle.id, currency: circle.currency),
                              MembersTab(circle: circle),
                              HistoryTab(circleId: circle.id, currency: circle.currency),
                            ],
                    ),
                  ),
                  if (circle.isDraft && circle.isOrganizer)
                    Container(
                      padding: const EdgeInsets.all(12),
                      decoration: BoxDecoration(
                        color: Theme.of(context).scaffoldBackgroundColor,
                        boxShadow: [BoxShadow(color: Colors.black.withOpacity(0.08), blurRadius: 4, offset: const Offset(0, -1))],
                      ),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.end,
                        children: [ActivateCircleButton(circleId: circle.id)],
                      ),
                    ),
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.end,
                      children: [
                        OutlinedButton.icon(
                          icon: const Icon(Icons.close),
                          label: Text(context.t('common.close')),
                          onPressed: () => context.go('/circles'),
                        ),
                      ],
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
