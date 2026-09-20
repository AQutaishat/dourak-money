import 'dart:io';

import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../api/api_client.dart';
import '../../api/models.dart';
import '../../l10n/app_localizations.dart';
import '../../state/providers.dart';
import '../../widgets/evidence_viewer.dart';
import '../../widgets/status_chips.dart';
import 'payment_dialogs.dart';

const _maxEvidenceBytes = 5 * 1024 * 1024;

/// Mirrors SubmitPaymentClaimDialog in PaymentClaimDialogs.tsx — a member self-report
/// with optional evidence, never marks the contribution paid on its own.
///
/// Like the organizer's [RecordPaymentDialog], it offers a cross-month picker when
/// [alternateTargets] is non-empty: the member can redirect their report to any *other* month
/// they still owe for (past or future) instead of only the current cycle. Picking one re-caps
/// the claimed amount to that month's own outstanding and submits the claim against that cycle.
/// [initialTargetCycleId] preselects an alternate — used when the current cycle is already fully
/// paid, so the dialog never opens on a dead-end "0 outstanding" target.
class SubmitPaymentClaimDialog extends ConsumerStatefulWidget {
  const SubmitPaymentClaimDialog({
    super.key,
    required this.circleId,
    required this.cycleId,
    required this.outstanding,
    required this.currency,
    this.alternateTargets = const [],
    this.initialTargetCycleId,
  });

  final int circleId;
  final int cycleId;
  final double outstanding;
  final String currency;
  final List<PaymentTarget> alternateTargets;
  final int? initialTargetCycleId;

  @override
  ConsumerState<SubmitPaymentClaimDialog> createState() => _SubmitPaymentClaimDialogState();
}

class _SubmitPaymentClaimDialogState extends ConsumerState<SubmitPaymentClaimDialog> {
  late final amount = TextEditingController(text: _initialOutstanding.toString());
  final note = TextEditingController();
  File? evidence;
  String? error;
  bool submitting = false;

  /// null = the current cycle (the default target).
  late int? targetCycleId = widget.initialTargetCycleId;

  PaymentTarget? _targetFor(int? cycleId) => cycleId == null
      ? null
      : widget.alternateTargets.where((t) => t.cycleId == cycleId).cast<PaymentTarget?>().firstWhere((_) => true, orElse: () => null);

  double get _initialOutstanding => _targetFor(widget.initialTargetCycleId)?.outstanding ?? widget.outstanding;

  double get _outstanding => _targetFor(targetCycleId)?.outstanding ?? widget.outstanding;

