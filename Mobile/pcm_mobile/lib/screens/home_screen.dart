import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../bloc/auth_bloc.dart';
import '../models/index.dart';
import '../services/api_service.dart';
import 'login_screen.dart';

class HomeScreen extends StatefulWidget {
  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  int _selectedIndex = 0;
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
        title: Text('PCM Mobile'),
        backgroundColor: Colors.teal,
        elevation: 0,
      ),
      body: BlocListener<AuthBloc, AuthState>(
        listener: (context, state) {
          if (state is AuthUnauthenticatedState) {
            Navigator.of(
              context,
            ).pushReplacement(MaterialPageRoute(builder: (_) => LoginScreen()));
          }
        },
        child: _buildBody(),
      ),
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: _selectedIndex,
        onTap: (index) {
          setState(() {
            _selectedIndex = index;
          });
        },
        items: [
          BottomNavigationBarItem(icon: Icon(Icons.home), label: 'Trang chủ'),
          BottomNavigationBarItem(
            icon: Icon(Icons.sports_tennis),
            label: 'Sân',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.calendar_today),
            label: 'Đặt sân',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.account_balance_wallet),
            label: 'Ví',
          ),
          BottomNavigationBarItem(icon: Icon(Icons.person), label: 'Cá nhân'),
        ],
      ),
    );
  }

  Widget _buildBody() {
    switch (_selectedIndex) {
      case 0:
        return _buildHomeTab();
      case 1:
        return _buildCourtsTab();
      case 2:
        return _buildBookingsTab();
      case 3:
        return _buildWalletTab();
      case 4:
        return _buildProfileTab();
      default:
        return _buildHomeTab();
    }
  }

  Widget _buildHomeTab() {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(Icons.sports_tennis, size: 80, color: Colors.teal),
          SizedBox(height: 24),
          Text(
            'Chào mừng đến với PCM Mobile',
            style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
          ),
          SizedBox(height: 12),
          Text(
            'Hệ thống quản lý CLB Pickleball',
            style: TextStyle(fontSize: 16, color: Colors.grey),
          ),
          SizedBox(height: 48),
          ElevatedButton.icon(
            onPressed: () {
              setState(() {
                _selectedIndex = 1;
              });
            },
            icon: Icon(Icons.sports_tennis),
            label: Text('Xem danh sách sân'),
            style: ElevatedButton.styleFrom(
              backgroundColor: Colors.teal,
              padding: EdgeInsets.symmetric(horizontal: 24, vertical: 12),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildCourtsTab() {
    return FutureBuilder<List<Court>>(
      future: apiService.getCourts(),
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return Center(child: CircularProgressIndicator());
        }
        if (snapshot.hasError) {
          return Center(child: Text('Lỗi: ${snapshot.error}'));
        }
        final courts = snapshot.data ?? [];
        return ListView.builder(
          padding: EdgeInsets.all(16),
          itemCount: courts.length,
          itemBuilder: (context, index) {
            final court = courts[index];
            return Card(
              margin: EdgeInsets.only(bottom: 16),
              child: ListTile(
                title: Text(court.name),
                subtitle: Text(
                  '${court.pricePerHour.toStringAsFixed(0)} VND/giờ',
                ),
                trailing: court.isActive
                    ? Chip(label: Text('Còn trống'))
                    : Chip(label: Text('Đóng')),
              ),
            );
          },
        );
      },
    );
  }

  Widget _buildBookingsTab() {
    return FutureBuilder<List<Booking>>(
      future: apiService.getMyBookings(),
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return Center(child: CircularProgressIndicator());
        }
        if (snapshot.hasError) {
          return Center(child: Text('Lỗi: ${snapshot.error}'));
        }
        final bookings = snapshot.data ?? [];
        if (bookings.isEmpty) {
          return Center(child: Text('Chưa có lịch đặt'));
        }
        return ListView.builder(
          padding: EdgeInsets.all(16),
          itemCount: bookings.length,
          itemBuilder: (context, index) {
            final booking = bookings[index];
            return Card(
              margin: EdgeInsets.only(bottom: 16),
              child: ListTile(
                title: Text('Sân ${booking.courtId}'),
                subtitle: Text(
                  '${booking.startTime.toString().split('.')[0]} - ${booking.endTime.toString().split('.')[0]}',
                ),
                trailing: Text('${booking.totalPrice.toStringAsFixed(0)} VND'),
              ),
            );
          },
        );
      },
    );
  }

  Widget _buildWalletTab() {
    return FutureBuilder<Map<String, dynamic>>(
      future: apiService.getBalance(),
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return Center(child: CircularProgressIndicator());
        }
        if (snapshot.hasError) {
          return Center(child: Text('Lỗi: ${snapshot.error}'));
        }
        final balance = snapshot.data?['balance'] ?? 0.0;
        return Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(Icons.account_balance_wallet, size: 80, color: Colors.teal),
              SizedBox(height: 24),
              Text(
                'Số dư ví',
                style: TextStyle(fontSize: 18, color: Colors.grey),
              ),
              SizedBox(height: 12),
              Text(
                '${(balance as num).toStringAsFixed(0)} VND',
                style: TextStyle(
                  fontSize: 32,
                  fontWeight: FontWeight.bold,
                  color: Colors.teal,
                ),
              ),
              SizedBox(height: 48),
              ElevatedButton.icon(
                onPressed: () {},
                icon: Icon(Icons.add),
                label: Text('Nạp tiền'),
                style: ElevatedButton.styleFrom(
                  backgroundColor: Colors.teal,
                  padding: EdgeInsets.symmetric(horizontal: 24, vertical: 12),
                ),
              ),
            ],
          ),
        );
      },
    );
  }

  Widget _buildProfileTab() {
    return BlocBuilder<AuthBloc, AuthState>(
      builder: (context, state) {
        if (state is AuthAuthenticatedState) {
          return Center(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                CircleAvatar(radius: 60, child: Icon(Icons.person, size: 60)),
                SizedBox(height: 24),
                Text(
                  state.user.fullName,
                  style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
                ),
                SizedBox(height: 8),
                Text(
                  state.user.email,
                  style: TextStyle(fontSize: 16, color: Colors.grey),
                ),
                SizedBox(height: 24),
                Text(
                  'Số dư: ${state.user.walletBalance.toStringAsFixed(0)} VND',
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                    color: Colors.teal,
                  ),
                ),
                SizedBox(height: 48),
                ElevatedButton.icon(
                  onPressed: () {
                    context.read<AuthBloc>().add(LogoutEvent());
                  },
                  icon: Icon(Icons.logout),
                  label: Text('Đăng xuất'),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: Colors.red,
                    padding: EdgeInsets.symmetric(horizontal: 24, vertical: 12),
                  ),
                ),
              ],
            ),
          );
        }
        return Center(child: Text('Chưa đăng nhập'));
      },
    );
  }
}
