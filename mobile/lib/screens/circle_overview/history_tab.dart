import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../l10n/app_localizations.dart';
import '../../state/providers.dart';

/// Mirrors HistoryTab.tsx: completed cycles with collected amount and unpaid/late lists.
class HistoryTab extends ConsumerWidget {
  const HistoryTab({super.key, required this.circleId, required this.currency});
  final int circleId;
  final String currency;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final historyAsync = ref.watch(historyProvider(circleId));
    return historyAsync.when(
      loading: () => Center(child: Text(context.t('common.loading'))),
      error: (_, __) => Center(child: Text(context.t('common.error'))),
      data: (history) {
        if (history.isEmpty) {
          return Center(child: Text('${context.t('circle.history')} — no completed cycles yet.'));
        }
        final locale = Localizations.localeOf(context).toString();
        return ListView.separated(
          padding: const EdgeInsets.all(16),
          itemCount: history.length,
          separatorBuilder: (_, __) => const SizedBox(height: 8),
          itemBuilder: (context, i) {
            final c = history[i];
            final monthLabel = DateFormat.yMMMM(locale).format(DateTime.parse(c.dueDate));
            return Card(
              child: Padding(
                padding: const EdgeInsets.all(12),
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [
                    Text('#${c.sequenceNumber} · ${c.recipientName}', style: const TextStyle(fontWeight: FontWeight.w600)),
                    Chip(
                      label: Text(context.t('circle.${c.payoutStatus == 'Paid' ? 'paid' : 'pending'}'), style: const TextStyle(color: Colors.white, fontSize: 11)),
                      backgroundColor: c.payoutStatus == 'Paid' ? Colors.green : Colors.grey,
                      visualDensity: VisualDensity.compact,
                    ),
                  ]),
                  Text(monthLabel, style: Theme.of(context).textTheme.bodySmall),
                  Text('${context.t('circle.collected')}: ${c.collected} / ${c.expectedPool} $currency'),
                  if (c.unpaidMembers.isNotEmpty) Text('${context.t('circle.unpaid')}: ${c.unpaidMembers.join(', ')}'),
                  if (c.lateMembers.isNotEmpty) Text('${context.t('circle.late')}: ${c.lateMembers.join(', ')}'),
                ]),
              ),
            );
          },
        );
      },
    );
  }
}
