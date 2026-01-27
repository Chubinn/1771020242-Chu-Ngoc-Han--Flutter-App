class WalletTransaction {
  final int id;
  final double amount;
  final String type; // Deposit, Booking, Tournament, Refund
  final String status; // Pending, Completed, Failed
  final String description;
  final DateTime createdDate;
  final String? proofImageUrl;

  WalletTransaction({
    required this.id,
    required this.amount,
    required this.type,
    required this.status,
    required this.description,
    required this.createdDate,
    this.proofImageUrl,
  });

  factory WalletTransaction.fromJson(Map<String, dynamic> json) {
    int id = 0;
    final idValue = json['id'];
    if (idValue is int)
      id = idValue;
    else if (idValue is String)
      id = int.tryParse(idValue) ?? 0;

    double amount = 0;
    final amountValue = json['amount'];
    if (amountValue is double)
      amount = amountValue;
    else if (amountValue is int)
      amount = amountValue.toDouble();
    else if (amountValue is String)
      amount = double.tryParse(amountValue) ?? 0;

    return WalletTransaction(
      id: id,
      amount: amount,
      type: json['type'] ?? 'Deposit',
      status: json['status'] ?? 'Pending',
      description: json['description'] ?? '',
      createdDate: DateTime.parse(
        json['createdDate'] ?? DateTime.now().toString(),
      ),
      proofImageUrl: json['proofImageUrl'],
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'amount': amount,
    'type': type,
    'status': status,
    'description': description,
    'createdDate': createdDate.toIso8601String(),
    'proofImageUrl': proofImageUrl,
  };
}
