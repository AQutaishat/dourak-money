import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../api/api_client.dart';
import '../../api/models.dart';
import '../../l10n/app_localizations.dart';
import '../../state/providers.dart';

/// Mirrors BasicInfoBlock.tsx: read-only info grid, editable (name/description/
/// start date only — contribution amount stays locked) while Draft + organizer.
/// `dense` mirrors the compact variant merged into the top of Current Cycle.
class BasicInfoTab extends ConsumerStatefulWidget {
  const BasicInfoTab({super.key, required this.circle, this.dense = false});
  final CircleDetail circle;
  final bool dense;

  @override
  ConsumerState<BasicInfoTab> createState() => _BasicInfoTabState();
}

class _BasicInfoTabState extends ConsumerState<BasicInfoTab> {
  bool editing = false;
  late TextEditingController name;
  late TextEditingController description;
  late DateTime startDate;
  String? error;
  bool saving = false;

  bool get canEdit => widget.circle.isDraft && widget.circle.isOrganizer && !widget.dense;

  @override
  void initState() {
    super.initState();
    _resetFields();
  }

  void _resetFields() {
    name = TextEditingController(text: widget.circle.name);
    description = TextEditingController(text: widget.circle.description ?? '');
    startDate = DateTime.parse(widget.circle.startDate);
  }

  Future<void> _save() async {
    setState(() {
      error = null;
      saving = true;
    });
    try {
      await ref.read(circlesApiProvider).updateBasicInfo(
            widget.circle.id,
            name: name.text.trim(),
            description: description.text.trim().isEmpty ? null : description.text.trim(),
            startDate: startDate.toIso8601String().substring(0, 10),
          );
      ref.read(refreshTickProvider.notifier).state++;
      setState(() => editing = false);
    } catch (err) {
      setState(() => error = extractErrorMessage(err, context.t('common.error')));
    } finally {
      if (mounted) setState(() => saving = false);
    }
  }

  String _monthYear(String? iso) {
    if (iso == null) return '—';
    return DateFormat.yMMMM(Localizations.localeOf(context).toString()).format(DateTime.parse(iso));
  }

  String _fullDate(String iso) => DateFormat.yMMMd(Localizations.localeOf(context).toString()).format(DateTime.parse(iso));

  @override
  Widget build(BuildContext context) {
    final circle = widget.circle;

    if (editing) {
      return Card(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (error != null) Padding(padding: const EdgeInsets.only(bottom: 12), child: Text(error!, style: const TextStyle(color: Colors.red))),
              TextField(controller: name, decoration: InputDecoration(labelText: context.t('circle.name'))),
              const SizedBox(height: 12),
              TextField(controller: description, decoration: InputDecoration(labelText: context.t('circle.description')), maxLines: 2),
              const SizedBox(height: 12),
              InkWell(
                onTap: () async {
                  final picked = await showDatePicker(
                    context: context,
                    initialDate: startDate,
                    firstDate: DateTime(2000),
                    lastDate: DateTime(2100),
                  );
                  if (picked != null) setState(() => startDate = picked);
                },
                child: InputDecorator(
                  decoration: InputDecoration(labelText: context.t('circle.startDate')),
                  child: Text(_fullDate(startDate.toIso8601String())),
                ),
              ),
              const SizedBox(height: 16),
              Row(children: [
                FilledButton(onPressed: (name.text.trim().isEmpty || saving) ? null : _save, child: Text(context.t('common.save'))),
                const SizedBox(width: 8),
                TextButton(onPressed: () => setState(() => editing = false), child: Text(context.t('common.cancel'))),
              ]),
            ],
          ),
        ),
      );
    }

    final rows = <MapEntry<String, String>>[
      MapEntry(context.t('circle.name'), circle.name),
      MapEntry(context.t('circle.description'), circle.description?.isNotEmpty == true ? circle.description! : '—'),
      MapEntry(context.t('circle.startDate'), _fullDate(circle.startDate)),
      MapEntry(context.t('circle.contributionAmount'), '${circle.contributionAmount} ${circle.currency}'),
      MapEntry(context.t('circle.totalMonthlyAmount'), '${circle.totalMonthlyAmount} ${circle.currency}'),
      MapEntry(context.t('circle.lastPaymentMonth'), _monthYear(circle.lastPaymentMonth)),
      MapEntry(context.t('circle.memberCount'), '${circle.memberCount}'),
      MapEntry(context.t('circle.organizer'), circle.organizerName),
      MapEntry(context.t('circle.createdAt'), _fullDate(circle.createdAt)),
    ];

    return Card(
      margin: widget.dense ? const EdgeInsets.only(bottom: 12) : EdgeInsets.zero,
      child: Padding(
        padding: EdgeInsets.all(widget.dense ? 12 : 16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (widget.dense || canEdit)
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  if (widget.dense) Text(context.t('circle.basicInfo'), style: Theme.of(context).textTheme.labelLarge),
                  if (canEdit)
                    TextButton.icon(
                      onPressed: () {
                        _resetFields();
                        setState(() => editing = true);
                      },
                      icon: const Icon(Icons.edit, size: 16),
                      label: Text(context.t('common.edit')),
                    ),
                ],
              ),
            Wrap(
              runSpacing: 8,
              children: rows
                  .map((r) => FractionallySizedBox(
                        widthFactor: 0.5,
                        child: Padding(
                          padding: const EdgeInsets.only(right: 8, bottom: 4),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(r.key, style: Theme.of(context).textTheme.bodySmall?.copyWith(color: Colors.grey)),
                              Text(r.value, style: TextStyle(fontWeight: FontWeight.w500, fontSize: widget.dense ? 13 : 15)),
                            ],
                          ),
                        ),
                      ))
                  .toList(),
            ),
          ],
        ),
      ),
    );
  }
}
