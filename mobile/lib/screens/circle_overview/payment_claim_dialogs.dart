import 'dart:io';

import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../api/api_client.dart';
import '../../api/models.dart';
import '../../l10n/app_localizations.dart';
import '../../state/providers.dart';
import '../../widgets/status_chips.dart';

const _maxEvidenceBytes = 5 * 1024 * 1024;

/// Mirrors SubmitPaymentClaimDialog in PaymentClaimDialogs.tsx — a member self-report
/// with optional evidence, never marks the contribution paid on its own.
class SubmitPaymentClaimDialog extends ConsumerStatefulWidget {
  const SubmitPaymentClaimDialog({
    super.key,
    required this.circleId,
    required this.cycleId,
    required this.outstanding,
    required this.currency,
  });

  final int circleId;
  final int cycleId;
  final double outstanding;
  final String currency;

  @override
  ConsumerState<SubmitPaymentClaimDialog> createState() => _SubmitPaymentClaimDialogState();
}

class _SubmitPaymentClaimDialogState extends ConsumerState<SubmitPaymentClaimDialog> {
  late final amount = TextEditingController(text: widget.outstanding.toString());
  final note = TextEditingController();
  File? evidence;
  String? error;
  bool submitting = false;

  @override
  void dispose() {
    amount.dispose();
    note.dispose();
    super.dispose();
  }

  Future<void> _pickFile() async {
    final result = await FilePicker.platform.pickFiles(type: FileType.custom, allowedExtensions: ['jpg', 'jpeg', 'png', 'pdf']);
    if (result == null || result.files.single.path == null) return;
    final file = File(result.files.single.path!);
    if (await file.length() > _maxEvidenceBytes) {
      setState(() => error = context.t('circle.claimEvidenceHint'));
      return;
    }
    setState(() {
      error = null;
      evidence = file;
    });
  }

  Future<void> _submit() async {
    final value = double.tryParse(amount.text) ?? 0;
    if (value <= 0 || value > widget.outstanding) return;
    setState(() {
      submitting = true;
      error = null;
    });
    try {
      await ref.read(paymentClaimsApiProvider).submit(
            widget.cycleId,
            claimedAmount: value,
            note: note.text.trim().isEmpty ? null : note.text.trim(),
            evidence: evidence,
          );
      ref.read(refreshTickProvider.notifier).state++;
      if (mounted) Navigator.of(context).pop();
    } catch (err) {
      setState(() => error = extractErrorMessage(err, context.t('common.error')));
    } finally {
      if (mounted) setState(() => submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final value = double.tryParse(amount.text) ?? 0;
    return AlertDialog(
      title: Text(context.t('circle.submitClaim')),
      content: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (error != null) ...[Text(error!, style: const TextStyle(color: Colors.red)), const SizedBox(height: 8)],
            TextField(
              controller: amount,
              keyboardType: const TextInputType.numberWithOptions(decimal: true),
              decoration: InputDecoration(labelText: '${context.t('circle.claimAmount')} (${widget.currency})'),
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: 12),
            TextField(controller: note, decoration: InputDecoration(labelText: context.t('circle.claimNote')), maxLines: 2),
            const SizedBox(height: 12),
            OutlinedButton.icon(
              onPressed: _pickFile,
              icon: const Icon(Icons.upload_file),
              label: Text(context.t('circle.claimEvidence')),
            ),
            const SizedBox(height: 4),
            Text(
              evidence != null ? evidence!.path.split('/').last : context.t('circle.claimEvidenceHint'),
              style: Theme.of(context).textTheme.bodySmall,
            ),
          ],
        ),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.of(context).pop(), child: Text(context.t('common.cancel'))),
        FilledButton(
          onPressed: (submitting || value <= 0 || value > widget.outstanding) ? null : _submit,
          child: Text(context.t('common.submit')),
        ),
      ],
    );
  }
}

/// Mirrors ReviewPaymentClaimsDialog — the organizer's approve/reject inbox.
class ReviewPaymentClaimsDialog extends ConsumerStatefulWidget {
  const ReviewPaymentClaimsDialog({super.key, required this.circleId});
  final int circleId;

  @override
  ConsumerState<ReviewPaymentClaimsDialog> createState() => _ReviewPaymentClaimsDialogState();
}

class _ReviewPaymentClaimsDialogState extends ConsumerState<ReviewPaymentClaimsDialog> {
  int? rejectingId;
  final reason = TextEditingController();

  @override
  void dispose() {
    reason.dispose();
    super.dispose();
  }

