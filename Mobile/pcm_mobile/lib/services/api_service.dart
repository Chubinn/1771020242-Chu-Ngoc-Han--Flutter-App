import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import '../models/index.dart';

class ApiService {
  final Dio dio;
  static const String _defaultBaseUrl = String.fromEnvironment(
    'PCM_API_BASE_URL',
    // Web/desktop should use localhost. Override for Android emulator if needed.
    defaultValue: 'http://localhost:5253/api',
  );
  static const String _tokenKey = 'pcm_jwt_token';

  final FlutterSecureStorage _storage = const FlutterSecureStorage();
  final String baseUrl;
  String? _token;

  ApiService({Dio? dioOverride, String? baseUrlOverride})
      : dio = dioOverride ?? Dio(),
        baseUrl = baseUrlOverride ?? _defaultBaseUrl {
    dio.options.baseUrl = baseUrl;
    dio.options.connectTimeout = Duration(seconds: 30);
    dio.options.receiveTimeout = Duration(seconds: 30);

    // Set default headers
    dio.options.headers = {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    };

    // Add interceptor for debugging
    dio.interceptors.add(
      LogInterceptor(requestBody: true, responseBody: true),
    );

    // Clear token on 401 responses so the app can redirect to login.
    dio.interceptors.add(
      InterceptorsWrapper(
        onError: (error, handler) async {
          if (error.response?.statusCode == 401) {
            await clearToken();
          }
          handler.next(error);
        },
      ),
    );
  }

  Future<void> init() async {
    final token = await _storage.read(key: _tokenKey);
    if (token != null && token.isNotEmpty) {
      await setToken(token, persist: false);
    }
  }

  Future<void> setToken(String? token, {bool persist = true}) async {
    _token = token;
    if (token == null || token.isEmpty) {
      dio.options.headers.remove('Authorization');
      if (persist) {
        await _storage.delete(key: _tokenKey);
      }
      return;
    }

    dio.options.headers['Authorization'] = 'Bearer $token';
    if (persist) {
      await _storage.write(key: _tokenKey, value: token);
    }
  }

  Future<String?> getToken() => _storage.read(key: _tokenKey);

  Future<void> clearToken() => setToken(null);

  bool get hasToken => _token != null && _token!.isNotEmpty;

  // Auth endpoints
  Future<Map<String, dynamic>> register({
    required String email,
    required String password,
    required String fullName,
  }) async {
    try {
      final response = await dio.post(
        '/auth/register',
        data: {'email': email, 'password': password, 'fullName': fullName},
      );
      return response.data;
    } catch (e) {
      rethrow;
    }
  }

  Future<Map<String, dynamic>> login({
    required String email,
    required String password,
  }) async {
    try {
      final response = await dio.post(
        '/auth/login',
        data: {'email': email, 'password': password},
      );
      return response.data;
    } catch (e) {
      rethrow;
    }
  }

  Future<User> getCurrentUser() async {
    try {
      final response = await dio.get('/auth/me');
      return User.fromJson(response.data);
    } catch (e) {
      rethrow;
    }
  }

  // Courts endpoints
  Future<List<Court>> getCourts() async {
    try {
      final response = await dio.get('/courts');
      final data = response.data as List;
      return data
          .map((e) => Court.fromJson(e as Map<String, dynamic>))
          .toList();
    } catch (e) {
      rethrow;
    }
  }

  Future<Court> getCourtDetails(int id) async {
    try {
      final response = await dio.get('/courts/$id');
      return Court.fromJson(response.data);
    } catch (e) {
      rethrow;
    }
  }

  // Bookings endpoints
  Future<Map<String, dynamic>> createBooking({
    required int courtId,
    required DateTime startTime,
    required DateTime endTime,
  }) async {
    try {
      final response = await dio.post(
        '/bookings',
        data: {
          'courtId': courtId,
          'startTime': startTime.toIso8601String(),
          'endTime': endTime.toIso8601String(),
        },
      );
      return response.data;
    } catch (e) {
      rethrow;
    }
  }

  Future<List<Booking>> getMyBookings() async {
    try {
      final response = await dio.get('/bookings/my-bookings');
      final data = response.data as List;
      return data
          .map((e) => Booking.fromJson(e as Map<String, dynamic>))
          .toList();
    } catch (e) {
      rethrow;
    }
  }

  // Wallet endpoints
  Future<Map<String, dynamic>> getBalance() async {
    try {
      final response = await dio.get('/wallet/balance');
      return response.data;
    } catch (e) {
      rethrow;
    }
  }

  Future<Map<String, dynamic>> requestDeposit({
    required double amount,
    required String description,
    String? proofImageUrl,
  }) async {
    try {
      final response = await dio.post(
        '/wallet/deposit',
        data: {
          'amount': amount,
          'description': description,
          'proofImageUrl': proofImageUrl,
        },
      );
      return response.data;
    } catch (e) {
      rethrow;
    }
  }

  Future<List<WalletTransaction>> getTransactions() async {
    try {
      final response = await dio.get('/wallet/transactions');
      final data = response.data as List;
      return data
          .map((e) => WalletTransaction.fromJson(e as Map<String, dynamic>))
          .toList();
    } catch (e) {
      rethrow;
    }
  }

  // Tournaments endpoints
  Future<List<Tournament>> getTournaments() async {
    try {
      final response = await dio.get('/tournaments');
      final data = response.data as List;
      return data
          .map((e) => Tournament.fromJson(e as Map<String, dynamic>))
          .toList();
    } catch (e) {
      rethrow;
    }
  }

  Future<Tournament> getTournamentDetails(int id) async {
    try {
      final response = await dio.get('/tournaments/$id');
      return Tournament.fromJson(response.data);
    } catch (e) {
      rethrow;
    }
  }

  Future<Map<String, dynamic>> joinTournament(int id) async {
    try {
      final response = await dio.post('/tournaments/$id/join', data: {});
      return response.data;
    } catch (e) {
      rethrow;
    }
  }

  // Notifications endpoints
  Future<List<AppNotification>> getNotifications() async {
    try {
      final response = await dio.get('/notifications');
      final data = response.data as List;
      return data
          .map((e) => AppNotification.fromJson(e as Map<String, dynamic>))
          .toList();
    } catch (e) {
      rethrow;
    }
  }

  Future<Map<String, dynamic>> markNotificationAsRead(int id) async {
    try {
      final response = await dio.put('/notifications/$id/read');
      return response.data;
    } catch (e) {
      rethrow;
    }
  }

  // Members endpoints
  Future<List<User>> getMembers({String? searchQuery}) async {
    try {
      final params = searchQuery != null
          ? <String, dynamic>{'search': searchQuery}
          : null;
      final response = await dio.get('/members', queryParameters: params);
      final data = response.data as List;
      return data.map((e) => User.fromJson(e as Map<String, dynamic>)).toList();
    } catch (e) {
      rethrow;
    }
  }

  Future<User> getMemberDetails(int id) async {
    try {
      final response = await dio.get('/members/$id');
      return User.fromJson(response.data);
    } catch (e) {
      rethrow;
    }
  }
}
