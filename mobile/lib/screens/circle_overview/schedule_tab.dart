import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../api/models.dart';
import '../../l10n/app_localizations.dart';
import '../../state/providers.dart';
import '../../utils/format.dart';
import '../../widgets/status_chips.dart';
import 'payment_claim_dialogs.dart';
import 'payment_dialogs.dart';
import 'payout_lines.dart';

/// Mirrors ScheduleTab.tsx ("الدورات الشهرية" / Monthly Cycles): one section per month, each
/// with its heading + collection/payout status badges, the recipient, every payout installment
/// already paid to them, and a full per-member payment breakdown — one line per installment
/// (an organizer-recorded payment, or a self-reported claim at any status), with the running
/// total paid. The organizer can record a contribution or confirm the payout for *any* month —
/// past, current, or one paid ahead of schedule — capped at that month's own outstanding.
class ScheduleTab extends ConsumerWidget {
  const ScheduleTab({super.key, required this.circle});
  final CircleDetail circle;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final monthsAsync = ref.watch(monthsDetailProvider(circle.id));
    final dashboard = ref.watch(dashboardProvider(circle.id)).valueOrNull;

    return monthsAsync.when(
      loading: () => Center(child: Text(context.t('common.loading'))),
      error: (_, __) => Center(child: Text(context.t('common.error'))),
      data: (months) {
        if (months.isEmpty) {
          return Center(child: Text('${context.t('circle.schedule')} — ${context.t('circle.activate')}'));
        }
        final currentIndex = dashboard == null ? -1 : months.indexWhere((m) => m.cycleId == dashboard.cycleId);
        return ListView.builder(
          padding: const EdgeInsets.all(16),
          itemCount: months.length,
          itemBuilder: (context, i) => _MonthSection(
            circle: circle,
            month: months[i],
            isCurrent: i == currentIndex,
            // With no current cycle at all (a fully Completed circle), nothing is in the future.
            isFuture: currentIndex != -1 && i > currentIndex,
          ),
        );
      },
    );
  }
}

class _MonthSection extends ConsumerWidget {
  const _MonthSection({required this.circle, required this.month, required this.isCurrent, required this.isFuture});

  final CircleDetail circle;
  final CircleMonth month;
  final bool isCurrent;
  final bool isFuture;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final locale = Localizations.localeOf(context).toString();
    final monthName = DateFormat.yMMMM(locale).format(DateTime.parse(month.dueDate));
    final canManage = circle.isOrganizer;
    final underCollection = !isFuture && !month.fullyCollected;

    return Padding(
      // Widened further so a quick scroll gives each month's block clear breathing room from
      // the next one, on top of the heading's own solid-color band below.
      padding: const EdgeInsets.only(bottom: 56),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // A solid-color banner (not a tinted card) so it reads as unmistakably different from
          // the white member Cards below it — a light tint was too close to those cards' own
          // background/shadow at a glance and still read as "just another row".
          Container(
            width: double.infinity,
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
            decoration: BoxDecoration(
              color: Theme.of(context).colorScheme.primary,
              borderRadius: BorderRadius.circular(10),
            ),
            child: Wrap(
              spacing: 8,
              runSpacing: 6,
              crossAxisAlignment: WrapCrossAlignment.center,
              children: [
                Text(
                  monthName,
                  style: Theme.of(context).textTheme.titleLarge?.copyWith(
                        fontWeight: FontWeight.bold,
                        color: Theme.of(context).colorScheme.onPrimary,
                      ),
                ),
                if (isCurrent)
                  SimpleBadge(label: context.t('circle.currentMonthBadge'), color: Theme.of(context).colorScheme.onPrimary, textColor: Theme.of(context).colorScheme.primary),
                // Badges only make sense once the month has actually started — a future month's
                // (always-zero) collected amount would otherwise read as "behind on collection".
                if (!isFuture) ...[
                  SimpleBadge(
                    label: context.t(month.fullyCollected ? 'circle.collectionDone' : 'circle.collectionUnderway'),
                    color: month.fullyCollected ? Colors.green.shade100 : Colors.orange.shade100,
                    textColor: month.fullyCollected ? Colors.green.shade900 : Colors.orange.shade900,
                  ),
                  SimpleBadge(
                    label: context.t(month.payoutStatus == 'Paid' ? 'circle.payoutPaidBadge' : 'circle.payoutPendingBadge'),
                    color: month.payoutStatus == 'Paid' ? Colors.green.shade100 : Colors.orange.shade100,
                    textColor: month.payoutStatus == 'Paid' ? Colors.green.shade900 : Colors.orange.shade900,
                  ),
                ],
              ],
            ),
          ),
          const SizedBox(height: 16),
          if (underCollection)
            Text(
              '${context.t('circle.collectedAmountsLabel')} : ${formatAmount(month.collectedAmount)} / ${formatAmount(month.expectedPoolAmount)}',
              style: Theme.of(context).textTheme.bodyMedium?.copyWith(color: Colors.grey.shade700),
            ),
          Text('${context.t('circle.currentRecipientLabel')}: ${month.recipientName}'),
          if (month.payoutRows.isNotEmpty) ...[
            const SizedBox(height: 4),
            PayoutRowsBlock(rows: month.payoutRows, recipientName: month.recipientName),
          ],
          if (canManage && month.payoutOutstanding > 0) ...[
            const SizedBox(height: 6),
            Align(
              alignment: AlignmentDirectional.centerStart,
              child: OutlinedButton(
                onPressed: () => showDialog<void>(
                  context: context,
                  builder: (dialogContext) => ConfirmPayoutDialog(
                    title: '${dialogContext.t('circle.confirmPayout')} — ${month.recipientName}',
                    outstanding: month.payoutOutstanding,
                    onSave: (amount, method, notes, evidence) => ref.read(cyclesApiProvider).recordPayout(
                          month.cycleId,
                          actualAmount: amount,
                          paymentMethod: method,
                          notes: notes,
                          evidence: evidence,
                        ),
                    onDone: () => ref.read(refreshTickProvider.notifier).state++,
                  ),
                ),
                child: Text(context.t(isFuture
                    ? 'circle.confirmPayoutAdvance'
                    : isCurrent
                        ? 'circle.confirmPayout'
                        : 'circle.confirmPayoutOld')),
              ),
            ),
          ],
          const SizedBox(height: 14),
          ...month.members.map((m) => _MemberCard(
                circle: circle,
                month: month,
                member: m,
                isCurrent: isCurrent,
                isFuture: isFuture,
              )),
        ],
      ),
    );
  }
}

