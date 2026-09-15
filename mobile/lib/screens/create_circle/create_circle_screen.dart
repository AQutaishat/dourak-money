import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../api/api_client.dart';
import '../../l10n/app_localizations.dart';
import '../../state/providers.dart';
import '../../widgets/validated_text_field.dart';

// prompt02 §Create Circle: JOD added to the supported currencies (mirrors CreateCirclePage.tsx).
const _currencies = ['SAR', 'JOD', 'USD', 'EGP', 'AED', 'KWD', 'QAR', 'MAD'];

class CreateCircleScreen extends ConsumerStatefulWidget {
  const CreateCircleScreen({super.key});

  @override
  ConsumerState<CreateCircleScreen> createState() => _CreateCircleScreenState();
}

class _CreateCircleScreenState extends ConsumerState<CreateCircleScreen> {
  late final name = ValidatedController(['required']);
  final description = TextEditingController();
  final amount = TextEditingController(text: '0');
  String currency = 'JOD';
  DateTime startDate = DateTime.now();
  String? error;
  bool submitting = false;

  @override
  void dispose() {
    name.dispose();
    description.dispose();
    amount.dispose();
    super.dispose();
  }

  Future<void> _pickDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: startDate,
      firstDate: DateTime(2000),
      lastDate: DateTime(2100),
    );
    if (picked != null) setState(() => startDate = picked);
  }

  Future<void> _submit() async {
    setState(() => error = null);
    if (!name.validateNow(context)) return;
    final contribution = double.tryParse(amount.text) ?? 0;
    if (contribution <= 0) return;

    setState(() => submitting = true);
    try {
      final id = await ref.read(circlesApiProvider).create(
            name: name.text.text.trim(),
            description: description.text.trim().isEmpty ? null : description.text.trim(),
            currency: currency,
            contributionAmount: contribution,
            startDate: startDate.toIso8601String().substring(0, 10),
          );
      ref.read(refreshTickProvider.notifier).state++;
      if (mounted) context.pushReplacement('/circles/$id');
    } catch (err) {
      setState(() => error = extractErrorMessage(err, context.t('common.error')));
    } finally {
      if (mounted) setState(() => submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final contribution = double.tryParse(amount.text) ?? 0;
    return Scaffold(
      appBar: AppBar(title: Text(context.t('circle.createCircle'))),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (error != null) ...[
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(color: Colors.red.shade50, borderRadius: BorderRadius.circular(8)),
                  child: Text(error!, style: TextStyle(color: Colors.red.shade800)),
                ),
                const SizedBox(height: 16),
              ],
              ValidatedTextField(controller: name, label: context.t('circle.name')),
              const SizedBox(height: 16),
              TextField(
                controller: description,
                decoration: InputDecoration(labelText: context.t('circle.description')),
                maxLines: 2,
              ),
              const SizedBox(height: 16),
              Row(
                children: [
                  Expanded(
                    child: DropdownButtonFormField<String>(
                      value: currency,
                      decoration: InputDecoration(labelText: context.t('circle.currency')),
                      items: _currencies.map((c) => DropdownMenuItem(value: c, child: Text(c))).toList(),
                      onChanged: (v) => setState(() => currency = v ?? currency),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    flex: 2,
                    child: TextField(
                      controller: amount,
                      keyboardType: const TextInputType.numberWithOptions(decimal: true),
                      decoration: InputDecoration(labelText: context.t('circle.contributionAmount')),
                      onChanged: (_) => setState(() {}),
                      onTap: () => amount.selection = TextSelection(baseOffset: 0, extentOffset: amount.text.length),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 16),
              InkWell(
                onTap: _pickDate,
                child: InputDecorator(
                  decoration: InputDecoration(labelText: context.t('circle.startDate')),
                  child: Text('${startDate.year}-${startDate.month.toString().padLeft(2, '0')}-${startDate.day.toString().padLeft(2, '0')}'),
                ),
              ),
              const SizedBox(height: 24),
              Row(
                mainAxisAlignment: MainAxisAlignment.end,
                children: [
                  TextButton(onPressed: () => Navigator.of(context).maybePop(), child: Text(context.t('common.cancel'))),
                  const SizedBox(width: 8),
                  FilledButton(
                    onPressed: (submitting || contribution <= 0) ? null : _submit,
                    child: Text(context.t('common.next')),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}
