import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../api/models.dart';
import '../../l10n/app_localizations.dart';
import '../../state/providers.dart';
import '../../utils/whatsapp.dart';
import '../../widgets/status_chips.dart';
import 'basic_info_tab.dart';
import 'payment_claim_dialogs.dart';

/// Mirrors CurrentCycleTab.tsx: dashboard stats, per-member rows, organizer
/// "record contribution"/"confirm payout" actions, member-only self-report button
/// (organizer-hidden per prompt03 §5), Share-to-WhatsApp next to the recipient line.
class CurrentCycleTab extends ConsumerWidget {
  const CurrentCycleTab({super.key, required this.circle});
  final CircleDetail circle;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final dashboardAsync = ref.watch(dashboardProvider(circle.id));
    final membersAsync = ref.watch(membersProvider(circle.id));

    return dashboardAsync.when(
      loading: () => Center(child: Text(context.t('common.loading'))),
      error: (_, __) => Center(child: Text(context.t('common.error'))),
      data: (dashboard) {
        if (dashboard == null) {
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              BasicInfoTab(circle: circle, dense: true),
              const SizedBox(height: 12),
              Text(context.t('circle.activate')),
            ],
          );
        }

        final locale = Localizations.localeOf(context).toString();
        final monthLabel = DateFormat.yMMMM(locale).format(DateTime.parse(dashboard.dueDate));
        final isArabic = Localizations.localeOf(context).languageCode == 'ar';
        final canManage = circle.isOrganizer;
        final myRow = circle.myMemberId != null
            ? dashboard.members.where((m) => m.memberId == circle.myMemberId).cast<CurrentCycleMemberRow?>().firstWhere((_) => true, orElse: () => null)
            : null;
        final myOutstanding = myRow != null ? myRow.expectedAmount - myRow.paidAmount : 0.0;
        final members = membersAsync.valueOrNull ?? <Member>[];
        String? phoneFor(int memberId) => members.where((m) => m.id == memberId).map((m) => m.phone).firstWhere((_) => true, orElse: () => null);

