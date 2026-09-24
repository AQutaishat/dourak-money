import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../api/api_client.dart';
import '../../api/models.dart';
import '../../l10n/app_localizations.dart';
import '../../state/providers.dart';
import '../../utils/whatsapp.dart';

/// Mirrors AddMemberDialog.tsx: user-search autocomplete + add, plus the token-based
/// WhatsApp invite for someone who isn't on Dourak yet — asks for their name, creates a
/// real Pending member row via `POST /circles/{id}/members/invite-unregistered`, and
/// sends a WhatsApp message linking to `{app}/invite/{token}`.
///
/// A full page (pushed via Navigator), not a dialog — a dialog's search results and Add
/// button had to compete with the keyboard for a small fixed box, and shrinking that box
/// to fit still left users unsure anything had appeared below the fold, with no visual cue
/// to scroll. A full Scaffold resizes around the keyboard the way any normal screen does:
/// the results list is obviously a big scrollable list (not a maybe-clipped few pixels),
/// and the Add button sits pinned directly under it, always on-screen.
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

  /// Asks for the invitee's display name first (it's what shows in the members table until
  /// they respond), then creates the token-carrying Pending row and shares the link.
  Future<void> _inviteByWhatsApp() async {
    final name = await showDialog<String>(context: context, builder: (_) => const _InviteNameDialog());
    if (name == null || !mounted) return;

    setState(() {
      adding = true;
      error = null;
    });
    try {
      final result = await ref.read(circlesApiProvider).inviteUnregistered(widget.circleId, name);
      if (!mounted) return;
      final isArabic = Localizations.localeOf(context).languageCode == 'ar';
      await shareToWhatsApp(buildInviteToRegisterText(
        personName: name,
        circleName: widget.circleName,
        organizerName: widget.organizerName,
        appUrl: inviteLinkUrl(result.token),
        isArabic: isArabic,
      ));
      ref.read(refreshTickProvider.notifier).state++;
      // The web closes the whole Add Member dialog once the invite is sent.
      if (mounted) Navigator.of(context).pop();
    } catch (err) {
      if (mounted) setState(() => error = extractErrorMessage(err, context.t('common.error')));
    } finally {
      if (mounted) setState(() => adding = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(context.t('circle.addMember')),
        actions: [
          TextButton(onPressed: () => Navigator.of(context).pop(), child: Text(context.t('common.close'))),
        ],
      ),
      // The default true — the body shrinks as the keyboard rises instead of the keyboard
      // simply covering the bottom of it, which is what keeps the Add button (last in this
      // Column, right under the Expanded results list) pinned in view above the keyboard.
      resizeToAvoidBottomInset: true,
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              if (error != null) ...[
                Text(error!, style: const TextStyle(color: Colors.red)),
                const SizedBox(height: 8),
              ],
              // Mirrors the web: the WhatsApp invite sits on the same line as the
              // "add a registered user" heading, pushed to the opposite end.
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Flexible(child: Text(context.t('circle.addExistingUser'), style: Theme.of(context).textTheme.labelLarge)),
                  OutlinedButton.icon(
                    onPressed: adding ? null : _inviteByWhatsApp,
                    icon: const Icon(Icons.chat, size: 18),
                    label: Text(context.t('circle.inviteByWhatsApp')),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              TextField(
                controller: term,
                autofocus: true,
                onChanged: _onChanged,
                decoration: InputDecoration(
                  labelText: context.t('circle.searchUsers'),
                  helperText: context.t('circle.searchUsersHint'),
                  suffixIcon: searching ? const Padding(padding: EdgeInsets.all(12), child: CircularProgressIndicator(strokeWidth: 2)) : null,
                ),
              ),
              const SizedBox(height: 4),
              // A full-height scrollable list (not a few clipped rows in a dialog) — its own
              // size and scrollbar make it obvious more content exists, and it's the only
              // thing that grows/shrinks here as the keyboard opens and closes.
              Expanded(
                child: term.text.trim().length >= 2 && options.isEmpty && !searching
                    ? Center(child: Text(context.t('circle.noUsersFound'), style: Theme.of(context).textTheme.bodySmall))
                    : ListView.builder(
                        itemCount: options.length,
                        itemBuilder: (context, i) {
                          final o = options[i];
                          return ListTile(
                            selected: selected?.userId == o.userId,
                            title: Text(o.displayLabel),
                            subtitle: Text([o.email, o.phone].where((s) => s != null && s.isNotEmpty).join(' · ')),
                            onTap: () => setState(() => selected = o),
                          );
                        },
                      ),
              ),
              const SizedBox(height: 8),
              SizedBox(
                width: double.infinity,
                child: FilledButton(
                  onPressed: (selected == null || adding) ? null : _addSelected,
                  child: Text(context.t('common.add')),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// The small "who are you inviting?" prompt the web shows before sending the WhatsApp
/// message — the name becomes the member row's temporary display name.
class _InviteNameDialog extends StatefulWidget {
  const _InviteNameDialog();

  @override
  State<_InviteNameDialog> createState() => _InviteNameDialogState();
}

class _InviteNameDialogState extends State<_InviteNameDialog> {
  final name = TextEditingController();

  @override
  void dispose() {
    name.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final trimmed = name.text.trim();
    return AlertDialog(
      title: Text(context.t('circle.inviteByWhatsApp')),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(context.t('circle.inviteUnregisteredNameHint'), style: Theme.of(context).textTheme.bodySmall),
          const SizedBox(height: 12),
          TextField(
            controller: name,
            autofocus: true,
            textInputAction: TextInputAction.done,
            onChanged: (_) => setState(() {}),
            onSubmitted: (v) => v.trim().isEmpty ? null : Navigator.of(context).pop(v.trim()),
            decoration: InputDecoration(labelText: context.t('circle.memberName')),
          ),
        ],
      ),
      actions: [
        TextButton(onPressed: () => Navigator.of(context).pop(), child: Text(context.t('common.cancel'))),
        FilledButton(
          onPressed: trimmed.isEmpty ? null : () => Navigator.of(context).pop(trimmed),
          child: Text(context.t('circle.inviteByWhatsApp')),
        ),
      ],
    );
  }
}
