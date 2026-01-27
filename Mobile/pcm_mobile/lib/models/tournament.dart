class Tournament {
  final int id;
  final String name;
  final DateTime startDate;
  final DateTime endDate;
  final double entryFee;
  final double prizePool;
  final String format;
  final int participantCount;
  final String status; // Open, Registering, DrawCompleted, Ongoing, Finished

  Tournament({
    required this.id,
    required this.name,
    required this.startDate,
    required this.endDate,
    required this.entryFee,
    required this.prizePool,
    required this.format,
    required this.participantCount,
    required this.status,
  });

  factory Tournament.fromJson(Map<String, dynamic> json) {
    final id = _parseInt(json['id']);
    final entryFee = _parseDouble(json['entryFee']);
    final prizePool = _parseDouble(json['prizePool']);
    final participantCount = _parseInt(json['participantCount']);
    final start = DateTime.tryParse(json['startDate']?.toString() ?? '');
    final end = DateTime.tryParse(json['endDate']?.toString() ?? '');

    return Tournament(
      id: id,
      name: json['name'] ?? '',
      startDate: start ?? DateTime.now(),
      endDate: end ?? DateTime.now(),
      entryFee: entryFee,
      prizePool: prizePool,
      format: (json['format'] ?? '').toString(),
      participantCount: participantCount,
      status: (json['status'] ?? 'Open').toString(),
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

  Map<String, dynamic> toJson() => {
    'id': id,
    'name': name,
    'startDate': startDate.toIso8601String(),
    'endDate': endDate.toIso8601String(),
    'entryFee': entryFee,
    'prizePool': prizePool,
    'format': format,
    'participantCount': participantCount,
    'status': status,
  };
}
