const int buttonPin = 7;
const int ledPin = 13;
const int potPin = A0; // 可變電阻接在 A0

int lastButtonState = HIGH;

void setup() {
  pinMode(buttonPin, INPUT_PULLUP);
  pinMode(ledPin, OUTPUT);

  Serial.begin(9600);
}

void loop() {
  // --- 1. 讀取按鈕狀態 ---
  int buttonState = digitalRead(buttonPin);
  String eventMessage = "NONE"; // 預設沒有事件

  if (buttonState != lastButtonState) {
    lastButtonState = buttonState;

    if (buttonState == LOW) {
      eventMessage = "JUMP_DOWN";
      digitalWrite(ledPin, HIGH); // 順便亮燈
    } else {
      eventMessage = "JUMP_UP";
      digitalWrite(ledPin, LOW);  // 熄燈
    }
  }

  // --- 2. 讀取可變電阻值 ---
  int potValue = analogRead(potPin);

  // --- 3. 打包資料並傳送給 Unity ---
  // 輸出格式會像這樣："NONE,512" 或 "JUMP_DOWN,1023"
  Serial.print(eventMessage);
  Serial.print(",");
  Serial.println(potValue);

  delay(20); // 稍微加長到 20ms，讓通訊更穩定
}