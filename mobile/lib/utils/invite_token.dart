import '../api/api_client.dart';

/// The mobile twin of `frontend/src/utils/inviteToken.ts`.
///
/// When someone opens a `https://dourak.money/invite/{token}` link while the app is
/// installed but they aren't signed in yet, the token has to survive the login/register
/// round trip. The web uses `localStorage`; mobile already ships `flutter_secure_storage`
/// (it holds the auth token), so the stash reuses that rather than pulling in
/// `shared_preferences` for one string.
const _inviteTokenKey = 'dourak_pending_invite_token';

Future<void> stashInviteToken(String token) => secureStorage.write(key: _inviteTokenKey, value: token);

/// Reads and clears the stashed token in one shot — it's single-use.
Future<String?> consumeStashedInviteToken() async {
  final token = await secureStorage.read(key: _inviteTokenKey);
  if (token != null) await secureStorage.delete(key: _inviteTokenKey);
  return token;
}

/// Extracts the token from an incoming deep link, for both supported shapes:
///   https://dourak.money/invite/{token}   (Android App Link — also opens the web page
///                                          when the app isn't installed)
///   dourak://invite/{token}               (custom-scheme fallback, always works even
///                                          before assetlinks.json verification is live)
/// Returns null for any other link.
String? inviteTokenFromUri(Uri uri) {
  final segments = uri.pathSegments.where((s) => s.isNotEmpty).toList();
  if (uri.scheme == 'dourak') {
    if (uri.host == 'invite' && segments.isNotEmpty) return segments.first;
    if (uri.host.isEmpty && segments.length >= 2 && segments[0] == 'invite') return segments[1];
    return null;
  }
  if (segments.length >= 2 && segments[0] == 'invite') return segments[1];
  return null;
}
