import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';

import '../../api/api_client.dart';
import '../../api/models.dart';
import '../../l10n/app_localizations.dart';
import '../../state/providers.dart';
import '../../utils/format.dart';
import '../../utils/invite_token.dart';
import '../../utils/whatsapp.dart';
import '../../widgets/app_scaffold.dart';
import '../../widgets/status_chips.dart';
import '../circle_overview/circle_timeline.dart';

/// Mirrors DashboardPage.tsx: pending invitations, active-circle progress cards,
/// then the full circle list.
class DashboardScreen extends ConsumerWidget {
  const DashboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final circlesAsync = ref.watch(circlesListProvider);

    return AppScaffold(
      title: context.t('nav.dashboard'),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => context.push('/circles/new'),
        icon: const Icon(Icons.add),
        label: Text(context.t('circle.createCircle')),
      ),
      body: RefreshIndicator(
        onRefresh: () async => ref.read(refreshTickProvider.notifier).state++,
        child: circlesAsync.when(
          loading: () => Center(child: Text(context.t('common.loading'))),
          error: (e, _) => Center(child: Text(context.t('common.error'))),
          data: (circles) {
            final activeCircles = circles.where((c) => c.status == 'Active').toList();
            return ListView(
              padding: const EdgeInsets.all(16),
              children: [
                const _PendingInvitationsSection(),
                if (circles.isEmpty) ...[
                  const SizedBox(height: 32),
                  Center(
                    child: Column(
                      children: [
                        Text(context.t('circle.myCircles'), style: Theme.of(context).textTheme.titleLarge),
                        const SizedBox(height: 8),
                        Text(context.t('app.tagline'), style: Theme.of(context).textTheme.bodyMedium),
                      ],
                    ),
                  ),
                ] else ...[
                  ...activeCircles.map((c) => Padding(
                        padding: const EdgeInsets.only(bottom: 12),
                        child: _CurrentCycleSummaryCard(circle: c),
                      )),
                  const SizedBox(height: 8),
                  Text(context.t('circle.myCircles'), style: Theme.of(context).textTheme.titleMedium),
                  const SizedBox(height: 8),
                  ...circles.map((c) => Padding(
                        padding: const EdgeInsets.only(bottom: 10),
                        child: _CircleInfoCard(circle: c),
                      )),
                ],
                const SizedBox(height: 80),
              ],
            );
          },
        ),
      ),
    );
  }
}

class _PendingInvitationsSection extends ConsumerStatefulWidget {
  const _PendingInvitationsSection();

  @override
  ConsumerState<_PendingInvitationsSection> createState() => _PendingInvitationsSectionState();
}

class _PendingInvitationsSectionState extends ConsumerState<_PendingInvitationsSection> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _linkStashedInviteToken());
  }

  /// Mirrors PendingInvitationsSection.tsx's mount effect: a WhatsApp invite opened by
  /// someone who wasn't signed in yet stashed its token (InviteScreen) — now that they're
  /// here and authenticated, attach it to their account so the invitation shows up below.
  /// Deliberately best-effort: an already-used/expired token just leaves the list as-is.
  Future<void> _linkStashedInviteToken() async {
    final token = await consumeStashedInviteToken();
    if (token == null || token.isEmpty) return;
    try {
      await ref.read(invitationsApiProvider).linkToken(token);
      if (!mounted) return;
      ref.read(refreshTickProvider.notifier).state++;
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(context.t('circle.inviteLinked'))));
    } catch (err) {
      if (!mounted) return;
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(extractErrorMessage(err, context.t('common.error')))));
    }
  }

  @override
  Widget build(BuildContext context) {
    final invitesAsync = ref.watch(pendingInvitationsProvider);
    return invitesAsync.when(
      loading: () => const SizedBox.shrink(),
      error: (_, __) => const SizedBox.shrink(),
      data: (invites) {
        if (invites.isEmpty) return const SizedBox.shrink();
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(children: [
              Icon(Icons.mark_email_unread_outlined, color: Theme.of(context).colorScheme.primary),
              const SizedBox(width: 8),
              Text(context.t('circle.pendingInvitations'), style: Theme.of(context).textTheme.titleMedium),
            ]),
            const SizedBox(height: 8),
            ...invites.map((invite) => _InvitationCard(invite: invite)),
            const SizedBox(height: 16),
          ],
        );
      },
    );
  }
}

