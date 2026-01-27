class User {
  final int memberId;
  final String userId;
  final String email;
  final String fullName;
  final String? role;
  final double walletBalance;
  final String tier;
  final double rankLevel;
  final String? avatarUrl;

  User({
    required this.memberId,
    required this.userId,
    required this.email,
    required this.fullName,
    this.role,
    this.walletBalance = 0.0,
    required this.tier,
    this.rankLevel = 0.0,
    this.avatarUrl,
  });

  factory User.fromJson(Map<String, dynamic> json) {
    return User(
      memberId: _parseInt(json['memberId'] ?? json['id']),
      userId: (json['userId'] ?? '').toString(),
      email: (json['email'] ?? '').toString(),
      fullName: json['fullName'] ?? '',
      role: json['role'],
      walletBalance: _parseDouble(json['walletBalance'] ?? 0),
      tier: json['tier'] ?? 'Standard',
      rankLevel: _parseDouble(json['rankLevel'] ?? 0),
      avatarUrl: json['avatarUrl'],
    );
  }

  static int _parseInt(dynamic value) {
    if (value is int) return value;
    if (value is String) return int.tryParse(value) ?? 0;
    return 0;
  }

  static double _parseDouble(dynamic value) {
    if (value is double) return value;
    if (value is int) return value.toDouble();
    if (value is String) return double.tryParse(value) ?? 0.0;
    return 0.0;
  }

  Map<String, dynamic> toJson() {
    return {
      'memberId': memberId,
      'userId': userId,
      'email': email,
      'fullName': fullName,
      'role': role,
      'walletBalance': walletBalance,
      'tier': tier,
      'rankLevel': rankLevel,
      'avatarUrl': avatarUrl,
    };
  }
}
