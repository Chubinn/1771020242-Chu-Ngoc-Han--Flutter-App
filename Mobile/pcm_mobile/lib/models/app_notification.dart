class AppNotification {
  final int id;
  final String message;
  final String type; // Info, Success, Warning, Error
  final bool isRead;
  final DateTime createdDate;

  AppNotification({
    required this.id,
    required this.message,
    required this.type,
    required this.isRead,
    required this.createdDate,
  });

  factory AppNotification.fromJson(Map<String, dynamic> json) {
    final id = _parseInt(json['id']);
    final created = DateTime.tryParse(json['createdDate']?.toString() ?? '');

    return AppNotification(
      id: id,
      message: json['message'] ?? '',
      type: json['type'] ?? 'System',
      isRead: json['isRead'] ?? false,
      createdDate: created ?? DateTime.now(),
    );
  }

  static int _parseInt(dynamic value) {
    if (value is int) return value;
    if (value is String) return int.tryParse(value) ?? 0;
    return 0;
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'message': message,
    'type': type,
    'isRead': isRead,
    'createdDate': createdDate.toIso8601String(),
  };
}
