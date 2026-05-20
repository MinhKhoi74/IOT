# Smart Parking System - IoT

He thong Smart Parking la do an quan ly bai do xe thong minh, ket hop backend .NET, dashboard web React, ung dung khach hang Flutter, module nhan dien bien so bang AI va mach Arduino dieu khien cong/den/LCD.

## Tong quan kien truc

Du an gom cac phan chinh:

- `SmartParkingSystem/`: Backend ASP.NET Core 8, cung cap REST API, SignalR realtime, JWT authentication, Entity Framework Core, SQL Server, Redis cache, thanh toan MoMo va giao tiep Arduino qua Serial.
- `free-react-tailwind-admin-dashboard-main/`: Dashboard quan tri/nhan vien bang React + Vite + Tailwind CSS. Dung de quan ly chi nhanh, cau truc bai do, camera, xe vao/ra, ve thang, dashboard realtime.
- `Appuser/`: Ung dung Flutter cho khach hang. Ho tro dang ky/dang nhap, quan ly ho so, xe ca nhan, lich su gui xe, vi, thanh toan va ve thang.
- `License-Plate-Recognition-main/`: Dich vu Python nhan dien bien so xe bang YOLOv5/OpenCV/PyTorch. Co camera cong vao/ra va camera dinh vi xe trong khu.
- `Arduino/`: Ma Arduino dieu khien servo cong vao/ra, cam bien IR, buzzer, LED va LCD theo lenh Serial tu backend.

## Tinh nang chinh

- Dang ky, dang nhap, phan quyen nguoi dung bang JWT.
- Quan ly chi nhanh, suc chua, so do bai do va vi tri xe.
- Check-in/check-out xe bang bien so.
- Nhan dien bien so tu IP Webcam/camera.
- Cap nhat realtime qua SignalR.
- Quan ly ve thang va gia ve thang.
- Vi dien tu, lich su giao dich, tich hop thanh toan MoMo test.
- Gui lenh mo cong/bao loi/thanh toan den Arduino qua cong COM.

## Yeu cau moi truong

Can cai dat truoc:

- .NET SDK 8.0
- SQL Server hoac SQL Server Express
- Redis server
- Node.js 18+ va npm
- Flutter SDK 3.3+
- Python 3.9+ khuyen nghi dung virtual environment
- Arduino IDE neu nap code cho mach that/Proteus
- IP Webcam hoac camera co stream OpenCV doc duoc

## Cau truc thu muc

```text
IOT/
|-- SmartParkingSystem/                         # ASP.NET Core backend
|-- free-react-tailwind-admin-dashboard-main/   # React admin/staff dashboard
|-- Appuser/                                    # Flutter customer app
|-- License-Plate-Recognition-main/             # Python AI license plate service
|-- Arduino/                                    # Arduino/Proteus source
`-- README.md
```

## Cai dat Backend

1. Di vao thu muc backend:

```powershell
cd SmartParkingSystem
```

2. Kiem tra chuoi ket noi trong `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=MINHKHOI\\SQLEXPRESS;Database=SmartParking_IOT;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

Neu may ban dung instance SQL Server khac, hay sua `Server=...` cho phu hop.

3. Kiem tra Redis trong `appsettings.json`:

```json
"Redis": {
  "Host": "localhost",
  "Port": 6379,
  "Database": 0
}
```

4. Tao/cap nhat database:

```powershell
dotnet restore
dotnet ef database update
```

Neu chua co `dotnet ef`, cai bang:

```powershell
dotnet tool install --global dotnet-ef
```

5. Chay backend:

```powershell
dotnet run --urls "http://localhost:5000"
```

Swagger se co tai:

```text
http://localhost:5000/swagger
```

## Cai dat Dashboard React

1. Di vao thu muc frontend:

```powershell
cd free-react-tailwind-admin-dashboard-main
```

2. Cai dependency:

```powershell
npm install
```

3. Tao file `.env` neu can doi API backend:

```env
VITE_API_URL=http://localhost:5000/api
```

Neu khong tao `.env`, frontend mac dinh dung `http://localhost:5000/api`.

4. Chay dashboard:

```powershell
npm run dev
```

Vite se hien URL tren terminal, thuong la:

```text
http://localhost:5173
```

## Cai dat App Flutter

1. Di vao thu muc app:

```powershell
cd Appuser
```

2. Cai dependency:

```powershell
flutter pub get
```

3. Chay app tren Chrome:

```powershell
flutter run -d chrome --dart-define=API_BASE_URL=http://localhost:5000/api
```

Neu chay tren Android emulator, `localhost` la emulator, khong phai may host. Khi do thuong dung:

```powershell
flutter run -d emulator --dart-define=API_BASE_URL=http://10.0.2.2:5000/api
```

Neu chay tren dien thoai that, thay bang IP LAN cua may dang chay backend:

```powershell
flutter run --dart-define=API_BASE_URL=http://<IP_MAY_TINH>:5000/api
```

## Cai dat dich vu nhan dien bien so Python

1. Di vao thu muc AI:

```powershell
cd License-Plate-Recognition-main
```

2. Tao virtual environment:

```powershell
python -m venv .venv
.\.venv\Scripts\Activate.ps1
```

3. Cai dependency co ban:

```powershell
pip install -r yolov5\requirements.txt
pip install opencv-python torch torchvision requests python-socketio
```

Neu dung GPU, cai PyTorch theo cau hinh CUDA phu hop voi may.

