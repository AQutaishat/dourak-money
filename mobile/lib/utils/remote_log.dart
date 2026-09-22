import '../api/api_client.dart';

/// Relays app-side errors/warnings to the backend's own Serilog→Seq pipeline (see
/// backend/src/Dourak.Api/Controllers/DiagnosticsController.cs) instead of the app needing
/// its own Seq client, ingestion URL, or API key — Seq itself isn't reachable from the public
/// internet (bound to 127.0.0.1 on the server, see the README's security note), so posting to
/// the same API base URL the app already talks to is the only way these reach Seq at all.
///
/// Off by default — enable with `--dart-define=ENABLE_REMOTE_LOGGING=true` (e.g. for a beta
/// build you want visibility into). Every call is best-effort: a failed relay never throws,
/// never retries, and never blocks the caller.
const bool remoteLoggingEnabled = bool.fromEnvironment('ENABLE_REMOTE_LOGGING');

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
