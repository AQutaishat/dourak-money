import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../api/models.dart';
import '../../l10n/app_localizations.dart';
import '../../state/providers.dart';
import '../../widgets/status_chips.dart';
import 'add_member_dialog.dart';

/// Mirrors MembersTab.tsx — organizer-only controls, contact info hidden rule is
/// enforced implicitly (we only ever render `phone`/`email` for organizer views,
/// since the backend already omits them for non-organizers per CircleReadAccessBehavior).
class MembersTab extends ConsumerWidget {
  const MembersTab({super.key, required this.circle});
  final CircleDetail circle;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final membersAsync = ref.watch(membersProvider(circle.id));
    final canManage = circle.isOrganizer;
    final isDraft = circle.isDraft;

    return membersAsync.when(
      loading: () => Center(child: Text(context.t('common.loading'))),
      error: (e, _) => Center(child: Text(context.t('common.error'))),
      data: (members) {
        final alreadyAMember = circle.myMemberId != null;
        return ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(context.t('circle.members'), style: Theme.of(context).textTheme.titleMedium),
                if (canManage && isDraft)
                  Wrap(spacing: 8, children: [
                    if (!alreadyAMember)
                      OutlinedButton.icon(
                        onPressed: () async {
                          await ref.read(circlesApiProvider).addSelfAsMember(circle.id);
                          ref.read(refreshTickProvider.notifier).state++;
                        },
                        icon: const Icon(Icons.person_add_alt, size: 16),
                        label: Text(context.t('circle.addSelfAsMember')),
                      ),
                    FilledButton(
                      // A full page push, not a dialog — see AddMemberDialog's doc comment.
                      onPressed: () => Navigator.of(context).push(MaterialPageRoute<void>(
                        builder: (_) => AddMemberDialog(circleId: circle.id, circleName: circle.name, organizerName: circle.organizerName),
                      )),
                      child: Text(context.t('circle.addMember')),
                    ),
                  ]),
              ],
            ),
            if (!canManage) ...[
              const SizedBox(height: 8),
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(color: Colors.blue.shade50, borderRadius: BorderRadius.circular(8)),
                child: Text(context.t('circle.viewOnlyNote')),
              ),
            ],
            const SizedBox(height: 8),
            ...members.map((m) => _MemberRow(circle: circle, member: m)),
          ],
        );
      },
    );
  }
}

class _MemberRow extends ConsumerWidget {
  const _MemberRow({required this.circle, required this.member});
  final CircleDetail circle;
  final Member member;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final canManage = circle.isOrganizer;
    final isDraft = circle.isDraft;
    final excluded = !member.isParticipating;
    final hasEmail = member.email != null && member.email!.isNotEmpty;
    final displayName = member.name.isNotEmpty ? member.name : (member.email ?? '');
    final textStyle = TextStyle(
      decoration: excluded ? TextDecoration.lineThrough : null,
      color: excluded ? Colors.grey : null,
    );

    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: ListTile(
        // Name and email are shown as two separate fields/columns (not "Name (email)"
        // combined), and phone is never displayed — mirrors MembersTab.tsx.
        title: Row(
          children: [
            Expanded(flex: 3, child: Text(displayName, style: textStyle)),
            if (hasEmail && member.name.isNotEmpty)
              Expanded(
                flex: 2,
                child: Text(
                  member.email!,
                  style: textStyle.copyWith(color: excluded ? Colors.grey : Colors.grey.shade600, fontSize: 13),
                  textAlign: TextAlign.start,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
          ],
        ),
        trailing: Wrap(
          spacing: 2,
          crossAxisAlignment: WrapCrossAlignment.center,
          children: [
            if (member.payoutPosition != null) Chip(label: Text('#${member.payoutPosition}'), visualDensity: VisualDensity.compact),
            // No badge once accepted — the invitation is a non-event at that point.
            if (member.invitationStatus != 'Accepted') InvitationStatusChip(status: member.invitationStatus),
            IconButton(
              tooltip: context.t('circle.viewHistory'),
              icon: const Icon(Icons.history, size: 20),
              onPressed: () => context.push('/circles/${circle.id}/members/${member.id}/history'),
            ),
            if (canManage && isDraft && member.invitationStatus == 'Declined')
              IconButton(
                tooltip: context.t('circle.reinvite'),
                icon: const Icon(Icons.replay, size: 20),
                onPressed: () async {
                  await ref.read(circlesApiProvider).reinviteMember(circle.id, member.id);
                  ref.read(refreshTickProvider.notifier).state++;
                },
              ),
            if (canManage && isDraft && member.isActive)
              IconButton(
                tooltip: context.t('circle.deactivate'),
                icon: const Icon(Icons.person_off_outlined, size: 20),
                onPressed: () async {
                  await ref.read(circlesApiProvider).deactivateMember(circle.id, member.id);
                  ref.read(refreshTickProvider.notifier).state++;
                },
              ),
            // prompt03 §1: full removal, draft circles only, any invitation status.
            if (canManage && isDraft)
              IconButton(
                tooltip: context.t('circle.removeMember'),
                icon: const Icon(Icons.delete_outline, size: 20, color: Colors.red),
                onPressed: () => _confirmRemove(context, ref),
              ),
          ],
        ),
      ),
    );
  }

  Future<void> _confirmRemove(BuildContext context, WidgetRef ref) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(context.t('circle.removeMember')),
        content: Text(context.t('circle.removeMemberConfirm', {'name': member.name})),
        actions: [
          TextButton(onPressed: () => Navigator.of(context).pop(false), child: Text(context.t('common.cancel'))),
          FilledButton(
            style: FilledButton.styleFrom(backgroundColor: Colors.red),
            onPressed: () => Navigator.of(context).pop(true),
            child: Text(context.t('common.delete')),
          ),
        ],
      ),
    );
    if (confirmed == true) {
      await ref.read(circlesApiProvider).removeMember(circle.id, member.id);
      ref.read(refreshTickProvider.notifier).state++;
    }
  }
}