4. Tao file `.env` tu file mau:

```powershell
Copy-Item .env.example .env
```

Mac dinh `.env.example` tro ve backend:

```env
BACKEND_API_URL=http://localhost:5001/api/parking
SIMULATE_API=False
DEBUG_MODE=False
```

Neu backend dang chay o `http://localhost:5000`, hay sua lai:

```env
BACKEND_API_URL=http://localhost:5000/api/parking
```

5. Chay camera cong vao/ra:

```powershell
python webcam_smart_lowlatency.py --ip 192.168.1.20 --port 8080 --station entrance --api-server --api-port 5001
```

Camera cong ra:

```powershell
python webcam_smart_lowlatency.py --ip 192.168.1.20 --port 8080 --station exit --api-server --api-port 5002
```

Chay khong hien cua so OpenCV:

```powershell
python webcam_smart_lowlatency.py --ip 192.168.1.20 --port 8080 --station entrance --api-server --api-port 5001 --headless
```

6. Chay camera dinh vi xe trong khu:

```powershell
python webcam_zone_locator.py --ip 192.168.1.21 --port 8080 --camera-id A_COL_1 --parking-lot A --zone A --column 1 --location-name "Bai A - Cot 1" --api-server --api-port 5101
```

Dashboard co the ket noi toi cac endpoint stream/detection cua Python qua host va port tu man hinh camera.

## Cai dat Arduino

Thu muc `Arduino/` gom:

- `BTL_update.ino`: source Arduino.
- `BTL_update.pdsprj`: file Proteus.

Phan cung/code dang dung:

- LCD 16x2 voi chan `A0` den `A5`.
- Servo cong vao chan `3`.
- Servo cong ra chan `9`.
- IR cong vao chan `2`.
- IR cong ra chan `4`.
- Buzzer chan `5`.
- LED xanh chan `6`.
- LED do chan `7`.
- Serial baud rate `9600`.

Backend gui lenh qua Serial:

- `IN_OK:<bien_so>`: mo cong vao.
- `OUT_OK:<bien_so>`: mo cong ra.
- `PAY:<bien_so>:<so_tien>`: hien thi yeu cau thanh toan.
- `FAIL`: bao loi nhan dien.

Trong `SmartParkingSystem/appsettings.json`, sua cau hinh cong COM cho dung voi may:

```json
"ArduinoSerial": {
  "Enabled": true,
  "PortName": "COM1",
  "BaudRate": 9600
}
```

Neu chua dung Arduino, co the tat:

```json
"ArduinoSerial": {
  "Enabled": false
}
```

## Thu tu chay de test toan he thong

1. Khoi dong SQL Server.
2. Khoi dong Redis o `localhost:6379`.
3. Chay backend:

```powershell
cd SmartParkingSystem
dotnet run --urls "http://localhost:5000"
```

4. Chay dashboard:

```powershell
cd free-react-tailwind-admin-dashboard-main
npm run dev
```

5. Chay Python camera service neu can test camera/check-in/check-out:

```powershell
cd License-Plate-Recognition-main
.\.venv\Scripts\Activate.ps1
python webcam_smart_lowlatency.py --ip <IP_CAMERA> --port 8080 --station entrance --api-server --api-port 5001
```

6. Chay app Flutter neu can test phia khach hang:

```powershell
cd Appuser
flutter run -d chrome --dart-define=API_BASE_URL=http://localhost:5000/api
```

## Cau hinh quan trong

- Backend API mac dinh: `http://localhost:5000/api`
- Swagger: `http://localhost:5000/swagger`
- SignalR parking hub: `http://localhost:5000/parkingHub`
- SignalR notification hub: `http://localhost:5000/notificationHub`
- React env: `VITE_API_URL`
- Flutter env: `API_BASE_URL`
- Python camera cong mac dinh: `--api-port 5001`
- Python zone camera mac dinh: `--api-port 5101`
- Redis mac dinh: `localhost:6379`

## Build production

Backend:

```powershell
cd SmartParkingSystem
dotnet publish -c Release
```

Dashboard:

```powershell
cd free-react-tailwind-admin-dashboard-main
npm run build
```

Flutter web:

```powershell
cd Appuser
flutter build web --dart-define=API_BASE_URL=http://<DOMAIN_OR_IP>/api
```

## Ghi chu bao mat

- Khong nen commit secret that len git. Cac gia tri JWT secret, chuoi ket noi database, MoMo secret, ngrok URL nen dua sang user-secrets, bien moi truong hoac file cau hinh rieng khi deploy.
- `appsettings.json` hien tai phu hop moi truong demo/local, can doi lai truoc khi dua len production.
- Khi app Flutter chay tren thiet bi that, backend phai lang nghe tren IP LAN va firewall phai cho phep truy cap cong `5000`.

## Xu ly loi thuong gap

- Frontend/Flutter khong goi duoc API: kiem tra backend co chay o `http://localhost:5000` khong va bien `VITE_API_URL`/`API_BASE_URL` co dung khong.
- Loi database: kiem tra SQL Server instance, chuoi ket noi va da chay `dotnet ef database update`.
- Loi Redis: dam bao Redis dang chay o `localhost:6379`.
- Python khong doc duoc camera: kiem tra IP Webcam URL, port `8080`, cung mang LAN va firewall.
- Arduino khong phan hoi: kiem tra `PortName`, baud rate `9600`, day USB/COM ao trong Proteus.