/// One member's row for one month: name, email, one line per installment (amount + date, plus
/// a clickable claim badge when that line came from a self-reported claim), the running total
/// paid, and — for the organizer — a "record payment" button while anything is still owed.
class _MemberCard extends ConsumerWidget {
  const _MemberCard({
    required this.circle,
    required this.month,
    required this.member,
    required this.isCurrent,
    required this.isFuture,
  });

  final CircleDetail circle;
  final CircleMonth month;
  final CircleMonthMember member;
  final bool isCurrent;
  final bool isFuture;

  /// A member's own still-Pending claim opens the editable/withdraw dialog; anything else
  /// (someone else's claim, or an already-reviewed one) opens the read-only detail view,
  /// which additionally offers approve/reject to the organizer while it's Pending.
  Future<void> _openClaim(BuildContext context, WidgetRef ref, int claimId, String status) async {
    final claims = await ref.read(circlesApiProvider).paymentClaims(circle.id);
    final claim = claims.where((c) => c.id == claimId).cast<PaymentClaim?>().firstWhere((_) => true, orElse: () => null);
    if (!context.mounted) return;
    if (claim == null) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(context.t('common.error'))));
      return;
    }
    final isMine = circle.myMemberId != null && member.memberId == circle.myMemberId;
    await showDialog<void>(
      context: context,
      builder: (_) => isMine && status == 'Pending'
          ? MyClaimDialog(claim: claim, outstanding: member.outstanding, currency: circle.currency)
          : ClaimDetailDialog(claim: claim, canReview: circle.isOrganizer),
    );
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final rows = member.effectiveRows;
    final canManage = circle.isOrganizer;

    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: Padding(
        padding: const EdgeInsets.all(10),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Row(children: [
            Expanded(child: Text(member.memberName, style: const TextStyle(fontWeight: FontWeight.w600))),
            Expanded(
              child: Text(
                member.email ?? '—',
                style: Theme.of(context).textTheme.bodySmall?.copyWith(color: Colors.grey),
                textAlign: TextAlign.end,
                overflow: TextOverflow.ellipsis,
              ),
            ),
          ]),
          const SizedBox(height: 6),
          if (rows.isEmpty)
            Text('${context.t('circle.amountPaidColumn')}: —', style: Theme.of(context).textTheme.bodySmall)
          else
            ...rows.map((r) => Padding(
                  padding: const EdgeInsets.only(bottom: 2),
                  child: Wrap(spacing: 8, runSpacing: 4, crossAxisAlignment: WrapCrossAlignment.center, children: [
                    Text(formatAmount(r.amount)),
                    Text(shortDate(r.date), style: Theme.of(context).textTheme.bodySmall?.copyWith(color: Colors.grey)),
                    if (r.claimStatus != null && r.claimId != null)
                      ClaimStatusChip(
                        status: r.claimStatus!,
                        onTap: () => _openClaim(context, ref, r.claimId!, r.claimStatus!),
                      ),
                  ]),
                )),
          const SizedBox(height: 4),
          Text(
            '${context.t('circle.totalPaidColumn')}: ${formatAmount(member.paidAmount)} / ${formatAmount(member.expectedAmount)} ${circle.currency}',
            style: const TextStyle(fontWeight: FontWeight.w600),
          ),
          // Works for any month — past, current, or a future one not due yet — as long as this
          // specific month's contribution isn't fully paid; what's recorded here only ever
          // counts toward this one month.
          if (canManage && member.outstanding > 0) ...[
            const SizedBox(height: 6),
            Align(
              alignment: AlignmentDirectional.centerStart,
              child: FilledButton(
                onPressed: () => showDialog<void>(
                  context: context,
                  builder: (dialogContext) => RecordPaymentDialog(
                    title: '${dialogContext.t('circle.recordPayment')} — ${member.memberName}',
                    outstanding: member.outstanding,
                    // No cross-month picker here — this tab already *is* a month-by-month view,
                    // so each button targets its own month explicitly.
                    onSave: (amount, _) => ref
                        .read(cyclesApiProvider)
                        .recordContribution(month.cycleId, memberId: member.memberId, paidAmount: amount),
                    onDone: () => ref.read(refreshTickProvider.notifier).state++,
                  ),
                ),
                child: Text(context.t(isFuture
                    ? 'circle.recordPaymentAdvance'
                    : isCurrent
                        ? 'circle.recordPayment'
                        : 'circle.recordPaymentOld')),
              ),
            ),
          ],
        ]),
      ),
    );
  }
}
