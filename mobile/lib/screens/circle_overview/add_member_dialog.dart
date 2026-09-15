import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../api/api_client.dart';
import '../../api/models.dart';
import '../../l10n/app_localizations.dart';
import '../../state/providers.dart';
import '../../utils/whatsapp.dart';

/// Mirrors AddMemberDialog.tsx: user-search autocomplete + add, plus prompt03 §2's
/// pure-share WhatsApp invite button (no name/phone fields, no backend call at all).
class AddMemberDialog extends ConsumerStatefulWidget {
  const AddMemberDialog({super.key, required this.circleId, required this.circleName, required this.organizerName});

  final int circleId;
  final String circleName;
  final String organizerName;

  @override
  ConsumerState<AddMemberDialog> createState() => _AddMemberDialogState();
}

class _AddMemberDialogState extends ConsumerState<AddMemberDialog> {
  final term = TextEditingController();
  Timer? _debounce;
  List<UserSearchResult> options = [];
  bool searching = false;
  UserSearchResult? selected;
  String? error;
  bool adding = false;

  @override
  void dispose() {
    _debounce?.cancel();
    term.dispose();
    super.dispose();
  }

  void _onChanged(String value) {
    setState(() => selected = null);
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 300), () => _search(value.trim()));
  }

  Future<void> _search(String q) async {
    if (q.length < 2) {
      setState(() => options = []);
      return;
    }
    setState(() => searching = true);
    try {
      final results = await ref.read(usersApiProvider).search(q);
      if (mounted) setState(() => options = results);
    } catch (_) {
      if (mounted) setState(() => options = []);
    } finally {
      if (mounted) setState(() => searching = false);
    }
  }

  Future<void> _addSelected() async {
    if (selected == null) return;
    setState(() {
      adding = true;
      error = null;
    });
    try {
      await ref.read(circlesApiProvider).addUserMember(widget.circleId, selected!.userId);
      ref.read(refreshTickProvider.notifier).state++;
      if (mounted) Navigator.of(context).pop();
    } catch (err) {
      setState(() => error = extractErrorMessage(err, context.t('common.error')));
    } finally {
      if (mounted) setState(() => adding = false);
    }
  }

  /// prompt03 §2: pure share action — no member/invitation record is ever created.
  void _inviteByWhatsApp() {
    final isArabic = Localizations.localeOf(context).languageCode == 'ar';
    shareToWhatsApp(buildInviteToRegisterText(
      circleName: widget.circleName,
      organizerName: widget.organizerName,
      appUrl: dourakAppUrl,
      isArabic: isArabic,
    ));
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: Text(context.t('circle.addMember')),
      content: SizedBox(
        width: 400,
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              if (error != null) ...[
                Text(error!, style: const TextStyle(color: Colors.red)),
                const SizedBox(height: 8),
              ],
              Text(context.t('circle.addExistingUser'), style: Theme.of(context).textTheme.labelLarge),
              const SizedBox(height: 8),
              TextField(
                controller: term,
                onChanged: _onChanged,
                decoration: InputDecoration(
                  labelText: context.t('circle.searchUsers'),
                  helperText: context.t('circle.searchUsersHint'),
                  suffixIcon: searching ? const Padding(padding: EdgeInsets.all(12), child: CircularProgressIndicator(strokeWidth: 2)) : null,
                ),
              ),
              const SizedBox(height: 8),
              if (term.text.trim().length >= 2 && options.isEmpty && !searching)
                Text(context.t('circle.noUsersFound'), style: Theme.of(context).textTheme.bodySmall),
              ...options.map((o) => ListTile(
                    dense: true,
                    selected: selected?.userId == o.userId,
                    title: Text(o.displayLabel),
                    subtitle: Text([o.email, o.phone].where((s) => s != null && s.isNotEmpty).join(' · ')),
                    onTap: () => setState(() => selected = o),
                  )),
              const SizedBox(height: 8),
              SizedBox(
                width: double.infinity,
                child: FilledButton(
                  onPressed: (selected == null || adding) ? null : _addSelected,
                  child: Text(context.t('common.add')),
                ),
              ),
              const Divider(height: 32),
              Text(context.t('circle.inviteUnregistered'), style: Theme.of(context).textTheme.labelLarge),
              const SizedBox(height: 4),
              Text(context.t('circle.inviteSentNote'), style: Theme.of(context).textTheme.bodySmall),
              const SizedBox(height: 8),
              SizedBox(
                width: double.infinity,
                child: OutlinedButton.icon(
                  onPressed: _inviteByWhatsApp,
                  icon: const Icon(Icons.chat),
                  label: Text(context.t('circle.inviteByWhatsApp')),
                ),
              ),
            ],
          ),
        ),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.of(context).pop(), child: Text(context.t('common.close'))),
      ],
    );
  }
}
