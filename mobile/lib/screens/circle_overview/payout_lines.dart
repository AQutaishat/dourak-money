import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../api/models.dart';
import '../../l10n/app_localizations.dart';
import '../../state/providers.dart';
import '../../utils/format.dart';
import '../../widgets/evidence_viewer.dart';

/// The payout-installment breakdown for one cycle, shared by the Monthly Cycles tab and the
/// Current Cycle tab (which shows the identical block for the current month) — the first
/// installment is spelled out in full ("تم دفع مبلغ {amount} ل{recipient} بتاريخ {date}"), any
/// further ones are just "{amount} بتاريخ {date}", each with an "المرفق" link when it carries
/// evidence, downloaded through the token-authenticated endpoint.
class PayoutRowsBlock extends StatelessWidget {
  const PayoutRowsBlock({super.key, required this.rows, required this.recipientName});

  final List<PayoutRow> rows;
  final String recipientName;

  @override
  Widget build(BuildContext context) {
    if (rows.isEmpty) return const SizedBox.shrink();
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: rows
          .asMap()
          .entries
          .map((e) => PayoutLine(row: e.value, isFirst: e.key == 0, recipientName: recipientName))
          .toList(),
    );
  }
}

/// One payout installment to a month's recipient — see [PayoutRowsBlock].
class PayoutLine extends ConsumerWidget {
  const PayoutLine({super.key, required this.row, required this.isFirst, required this.recipientName});

  final PayoutRow row;
  final bool isFirst;
  final String recipientName;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final text = isFirst
        ? context.t('circle.payoutPaidLine', {
            'amount': formatAmount(row.amount),
            'recipientName': recipientName,
            'date': shortDate(row.date),
          })
        : context.t('circle.payoutExtraPaymentLine', {'amount': formatAmount(row.amount), 'date': shortDate(row.date)});

    return Row(
      crossAxisAlignment: CrossAxisAlignment.center,
      children: [
        Flexible(child: Text(text, style: Theme.of(context).textTheme.bodyMedium?.copyWith(color: Colors.grey.shade700))),
        if (row.hasEvidence)
          TextButton(
            style: TextButton.styleFrom(visualDensity: VisualDensity.compact, padding: const EdgeInsets.symmetric(horizontal: 8)),
            onPressed: () async {
              final download = await ref.read(cyclesApiProvider).payoutEvidence(row.payoutPaymentId);
              if (context.mounted) await showEvidence(context, download);
            },
            child: Text(context.t('circle.attachmentLink')),
          ),
      ],
    );
  }
}
