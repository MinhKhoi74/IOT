class AppUser {
  const AppUser({
    required this.id,
    required this.email,
    required this.fullName,
    this.phoneNumber,
  });

  final String id;
  final String email;
  final String fullName;
  final String? phoneNumber;

  factory AppUser.fromJson(Map<String, dynamic> json) => AppUser(
        id: json['id']?.toString() ?? '',
        email: json['email']?.toString() ?? '',
        fullName: json['fullName']?.toString() ?? '',
        phoneNumber: json['phoneNumber']?.toString(),
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
