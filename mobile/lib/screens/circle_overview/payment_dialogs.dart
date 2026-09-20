import 'dart:io';

import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';

import '../../api/api_client.dart';
import '../../l10n/app_localizations.dart';

/// The organizer's "record a contribution" dialog, shared by the Current Cycle tab and the
/// Monthly Cycles tab (which can target *any* month, not just the current one).
///
/// `POST /cycles/{id}/contributions` is additive (adds on top of what's already paid, capped
/// server-side at the outstanding balance), so this defaults to and caps at the member's
/// remaining outstanding amount for that one month and surfaces an inline error both for a
/// client-side over-limit and for a server rejection, instead of failing silently.
///
/// When [alternateTargets] is non-empty the dialog also offers a "تسجيل المبلغ على" picker under
/// the amount field, letting the same payment be redirected to any *other* month this member
/// hasn't fully paid yet (past or future) without leaving the current tab — picking one re-caps
/// the amount to that month's own outstanding, and the save targets that cycle instead.
class RecordPaymentDialog extends StatefulWidget {
  const RecordPaymentDialog({
    super.key,
    required this.title,
    required this.outstanding,
    required this.onSave,
    required this.onDone,
    this.alternateTargets = const [],
  });

  final String title;

  /// The default (current) target's remaining balance.
  final double outstanding;
  final List<PaymentTarget> alternateTargets;

  /// `targetCycleId` is null for the default target, or the picked month's cycle id.
  final Future<void> Function(double amount, int? targetCycleId) onSave;
  final VoidCallback onDone;

  @override
  State<RecordPaymentDialog> createState() => _RecordPaymentDialogState();
}

/// One alternate month offered by [RecordPaymentDialog]'s cross-month picker.
class PaymentTarget {
  const PaymentTarget({required this.cycleId, required this.label, required this.outstanding});

  final int cycleId;
  final String label;
  final double outstanding;
}

class _RecordPaymentDialogState extends State<RecordPaymentDialog> {
  late final controller = TextEditingController(text: widget.outstanding.toString());
  String? error;
  bool saving = false;

  /// null = the current cycle (the default target).
  int? targetCycleId;

  @override
  void dispose() {
    controller.dispose();
    super.dispose();
  }

  PaymentTarget? get _target =>
      targetCycleId == null
          ? null
          : widget.alternateTargets
              .where((t) => t.cycleId == targetCycleId)
              .cast<PaymentTarget?>()
              .firstWhere((_) => true, orElse: () => null);

  double get _outstanding => _target?.outstanding ?? widget.outstanding;

  void _pickTarget(int? cycleId) {
    setState(() {
      targetCycleId = cycleId;
      error = null;
      controller.text = _outstanding.toString();
    });
  }

  bool get _exceeds {
    final value = double.tryParse(controller.text);
    return value != null && value > _outstanding;
  }

  Future<void> _save() async {
    final amount = double.tryParse(controller.text) ?? 0;
    if (amount <= 0 || amount > _outstanding) {
      setState(() => error = context.t('circle.recordPaymentExceedsOutstanding'));
      return;
    }
    setState(() {
      saving = true;
      error = null;
    });
    try {
      await widget.onSave(amount, targetCycleId);
      widget.onDone();
      if (mounted) Navigator.of(context).pop();
    } catch (err) {
      setState(() => error = extractErrorMessage(err, context.t('common.error')));
    } finally {
      if (mounted) setState(() => saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: Text(widget.title),
      content: SingleChildScrollView(
        child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text('${context.t('circle.outstanding')}: $_outstanding', style: Theme.of(context).textTheme.bodySmall),
          const SizedBox(height: 8),
          TextField(
            controller: controller,
            keyboardType: const TextInputType.numberWithOptions(decimal: true),
            decoration: InputDecoration(
              labelText: context.t('circle.contributionAmount'),
              errorText: _exceeds ? context.t('circle.recordPaymentExceedsOutstanding') : null,
            ),
            onChanged: (_) => setState(() => error = null),
          ),
          if (widget.alternateTargets.isNotEmpty) ...[
            const SizedBox(height: 12),
            Text(context.t('circle.recordPaymentOnLabel'), style: Theme.of(context).textTheme.bodySmall),
            DropdownButton<int?>(
              value: targetCycleId,
              isExpanded: true,
              onChanged: saving ? null : _pickTarget,
              items: [
                DropdownMenuItem<int?>(value: null, child: Text(context.t('circle.currentCycle'))),
                ...widget.alternateTargets.map((t) => DropdownMenuItem<int?>(value: t.cycleId, child: Text(t.label))),
              ],
            ),
          ],
          if (error != null) Padding(padding: const EdgeInsets.only(top: 8), child: Text(error!, style: const TextStyle(color: Colors.red))),
        ]),
      ),
      actions: [
        TextButton(onPressed: saving ? null : () => Navigator.of(context).pop(), child: Text(context.t('common.cancel'))),
        FilledButton(onPressed: saving ? null : _save, child: Text(context.t('common.save'))),
      ],
    );
  }
}

