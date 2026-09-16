import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Mirrors frontend/src/api/client.ts.
///
/// Base URL: mirrors `VITE_API_BASE_URL`. Pass at build/run time with
/// `--dart-define=API_BASE_URL=<url>`:
///   - Local emulator debug: `http://10.0.2.2:5210/api` (10.0.2.2 is the Android
///     emulator's alias for the host machine running the backend) — the default
///     below, for debug convenience.
///   - Real production builds: `https://dourak.money/api` (the real domain, served
///     over HTTPS via Caddy's Let's Encrypt cert — see docker-compose.yml/Caddyfile).
/// See docs/progress.md for the release note.
const String _defaultBaseUrl = 'http://10.0.2.2:5210/api';
const String apiBaseUrl = String.fromEnvironment('API_BASE_URL', defaultValue: _defaultBaseUrl);

const _tokenKey = 'dourak_token';
final secureStorage = FlutterSecureStorage();

/// Endpoints where a 401 is an expected answer (bad credentials), not an expired
/// session — mirrors the `AUTH_ENDPOINTS` special-case in client.ts so a failed
/// login/register doesn't get treated as a session bounce.
const List<String> authEndpoints = ['/auth/login', '/auth/register'];

typedef UnauthorizedCallback = void Function();

class ApiClient {
  ApiClient._internal(this.dio);

  static ApiClient? _instance;

  final Dio dio;

  /// Called when a non-auth-attempt request gets a 401 (session expired) — the app
  /// wires this to clear the token and route to /login (mirrors the axios interceptor
  /// redirecting to window.location.href = "/login").
  UnauthorizedCallback? onUnauthorized;

  factory ApiClient() {
    if (_instance != null) return _instance!;
    final dio = Dio(BaseOptions(baseUrl: apiBaseUrl, connectTimeout: const Duration(seconds: 20)));
    final client = ApiClient._internal(dio);

    dio.interceptors.add(InterceptorsWrapper(
      onRequest: (options, handler) async {
        final token = await secureStorage.read(key: _tokenKey);
        if (token != null) {
          options.headers['Authorization'] = 'Bearer $token';
        }
        handler.next(options);
      },
      onError: (error, handler) {
        final path = error.requestOptions.path;
        final isAuthAttempt = authEndpoints.any((p) => path.contains(p));
        if (error.response?.statusCode == 401 && !isAuthAttempt) {
          secureStorage.delete(key: _tokenKey);
          client.onUnauthorized?.call();
        }
        handler.next(error);
      },
    ));

    _instance = client;
    return client;
  }

  static Future<void> saveToken(String token) => secureStorage.write(key: _tokenKey, value: token);
  static Future<String?> readToken() => secureStorage.read(key: _tokenKey);
  static Future<void> clearToken() => secureStorage.delete(key: _tokenKey);
}

/// Extracts the ASP.NET ProblemDetails `title`, mirroring the frontend's
/// `err.response?.data?.title` fallback pattern used throughout the React app.
String extractErrorMessage(Object err, String fallback) {
  if (err is DioException) {
    final data = err.response?.data;
    if (data is Map && data['title'] is String) return data['title'] as String;
    if (data is Map && data['errors'] is List && (data['errors'] as List).isNotEmpty) {
      return (data['errors'] as List).join(' ');
    }
  }
  return fallback;
}
