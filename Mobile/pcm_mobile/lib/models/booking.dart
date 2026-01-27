class Booking {
  final int id;
  final int courtId;
  final int memberId;
  final DateTime startTime;
  final DateTime endTime;
  final double totalPrice;
  final String status;
  final DateTime createdDate;
  final int? transactionId;

  Booking({
    required this.id,
    required this.courtId,
    required this.memberId,
    required this.startTime,
    required this.endTime,
    required this.totalPrice,
    required this.status,
    required this.createdDate,
    this.transactionId,
  });

  factory Booking.fromJson(Map<String, dynamic> json) {
    final id = _parseInt(json['id']);
    final courtId = _parseInt(json['courtId']);
    final memberId = _parseInt(json['memberId']);
    final status = (json['status'] ?? 'Unknown').toString();
    final start = DateTime.tryParse(json['startTime']?.toString() ?? '');
    final end = DateTime.tryParse(json['endTime']?.toString() ?? '');
    final created = DateTime.tryParse(json['createdDate']?.toString() ?? '');

    return Booking(
      id: id,
      courtId: courtId,
      memberId: memberId,
      startTime: start ?? DateTime.now(),
      endTime: end ?? DateTime.now(),
      totalPrice: _parseDouble(json['totalPrice']),
      status: status,
      createdDate: created ?? DateTime.now(),
      transactionId: _parseNullableInt(json['transactionId']),
    );
  }

  static int _parseInt(dynamic value) {
    if (value is int) return value;
    if (value is String) return int.tryParse(value) ?? 0;
    return 0;
  }

  static int? _parseNullableInt(dynamic value) {
    if (value == null) return null;
    if (value is int) return value;
    if (value is String) return int.tryParse(value);
    return null;
  }

  static double _parseDouble(dynamic value) {
    if (value is double) return value;
    if (value is int) return value.toDouble();
    if (value is String) return double.tryParse(value) ?? 0.0;
    return 0.0;
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'courtId': courtId,
      'memberId': memberId,
      'startTime': startTime.toIso8601String(),
      'endTime': endTime.toIso8601String(),
      'totalPrice': totalPrice,
      'status': status,
      'createdDate': createdDate.toIso8601String(),
      'transactionId': transactionId,
    };
  }
}
