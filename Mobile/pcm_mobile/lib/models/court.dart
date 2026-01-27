class Court {
  final int id;
  final String name;
  final double pricePerHour;
  final bool isActive;
  final String? description;

  Court({
    required this.id,
    required this.name,
    required this.pricePerHour,
    required this.isActive,
    this.description,
  });

  factory Court.fromJson(Map<String, dynamic> json) {
    int id = 0;
    final idValue = json['id'];
    if (idValue is int) {
      id = idValue;
    } else if (idValue is String) {
      id = int.tryParse(idValue) ?? 0;
    }

    return Court(
      id: id,
      name: json['name'] ?? '',
      pricePerHour: (json['pricePerHour'] ?? 0).toDouble(),
      isActive: json['isActive'] ?? false,
      description: json['description'],
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name,
      'pricePerHour': pricePerHour,
      'isActive': isActive,
      'description': description,
    };
  }
}
