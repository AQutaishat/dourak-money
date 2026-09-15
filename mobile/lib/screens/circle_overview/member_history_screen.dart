import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../l10n/app_localizations.dart';
import '../../state/providers.dart';
import '../../widgets/status_chips.dart';

/// Mirrors MemberHistoryPage.tsx — Back and Close both return to the Members tab.
class MemberHistoryScreen extends ConsumerWidget {
  const MemberHistoryScreen({super.key, required this.circleId, required this.memberId});
  final int circleId;
  final int memberId;

  void _backToMembers(BuildContext context) => context.go('/circles/$circleId?tab=members');

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final historyAsync = ref.watch(memberHistoryProvider((circleId, memberId)));
    return Scaffold(
      appBar: AppBar(
        leading: BackButton(onPressed: () => _backToMembers(context)),
        title: Text(context.t('circle.history')),
      ),
      body: SafeArea(
        child: historyAsync.when(
          loading: () => Center(child: Text(context.t('common.loading'))),
          error: (_, __) => Center(child: Text(context.t('common.error'))),
          data: (history) {
            final locale = Localizations.localeOf(context).toString();
            return ListView(
              padding: const EdgeInsets.all(16),
              children: [
                Text(context.t('circle.memberHistoryTitle', {'name': history.memberName}), style: Theme.of(context).textTheme.titleLarge),
                if (history.payoutPosition != null)
                  Text('${context.t('circle.payoutOrder')}: #${history.payoutPosition}', style: Theme.of(context).textTheme.bodyMedium),
                const SizedBox(height: 12),
                ...history.entries.map((e) {
                  final monthLabel = DateFormat.yMMMM(locale).format(DateTime.parse(e.dueDate));
                  return Card(
                    margin: const EdgeInsets.only(bottom: 8),
                    child: ListTile(
                      leading: CircleAvatar(child: Text('${e.sequenceNumber}')),
                      title: Text(monthLabel),
                      subtitle: Text('${e.paidAmount} / ${e.expectedAmount}'),
                      trailing: Wrap(spacing: 6, crossAxisAlignment: WrapCrossAlignment.center, children: [
                        ContributionStatusChip(status: e.status),
                        if (e.isRecipientThisCycle)
                          Chip(
                            label: Text(
                              context.t('circle.${e.payoutStatusIfRecipient == 'Paid' ? 'paid' : 'pending'}'),
                              style: const TextStyle(color: Colors.white, fontSize: 11),
                            ),
                            backgroundColor: e.payoutStatusIfRecipient == 'Paid' ? Colors.green : Colors.grey,
                            visualDensity: VisualDensity.compact,
                          ),
                      ]),
                    ),
                  );
                }),
                const SizedBox(height: 16),
                Align(
                  alignment: AlignmentDirectional.centerEnd,
                  child: OutlinedButton(onPressed: () => _backToMembers(context), child: Text(context.t('common.close'))),
                ),
              ],
            );
          },
        ),
      ),
    );
  }
}