class _InvitationCard extends ConsumerWidget {
  const _InvitationCard({required this.invite});
  final PendingInvitation invite;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final monthLabel = DateFormat.yMMMM(Localizations.localeOf(context).toString()).format(DateTime.parse(invite.startDate));
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      shape: RoundedRectangleBorder(
        side: BorderSide(color: Theme.of(context).colorScheme.primary.withOpacity(0.3)),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(invite.circleName, style: const TextStyle(fontWeight: FontWeight.bold)),
            Text(context.t('circle.invitedBy', {'name': invite.organizerName}), style: Theme.of(context).textTheme.bodySmall),
            Text(
              '${invite.contributionAmount} ${invite.currency} · ${invite.memberCount} ${context.t('circle.members')} · $monthLabel',
              style: Theme.of(context).textTheme.bodySmall,
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                FilledButton(
                  onPressed: () async {
                    await ref.read(invitationsApiProvider).accept(invite.memberId);
                    ref.read(refreshTickProvider.notifier).state++;
                  },
                  child: Text(context.t('circle.accept')),
                ),
                const SizedBox(width: 8),
                TextButton(
                  onPressed: () async {
                    await ref.read(invitationsApiProvider).decline(invite.memberId);
                    ref.read(refreshTickProvider.notifier).state++;
                  },
                  child: Text(context.t('circle.decline')),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _CircleInfoCard extends ConsumerWidget {
  const _CircleInfoCard({required this.circle});
  final CircleSummary circle;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    // Only an Active circle has a cycle to report collection/payout progress for, so the
    // dashboard is only fetched (and the badges only shown) in that case.
    final dashboard = circle.status == 'Active' ? ref.watch(dashboardProvider(circle.id)).valueOrNull : null;
    final created = DateFormat.yMMMd(Localizations.localeOf(context).toString()).format(DateTime.parse(circle.createdAt));
    return Card(
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: () => context.push('/circles/${circle.id}'),
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Expanded(child: Text(circle.name, style: const TextStyle(fontWeight: FontWeight.w600))),
                  Chip(
                    label: Text(context.t('circle.${circle.status.toLowerCase()}'), style: const TextStyle(color: Colors.white, fontSize: 11)),
                    backgroundColor: circleStatusColor(circle.status),
                    visualDensity: VisualDensity.compact,
                  ),
                ],
              ),
              if (dashboard != null) ...[
                const SizedBox(height: 4),
                CollectionPayoutBadges(
                  collected: dashboard.collected,
                  expected: dashboard.expected,
                  payoutStatus: dashboard.payoutStatus,
                ),
              ],
              const SizedBox(height: 4),
              Text(
                '${context.t('circle.memberCount')}: ${circle.memberCount} · ${context.t('circle.perMemberAmount')}: ${circle.contributionAmount} ${circle.currency}',
                style: Theme.of(context).textTheme.bodySmall,
              ),
              const SizedBox(height: 4),
              Text('${context.t('circle.organizer')}: ${circle.organizerName}', style: Theme.of(context).textTheme.bodySmall),
              Text('${context.t('circle.createdAt')}: $created', style: Theme.of(context).textTheme.bodySmall),
              if (!circle.isOrganizer) ...[
                const SizedBox(height: 4),
                Align(
                  alignment: AlignmentDirectional.centerStart,
                  child: Chip(label: Text(context.t('circle.viewOnly')), visualDensity: VisualDensity.compact),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}

class _CurrentCycleSummaryCard extends ConsumerWidget {
  const _CurrentCycleSummaryCard({required this.circle});
  final CircleSummary circle;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final dashboardAsync = ref.watch(dashboardProvider(circle.id));
    return dashboardAsync.when(
      loading: () => const SizedBox.shrink(),
      error: (_, __) => const SizedBox.shrink(),
      data: (dashboard) {
        if (dashboard == null) return const SizedBox.shrink();
        final progress = dashboard.expected > 0 ? (dashboard.collected / dashboard.expected).clamp(0, 1).toDouble() : 0.0;
        final monthLabel = DateFormat.yMMMM(Localizations.localeOf(context).toString()).format(DateTime.parse(dashboard.dueDate));
        final isArabic = Localizations.localeOf(context).languageCode == 'ar';
        // The circle's total month count — the same `schedule` query the timeline below reads.
        final totalMonths = (ref.watch(scheduleProvider(circle.id)).valueOrNull ?? const <ScheduleCycle>[]).length;

        return Card(
          child: InkWell(
            borderRadius: BorderRadius.circular(12),
            onTap: () => context.push('/circles/${circle.id}'),
            child: Padding(
              padding: const EdgeInsets.all(14),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Expanded(child: Text(circle.name, style: const TextStyle(fontWeight: FontWeight.bold))),
                      // The corner used to show the cycle's month; it now shows the circle's
                      // creation date (month + year), with the full dd/mm/yyyy behind a tooltip,
                      // bidi-isolated so the sequence can't be reordered inside Arabic text.
                      Tooltip(
                        triggerMode: TooltipTriggerMode.tap,
                        message: '${context.t('circle.createdAt')} ${ltrIsolate(DateFormat('dd/MM/yyyy').format(DateTime.parse(circle.createdAt).toLocal()))}',
                        child: Text(
                          DateFormat.yMMMM(Localizations.localeOf(context).toString()).format(DateTime.parse(circle.createdAt).toLocal()),
                          style: Theme.of(context).textTheme.bodySmall,
                        ),
                      ),
                    ],
                  ),
                  Text(
                    '${context.t('circle.organizer')}: ${circle.organizerName}. '
                    '${context.t('circle.members')}: ${circle.memberCount}. '
                    '${context.t('circle.installmentLabel')}: ${formatAmount(circle.contributionAmount)} ${circle.currency}.',
                    style: Theme.of(context).textTheme.bodySmall,
                  ),
                  // The same month-by-month timeline the circle-details page shows, reusing the
                  // identical widget and the same `schedule` provider entry.
                  CircleTimeline(circleId: circle.id),
                  const SizedBox(height: 4),
                  Wrap(spacing: 8, runSpacing: 4, crossAxisAlignment: WrapCrossAlignment.center, children: [
                    Text(context.t('circle.currentCycle'), style: const TextStyle(fontWeight: FontWeight.bold)),
                    Text(
                      context.t('circle.monthOrdinalDetail', {
                        'month': DateFormat.MMMM(Localizations.localeOf(context).toString()).format(DateTime.parse(dashboard.dueDate)),
                        'ordinal': monthOrdinalWord(dashboard.sequenceNumber, isArabic),
                        'total': '$totalMonths',
                      }),
                      style: Theme.of(context).textTheme.bodySmall,
                    ),
                  ]),
                  const SizedBox(height: 4),
                  Text.rich(TextSpan(children: [
                    TextSpan(text: '${context.t('circle.recipient')}: '),
                    TextSpan(text: dashboard.recipientName, style: const TextStyle(fontWeight: FontWeight.bold)),
                  ])),
                  const SizedBox(height: 6),
                  CollectionPayoutBadges(
                    collected: dashboard.collected,
                    expected: dashboard.expected,
                    payoutStatus: dashboard.payoutStatus,
                  ),
                  const SizedBox(height: 6),
                  Wrap(spacing: 8, children: [
                    Chip(
                      label: Text('${context.t('circle.paid')}: ${dashboard.membersPaid}/${dashboard.membersTotal}',
                          style: const TextStyle(color: Colors.white, fontSize: 11)),
                      backgroundColor: Colors.green,
                      visualDensity: VisualDensity.compact,
                    ),
                    if (dashboard.membersLate > 0)
                      Chip(
                        label: Text('${context.t('circle.late')}: ${dashboard.membersLate}', style: const TextStyle(color: Colors.white, fontSize: 11)),
                        backgroundColor: Colors.red,
                        visualDensity: VisualDensity.compact,
                      ),
                  ]),
                  const SizedBox(height: 8),
                  ClipRRect(
                    borderRadius: BorderRadius.circular(4),
                    child: LinearProgressIndicator(value: progress, minHeight: 8),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    '${dashboard.collected} / ${dashboard.expected} ${circle.currency} · ${context.t('circle.outstanding')}: ${dashboard.outstanding} ${circle.currency}',
                    style: Theme.of(context).textTheme.bodySmall,
                  ),
                  const SizedBox(height: 8),
                  Align(
                    alignment: AlignmentDirectional.centerEnd,
                    child: TextButton.icon(
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
                  ),
                ],
              ),
            ),
          ),
        );
      },
    );
  }
}