/// The organizer's "confirm the recipient received their payout" dialog, shared by the Current
/// Cycle tab and the Monthly Cycles tab (which can confirm a payout for any month — a past one
/// still owed, the current one, or one paid ahead of schedule).
///
/// `POST /cycles/{id}/payout` is multipart/form-data and additive (adds to what's already been
/// paid, capped at what's still outstanding) rather than an all-or-nothing single shot, so the
/// amount defaults to and caps at that month's own remaining payout balance and an evidence
/// file can be attached to each installment.
class ConfirmPayoutDialog extends StatefulWidget {
  const ConfirmPayoutDialog({
    super.key,
    required this.title,
    required this.outstanding,
    required this.onSave,
    required this.onDone,
    this.unpaidCount = 0,
  });

  final String title;
  final double outstanding;
  final int unpaidCount;
  final Future<void> Function(double amount, String? method, String? notes, File? evidence) onSave;
  final VoidCallback onDone;

  @override
  State<ConfirmPayoutDialog> createState() => _ConfirmPayoutDialogState();
}

class _ConfirmPayoutDialogState extends State<ConfirmPayoutDialog> {
  late final amount = TextEditingController(text: widget.outstanding.toString());
  final notes = TextEditingController();
  File? evidence;
  String? error;
  bool saving = false;

  @override
  void dispose() {
    amount.dispose();
    notes.dispose();
    super.dispose();
  }

  bool get _exceeds {
    final value = double.tryParse(amount.text);
    return value != null && value > widget.outstanding;
  }

  Future<void> _pickFile() async {
    final result = await FilePicker.platform.pickFiles(type: FileType.custom, allowedExtensions: ['jpg', 'jpeg', 'png', 'pdf']);
    if (result == null || result.files.single.path == null) return;
    setState(() => evidence = File(result.files.single.path!));
  }

  Future<void> _save() async {
    final value = double.tryParse(amount.text) ?? 0;
    if (value <= 0 || value > widget.outstanding) {
      setState(() => error = context.t('circle.recordPaymentExceedsOutstanding'));
      return;
    }
    setState(() {
      saving = true;
      error = null;
    });
    try {
      await widget.onSave(value, null, notes.text.trim().isEmpty ? null : notes.text.trim(), evidence);
      widget.onDone();
      if (mounted) Navigator.of(context).pop();
    } catch (err) {
      setState(() => error = extractErrorMessage(err, context.t('common.error')));
    } finally {
      if (mounted) setState(() => saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: Text(widget.title),
      content: SingleChildScrollView(
        child: Column(mainAxisSize: MainAxisSize.min, crossAxisAlignment: CrossAxisAlignment.start, children: [
          if (widget.unpaidCount > 0)
            Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: Text('${context.t('circle.unpaid')}: ${widget.unpaidCount}', style: const TextStyle(color: Colors.orange)),
            ),
          Text('${context.t('circle.outstanding')}: ${widget.outstanding}', style: Theme.of(context).textTheme.bodySmall),
          const SizedBox(height: 8),
          TextField(
            controller: amount,
            keyboardType: const TextInputType.numberWithOptions(decimal: true),
            decoration: InputDecoration(
              labelText: context.t('circle.expectedPool'),
              errorText: _exceeds ? context.t('circle.recordPaymentExceedsOutstanding') : null,
            ),
            onChanged: (_) => setState(() => error = null),
          ),
          const SizedBox(height: 12),
          TextField(controller: notes, decoration: InputDecoration(labelText: context.t('circle.payoutNotes')), maxLines: 2),
          const SizedBox(height: 12),
          OutlinedButton.icon(onPressed: _pickFile, icon: const Icon(Icons.upload_file), label: Text(context.t('circle.payoutEvidence'))),
          if (evidence != null)
            Padding(
              padding: const EdgeInsets.only(top: 4),
              child: Text(evidence!.path.split(Platform.pathSeparator).last, style: Theme.of(context).textTheme.bodySmall),
            ),
          if (error != null) Padding(padding: const EdgeInsets.only(top: 8), child: Text(error!, style: const TextStyle(color: Colors.red))),
        ]),
      ),
      actions: [
        TextButton(onPressed: saving ? null : () => Navigator.of(context).pop(), child: Text(context.t('common.cancel'))),
        FilledButton(onPressed: saving ? null : _save, child: Text(context.t('common.confirm'))),
      ],
    );
  }
}
