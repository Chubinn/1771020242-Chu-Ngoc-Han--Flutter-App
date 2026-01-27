import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:table_calendar/table_calendar.dart';
import '../models/index.dart';
import '../services/api_service.dart';

class BookingScreen extends StatefulWidget {
  @override
  State<BookingScreen> createState() => _BookingScreenState();
}

class _BookingScreenState extends State<BookingScreen> {
  static final DateTime _firstDay = DateTime(2024, 1, 1);
  static final DateTime _lastDay = DateTime(2030, 12, 31);

  late DateTime _focusedDay;
  late DateTime _selectedDay;
  late TimeOfDay _startTime;
  late TimeOfDay _endTime;
  int? _selectedCourtId;
  late final ApiService _apiService;

  @override
  void initState() {
    super.initState();
    _apiService = context.read<ApiService>();
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    _focusedDay = _clampDay(today);
    _selectedDay = _focusedDay;
    _startTime = const TimeOfDay(hour: 8, minute: 0);
    _endTime = const TimeOfDay(hour: 9, minute: 0);
  }

  DateTime _clampDay(DateTime day) {
    if (day.isBefore(_firstDay)) return _firstDay;
    if (day.isAfter(_lastDay)) return _lastDay;
    return day;
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Đặt sân'),
        backgroundColor: Colors.teal,
        elevation: 0,
      ),
      body: DefaultTabController(
        length: 2,
        child: Column(
          children: [
            const TabBar(
              labelColor: Colors.teal,
              unselectedLabelColor: Colors.grey,
              indicatorColor: Colors.teal,
              tabs: [
                Tab(text: 'Lịch'),
                Tab(text: 'Lịch của tôi'),
              ],
            ),
            Expanded(
              child: TabBarView(
                children: [_buildCalendarTab(), _buildMyBookingsTab()],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildCalendarTab() {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(12),
      child: Column(
        children: [
          Card(
            child: TableCalendar(
              firstDay: _firstDay,
              lastDay: _lastDay,
              focusedDay: _focusedDay,
              selectedDayPredicate: (day) => isSameDay(_selectedDay, day),
              onDaySelected: (selectedDay, focusedDay) {
                setState(() {
                  _selectedDay = _clampDay(selectedDay);
                  _focusedDay = _clampDay(focusedDay);
                });
              },
              calendarStyle: CalendarStyle(
                selectedDecoration: BoxDecoration(
                  color: Colors.teal,
                  shape: BoxShape.circle,
                ),
                todayDecoration: BoxDecoration(
                  color: Colors.teal.shade300,
                  shape: BoxShape.circle,
                ),
              ),
            ),
          ),
          const SizedBox(height: 16),
          Text(
            'Ngày đã chọn: ${_selectedDay.toString().split(' ')[0]}',
            style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 16),
          FutureBuilder<List<Court>>(
            future: _apiService.getCourts(),
            builder: (context, snapshot) {
              if (snapshot.connectionState == ConnectionState.waiting) {
                return const CircularProgressIndicator();
              }
              final courts = snapshot.data ?? [];
              return Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Chọn sân:',
                    style: TextStyle(fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 8),
                  ...courts.map((court) {
                    final isSelected = _selectedCourtId == court.id;
                    return InkWell(
                      onTap: () => setState(() => _selectedCourtId = court.id),
                      child: Card(
                        color: isSelected ? Colors.teal.shade100 : Colors.white,
                        child: Padding(
                          padding: const EdgeInsets.all(12),
                          child: Row(
                            children: [
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(
                                      court.name,
                                      style: const TextStyle(
                                        fontWeight: FontWeight.bold,
                                      ),
                                    ),
                                    Text(
                                      '${court.pricePerHour.toStringAsFixed(0)} VND/giờ',
                                      style: const TextStyle(
                                        fontSize: 12,
                                        color: Colors.grey,
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                              Radio<int>(
                                value: court.id,
                                groupValue: _selectedCourtId,
                                onChanged: (_) =>
                                    setState(() => _selectedCourtId = court.id),
                                activeColor: Colors.teal,
                              ),
                            ],
                          ),
                        ),
                      ),
                    );
                  }),
                  const SizedBox(height: 16),
                  const Text(
                    'Chọn giờ:',
                    style: TextStyle(fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      Expanded(
                        child: ElevatedButton.icon(
                          onPressed: () async {
                            final time = await showTimePicker(
                              context: context,
                              initialTime: _startTime,
                            );
                            if (time != null) {
                              setState(() => _startTime = time);
                            }
                          },
                          icon: const Icon(Icons.access_time),
                          label: Text(
                            'Bắt đầu: ${_startTime.format(context)}',
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: ElevatedButton.icon(
                          onPressed: () async {
                            final time = await showTimePicker(
                              context: context,
                              initialTime: _endTime,
                            );
                            if (time != null) {
                              setState(() => _endTime = time);
                            }
                          },
                          icon: const Icon(Icons.access_time),
                          label: Text(
                            'Kết thúc: ${_endTime.format(context)}',
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 16),
                  ElevatedButton(
                    onPressed: _selectedCourtId != null ? _bookCourt : null,
                    style: ElevatedButton.styleFrom(
                      backgroundColor: Colors.teal,
                      minimumSize: const Size.fromHeight(48),
                    ),
                    child: const Text(
                      'Đặt sân',
                      style: TextStyle(color: Colors.white, fontSize: 16),
                    ),
                  ),
                ],
              );
            },
          ),
        ],
      ),
    );
  }

  Widget _buildMyBookingsTab() {
    return FutureBuilder<List<Booking>>(
      future: _apiService.getMyBookings(),
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return const Center(child: CircularProgressIndicator());
        }
        if (snapshot.hasError) {
          return Center(child: Text('Lỗi: ${snapshot.error}'));
        }
        final bookings = snapshot.data ?? [];
        if (bookings.isEmpty) {
          return const Center(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Icon(Icons.calendar_today, size: 80, color: Colors.grey),
                SizedBox(height: 16),
                Text('Chưa có lịch đặt', style: TextStyle(fontSize: 18)),
              ],
            ),
          );
        }
        return ListView.builder(
          padding: const EdgeInsets.all(12),
          itemCount: bookings.length,
          itemBuilder: (context, index) {
            final booking = bookings[index];
            return Card(
              child: Padding(
                padding: const EdgeInsets.all(12),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Sân: ${booking.courtId}',
                      style: const TextStyle(
                        fontWeight: FontWeight.bold,
                        fontSize: 16,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Text('Bắt đầu: ${booking.startTime}'),
                    Text('Kết thúc: ${booking.endTime}'),
                    Text(
                      'Giá: ${booking.totalPrice.toStringAsFixed(0)} VND',
                      style: const TextStyle(
                        color: Colors.teal,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    Text('Trạng thái: ${booking.status}'),
                  ],
                ),
              ),
            );
          },
        );
      },
    );
  }

  void _bookCourt() async {
    if (_selectedCourtId == null) return;

    final startDateTime = DateTime(
      _selectedDay.year,
      _selectedDay.month,
      _selectedDay.day,
      _startTime.hour,
      _startTime.minute,
    );
    final endDateTime = DateTime(
      _selectedDay.year,
      _selectedDay.month,
      _selectedDay.day,
      _endTime.hour,
      _endTime.minute,
    );

    if (!endDateTime.isAfter(startDateTime)) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Giờ kết thúc phải sau giờ bắt đầu')),
      );
      return;
    }

    try {
      await _apiService.createBooking(
        courtId: _selectedCourtId!,
        startTime: startDateTime,
        endTime: endDateTime,
      );
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Đặt sân thành công!')),
      );
      setState(() {
        final now = DateTime.now();
        final today = DateTime(now.year, now.month, now.day);
        _selectedDay = _clampDay(today);
        _focusedDay = _selectedDay;
        _selectedCourtId = null;
      });
    } catch (e) {
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text('Lỗi: $e')));
    }
  }
}

