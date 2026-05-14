class AppUser {
  const AppUser({
    required this.id,
    required this.email,
    required this.fullName,
    this.phoneNumber,
    this.userName,
    this.roles = const [],
    this.isActive = true,
    this.vehicles = const [],
    this.monthlyPasses = const [],
  });

  final String id;
  final String email;
  final String fullName;
  final String? phoneNumber;
  final String? userName;
  final List<String> roles;
  final bool isActive;
  final List<VehicleInfo> vehicles;
  final List<MonthlyPassInfo> monthlyPasses;

  factory AppUser.fromJson(Map<String, dynamic> json) => AppUser(
        id: json['id']?.toString() ?? '',
        email: json['email']?.toString() ?? '',
        fullName: json['fullName']?.toString() ?? '',
        phoneNumber: json['phoneNumber']?.toString(),
        userName: json['userName']?.toString(),
        roles: (json['roles'] as List<dynamic>? ?? [])
            .map((item) => item.toString())
            .toList(),
        isActive: json['isActive'] != false,
        vehicles: (json['vehicles'] as List<dynamic>? ?? [])
            .map((item) => VehicleInfo.fromJson(item as Map<String, dynamic>))
            .toList(),
        monthlyPasses: (json['monthlyPasses'] as List<dynamic>? ?? [])
            .map((item) => MonthlyPassInfo.fromJson(item as Map<String, dynamic>))
            .toList(),
      );
}

class WalletInfo {
  const WalletInfo({required this.balance, required this.currency});

  final double balance;
  final String currency;

  factory WalletInfo.fromJson(Map<String, dynamic> json) => WalletInfo(
        balance: (json['balance'] as num? ?? 0).toDouble(),
        currency: json['currency']?.toString() ?? 'VND',
      );
}

class WalletTransaction {
  const WalletTransaction({
    required this.type,
    required this.amount,
    required this.balanceAfter,
    required this.description,
    required this.createdAt,
  });

  final String type;
  final double amount;
  final double balanceAfter;
  final String description;
  final DateTime createdAt;

  factory WalletTransaction.fromJson(Map<String, dynamic> json) =>
      WalletTransaction(
        type: json['type']?.toString() ?? '',
        amount: (json['amount'] as num? ?? 0).toDouble(),
        balanceAfter: (json['balanceAfter'] as num? ?? 0).toDouble(),
        description: json['description']?.toString() ?? '',
        createdAt: DateTime.tryParse(json['createdAt']?.toString() ?? '') ??
            DateTime.now(),
      );
}

class VehicleInfo {
  const VehicleInfo({
    required this.id,
    required this.licensePlate,
    required this.vehicleType,
    required this.brand,
    required this.color,
    required this.isDefault,
  });

  final String id;
  final String licensePlate;
  final String vehicleType;
  final String brand;
  final String color;
  final bool isDefault;

  factory VehicleInfo.fromJson(Map<String, dynamic> json) => VehicleInfo(
        id: json['id']?.toString() ?? '',
        licensePlate: json['licensePlate']?.toString() ?? '',
        vehicleType: json['vehicleType']?.toString() ?? '',
        brand: json['brand']?.toString() ?? '',
        color: json['color']?.toString() ?? '',
        isDefault: json['isDefault'] == true,
      );
}

class MonthlyPassInfo {
  const MonthlyPassInfo({
    required this.id,
    required this.licensePlate,
    required this.ownerName,
    this.ownerPhone,
    required this.validFrom,
    required this.validTo,
    required this.isActive,
  });

  final int id;
  final String licensePlate;
  final String ownerName;
  final String? ownerPhone;
  final DateTime validFrom;
  final DateTime validTo;
  final bool isActive;

  bool get isValidNow {
    final now = DateTime.now();
    return isActive && !now.isBefore(validFrom) && !now.isAfter(validTo);
  }

  factory MonthlyPassInfo.fromJson(Map<String, dynamic> json) =>
      MonthlyPassInfo(
        id: (json['id'] as num? ?? 0).toInt(),
        licensePlate: json['licensePlate']?.toString() ?? '',
        ownerName: json['ownerName']?.toString() ?? '',
        ownerPhone: json['ownerPhone']?.toString(),
        validFrom: DateTime.tryParse(json['validFrom']?.toString() ?? '') ??
            DateTime.now(),
        validTo: DateTime.tryParse(json['validTo']?.toString() ?? '') ??
            DateTime.now(),
        isActive: json['isActive'] == true,
      );
}

class ParkingHistoryItem {
  const ParkingHistoryItem({
    required this.licensePlate,
    required this.checkInTime,
    this.checkOutTime,
    this.durationMinutes,
    this.feeAmount,
    required this.paymentStatus,
    required this.status,
  });

  final String licensePlate;
  final DateTime checkInTime;
  final DateTime? checkOutTime;
  final int? durationMinutes;
  final double? feeAmount;
  final String paymentStatus;
  final String status;

  factory ParkingHistoryItem.fromJson(Map<String, dynamic> json) =>
      ParkingHistoryItem(
        licensePlate: json['licensePlate']?.toString() ?? '',
        checkInTime:
            DateTime.tryParse(json['checkInTime']?.toString() ?? '') ??
                DateTime.now(),
        checkOutTime: DateTime.tryParse(json['checkOutTime']?.toString() ?? ''),
        durationMinutes: (json['durationMinutes'] as num?)?.toInt(),
        feeAmount: (json['feeAmount'] as num?)?.toDouble(),
        paymentStatus: json['paymentStatus']?.toString() ?? '',
        status: json['status']?.toString() ?? '',
      );
}

class VehicleLocationInfo {
  const VehicleLocationInfo({
    required this.id,
    required this.licensePlate,
    required this.cameraId,
    required this.locationName,
    required this.confidence,
    this.imageBase64,
    this.fullFrameImageBase64,
    required this.detectedAt,
    required this.status,
    required this.severity,
    required this.message,
    this.parkingLotCode,
    this.zoneCode,
    this.columnCode,
  });

  final int id;
  final String licensePlate;
  final String cameraId;
  final String locationName;
  final double confidence;
  final String? imageBase64;
  final String? fullFrameImageBase64;
  final DateTime detectedAt;
  final String status;
  final String severity;
  final String message;
  final String? parkingLotCode;
  final String? zoneCode;
  final String? columnCode;

  factory VehicleLocationInfo.fromJson(Map<String, dynamic> json) =>
      VehicleLocationInfo(
        id: (json['id'] as num? ?? 0).toInt(),
        licensePlate: json['licensePlate']?.toString() ?? '',
        cameraId: json['cameraId']?.toString() ?? '',
        locationName: json['locationName']?.toString() ?? '',
        confidence: (json['confidence'] as num? ?? 0).toDouble(),
        imageBase64: json['imageBase64']?.toString(),
        fullFrameImageBase64: json['fullFrameImageBase64']?.toString(),
        detectedAt: DateTime.tryParse(json['detectedAt']?.toString() ?? '') ??
            DateTime.now(),
        status: json['status']?.toString() ?? '',
        severity: json['severity']?.toString() ?? '',
        message: json['message']?.toString() ?? '',
        parkingLotCode: json['parkingLotCode']?.toString(),
        zoneCode: json['zoneCode']?.toString(),
        columnCode: json['columnCode']?.toString(),
      );
}
