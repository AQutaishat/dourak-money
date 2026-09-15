import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../l10n/app_localizations.dart';
import '../../state/providers.dart';

/// Mirrors ScheduleTab.tsx: the full projected cycle schedule for an active circle.
class ScheduleTab extends ConsumerWidget {
  const ScheduleTab({super.key, required this.circleId, required this.currency});
  final int circleId;
  final String currency;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final scheduleAsync = ref.watch(scheduleProvider(circleId));
    return scheduleAsync.when(
      loading: () => Center(child: Text(context.t('common.loading'))),
      error: (_, __) => Center(child: Text(context.t('common.error'))),
      data: (schedule) {
        if (schedule.isEmpty) {
          return Center(child: Text('${context.t('circle.schedule')} — ${context.t('circle.activate')}'));
        }
        final locale = Localizations.localeOf(context).toString();
        return ListView.separated(
          padding: const EdgeInsets.all(16),
          itemCount: schedule.length,
          separatorBuilder: (_, __) => const SizedBox(height: 8),
          itemBuilder: (context, i) {
            final c = schedule[i];
            final monthLabel = DateFormat.yMMMM(locale).format(DateTime.parse(c.dueDate));
            return Card(
              child: ListTile(
                leading: CircleAvatar(child: Text('${c.sequenceNumber}')),
                title: Text(c.recipientName),
                subtitle: Text('$monthLabel · ${c.expectedPoolAmount} $currency'),
                trailing: Chip(
                  label: Text(context.t('circle.${c.payoutStatus == 'Paid' ? 'paid' : 'pending'}'), style: const TextStyle(color: Colors.white, fontSize: 11)),
                  backgroundColor: c.payoutStatus == 'Paid' ? Colors.green : Colors.grey,
                  visualDensity: VisualDensity.compact,
                ),
              ),
            );
          },
        );
      },
    );
  }
}