  Future<void> _review(int claimId, bool approve, {String? rejectionReason}) async {
    await ref.read(paymentClaimsApiProvider).review(claimId, approve: approve, rejectionReason: rejectionReason);
    ref.read(refreshTickProvider.notifier).state++;
    setState(() {
      rejectingId = null;
      reason.clear();
    });
  }

  @override
  Widget build(BuildContext context) {
    final claimsAsync = ref.watch(paymentClaimsProvider(widget.circleId));
    return AlertDialog(
      title: Text(context.t('circle.paymentClaims')),
      content: SizedBox(
        width: 420,
        child: claimsAsync.when(
          loading: () => const SizedBox(height: 100, child: Center(child: CircularProgressIndicator())),
          error: (_, __) => Text(context.t('common.error')),
          data: (claims) {
            final pending = claims.where((c) => c.status == 'Pending').toList();
            final reviewed = claims.where((c) => c.status != 'Pending').toList();
            return SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  if (pending.isEmpty) Text(context.t('circle.noPendingClaims')),
                  ...pending.map((claim) => _ClaimCard(
                        claim: claim,
                        child: rejectingId == claim.id
                            ? Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                                TextField(
                                  controller: reason,
                                  decoration: InputDecoration(labelText: context.t('circle.rejectReason')),
                                  maxLines: 2,
                                ),
                                const SizedBox(height: 4),
                                Row(children: [
                                  TextButton(onPressed: () => setState(() => rejectingId = null), child: Text(context.t('common.cancel'))),
                                  FilledButton(
                                    style: FilledButton.styleFrom(backgroundColor: Colors.red),
                                    onPressed: () => _review(claim.id, false, rejectionReason: reason.text.trim().isEmpty ? null : reason.text.trim()),
                                    child: Text(context.t('circle.reject')),
                                  ),
                                ]),
                              ])
                            : Row(children: [
                                FilledButton(
                                  style: FilledButton.styleFrom(backgroundColor: Colors.green),
                                  onPressed: () => _review(claim.id, true),
                                  child: Text(context.t('circle.approve')),
                                ),
                                const SizedBox(width: 8),
                                TextButton(
                                  style: TextButton.styleFrom(foregroundColor: Colors.red),
                                  onPressed: () => setState(() => rejectingId = claim.id),
                                  child: Text(context.t('circle.reject')),
                                ),
                              ]),
                      )),
                  if (reviewed.isNotEmpty) ...[
                    const Divider(),
                    ...reviewed.map((claim) => _ClaimCard(claim: claim)),
                  ],
                ],
              ),
            );
          },
        ),
      ),
      actions: [TextButton(onPressed: () => Navigator.of(context).pop(), child: Text(context.t('common.close')))],
    );
  }
}

class _ClaimCard extends ConsumerWidget {
  const _ClaimCard({required this.claim, this.child});
  final PaymentClaim claim;
  final Widget? child;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final monthLabel = DateFormat.yMMMM(Localizations.localeOf(context).toString()).format(DateTime.parse(claim.dueDate));
    return Container(
      margin: const EdgeInsets.only(bottom: 10),
      padding: const EdgeInsets.all(10),
      decoration: BoxDecoration(border: Border.all(color: Colors.grey.shade300), borderRadius: BorderRadius.circular(10)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [
            Expanded(child: Text(context.t('circle.claimSubmittedBy', {'name': claim.memberName}), style: const TextStyle(fontWeight: FontWeight.w600))),
            ClaimStatusChip(status: claim.status),
          ]),
          Text('${claim.claimedAmount} / ${claim.expectedAmount} ${claim.currency} · $monthLabel', style: Theme.of(context).textTheme.bodySmall),
          if (claim.note != null && claim.note!.isNotEmpty) Text(claim.note!),
          if (claim.status == 'Rejected' && claim.rejectionReason != null)
            Padding(
              padding: const EdgeInsets.only(top: 4),
              child: Text(context.t('circle.claimRejectedReason', {'reason': claim.rejectionReason!}), style: const TextStyle(color: Colors.orange)),
            ),
          if (claim.hasEvidence)
            TextButton(
              onPressed: () async {
                await ref.read(paymentClaimsApiProvider).evidenceBytes(claim.id);
                if (context.mounted) {
                  ScaffoldMessenger.of(context).showSnackBar(
                    SnackBar(content: Text(context.t('circle.viewEvidence'))),
                  );
                }
              },
              child: Text(context.t('circle.viewEvidence')),
            ),
          if (child != null) Padding(padding: const EdgeInsets.only(top: 4), child: child),
        ],
      ),
    );
  }
}
