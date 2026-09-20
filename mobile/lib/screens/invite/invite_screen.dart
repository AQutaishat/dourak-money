import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../state/providers.dart';
import '../../utils/invite_token.dart';

/// The mobile twin of `frontend/src/pages/Invite/InvitePage.tsx`.
///
/// Landing spot for a `https://dourak.money/invite/{token}` (or `dourak://invite/{token}`)
/// link opened on a device that has the app. It only stashes the token and forwards the
/// person on — the dashboard's pending-invitations section consumes the stash once they're
/// signed in and calls `POST /invitations/link/{token}`, exactly like the web does.
class InviteScreen extends ConsumerStatefulWidget {
  const InviteScreen({super.key, required this.token});

  final String token;

  @override
  ConsumerState<InviteScreen> createState() => _InviteScreenState();
}

class _InviteScreenState extends ConsumerState<InviteScreen> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) async {
      await stashInviteToken(widget.token);
      if (!mounted) return;
      final isAuthenticated = ref.read(authControllerProvider).isAuthenticated;
      context.go(isAuthenticated ? '/' : '/login');
    });
  }

  @override
  Widget build(BuildContext context) =>
      const Scaffold(body: Center(child: CircularProgressIndicator()));
}
