import 'dart:convert';

import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

import '../models/app_models.dart';

class ApiException implements Exception {
  ApiException(this.message);

  final String message;

  @override
  String toString() => message;
}

class ApiService {
  ApiService({http.Client? client}) : _client = client ?? http.Client();

  static const baseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://localhost:5000/api',
  );

  final http.Client _client;
  String? _token;
  String? _refreshToken;

  bool get isSignedIn => _token != null && _token!.isNotEmpty;

  Future<void> loadSession() async {
    final prefs = await SharedPreferences.getInstance();
    _token = prefs.getString('token');
    _refreshToken = prefs.getString('refreshToken');
  }

  Future<void> clearSession() async {
    _token = null;
    _refreshToken = null;
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove('token');
    await prefs.remove('refreshToken');
  }

  Future<AppUser> login(String email, String password) async {
    final json = await _post('/auth/login', {
      'email': email,
      'password': password,
    });
    await _saveAuth(json);
    return AppUser.fromJson(json['user'] as Map<String, dynamic>);
  }

  Future<AppUser> register(
      String fullName, String email, String password) async {
    final json = await _post('/auth/register', {
      'fullName': fullName,
      'email': email,
      'password': password,
    });
    await _saveAuth(json);
    return AppUser.fromJson(json['user'] as Map<String, dynamic>);
  }

  Future<AppUser> getProfile() async {
    final json = await _get('/users/profile');
    return AppUser.fromJson(json);
  }

  Future<void> updateProfile(String fullName, String phoneNumber) async {
    await _put('/users/profile', {
      'fullName': fullName,
      'phoneNumber': phoneNumber,
    });
  }

  Future<WalletInfo> getWallet() async {
    final json = await _get('/wallet/me');
    return WalletInfo.fromJson(json);
  }

  Future<List<WalletTransaction>> getWalletTransactions() async {
    final data = await _getList('/wallet/transactions?take=30');
    return data
        .map((item) => WalletTransaction.fromJson(item as Map<String, dynamic>))
        .toList();
  }

  Future<Map<String, dynamic>> createMomoTopUp(
    double amount,
    String paymentMethod,
  ) async {
    return _post('/wallet/top-up/momo', {
      'amount': amount,
      'description': 'Nạp tiền ví SmartParking',
      'paymentMethod': paymentMethod,
    });
  }

  Future<Map<String, dynamic>> createMonthlyPassMomoPayment({
    required double amount,
    required String paymentMethod,
    required String licensePlate,
    required String ownerName,
    required String ownerPhone,
    required DateTime validFrom,
    required DateTime validTo,
  }) async {
    return _post('/monthly-passes/momo', {
      'amount': amount,
      'paymentMethod': paymentMethod,
      'licensePlate': licensePlate.trim().toUpperCase(),
      'ownerName': ownerName.trim(),
      'ownerPhone': ownerPhone.trim(),
      'validFrom': validFrom.toIso8601String(),
      'validTo': validTo.toIso8601String(),
      'isActive': true,
    });
  }

  Future<double> getMonthlyPassPrice() async {
    final json = await _get('/monthly-passes/price');
    return (json['monthlyAmount'] as num? ?? 200000).toDouble();
  }

  Future<List<ParkingHistoryItem>> getParkingHistory() async {
    final data = await _getList('/parking/history/me');
    return data
        .map(
            (item) => ParkingHistoryItem.fromJson(item as Map<String, dynamic>))
        .toList();
  }

  Future<List<VehicleLocationInfo>> getVehicleLocations() async {
    final data = await _getList('/parking/vehicle-locations/me');
    return data
        .map((item) =>
            VehicleLocationInfo.fromJson(item as Map<String, dynamic>))
        .toList();
  }

  Future<ParkingMapInfo> getParkingMap(String branchId) async {
    final json = await _get('/parking-map/branches/$branchId');
    return ParkingMapInfo.fromJson(json);
  }

  Future<List<VehicleInfo>> getVehicles() async {
    final data = await _getList('/vehicles');
    return data
        .map((item) => VehicleInfo.fromJson(item as Map<String, dynamic>))
        .toList();
  }

  Future<void> createVehicle({
    required String licensePlate,
    required String vehicleType,
    required String brand,
    required String color,
    required bool isDefault,
  }) async {
    await _post('/vehicles', {
      'licensePlate': licensePlate.trim().toUpperCase(),
      'vehicleType': vehicleType,
      'brand': brand,
      'color': color,
      'isDefault': isDefault,
    });
  }

  Future<void> updateVehicle({
    required String id,
    required String brand,
    required String color,
    required bool isDefault,
  }) async {
    await _put('/vehicles/$id', {
      'brand': brand,
      'color': color,
      'isDefault': isDefault,
    });
  }

  Future<void> deleteVehicle(String id) async {
    await _delete('/vehicles/$id');
  }

  Future<void> registerMonthlyPass({
    required String licensePlate,
    required String ownerName,
    required String ownerPhone,
    required DateTime validFrom,
    required DateTime validTo,
  }) async {
    await _post('/monthly-passes/register', {
      'licensePlate': licensePlate.trim().toUpperCase(),
      'ownerName': ownerName,
      'ownerPhone': ownerPhone,
      'validFrom': validFrom.toIso8601String(),
      'validTo': validTo.toIso8601String(),
      'isActive': true,
    });
  }

  Future<void> _saveAuth(Map<String, dynamic> json) async {
    _token = json['token']?.toString();
    _refreshToken = json['refreshToken']?.toString();
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('token', _token ?? '');
    await prefs.setString('refreshToken', _refreshToken ?? '');
  }

  Future<Map<String, dynamic>> _get(String path) async {
    final response = await _client.get(_uri(path), headers: _headers());
    return _decodeMap(response);
  }

  Future<List<dynamic>> _getList(String path) async {
    final response = await _client.get(_uri(path), headers: _headers());
    final data = _decode(response);
    if (data is List) return data;
    throw ApiException('Dữ liệu trả về không hợp lệ.');
  }

  Future<Map<String, dynamic>> _post(
      String path, Map<String, dynamic> body) async {
    final response = await _client.post(
      _uri(path),
      headers: _headers(),
      body: jsonEncode(body),
    );
    return _decodeMap(response);
  }

  Future<Map<String, dynamic>> _put(
      String path, Map<String, dynamic> body) async {
    final response = await _client.put(
      _uri(path),
      headers: _headers(),
      body: jsonEncode(body),
    );
    return _decodeMap(response);
  }

  Future<Map<String, dynamic>> _delete(String path) async {
    final response = await _client.delete(_uri(path), headers: _headers());
    return _decodeMap(response);
  }

  Uri _uri(String path) => Uri.parse('$baseUrl$path');

  Map<String, String> _headers() => {
        'Content-Type': 'application/json',
        if (_token != null && _token!.isNotEmpty)
          'Authorization': 'Bearer $_token',
      };

  Map<String, dynamic> _decodeMap(http.Response response) {
    final data = _decode(response);
    if (data is Map<String, dynamic>) return data;
    if (response.statusCode >= 200 && response.statusCode < 300) return {};
    throw ApiException('Dữ liệu trả về không hợp lệ.');
  }

  dynamic _decode(http.Response response) {
    final text = utf8.decode(response.bodyBytes);
    final data = text.isEmpty ? null : jsonDecode(text);
    if (response.statusCode >= 200 && response.statusCode < 300) return data;
    final message = data is Map ? data['message']?.toString() : null;
    throw ApiException(message ?? 'Lỗi ${response.statusCode}: $text');
  }
}
