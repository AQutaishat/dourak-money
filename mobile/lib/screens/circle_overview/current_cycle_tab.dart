import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../api/models.dart';
import '../../l10n/app_localizations.dart';
import '../../state/providers.dart';
import '../../utils/format.dart';
import '../../utils/whatsapp.dart';
import '../../widgets/status_chips.dart';
import 'basic_info_tab.dart';
import 'payment_claim_dialogs.dart';
import 'payment_dialogs.dart';
import 'payout_lines.dart';

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
    // Watched (not just read on demand) so the record-payment dialog's cross-month picker has
    // the data ready — same cache entry the Monthly Cycles tab uses. Amounts are visible to
    // every viewer already (only claim status is privacy-masked), so members fetch it too.
    ref.watch(monthsDetailProvider(circle.id));

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
        final monthNameOnly = DateFormat.MMMM(locale).format(DateTime.parse(dashboard.dueDate));
        final isArabic = Localizations.localeOf(context).languageCode == 'ar';
        final canManage = circle.isOrganizer;
        final myRow = circle.myMemberId != null
            ? dashboard.members.where((m) => m.memberId == circle.myMemberId).cast<CurrentCycleMemberRow?>().firstWhere((_) => true, orElse: () => null)
            : null;
        final myOutstanding = myRow != null ? myRow.expectedAmount - myRow.paidAmount : 0.0;
        final myClaimTargets = myRow == null ? const <PaymentTarget>[] : _alternateTargets(context, ref, dashboard.cycleId, myRow.memberId);
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
            // The collection/payout badges sit on their own line under "دور: {name}" — the
            // web's final placement for them (they started next to the circle-name status chip).
            Align(
              alignment: AlignmentDirectional.centerStart,
              child: CollectionPayoutBadges(
                collected: dashboard.collected,
                expected: dashboard.expected,
                payoutStatus: dashboard.payoutStatus,
              ),
            ),
            // Any payout installments already paid to this cycle's recipient — the identical
            // block the Monthly Cycles tab renders for each month, shown here under the header.
            if (dashboard.payoutRows.isNotEmpty) ...[
              const SizedBox(height: 4),
              PayoutRowsBlock(rows: dashboard.payoutRows, recipientName: dashboard.recipientName),
            ],
            const SizedBox(height: 8),
            GridView.count(
              crossAxisCount: 2,
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              mainAxisSpacing: 8,
              crossAxisSpacing: 8,
              childAspectRatio: 2.6,
              children: [
                // The web's 4th card became the month ordinal ("الأول (أكتوبر)") instead of the
                // next recipient; on mobile it leads the grid the same way it does on the web.
                _StatCard(
                  label: context.t('circle.monthLabel'),
                  value: '${monthOrdinalWord(dashboard.sequenceNumber, isArabic)} ($monthNameOnly)',
                ),
                _StatCard(label: context.t('circle.paid'), value: '${dashboard.membersPaid}/${dashboard.membersTotal}'),
                _StatCard(label: context.t('circle.collected'), value: '${dashboard.collected} ${circle.currency}'),
                _StatCard(label: context.t('circle.outstanding'), value: '${dashboard.outstanding} ${circle.currency}'),
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
              // It stays available when the *current* cycle is fully paid as long as any other
              // month still owes something — the dialog then defaults straight to that month.
              if (!canManage && myRow != null && !myRow.hasPendingClaim && (myOutstanding > 0 || myClaimTargets.isNotEmpty))
                OutlinedButton(
                  onPressed: () => showDialog(
                    context: context,
                    builder: (_) => SubmitPaymentClaimDialog(
                      circleId: circle.id,
                      cycleId: dashboard.cycleId,
                      outstanding: myOutstanding,
                      currency: circle.currency,
                      alternateTargets: myClaimTargets,
                      initialTargetCycleId: myOutstanding > 0 ? null : myClaimTargets.first.cycleId,
                    ),
                  ),
                  child: Text(context.t('circle.iPaid')),
                ),
              if (!canManage && myRow?.myClaimStatus != null) ClaimStatusChip(status: myRow!.myClaimStatus!),
            ]),
            const SizedBox(height: 8),
            Text(context.t('circle.gracePeriodHint'), style: Theme.of(context).textTheme.bodySmall?.copyWith(color: Colors.grey)),
            const SizedBox(height: 12),
            ...dashboard.members.map((m) {
              final isUnpaid = m.status != 'Paid';
              return Card(
                margin: const EdgeInsets.only(bottom: 8),
                child: Padding(
                  padding: const EdgeInsets.all(10),
                  child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                    Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [
                      Expanded(
                        child: Wrap(spacing: 6, runSpacing: 4, crossAxisAlignment: WrapCrossAlignment.center, children: [
                          Text(m.memberName, style: const TextStyle(fontWeight: FontWeight.w600)),
                          // Paid this cycle's contribution while an earlier cycle was still the
                          // current one — i.e. ahead of schedule.
                          if (m.paidInAdvance)
                            Chip(
                              label: Text(context.t('circle.paidInAdvance'), style: const TextStyle(color: Colors.white, fontSize: 11)),
                              backgroundColor: Colors.blue,
                              visualDensity: VisualDensity.compact,
                              padding: EdgeInsets.zero,
                            ),
                        ]),
                      ),
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

  /// Every *other* month this member still owes something for (past or future), built off the
  /// Monthly Cycles tab's own `monthsDetail` cache rather than a second fetch. Shared by the
  /// organizer's record-payment dialog and a member's own submit-claim dialog, which both let
  /// the payment/report be redirected to one of these months.
  List<PaymentTarget> _alternateTargets(BuildContext context, WidgetRef ref, int cycleId, int memberId) {
    final locale = Localizations.localeOf(context).toString();
    final months = ref.read(monthsDetailProvider(circle.id)).valueOrNull ?? const <CircleMonth>[];
    final targets = <PaymentTarget>[];
    for (final month in months) {
      if (month.cycleId == cycleId) continue;
      final memberRow = month.members.where((m) => m.memberId == memberId);
      if (memberRow.isEmpty || memberRow.first.outstanding <= 0) continue;
      targets.add(PaymentTarget(
        cycleId: month.cycleId,
        label: DateFormat.yMMMM(locale).format(DateTime.parse(month.dueDate)),
        outstanding: memberRow.first.outstanding,
      ));
    }
    return targets;
  }

  /// `POST /cycles/{id}/contributions` is additive (adds on top of what's already
  /// paid, capped server-side at the outstanding balance) — mirrors the web app's
  /// dialog defaulting/capping to the member's remaining outstanding amount, not the
  /// full contribution amount, and surfacing an error instead of failing silently.
  ///
  /// The dialog additionally offers every *other* month this same member hasn't fully paid yet
  /// (past or future) as an alternate target — the mobile form of the web's "تسجيل المبلغ على"
  /// picker — reusing the Monthly Cycles tab's own `monthsDetail` cache rather than a second
  /// fetch. Picking one re-caps the amount to that month's outstanding and records against it.
  Future<void> _recordPaymentDialog(BuildContext context, WidgetRef ref, int cycleId, CurrentCycleMemberRow row) async {
    final targets = _alternateTargets(context, ref, cycleId, row.memberId);
    if (!context.mounted) return;
    await showDialog<void>(
      context: context,
      builder: (dialogContext) => RecordPaymentDialog(
        title: '${dialogContext.t('circle.recordPayment')} — ${row.memberName}',
        outstanding: row.outstanding,
        alternateTargets: targets,
        onSave: (amount, targetCycleId) => ref
            .read(cyclesApiProvider)
            .recordContribution(targetCycleId ?? cycleId, memberId: row.memberId, paidAmount: amount),
        onDone: () => ref.read(refreshTickProvider.notifier).state++,
      ),
    );
  }

  /// `POST /cycles/{id}/payout` is now multipart/form-data and additive (adds to what's
  /// already paid, capped at outstanding) instead of the old single-shot JSON call.
  Future<void> _confirmPayoutDialog(BuildContext context, WidgetRef ref, CurrentCycleDashboard dashboard) async {
    // `PayoutExpectedAmount`/`PayoutActualAmount` are now on the dashboard DTO too, so this
    // caps at what's genuinely left rather than re-offering the whole collected pool after a
    // first installment. Falls back to the collected pool for a cycle with no payout row yet.
    final outstanding = dashboard.payoutExpectedAmount > 0 ? dashboard.payoutOutstanding : dashboard.collected;
    await showDialog<void>(
      context: context,
      builder: (dialogContext) => ConfirmPayoutDialog(
        title: '${dialogContext.t('circle.confirmPayout')} — ${dashboard.recipientName}',
        outstanding: outstanding,
        unpaidCount: dashboard.membersUnpaid + dashboard.membersLate,
        onSave: (amount, method, notes, evidence) => ref.read(cyclesApiProvider).recordPayout(
          dashboard.cycleId,
          actualAmount: amount,
          paymentMethod: method,
          notes: notes,
          evidence: evidence,
        ),
        onDone: () => ref.read(refreshTickProvider.notifier).state++,
      ),
    );
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
