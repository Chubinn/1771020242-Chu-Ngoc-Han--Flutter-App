import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:image_picker/image_picker.dart';
import 'dart:io';
import '../models/index.dart';
import '../services/api_service.dart';
import '../bloc/auth_bloc.dart';

class WalletScreen extends StatefulWidget {
  @override
  State<WalletScreen> createState() => _WalletScreenState();
}

class _WalletScreenState extends State<WalletScreen> {
  late ApiService apiService;

  @override
  void initState() {
    super.initState();
    apiService = context.read<ApiService>();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Wallet'),
        backgroundColor: Colors.teal,
        elevation: 0,
      ),
      body: DefaultTabController(
        length: 2,
        child: Column(
          children: [
            TabBar(
              labelColor: Colors.teal,
              unselectedLabelColor: Colors.grey,
              indicatorColor: Colors.teal,
              tabs: [
                Tab(text: 'Balance'),
                Tab(text: 'Transactions'),
              ],
            ),
            Expanded(
              child: TabBarView(
                children: [_buildBalanceTab(), _buildTransactionsTab()],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildBalanceTab() {
    return SingleChildScrollView(
      padding: EdgeInsets.all(16),
      child: Column(
        children: [
          SizedBox(height: 24),
          // Balance card
          Card(
            elevation: 4,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(16),
            ),
            color: Colors.teal,
            child: Padding(
              padding: EdgeInsets.all(24),
              child: Column(
                children: [
                  Text(
                    'Available Balance',
                    style: TextStyle(color: Colors.white70, fontSize: 16),
                  ),
                  SizedBox(height: 12),
                  BlocBuilder<AuthBloc, AuthState>(
                    builder: (context, state) {
                      if (state is AuthAuthenticatedState) {
                        return Text(
                          '${state.user.walletBalance.toStringAsFixed(0)} VND',
                          style: TextStyle(
                            fontSize: 40,
                            fontWeight: FontWeight.bold,
                            color: Colors.white,
                          ),
                        );
                      }
                      return Text('---');
                    },
                  ),
                  SizedBox(height: 12),
                  BlocBuilder<AuthBloc, AuthState>(
                    builder: (context, state) {
                      if (state is AuthAuthenticatedState) {
                        return Text(
                          'Tier: ${state.user.tier}',
                          style: TextStyle(color: Colors.white70),
                        );
                      }
                      return SizedBox();
                    },
                  ),
                ],
              ),
            ),
          ),
          SizedBox(height: 32),
          // Deposit form
          Text(
            'Top Up Your Wallet',
            style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
          ),
          SizedBox(height: 16),
          _DepositForm(apiService: apiService),
        ],
      ),
    );
  }

  Widget _buildTransactionsTab() {
    return FutureBuilder<List<WalletTransaction>>(
      future: apiService.getTransactions(),
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return Center(child: CircularProgressIndicator());
        }
        if (snapshot.hasError) {
          return Center(child: Text('Error: ${snapshot.error}'));
        }
        final transactions = snapshot.data ?? [];
        if (transactions.isEmpty) {
          return Center(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Icon(Icons.receipt_long, size: 80, color: Colors.grey),
                SizedBox(height: 16),
                Text('No transactions yet', style: TextStyle(fontSize: 18)),
              ],
            ),
          );
        }
        return ListView.builder(
          padding: EdgeInsets.all(12),
          itemCount: transactions.length,
          itemBuilder: (context, index) {
            final tx = transactions[index];
            final isIncoming = tx.type == 'Deposit' || tx.type == 'Refund';
            return Card(
              margin: EdgeInsets.only(bottom: 12),
              child: ListTile(
                leading: Icon(
                  isIncoming ? Icons.arrow_downward : Icons.arrow_upward,
                  color: isIncoming ? Colors.green : Colors.red,
                ),
                title: Text(tx.type),
                subtitle: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(tx.description, style: TextStyle(fontSize: 12)),
                    Text(
                      tx.createdDate.toString().split('.')[0],
                      style: TextStyle(fontSize: 11, color: Colors.grey),
                    ),
                  ],
                ),
                trailing: Text(
                  '${isIncoming ? '+' : '-'}${tx.amount.abs().toStringAsFixed(0)} VND',
                  style: TextStyle(
                    fontWeight: FontWeight.bold,
                    color: isIncoming ? Colors.green : Colors.red,
                  ),
                ),
              ),
            );
          },
        );
      },
    );
  }
}

class _DepositForm extends StatefulWidget {
  final ApiService apiService;

  const _DepositForm({required this.apiService});

  @override
  State<_DepositForm> createState() => __DepositFormState();
}

class __DepositFormState extends State<_DepositForm> {
  final TextEditingController _amountController = TextEditingController();
  final TextEditingController _descriptionController = TextEditingController();
  File? _selectedImage;
  bool _isLoading = false;

  @override
  void dispose() {
    _amountController.dispose();
    _descriptionController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        // Amount input
        TextField(
          controller: _amountController,
          keyboardType: TextInputType.number,
          decoration: InputDecoration(
            labelText: 'Amount (VND)',
            prefixIcon: Icon(Icons.currency_exchange),
            border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
          ),
        ),
        SizedBox(height: 16),
        // Description input
        TextField(
          controller: _descriptionController,
          decoration: InputDecoration(
            labelText: 'Description',
            prefixIcon: Icon(Icons.description),
            border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
          ),
        ),
        SizedBox(height: 16),
        // Image picker
        Container(
          width: double.infinity,
          padding: EdgeInsets.all(12),
          decoration: BoxDecoration(
            border: Border.all(color: Colors.grey),
            borderRadius: BorderRadius.circular(8),
          ),
          child: Column(
            children: [
              if (_selectedImage != null)
                Image.file(_selectedImage!, height: 150)
              else
                Icon(Icons.image, size: 80, color: Colors.grey),
              SizedBox(height: 12),
              ElevatedButton.icon(
                onPressed: _pickImage,
                icon: Icon(Icons.photo_camera),
                label: Text('Pick Proof Image'),
                style: ElevatedButton.styleFrom(
                  backgroundColor: Colors.teal.shade200,
                ),
              ),
            ],
          ),
        ),
        SizedBox(height: 24),
        // Submit button
        ElevatedButton(
          onPressed: _isLoading ? null : _submitDeposit,
          style: ElevatedButton.styleFrom(
            backgroundColor: Colors.teal,
            minimumSize: Size.fromHeight(48),
          ),
          child: _isLoading
              ? CircularProgressIndicator(color: Colors.white)
              : Text(
                  'Submit Deposit Request',
                  style: TextStyle(color: Colors.white, fontSize: 16),
                ),
        ),
      ],
    );
  }

  void _pickImage() async {
    final picker = ImagePicker();
    final image = await picker.pickImage(source: ImageSource.gallery);
    if (image != null) {
      setState(() => _selectedImage = File(image.path));
    }
  }

  void _submitDeposit() async {
    final amount = double.tryParse(_amountController.text);
    final description = _descriptionController.text;

    if (amount == null || amount <= 0) {
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text('Please enter a valid amount')));
      return;
    }

    if (description.isEmpty) {
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text('Please enter a description')));
      return;
    }

    setState(() => _isLoading = true);

    try {
      await widget.apiService.requestDeposit(
        amount: amount,
        description: description,
        proofImageUrl: _selectedImage?.path,
      );
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text('Deposit request submitted!')));
      _amountController.clear();
      _descriptionController.clear();
      setState(() => _selectedImage = null);
    } catch (e) {
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text('Error: $e')));
    } finally {
      setState(() => _isLoading = false);
    }
  }
}