  void _pickTarget(int? cycleId) {
    setState(() {
      targetCycleId = cycleId;
      error = null;
      amount.text = _outstanding.toString();
    });
  }

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
    if (value <= 0 || value > _outstanding) return;
    setState(() {
      submitting = true;
      error = null;
    });
    try {
      await ref.read(paymentClaimsApiProvider).submit(
            targetCycleId ?? widget.cycleId,
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
              decoration: InputDecoration(
                labelText: '${context.t('circle.claimAmount')} (${widget.currency})',
                errorText: value > _outstanding ? context.t('circle.recordPaymentExceedsOutstanding') : null,
              ),
              onChanged: (_) => setState(() {}),
            ),
            if (widget.alternateTargets.isNotEmpty) ...[
              const SizedBox(height: 12),
              Text(context.t('circle.recordPaymentOnLabel'), style: Theme.of(context).textTheme.bodySmall),
              DropdownButton<int?>(
                value: targetCycleId,
                isExpanded: true,
                onChanged: submitting ? null : _pickTarget,
                items: [
                  // Only offered when the current cycle actually still has something owing —
                  // otherwise the picker would open on a dead-end 0-outstanding target.
                  if (widget.outstanding > 0)
                    DropdownMenuItem<int?>(value: null, child: Text(context.t('circle.currentCycle'))),
                  ...widget.alternateTargets.map((t) => DropdownMenuItem<int?>(value: t.cycleId, child: Text(t.label))),
                ],
              ),
            ],
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
          onPressed: (submitting || value <= 0 || value > _outstanding) ? null : _submit,
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

/// Mirrors ClaimDetailDialog — one claim's detail, opened from its status badge. Approve/reject
/// only show while the claim is still Pending and the viewer can review it; otherwise it's a
/// read-only view of the same details (rejection reason and evidence included).
class ClaimDetailDialog extends ConsumerStatefulWidget {
  const ClaimDetailDialog({super.key, required this.claim, required this.canReview});
  final PaymentClaim claim;
  final bool canReview;

  @override
  ConsumerState<ClaimDetailDialog> createState() => _ClaimDetailDialogState();
}

class _ClaimDetailDialogState extends ConsumerState<ClaimDetailDialog> {
  bool rejecting = false;
  bool busy = false;
  String? error;
  final reason = TextEditingController();

  @override
  void dispose() {
    reason.dispose();
    super.dispose();
  }

  Future<void> _review(bool approve, {String? rejectionReason}) async {
    setState(() {
      busy = true;
      error = null;
    });
    try {
      await ref.read(paymentClaimsApiProvider).review(widget.claim.id, approve: approve, rejectionReason: rejectionReason);
      ref.read(refreshTickProvider.notifier).state++;
      if (mounted) Navigator.of(context).pop();
    } catch (err) {
      setState(() => error = extractErrorMessage(err, context.t('common.error')));
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final canAct = widget.canReview && widget.claim.status == 'Pending';
    return AlertDialog(
      title: Text(context.t('circle.paymentClaims')),
      content: SingleChildScrollView(
        child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
          if (error != null) Padding(padding: const EdgeInsets.only(bottom: 8), child: Text(error!, style: const TextStyle(color: Colors.red))),
          _ClaimCard(
            claim: widget.claim,
            child: !canAct
                ? null
                : rejecting
                    ? Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                        TextField(controller: reason, decoration: InputDecoration(labelText: context.t('circle.rejectReason')), maxLines: 2),
                        const SizedBox(height: 4),
                        Wrap(spacing: 8, children: [
                          TextButton(onPressed: busy ? null : () => setState(() => rejecting = false), child: Text(context.t('common.cancel'))),
                          FilledButton(
                            style: FilledButton.styleFrom(backgroundColor: Colors.red),
                            onPressed: busy ? null : () => _review(false, rejectionReason: reason.text.trim().isEmpty ? null : reason.text.trim()),
                            child: Text(context.t('circle.reject')),
                          ),
                        ]),
                      ])
                    : Wrap(spacing: 8, children: [
                        FilledButton(
                          style: FilledButton.styleFrom(backgroundColor: Colors.green),
                          onPressed: busy ? null : () => _review(true),
                          child: Text(context.t('circle.approve')),
                        ),
                        TextButton(
                          style: TextButton.styleFrom(foregroundColor: Colors.red),
                          onPressed: busy ? null : () => setState(() => rejecting = true),
                          child: Text(context.t('circle.reject')),
                        ),
                      ]),
          ),
        ]),
      ),
      actions: [TextButton(onPressed: () => Navigator.of(context).pop(), child: Text(context.t('common.close')))],
    );
  }
}

/// Mirrors MyClaimDialog — the submitting member's own view of their still-Pending claim:
/// amount/note/evidence are editable, and it can be withdrawn ("unsent") outright.
class MyClaimDialog extends ConsumerStatefulWidget {
  const MyClaimDialog({super.key, required this.claim, required this.outstanding, required this.currency});
  final PaymentClaim claim;
  final double outstanding;
  final String currency;

  @override
  ConsumerState<MyClaimDialog> createState() => _MyClaimDialogState();
}

