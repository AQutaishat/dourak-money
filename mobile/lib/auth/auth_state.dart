import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../api/api_client.dart';
import '../api/auth_api.dart';
import '../api/models.dart';

/// Mirrors frontend/src/auth/AuthContext.tsx's AuthError: turns a login/register
/// failure into a message the screen can show inline instead of a generic error.
class AuthException implements Exception {
  AuthException(this.message, this.kind);
  final String message;
  final String kind; // 'invalid-credentials' | 'server'
}

AuthException _toAuthError(Object err, String fallback) {
  if (err is DioException) {
    final data = err.response?.data;
    final errors = (data is Map && data['errors'] is List) ? (data['errors'] as List).cast<String>() : null;
    if (errors != null && errors.isNotEmpty) {
      return AuthException(errors.join(' '), err.response?.statusCode == 401 ? 'invalid-credentials' : 'server');
    }
    if (err.response?.statusCode == 401) return AuthException(fallback, 'invalid-credentials');
  }
  if (err is AuthException) return err;
  return AuthException(err.toString(), 'server');
}

class AuthData {
  const AuthData({required this.isAuthenticated, this.profile});
  final bool isAuthenticated;
  final UserProfile? profile;

  /// Mirrors `displayLabel` fallback chain in AuthContext.tsx.
  String get displayLabel => profile?.displayLabel ?? profile?.name ?? profile?.email ?? '';

  AuthData copyWith({bool? isAuthenticated, UserProfile? profile, bool clearProfile = false}) {
    return AuthData(
      isAuthenticated: isAuthenticated ?? this.isAuthenticated,
      profile: clearProfile ? null : (profile ?? this.profile),
    );
  }
}

class AuthController extends StateNotifier<AuthData> {
  AuthController(this._authApi) : super(const AuthData(isAuthenticated: false)) {
    _bootstrap();
  }

  final AuthApi _authApi;

  Future<void> _bootstrap() async {
    final token = await ApiClient.readToken();
    if (token != null) {
      state = state.copyWith(isAuthenticated: true);
      await refreshProfile();
    }
  }

  Future<void> refreshProfile() async {
    final token = await ApiClient.readToken();
    if (token == null) {
      state = state.copyWith(isAuthenticated: false, clearProfile: true);
      return;
    }
    try {
      final profile = await _authApi.profile();
      state = state.copyWith(profile: profile);
    } catch (_) {
      // A missing profile must never block the app (mirrors AuthContext.tsx).
      state = state.copyWith(clearProfile: true);
    }
  }

  Future<void> _applyResult(AuthResult result) async {
    if (!result.succeeded || result.token == null) {
      throw AuthException(result.errors.isNotEmpty ? result.errors.join(' ') : 'Authentication failed.', 'server');
    }
    await ApiClient.saveToken(result.token!);
    state = state.copyWith(isAuthenticated: true);
    await refreshProfile();
  }

  Future<void> login(String email, String password) async {
    try {
      await _applyResult(await _authApi.login(email: email, password: password));
    } catch (err) {
      throw _toAuthError(err, 'Invalid credentials');
    }
  }

  Future<void> register(String email, String password) async {
    try {
      await _applyResult(await _authApi.register(email: email, password: password));
    } catch (err) {
      throw _toAuthError(err, 'Registration failed');
    }
  }

  Future<void> logout() async {
    await ApiClient.clearToken();
    state = const AuthData(isAuthenticated: false);
  }

  /// Wired to ApiClient.onUnauthorized — a 401 on a non-auth-attempt request means
  /// the session expired; log out locally so the router bounces to /login.
  void forceLogout() {
    state = const AuthData(isAuthenticated: false);
  }
}
