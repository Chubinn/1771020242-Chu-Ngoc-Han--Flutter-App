🎾 Pickleball Club Management - Vợt Thủ Phố Núi (PCM) - Mobile Edition

Sinh viên: Đoàn Đình Hậu
MSSV: 1771020250 (đuôi 250)  
Môn học: Lập trình Mobile với Flutter

Hệ thống quản lý CLB Pickleball với Backend (ASP.NET Core Web API), Frontend (Flutter Mobile/Web/Desktop) và Database (MySQL qua EF Core Code First).

📁 Cấu trúc dự án

MOBILE_FLUTTER_1771020250_doandinhhau/
├── Backend/                         # ASP.NET Core Web API (.NET 8)
│   └── PcmApi/
│       ├── Controllers/            # API Controllers (Auth, Wallet, Bookings, ...)
│       ├── Models/                 # Entities + Enums (prefix bảng 250_)
│       ├── Data/                   # PcmDbContext + DbSeeder
│       ├── Dtos/                   # Data Transfer Objects
│       ├── Hubs/                   # SignalR Hub (PcmHub)
│       ├── Services/               # Business services + Background services
│       ├── Migrations/             # EF Core migrations
│       ├── Program.cs              # Cấu hình DI, JWT, CORS, Swagger, SignalR
│       └── appsettings.json        # Connection string + JWT settings
├── Mobile/
│   └── pcm_mobile/                 # Flutter project
│       ├── lib/
│       │   ├── bloc/               # AuthBloc
│       │   ├── models/             # Dart models
│       │   ├── screens/            # Các màn hình chính
│       │   └── services/           # ApiService (Dio + JWT storage)
│       ├── test/                   # Widget tests cơ bản
│       └── pubspec.yaml
└── API_TESTING_GUIDE.md            # Hướng dẫn test nhanh API

🛠️ Tech Stack

Backend
- Framework: ASP.NET Core Web API (.NET 8)
- ORM: Entity Framework Core (Code First + Migrations)
- Authentication: ASP.NET Core Identity + JWT Bearer
- Real-time: SignalR (PcmHub)
- Background Services: HostedService (dọn hold, nhắc trận đấu)
- Database: MySQL (theo cấu hình hiện tại trong appsettings.json)

Frontend (Flutter)
- Framework: Flutter 3.x, Dart 3.x
- State Management: flutter_bloc (AuthBloc)
- Networking: Dio (Interceptor + JWT)
- Storage: flutter_secure_storage
- UI: table_calendar, fl_chart, material widgets

🚀 Hướng dẫn cài đặt & chạy

0) Lưu ý về API URL theo nền tảng
- Web/Desktop (Chrome/Edge/Windows): dùng `http://localhost:5253/api`
- Android Emulator: dùng `http://10.0.2.2:5253/api`

Trong dự án này, `ApiService` đang mặc định:
- `Mobile/pcm_mobile/lib/services/api_service.dart`: `http://localhost:5253/api`

Bạn có thể override khi chạy:

PowerShell (ví dụ cho Android Emulator):
```powershell
flutter run -d emulator-5554 --dart-define=PCM_API_BASE_URL=http://10.0.2.2:5253/api
```

1) Backend API (ASP.NET Core)

Chạy tại thư mục backend:
```powershell
cd d:\MOBILE_FLUTTER_1771020250_doandinhhau\Backend\PcmApi

# Restore packages
dotnet restore

# Chạy ở môi trường Development để auto migrate + seed
$env:ASPNETCORE_ENVIRONMENT='Development'
dotnet run
```

Mặc định theo `launchSettings.json`:
- API base URL: `http://localhost:5253`
- Swagger UI: `http://localhost:5253/swagger`

Ghi chú:
- Khi chạy Development, hệ thống sẽ:
  - Tự động `Database.Migrate()`
  - Tự động seed dữ liệu qua `DbSeeder`

2) Frontend Flutter

Chạy Flutter app:
```powershell
cd d:\MOBILE_FLUTTER_1771020250_doandinhhau\Mobile\pcm_mobile

# Lấy dependencies
flutter pub get

# Chạy trên Edge (web)
flutter run -d edge

# Hoặc chạy trên Windows desktop
flutter run -d windows
```

👤 Tài khoản demo (seed sẵn)

Dữ liệu seed nằm tại:
- `Backend/PcmApi/Data/DbSeeder.cs`

Các tài khoản chính:

| Email | Password | Role | Ghi chú |
| --- | --- | --- | --- |
| admin@pcm.com | Admin@123 | Admin | Quản trị hệ thống |
| treasurer@pcm.com | Treasurer@123 | Treasurer | Duyệt nạp tiền, theo dõi quỹ |
| referee@pcm.com | Referee@123 | Referee | Cập nhật kết quả trận đấu |

