import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import '../models/index.dart';
import '../services/api_service.dart';

class NotificationsScreen extends StatefulWidget {
  @override
  State<NotificationsScreen> createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends State<NotificationsScreen> {
  late ApiService apiService;

  @override
  void initState() {
    super.initState();
    apiService = context.read<ApiService>();
  }

  @override
  Widget build(BuildContext context) {
    final apiService = context.read<ApiService>();
    return Scaffold(
      appBar: AppBar(
        title: Text('Thông báo'),
        backgroundColor: Colors.teal,
        elevation: 0,
      ),
      body: FutureBuilder<List<AppNotification>>(
        future: apiService.getNotifications(),
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return Center(child: CircularProgressIndicator());
          }
          if (snapshot.hasError) {
            return Center(child: Text('Lỗi: ${snapshot.error}'));
          }
          final notifications = snapshot.data ?? [];
          if (notifications.isEmpty) {
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(Icons.notifications_off, size: 80, color: Colors.grey),
                  SizedBox(height: 16),
                  Text('Không có thông báo', style: TextStyle(fontSize: 18)),
                ],
              ),
            );
          }
          return ListView.builder(
            padding: EdgeInsets.all(8),
            itemCount: notifications.length,
            itemBuilder: (context, index) {
              final notif = notifications[index];
              return Card(
                margin: EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                color: notif.isRead ? Colors.white : Colors.teal.shade50,
                child: ListTile(
                  leading: _getNotificationIcon(notif.type),
                  title: Text(
                    notif.message,
                    style: TextStyle(
                      fontWeight: notif.isRead
                          ? FontWeight.normal
                          : FontWeight.bold,
                    ),
                  ),
                  subtitle: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      SizedBox(height: 4),
                      Text(
                        '${notif.type} - ${_formatDate(notif.createdDate)}',
                        style: TextStyle(fontSize: 12, color: Colors.grey),
                      ),
                    ],
                  ),
                  trailing: !notif.isRead
                      ? Container(
                          width: 12,
                          height: 12,
                          decoration: BoxDecoration(
                            color: Colors.teal,
                            shape: BoxShape.circle,
                          ),
                        )
                      : null,
                  onTap: !notif.isRead
                      ? () async {
                          await apiService.markNotificationAsRead(notif.id);
                          setState(() {});
                        }
                      : null,
                ),
              );
            },
          );
        },
      ),
    );
  }

  Widget _getNotificationIcon(String type) {
    IconData icon;
    Color color;
    switch (type) {
      case 'Success':
        icon = Icons.check_circle;
        color = Colors.green;
        break;
      case 'Warning':
        icon = Icons.warning;
        color = Colors.orange;
        break;
      case 'Error':
        icon = Icons.error;
        color = Colors.red;
        break;
      default:
        icon = Icons.notifications;
        color = Colors.teal;
    }
    return Icon(icon, color: color);
  }

  String _formatDate(DateTime date) {
    final now = DateTime.now();
    final diff = now.difference(date);
    if (diff.inMinutes < 1) return 'Vừa xong';
    if (diff.inMinutes < 60) return '${diff.inMinutes} phút trước';
    if (diff.inHours < 24) return '${diff.inHours} giờ trước';
    if (diff.inDays < 7) return '${diff.inDays} ngày trước';
    return '${date.day}/${date.month}/${date.year}';
  }
}