        return ListView(
          padding: const EdgeInsets.all(16),
          children: [
            BasicInfoTab(circle: circle, dense: true),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Expanded(
                  child: Text.rich(TextSpan(children: [
                    TextSpan(text: '${context.t('circle.currentRecipientLabel')}: ', style: Theme.of(context).textTheme.titleMedium),
                    TextSpan(
                      text: dashboard.recipientName,
                      style: Theme.of(context).textTheme.titleLarge?.copyWith(color: Theme.of(context).colorScheme.primary, fontWeight: FontWeight.bold),
                    ),
                  ])),
                ),
                TextButton.icon(
                  onPressed: () => shareToWhatsApp(buildCurrentCycleShareText(
                    circleName: circle.name,
                    monthLabel: monthLabel,
                    paid: dashboard.membersPaid,
                    total: dashboard.membersTotal,
                    collected: dashboard.collected,
                    expected: dashboard.expected,
                    currency: circle.currency,
                    recipientName: dashboard.recipientName,
                    isArabic: isArabic,
                  )),
                  icon: const Icon(Icons.chat, size: 18),
                  label: Text(context.t('circle.shareStatus')),
                ),
              ],
            ),
            const SizedBox(height: 8),
            GridView.count(
              crossAxisCount: 2,
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              mainAxisSpacing: 8,
              crossAxisSpacing: 8,
              childAspectRatio: 2.6,
              children: [
                _StatCard(label: context.t('circle.paid'), value: '${dashboard.membersPaid}/${dashboard.membersTotal}'),
                _StatCard(label: context.t('circle.collected'), value: '${dashboard.collected} ${circle.currency}'),
                _StatCard(label: context.t('circle.outstanding'), value: '${dashboard.outstanding} ${circle.currency}'),
                _StatCard(label: context.t('circle.nextRecipient'), value: dashboard.nextRecipientName ?? '—'),
              ],
            ),
            const SizedBox(height: 12),
            Wrap(spacing: 8, crossAxisAlignment: WrapCrossAlignment.center, children: [
              if (canManage)
                Badge(
                  label: Text('${dashboard.pendingClaimCount}'),
                  isLabelVisible: dashboard.pendingClaimCount > 0,
                  child: OutlinedButton(
                    onPressed: () => showDialog(context: context, builder: (_) => ReviewPaymentClaimsDialog(circleId: circle.id)),
                    child: Text(context.t('circle.paymentClaims')),
                  ),
                ),
              // prompt03 §5: member self-report only, never shown to the organizer.
              if (!canManage && myRow != null && myOutstanding > 0 && !myRow.hasPendingClaim)
                OutlinedButton(
                  onPressed: () => showDialog(
                    context: context,
                    builder: (_) => SubmitPaymentClaimDialog(
                      circleId: circle.id,
                      cycleId: dashboard.cycleId,
                      outstanding: myOutstanding,
                      currency: circle.currency,
                    ),
                  ),
                  child: Text(context.t('circle.iPaid')),
                ),
              if (!canManage && myRow?.myClaimStatus != null) ClaimStatusChip(status: myRow!.myClaimStatus!),
            ]),
            const SizedBox(height: 12),
            ...dashboard.members.map((m) {
              final isUnpaid = m.status != 'Paid';
              return Card(
                margin: const EdgeInsets.only(bottom: 8),
                child: Padding(
                  padding: const EdgeInsets.all(10),
                  child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                    Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [
                      Expanded(child: Text(m.memberName, style: const TextStyle(fontWeight: FontWeight.w600))),
                      Text('${m.paidAmount} / ${m.expectedAmount} ${circle.currency}'),
                    ]),
                    const SizedBox(height: 4),
                    Wrap(spacing: 6, crossAxisAlignment: WrapCrossAlignment.center, children: [
                      ContributionStatusChip(status: m.status),
                      if (m.myClaimStatus != null) ClaimStatusChip(status: m.myClaimStatus!),
                    ]),
                    if (canManage) ...[
                      const SizedBox(height: 6),
                      Wrap(spacing: 8, children: [
                        if (isUnpaid)
                          OutlinedButton.icon(
                            onPressed: () => shareToWhatsApp(
                              buildPaymentReminderText(
                                memberName: m.memberName,
                                circleName: circle.name,
                                monthLabel: monthLabel,
                                outstanding: m.expectedAmount - m.paidAmount,
                                currency: circle.currency,
                                isArabic: isArabic,
                              ),
                              phoneFor(m.memberId),
                            ),
                            icon: const Icon(Icons.notifications_active_outlined, size: 16),
                            label: Text(context.t('circle.sendReminder')),
                          ),
                        FilledButton(
                          onPressed: () => _recordPaymentDialog(context, ref, dashboard.cycleId, m),
                          child: Text(context.t('circle.recordPayment')),
                        ),
                      ]),
                    ],
                  ]),
                ),
              );
            }),
            if (canManage && dashboard.payoutStatus == 'Pending') ...[
              const SizedBox(height: 8),
              Align(
                alignment: AlignmentDirectional.centerEnd,
                child: FilledButton(
                  onPressed: () => _confirmPayoutDialog(context, ref, dashboard),
                  child: Text(context.t('circle.confirmPayout')),
                ),
              ),
            ],
          ],
        );
      },
    );
  }

  Future<void> _recordPaymentDialog(BuildContext context, WidgetRef ref, int cycleId, CurrentCycleMemberRow row) async {
    final controller = TextEditingController(text: row.expectedAmount.toString());
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text('${context.t('circle.recordPayment')} — ${row.memberName}'),
        content: TextField(
          controller: controller,
          keyboardType: const TextInputType.numberWithOptions(decimal: true),
          decoration: InputDecoration(labelText: context.t('circle.contributionAmount')),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.of(context).pop(false), child: Text(context.t('common.cancel'))),
          FilledButton(onPressed: () => Navigator.of(context).pop(true), child: Text(context.t('common.save'))),
        ],
      ),
    );
    if (confirmed == true) {
      final amount = double.tryParse(controller.text) ?? row.expectedAmount;
      await ref.read(cyclesApiProvider).recordContribution(cycleId, memberId: row.memberId, paidAmount: amount);
      ref.read(refreshTickProvider.notifier).state++;
    }
  }

  Future<void> _confirmPayoutDialog(BuildContext context, WidgetRef ref, CurrentCycleDashboard dashboard) async {
    final controller = TextEditingController(text: dashboard.collected.toString());
    final unpaidCount = dashboard.membersUnpaid + dashboard.membersLate;
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text('${context.t('circle.confirmPayout')} — ${dashboard.recipientName}'),
        content: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
          if (unpaidCount > 0) Padding(padding: const EdgeInsets.only(bottom: 8), child: Text('${context.t('circle.unpaid')}: $unpaidCount', style: const TextStyle(color: Colors.orange))),
          TextField(controller: controller, keyboardType: const TextInputType.numberWithOptions(decimal: true), decoration: InputDecoration(labelText: context.t('circle.expectedPool'))),
        ]),
        actions: [
          TextButton(onPressed: () => Navigator.of(context).pop(false), child: Text(context.t('common.cancel'))),
          FilledButton(onPressed: () => Navigator.of(context).pop(true), child: Text(context.t('common.confirm'))),
        ],
      ),
    );
    if (confirmed == true) {
      final amount = double.tryParse(controller.text) ?? dashboard.collected;
      await ref.read(cyclesApiProvider).recordPayout(dashboard.cycleId, actualAmount: amount);
      ref.read(refreshTickProvider.notifier).state++;
    }
  }
}

class _StatCard extends StatelessWidget {
  const _StatCard({required this.label, required this.value});
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Card(
      shape: RoundedRectangleBorder(side: BorderSide(color: Colors.grey.shade300), borderRadius: BorderRadius.circular(10)),
      child: Padding(
        padding: const EdgeInsets.all(10),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, mainAxisAlignment: MainAxisAlignment.center, children: [
          Text(label, style: Theme.of(context).textTheme.bodySmall?.copyWith(color: Colors.grey)),
          Text(value, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
        ]),
      ),
    );
  }
}
