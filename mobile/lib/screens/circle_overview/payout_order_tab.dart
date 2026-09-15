import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../api/models.dart';
import '../../l10n/app_localizations.dart';
import '../../state/providers.dart';

/// Mirrors PayoutOrderTab.tsx: manual reorder (move up/down instead of drag-drop —
/// simpler and more reliable on touch than a drag target), random draw, reset.
/// No Activate button here (prompt03 §1 moved it to the persistent footer).
class PayoutOrderTab extends ConsumerStatefulWidget {
  const PayoutOrderTab({super.key, required this.circle});
  final CircleDetail circle;

  @override
  ConsumerState<PayoutOrderTab> createState() => _PayoutOrderTabState();
}

class _PayoutOrderTabState extends ConsumerState<PayoutOrderTab> {
  List<({int memberId, String memberName})> localOrder = [];
  bool _initialized = false;

  void _seed(List<Member> members, List<PayoutOrderEntry> order) {
    if (_initialized) return;
    if (order.isNotEmpty) {
      localOrder = order.map((o) => (memberId: o.memberId, memberName: o.memberName)).toList();
    } else {
      final participating = members.where((m) => m.isParticipating);
      localOrder = participating.map((m) => (memberId: m.id, memberName: m.name)).toList();
    }
    _initialized = true;
  }

  void _move(int index, int direction) {
    final target = index + direction;
    if (target < 0 || target >= localOrder.length) return;
    setState(() {
      final item = localOrder.removeAt(index);
      localOrder.insert(target, item);
    });
  }

  @override
  Widget build(BuildContext context) {
    final circle = widget.circle;
    final membersAsync = ref.watch(membersProvider(circle.id));
    final orderAsync = ref.watch(payoutOrderProvider(circle.id));

    if (!circle.isDraft) {
      return orderAsync.when(
        loading: () => Center(child: Text(context.t('common.loading'))),
        error: (_, __) => Center(child: Text(context.t('common.error'))),
        data: (order) => ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Text(context.t('circle.payoutOrder'), style: Theme.of(context).textTheme.titleMedium),
            const SizedBox(height: 8),
            ...order.map((o) => ListTile(title: Text('${o.position}. ${o.memberName}'))),
          ],
        ),
      );
    }

    return membersAsync.when(
      loading: () => Center(child: Text(context.t('common.loading'))),
      error: (_, __) => Center(child: Text(context.t('common.error'))),
      data: (members) {
        final participating = members.where((m) => m.isParticipating).toList();
        if (participating.isEmpty) {
          return Center(child: Text(context.t('circle.addMember')));
        }
        return orderAsync.when(
          loading: () => Center(child: Text(context.t('common.loading'))),
          error: (_, __) => Center(child: Text(context.t('common.error'))),
          data: (order) {
            _seed(members, order);
            final canManage = circle.isOrganizer;
            final hasOrder = order.isNotEmpty;

            return ListView(
              padding: const EdgeInsets.all(16),
              children: [
                Text(context.t('circle.payoutOrder'), style: Theme.of(context).textTheme.titleMedium),
                const SizedBox(height: 12),
                if (canManage)
                  Row(children: [
                    OutlinedButton.icon(
                      onPressed: () async {
                        await ref.read(circlesApiProvider).runDraw(circle.id);
                        setState(() => _initialized = false);
                        ref.read(refreshTickProvider.notifier).state++;
                      },
                      icon: const Icon(Icons.shuffle),
                      label: Text(context.t('circle.runDraw')),
                    ),
                    const SizedBox(width: 8),
                    if (hasOrder)
                      TextButton(
                        style: TextButton.styleFrom(foregroundColor: Colors.orange.shade800),
                        onPressed: () async {
                          await ref.read(circlesApiProvider).resetOrder(circle.id);
                          setState(() => _initialized = false);
                          ref.read(refreshTickProvider.notifier).state++;
                        },
                        child: Text(context.t('circle.resetOrder')),
                      ),
                  ]),
                const SizedBox(height: 12),
                ...List.generate(localOrder.length, (index) {
                  final entry = localOrder[index];
                  return Card(
                    margin: const EdgeInsets.only(bottom: 8),
                    child: ListTile(
                      leading: CircleAvatar(radius: 14, child: Text('${index + 1}', style: const TextStyle(fontSize: 12))),
                      title: Text(entry.memberName),
                      trailing: canManage
                          ? Row(mainAxisSize: MainAxisSize.min, children: [
                              IconButton(
                                tooltip: context.t('circle.moveUp'),
                                icon: const Icon(Icons.arrow_upward, size: 18),
                                onPressed: index == 0 ? null : () => _move(index, -1),
                              ),
                              IconButton(
                                tooltip: context.t('circle.moveDown'),
                                icon: const Icon(Icons.arrow_downward, size: 18),
                                onPressed: index == localOrder.length - 1 ? null : () => _move(index, 1),
                              ),
                            ])
                          : null,
                    ),
                  );
                }),
                if (canManage) ...[
                  const SizedBox(height: 8),
                  FilledButton(
                    onPressed: () async {
                      await ref.read(circlesApiProvider).setManualOrder(circle.id, localOrder.map((e) => e.memberId).toList());
                      ref.read(refreshTickProvider.notifier).state++;
                    },
                    child: Text(context.t('common.save')),
                  ),
                ],
              ],
            );
          },
        );
      },
    );
  }
}
