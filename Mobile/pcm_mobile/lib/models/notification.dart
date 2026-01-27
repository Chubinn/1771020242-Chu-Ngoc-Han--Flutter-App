class Notification {
  final int id;
  final int memberId;
  final String title;
  final String message;
  final String type; // Booking, Deposit, Tournament, Match, System
  final bool isRead;
  final DateTime createdDate;
  final int? relatedId; // Booking ID, Tournament ID, etc.

  Notification({
    required this.id,
    required this.memberId,
    required this.title,
    required this.message,
    required this.type,
    required this.isRead,
    required this.createdDate,
    this.relatedId,
  });

  factory Notification.fromJson(Map<String, dynamic> json) {
    int id = 0;
    final idValue = json['id'];
    if (idValue is int)
      id = idValue;
    else if (idValue is String)
      id = int.tryParse(idValue) ?? 0;

    int memberId = 0;
    final memberIdValue = json['memberId'];
    if (memberIdValue is int)
      memberId = memberIdValue;
    else if (memberIdValue is String)
      memberId = int.tryParse(memberIdValue) ?? 0;

    return Notification(
      id: id,
      memberId: memberId,
      title: json['title'] ?? '',
      message: json['message'] ?? '',
      type: json['type'] ?? 'System',
      isRead: json['isRead'] ?? false,
      createdDate: DateTime.parse(
        json['createdDate'] ?? DateTime.now().toString(),
      ),
      relatedId: json['relatedId'] as int?,
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'memberId': memberId,
    'title': title,
    'message': message,
    'type': type,
    'isRead': isRead,
    'createdDate': createdDate.toIso8601String(),
    'relatedId': relatedId,
  };
}
