import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../api/api_client.dart';
import '../../l10n/app_localizations.dart';
import '../../state/providers.dart';

/// Mirrors ActivateCircleButton.tsx — the persistent action beneath all tabs on a
/// draft circle, with the member-order confirmation dialog.
class ActivateCircleButton extends ConsumerStatefulWidget {
  const ActivateCircleButton({super.key, required this.circleId});
  final int circleId;

  @override
  ConsumerState<ActivateCircleButton> createState() => _ActivateCircleButtonState();
}

class _ActivateCircleButtonState extends ConsumerState<ActivateCircleButton> {
  bool activating = false;

  Future<void> _activate() async {
    String? error;
    setState(() => activating = true);
    try {
      await ref.read(circlesApiProvider).activate(widget.circleId);
      ref.read(refreshTickProvider.notifier).state++;
      if (mounted) Navigator.of(context).pop();
    } catch (err) {
      if (mounted) {
        final raw = extractErrorMessage(err, context.t('common.error'));
        // Mirrors ActivateCircleButton.tsx's specific mapping of the "still has a
        // Pending invitee" domain error to a proper localized message instead of
        // showing the raw backend text.
        error = raw.toLowerCase().contains('responded') ? context.t('circle.activateBlockedPendingInvites') : raw;
      }
    } finally {
      if (mounted) setState(() => activating = false);
      if (error != null && mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(error)));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return FilledButton(
      onPressed: () => showDialog(
        context: context,
        builder: (dialogContext) => AlertDialog(
          title: Text(context.t('circle.activateConfirmTitle')),
          content: Text(context.t('circle.activateConfirmMessage')),
          actions: [
            TextButton(onPressed: () => Navigator.of(dialogContext).pop(), child: Text(context.t('common.cancel'))),
            FilledButton(
              onPressed: activating ? null : _activate,
              child: Text(context.t('common.confirm')),
            ),
          ],
        ),
      ),
      child: Text(context.t('circle.activate')),
    );
  }
}
