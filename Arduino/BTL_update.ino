#include <LiquidCrystal.h>
#include <Servo.h>

// Giữ nguyên sơ đồ chân bạn đã cung cấp
LiquidCrystal lcd(A0, A1, A2, A3, A4, A5);
Servo servoVao;
Servo servoRa;

const int irVao = 2;
const int irRa  = 4;
const int pinServoVao = 3;
const int pinServoRa  = 9; 
const int buzzer = 5;
const int ledXanh = 6;
const int ledDo = 7;

void setup() {
  Serial.begin(9600);
  
  // 1. GIẢM TIMEOUT: Ép Arduino xử lý chuỗi ngay lập tức (mặc định là 1000ms)
  Serial.setTimeout(10); 

  pinMode(irVao, INPUT_PULLUP);
  pinMode(irRa,  INPUT_PULLUP);
  pinMode(buzzer, OUTPUT);
  pinMode(ledXanh, OUTPUT);
  pinMode(ledDo, OUTPUT);

  servoVao.attach(pinServoVao);
  servoRa.attach(pinServoRa);
  servoVao.write(0);
  servoRa.write(0);

  lcd.begin(16, 2);
  lcd.print("SYSTEM READY");
  // Loại bỏ delay(1000) lớn để sẵn sàng nhận Serial ngay khi khởi động
}

void loop() {
  // 2. PHẢN HỒI TỨC THÌ: Kiểm tra dữ liệu Serial liên tục
  if (Serial.available() > 0) {
    // Đọc cho đến khi gặp ký tự xuống dòng (\n)
    String command = Serial.readStringUntil('\n');
    command.trim();

    if (command.length() > 0) {
      if (command.startsWith("IN_OK:")) {
        controlGate(servoVao, irVao, "WELCOME IN", command.substring(6));
      } 
      else if (command.startsWith("OUT_OK:")) {
        controlGate(servoRa, irRa, "GOODBYE", command.substring(7));
      }
      else if (command.startsWith("PAY:")) {
        showPayment(command.substring(4));
      }
      else if (command == "FAIL") {
        errorAlert();
      }
    }
  }
}

void controlGate(Servo &s, int irPin, String title, String msg) {
  lcd.clear();
  lcd.print(title);
  lcd.setCursor(0,1); 
  lcd.print(msg);
  
  digitalWrite(ledXanh, HIGH);
  digitalWrite(buzzer, HIGH); 
  delay(100); 
  digitalWrite(buzzer, LOW);

  // Mở cổng
  for(int p=0; p<=90; p+=5){ s.write(p); delay(10); }

  // LOGIC AN TOÀN TRÊN PROTEUS:
  // (1) Đợi nhấn IR xuống 0 (Xe bắt đầu che)
  while(digitalRead(irPin) == HIGH); 
  // (2) Đợi nhấn IR lên 1 (Xe đã đi qua hẳn)
  while(digitalRead(irPin) == LOW);
  
  delay(300); // Giảm độ trễ đóng cổng

  // Đóng cổng
  for(int p=90; p>=0; p-=5){ s.write(p); delay(10); }
  digitalWrite(ledXanh, LOW);
  
  // Trả về màn hình sẵn sàng ngay sau khi đóng cổng
  lcd.clear();
  lcd.print("READY FOR SCAN...");
}

void showPayment(String data) {
  int split = data.indexOf(':');
  lcd.clear();
  if (split != -1) {
    lcd.print("BS: " + data.substring(0, split));
    lcd.setCursor(0,1); 
    lcd.print("FEE: " + data.substring(split+1));
  } else {
    lcd.print("PAYMENT REQ");
    lcd.setCursor(0,1);
    lcd.print(data);
  }
  digitalWrite(ledDo, HIGH);
  // Không dùng delay ở đây để có thể nhận lệnh OUT_OK ngay lập tức
}

void errorAlert() {
  lcd.clear(); 
  lcd.print("LPR ERROR!");
  digitalWrite(ledDo, HIGH);
  for(int i=0; i<2; i++){ 
    digitalWrite(buzzer, HIGH); delay(100); 
    digitalWrite(buzzer, LOW); delay(100); 
  }
  digitalWrite(ledDo, LOW);
  lcd.setCursor(0,1);
  lcd.print("TRY AGAIN...");
}