class _MyClaimDialogState extends ConsumerState<MyClaimDialog> {
  late final amount = TextEditingController(text: widget.claim.claimedAmount.toString());
  late final note = TextEditingController(text: widget.claim.note ?? '');
  File? evidence;
  bool removeEvidence = false;
  bool confirmingWithdraw = false;
  bool busy = false;
  String? error;

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
      if (mounted) setState(() => error = context.t('circle.claimEvidenceHint'));
      return;
    }
    setState(() {
      error = null;
      evidence = file;
      removeEvidence = false;
    });
  }

  Future<void> _run(Future<void> Function() action) async {
    setState(() {
      busy = true;
      error = null;
    });
    try {
      await action();
      ref.read(refreshTickProvider.notifier).state++;
      if (mounted) Navigator.of(context).pop();
    } catch (err) {
      setState(() {
        error = extractErrorMessage(err, context.t('common.error'));
        confirmingWithdraw = false;
      });
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final value = double.tryParse(amount.text) ?? 0;
    final exceeds = value > widget.outstanding;
    final api = ref.read(paymentClaimsApiProvider);
    final evidenceLabel = evidence != null
        ? evidence!.path.split(Platform.pathSeparator).last
        : widget.claim.hasEvidence && !removeEvidence
            ? (widget.claim.evidenceFileName ?? context.t('circle.viewEvidence'))
            : context.t('circle.claimEvidenceHint');

    return AlertDialog(
      title: Text(context.t('circle.submitClaim')),
      content: SingleChildScrollView(
        child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
          if (error != null) Padding(padding: const EdgeInsets.only(bottom: 8), child: Text(error!, style: const TextStyle(color: Colors.red))),
          TextField(
            controller: amount,
            keyboardType: const TextInputType.numberWithOptions(decimal: true),
            decoration: InputDecoration(
              labelText: '${context.t('circle.claimAmount')} (${widget.currency})',
              errorText: exceeds ? context.t('circle.recordPaymentExceedsOutstanding') : null,
            ),
            onChanged: (_) => setState(() => error = null),
          ),
          const SizedBox(height: 12),
          TextField(controller: note, decoration: InputDecoration(labelText: context.t('circle.claimNote')), maxLines: 2),
          const SizedBox(height: 12),
          OutlinedButton.icon(onPressed: busy ? null : _pickFile, icon: const Icon(Icons.upload_file), label: Text(context.t('circle.claimEvidence'))),
          const SizedBox(height: 4),
          Text(evidenceLabel, style: Theme.of(context).textTheme.bodySmall),
          if (widget.claim.hasEvidence && evidence == null && !removeEvidence)
            TextButton(
              style: TextButton.styleFrom(foregroundColor: Colors.red),
              onPressed: busy ? null : () => setState(() => removeEvidence = true),
              child: Text(context.t('common.delete')),
            ),
          const SizedBox(height: 8),
          if (confirmingWithdraw)
            Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(context.t('circle.withdrawClaimConfirm'), style: const TextStyle(color: Colors.orange)),
              const SizedBox(height: 4),
              Wrap(spacing: 8, children: [
                TextButton(onPressed: busy ? null : () => setState(() => confirmingWithdraw = false), child: Text(context.t('common.cancel'))),
                FilledButton(
                  style: FilledButton.styleFrom(backgroundColor: Colors.red),
                  onPressed: busy ? null : () => _run(() => api.withdraw(widget.claim.id)),
                  child: Text(context.t('circle.withdrawClaim')),
                ),
              ]),
            ])
          else
            TextButton(
              style: TextButton.styleFrom(foregroundColor: Colors.red),
              onPressed: busy ? null : () => setState(() => confirmingWithdraw = true),
              child: Text(context.t('circle.withdrawClaim')),
            ),
        ]),
      ),
      actions: [
        TextButton(onPressed: busy ? null : () => Navigator.of(context).pop(), child: Text(context.t('common.cancel'))),
        FilledButton(
          onPressed: (busy || value <= 0 || exceeds)
              ? null
              : () => _run(() => api.update(
                    widget.claim.id,
                    claimedAmount: value,
                    note: note.text.trim().isEmpty ? null : note.text.trim(),
                    removeEvidence: removeEvidence,
                    evidence: evidence,
                  )),
          child: Text(context.t('common.save')),
        ),
      ],
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
                final download = await ref.read(paymentClaimsApiProvider).evidence(claim.id);
                if (context.mounted) await showEvidence(context, download);
              },
              child: Text(context.t('circle.viewEvidence')),
            ),
          if (child != null) Padding(padding: const EdgeInsets.only(top: 4), child: child),
        ],
      ),
    );
  }
}
