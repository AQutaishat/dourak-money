import 'api_client.dart';
import 'models.dart';

/// Mirrors frontend/src/api/auth.ts.
class AuthApi {
  AuthApi(this._client);
  final ApiClient _client;

  Future<AuthResult> register({required String email, required String password}) async {
    final res = await _client.dio.post('/auth/register', data: {'email': email, 'password': password});
    return AuthResult.fromJson(res.data as Map<String, dynamic>);
  }

  Future<AuthResult> login({required String email, required String password}) async {
    final res = await _client.dio.post('/auth/login', data: {'email': email, 'password': password});
    return AuthResult.fromJson(res.data as Map<String, dynamic>);
  }

  Future<UserProfile> profile() async {
    final res = await _client.dio.get('/auth/profile');
    return UserProfile.fromJson(res.data as Map<String, dynamic>);
  }

  Future<void> updateProfile({String? name, String? phone, String? preferredLanguage}) {
    return _client.dio.put('/auth/profile', data: {
      'name': name,
      'phone': phone,
      'preferredLanguage': preferredLanguage,
    });
  }
}

/// Mirrors the `usersApi` export in frontend/src/api/auth.ts.
class UsersApi {
  UsersApi(this._client);
  final ApiClient _client;

  Future<List<UserSearchResult>> search(String q) async {
    final res = await _client.dio.get('/users/search', queryParameters: {'q': q});
    return (res.data as List<dynamic>).map((e) => UserSearchResult.fromJson(e as Map<String, dynamic>)).toList();
  }
}