Tài khoản hội viên:
- Từ `member1@pcm.com` đến `member20@pcm.com`
- Mật khẩu chung: `Member@123`
- Role: Member

📱 Tính năng chính (bám theo đề kiểm tra)

1) Xác thực & hội viên
- Đăng ký / đăng nhập bằng JWT
- Lấy thông tin user hiện tại: `/api/auth/me`
- Danh sách hội viên + profile: `/api/members`, `/api/members/{id}/profile`

2) Ví điện tử (Wallet)
- Tạo yêu cầu nạp tiền: `/api/wallet/deposit`
- Xem số dư: `/api/wallet/balance`
- Lịch sử giao dịch: `/api/wallet/transactions`
- Admin/Treasurer duyệt nạp: `/api/admin/wallet/approve/{transactionId}`

3) Đặt sân thông minh (Booking)
- Xem lịch: `/api/bookings/calendar?from=...&to=...`
- Giữ chỗ 5 phút: `/api/bookings/hold`
- Đặt sân + trừ ví: `/api/bookings`
- Hủy sân: `/api/bookings/cancel/{id}` hoặc `DELETE /api/bookings/{id}`
- Đặt sân định kỳ (VIP): `/api/bookings/recurring`

4) Giải đấu & trận đấu
- Danh sách/tạo giải: `/api/tournaments`, `POST /api/tournaments`
- Tham gia giải (trừ entry fee): `/api/tournaments/{id}/join`
- Sinh lịch tự động: `/api/tournaments/{id}/generate-schedule`
- Cập nhật kết quả: `/api/matches/{id}/result`

5) Thông báo & real-time
- SignalR Hub: `/pcm-hub`
- Notification API: `/api/notifications`, `/api/notifications/{id}/read`
- Backend đã broadcast các sự kiện:
  - `ReceiveNotification`
  - `UpdateCalendar`
  - `UpdateMatchScore`

Ghi chú quan trọng:
- Backend đã có SignalR đầy đủ.
- Flutter hiện chủ yếu gọi REST API; phần client SignalR có thể mở rộng thêm.

🧩 Database & quy ước MSSV

Theo yêu cầu đề bài, tên bảng có prefix 3 số cuối MSSV.

Trong dự án này:
- MSSV: `1771020250`
- Prefix: `250_`

Các bảng chính:
- `250_Members`
- `250_WalletTransactions`
- `250_Courts`
- `250_Bookings`
- `250_Tournaments`
- `250_TournamentParticipants`
- `250_Matches`
- `250_Notifications`
- `250_News`
- `250_TransactionCategories`

🔧 Một số lỗi thường gặp & cách xử lý nhanh

1) Đăng nhập bị treo / DioException trên Web
- Nguyên nhân hay gặp: dùng `10.0.2.2` khi chạy web/desktop.
- Cách xử lý:
  - Dùng `http://localhost:5253/api`
  - Hoặc chạy với `--dart-define=PCM_API_BASE_URL=...`

2) Đỏ màn hình ở tab Đặt sân (TableCalendar assertion)
- Nguyên nhân: `focusedDay` vượt quá `lastDay`.
- Đã xử lý bằng cách mở rộng khoảng ngày và clamp ngày hợp lệ.

3) Không gọi được API
- Kiểm tra backend đang chạy chưa:
  - `http://localhost:5253/swagger`
- Kiểm tra đúng base URL theo nền tảng (web vs emulator).

📌 Gợi ý demo đúng yêu cầu chấm bài

Luồng gợi ý để quay video demo:
1) Mở app → đăng nhập bằng `admin@pcm.com`
2) Vào Ví → gửi yêu cầu nạp tiền
3) Duyệt nạp tiền (qua API/Swagger hoặc role phù hợp)
4) Vào Đặt sân → chọn sân, chọn giờ → đặt sân
5) Kiểm tra số dư ví giảm sau khi đặt

🎓 Thông tin sinh viên

- Họ tên: Đoàn Đình Hậu
- MSSV: 1771020250
- Đuôi MSSV dùng prefix bảng: 250


## Getting Started

This project is a starting point for a Flutter application.

A few resources to get you started if this is your first Flutter project:

- [Lab: Write your first Flutter app](https://docs.flutter.dev/get-started/codelab)
- [Cookbook: Useful Flutter samples](https://docs.flutter.dev/cookbook)

For help getting started with Flutter development, view the
[online documentation](https://docs.flutter.dev/), which offers tutorials,
samples, guidance on mobile development, and a full API reference.
#   1 7 7 1 0 2 0 2 4 2 - C h u - N g o c - H a n - - F l u t t e r - A p p  
 