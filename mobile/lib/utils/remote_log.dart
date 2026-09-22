import '../api/api_client.dart';

/// Relays app-side errors/warnings to the backend's own Serilog→Seq pipeline (see
/// backend/src/Dourak.Api/Controllers/DiagnosticsController.cs) instead of the app needing
/// its own Seq client, ingestion URL, or API key — Seq itself isn't reachable from the public
/// internet (bound to 127.0.0.1 on the server, see the README's security note), so posting to
/// the same API base URL the app already talks to is the only way these reach Seq at all.
///
/// On by default (flip off per build with `--dart-define=ENABLE_REMOTE_LOGGING=false` if a
/// build's relay traffic ever needs to be silenced). Every call is best-effort: a failed relay
/// never throws, never retries, and never blocks the caller. Which Seq environment this reaches
/// is entirely a function of `apiBaseUrl` (api_client.dart) — a release build defaults to
/// production there, so logging and the API it talks to always point at the same place.
const bool remoteLoggingEnabled = bool.fromEnvironment('ENABLE_REMOTE_LOGGING', defaultValue: true);

/// The one path this must never log about — logging a failure to log would recurse.
const String _diagnosticsPath = '/diagnostics/log';

Future<void> logRemote(String message, {String level = 'error', Object? error}) async {
  if (!remoteLoggingEnabled) return;
  try {
    await ApiClient().dio.post(_diagnosticsPath, data: {
      'level': level,
      'message': message,
      'details': error?.toString(),
    });
  } catch (_) {
    // Never surface a logging failure to the caller, and never loop back into itself.
  }
}
