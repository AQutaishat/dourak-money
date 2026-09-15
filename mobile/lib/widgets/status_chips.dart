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

class ClaimStatusChip extends StatelessWidget {
  const ClaimStatusChip({super.key, required this.status});
  final String status;

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
    return Chip(
      label: Text(context.t('circle.claim$status'), style: const TextStyle(color: Colors.white, fontSize: 11)),
      backgroundColor: color,
      visualDensity: VisualDensity.compact,
      padding: EdgeInsets.zero,
    );
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
