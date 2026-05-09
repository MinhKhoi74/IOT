# Appuser

Flutter app cho khách hàng SmartParking.

## Chạy app

Máy này chưa có Flutter trong PATH, nên sau khi cài Flutter SDK có thể chạy:

```powershell
cd Appuser
flutter create . --platforms=android,web
flutter pub get
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5000/api
```

Gợi ý `API_BASE_URL`:

- Android emulator: `http://10.0.2.2:5000/api`
- iOS simulator hoặc web local: `http://localhost:5000/api`
- Máy thật: dùng IP LAN của máy chạy backend, ví dụ `http://192.168.1.10:5000/api`

## Chức năng

- Đăng ký, đăng nhập và lưu JWT.
- Menu đáy: Hồ sơ, Ví, Lịch sử, Xe.
- Sửa thông tin cơ bản.
- Xem ví, lịch sử giao dịch, nạp tiền MoMo.
- Xem lịch sử gửi xe.
- Đăng ký xe và đăng ký vé tháng qua backend.
