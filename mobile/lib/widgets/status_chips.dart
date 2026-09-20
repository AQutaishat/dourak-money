import 'package:flutter/material.dart';

import '../l10n/app_localizations.dart';

/// Mirrors frontend/src/pages/CircleOverview/StatusChip.tsx — same color mapping,
/// used identically across the current-cycle table, member history, and dashboard.
Color _contributionColor(BuildContext context, String status) {
  final scheme = Theme.of(context).colorScheme;
  switch (status) {
    case 'Paid':
      return Colors.green;
    case 'PartiallyPaid':
      return Colors.orange;
    case 'Late':
      return Colors.red;
    default:
      return scheme.surfaceContainerHighest;
  }
}

class ContributionStatusChip extends StatelessWidget {
  const ContributionStatusChip({super.key, required this.status});
  final String status;

  @override
  Widget build(BuildContext context) {
    final key = status == 'PartiallyPaid' ? 'partiallyPaid' : status[0].toLowerCase() + status.substring(1);
    final color = _contributionColor(context, status);
    return Chip(
      label: Text(context.t('circle.$key'), style: TextStyle(color: color == Colors.grey.shade300 ? null : Colors.white, fontSize: 12)),
      backgroundColor: color,
      visualDensity: VisualDensity.compact,
      padding: EdgeInsets.zero,
    );
  }
}

class InvitationStatusChip extends StatelessWidget {
  const InvitationStatusChip({super.key, required this.status});
  final String status;

  @override
  Widget build(BuildContext context) {
    Color color;
    switch (status) {
      case 'Accepted':
        color = Colors.green;
        break;
      case 'Pending':
        color = Colors.orange;
        break;
      case 'Declined':
        color = Colors.red;
        break;
      default:
        color = Colors.grey;
    }
    return Chip(
      label: Text(context.t('circle.invitation$status'), style: TextStyle(color: color, fontSize: 11)),
      side: BorderSide(color: color),
      backgroundColor: Colors.transparent,
      visualDensity: VisualDensity.compact,
      padding: EdgeInsets.zero,
    );
  }
}

/// The single, canonical claim-status badge — same color and wording everywhere a payment
/// claim's status is shown (Current Cycle tab, for both the organizer's and a member's own
/// row, and the Monthly Cycles tab). Mirrors the web's unified `ClaimStatusChip`, including
/// the "دفعة معلقة"/"دفعة مقبولة" wording it settled on for Pending/Approved.
class ClaimStatusChip extends StatelessWidget {
  const ClaimStatusChip({super.key, required this.status, this.onTap});
  final String status;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    Color color;
    switch (status) {
      case 'Approved':
        color = Colors.green;
        break;
      case 'Rejected':
        color = Colors.red;
        break;
      default:
        color = Colors.orange;
    }
    final label = status == 'Pending'
        ? context.t('circle.pendingPaymentBadge')
        : status == 'Approved'
            ? context.t('circle.approvedPaymentBadge')
            : context.t('circle.claim$status');
    final chip = Chip(
      label: Text(label, style: const TextStyle(color: Colors.white, fontSize: 11)),
      backgroundColor: color,
      visualDensity: VisualDensity.compact,
      padding: EdgeInsets.zero,
    );
    if (onTap == null) return chip;
    return InkWell(borderRadius: BorderRadius.circular(16), onTap: onTap, child: chip);
  }
}

/// A plain filled badge — the shared building block behind the month/collection/payout badges
/// the Monthly Cycles tab, the Current Cycle tab and the home-screen card all show.
class SimpleBadge extends StatelessWidget {
  const SimpleBadge({super.key, required this.label, required this.color});
  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Chip(
      label: Text(label, style: const TextStyle(color: Colors.white, fontSize: 11)),
      backgroundColor: color,
      visualDensity: VisualDensity.compact,
      padding: EdgeInsets.zero,
    );
  }
}

/// The collection ("تحت التحصيل"/"تم التحصيل") and payout ("بانتظار الدفع لصاحب الدور"/"تم
/// الدفع لصاحب الدور") badges, rendered exactly as the web does: collection always, and the
/// payout badge once collection is done *or* the payout has already been paid (the organizer
/// may pay the recipient ahead of finishing collection, and hiding that would be misleading).
class CollectionPayoutBadges extends StatelessWidget {
  const CollectionPayoutBadges({super.key, required this.collected, required this.expected, required this.payoutStatus});
  final double collected;
  final double expected;
  final String payoutStatus;

  @override
  Widget build(BuildContext context) {
    final done = expected > 0 && collected >= expected;
    final paidOut = payoutStatus == 'Paid';
    return Wrap(spacing: 6, runSpacing: 4, children: [
      SimpleBadge(
        label: context.t(done ? 'circle.collectionDone' : 'circle.collectionUnderway'),
        color: done ? Colors.green : Colors.orange,
      ),
      if (done || paidOut)
        SimpleBadge(
          label: context.t(paidOut ? 'circle.payoutPaidBadge' : 'circle.payoutPendingBadge'),
          color: paidOut ? Colors.green : Colors.blue,
        ),
    ]);
  }
}

/// Circle-status badge used on dashboard/my-circles cards — mirrors CircleInfoCard.tsx's
/// STATUS_COLOR map.
Color circleStatusColor(String status) {
  switch (status) {
    case 'Active':
      return Colors.green;
    case 'Draft':
      return Colors.blue;
    case 'Paused':
      return Colors.orange;
    default:
      return Colors.grey;
  }
}
