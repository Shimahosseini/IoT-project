 

#include <WiFi.h>
#include <MQTT.h>
#include <string.h>
//For lcd SDA is pin=D21 SCL =D22
#include <LiquidCrystal_I2C.h>
//vcc=5 volt and SDA=D21, SCL=D22
// set the LCD number of columns and rows
int lcdColumns = 16;
int lcdRows = 2;

// set LCD address, number of columns and rows
// if you don't know your display address, run an I2C scanner sketch
LiquidCrystal_I2C lcd(0x27, lcdColumns, lcdRows);

const int LedGreen = 18; 
const int LedYellow = 4; 
//const int LedSpindle = 4; 
const int pinButtonRed =2;
const int pinButtonGreen =17; 

const int pinMotor1 = 25;
const int pinMotor2 = 12;
const int pinMotor3 = 32;
const int pinSpindle = 13;


int st=0;
int cnt=0;
unsigned long timer1;
unsigned long timer2;
unsigned long timer3;

int physicalbuttonRed;
int physicalbuttonGreen;

const int potPin = 39; 
const char ssid[] = "";  //Internet name
const char pass[] = "";  //Internet password
const char brokerAddress[] = ""; //Message Broker IP address


const char topicSub[] = "";  //TOpic to subscribe
const char topicPub[] = "";  //Topic to publish

String oldDataLDR;
String olddatatotal;
int unitybuttonRed;
int unitybuttonGreen;
String delimiter = "-";
String load;


WiFiClient net;
MQTTClient client;
IPAddress ip;


void connect() {
  Serial.print("Checking wifi networks");
  while (WiFi.status() != WL_CONNECTED) {
    Serial.print(".");
    delay(250);
  }
  Serial.println();
  ip = WiFi.localIP();
  Serial.print("Arduino IP address: ");
  Serial.println(ip);
  Serial.print("Connecting to Broker");
  while (!client.connect("tutorial", "default", "default")) {
    Serial.print(".");
    delay(100);
  }
  Serial.println("");
  Serial.println("Connected to broker");
  delay(500);
  client.subscribe(topicSub);
  delay(100);
}

void messageReceived(String &topicSub, String &payload) {
  //Serial.println(payload + " value comming from topic " + topicSub);

   //unity = atoi(payload.c_str());
   //Serial.println(unity);
   String load=payload;
   Serial.println(load);
   int n=load.length();
   // declaring character array
   char unitybuttonRedGreen[n+1];
  //copying the contents of the string to char array
   strcpy(unitybuttonRedGreen, load.c_str());
   //orrrrr char unitybuttonRedGreen[2] ={payload};
   char* token=strtok(unitybuttonRedGreen, "-");
   unitybuttonRed = atoi(token);
   Serial.println(unitybuttonRed);
   token=strtok(0,"-");
   unitybuttonGreen=atoi(token);
   Serial.println(unitybuttonGreen);
   
}

void setup() {
  Serial.begin(115200);
  WiFi.begin(ssid, pass);
  delay(500);
  client.begin(brokerAddress, net);
  client.onMessage(messageReceived);
  connect();
  pinMode(LedYellow, OUTPUT);
  pinMode(LedGreen, OUTPUT);
  //pinMode(LedSpindle, OUTPUT); 
  pinMode(pinButtonRed, INPUT);
  pinMode(pinButtonGreen, INPUT);
  pinMode(potPin, OUTPUT);
  Serial.println("Welcome");
  pinMode(pinMotor1, OUTPUT);
  pinMode(pinMotor2, OUTPUT);
  pinMode(pinMotor3, OUTPUT);
  pinMode(pinSpindle, OUTPUT);

   // initialize LCD
  lcd.init();
  // turn on LCD backlight                      
  lcd.backlight();
}

void loop()
{
   client.loop();
   if (!client.connected()) {
     connect();
   }
    
    int physicalbuttonRed1 = digitalRead(pinButtonRed);
    if (physicalbuttonRed1==LOW)
    {
      physicalbuttonRed=HIGH;
    }
    else if(physicalbuttonRed1==HIGH)
    {
      physicalbuttonRed=LOW;
    }
    int physicalbuttonGreen1= digitalRead(pinButtonGreen);
    if (physicalbuttonGreen1==LOW)
    {
      physicalbuttonGreen=HIGH;
    }
    else if(physicalbuttonGreen1==HIGH)
    {
      physicalbuttonGreen=LOW;
    }
  
    //int conditionStart=(st==0) && (((physicalbuttonRed == HIGH)  && (physicalbuttonGreen ==LOW )|| (unitybuttonRed == 1 && unitybuttonGreen == 0)) ) ;
    int conditionStart=(st==0) && (((physicalbuttonRed == LOW)  && (physicalbuttonGreen ==HIGH )|| (unitybuttonRed == 0 && unitybuttonGreen == 1)) ) ;
    int conditionStop= ((physicalbuttonRed == HIGH)  && (physicalbuttonGreen == LOW))|| ((unitybuttonGreen == 0) && (unitybuttonRed == 1)) ;
    if( (conditionStart ) || ((st==4)&&(cnt<5)))
    {
      st=1;
      timer1 = millis();
      //Serial.println(millis()-timer1);
    }
     else if((st==1) && ((millis()-timer1)>5000))
    {
      st=2;
      timer2 = millis();
    }
     else if((st==2) && (millis()-timer2)>5000 )
    {
      st=3;
      timer3 = millis();
    }
    else if((st==3)  && (millis()-timer3)>5000)
    {
      st=4;
    }
     else if(((st==4) && (cnt>5))||(conditionStop))
    {
      st=0;
    }
    
    //LedGreen is always on to show the machine is ready to work
    if(st==0)
    {
      digitalWrite(pinMotor1, LOW);
      digitalWrite(pinMotor2, LOW);
      digitalWrite(pinMotor3, LOW);
      digitalWrite(pinSpindle, LOW);
      digitalWrite(LedYellow, LOW);
      //digitalWrite(LedSpindle, LOW);
      digitalWrite(LedGreen, HIGH);
  
      Serial.println(" state is 0");
  //    Serial.println(physicalbuttonRed);
  //    Serial.println(physicalbuttonGreen);
  
      cnt=0;
      lcd.clear();
      lcd.setCursor(8, 0);
      // print message
      lcd.print(cnt);
    }
    else if(st==1)
    {
      digitalWrite(pinMotor1, HIGH);
      digitalWrite(pinMotor2,LOW );
      digitalWrite(pinMotor3, LOW);
      digitalWrite(LedYellow, HIGH);
      digitalWrite(LedGreen, HIGH);
      digitalWrite(pinSpindle, HIGH);
      //digitalWrite(LedSpindle, HIGH);
  
      Serial.println(" state is 1");
      
    }
    else if(st==2)
    {
      digitalWrite(pinMotor1, LOW);
      digitalWrite(pinMotor2,HIGH );
      digitalWrite(pinMotor3, LOW);
      digitalWrite(LedYellow, HIGH);
      digitalWrite(LedGreen, HIGH);
      digitalWrite(pinSpindle, HIGH);
      //digitalWrite(LedSpindle, HIGH);
  
      Serial.println(" state is 2");
      
    }
    else if(st==3)
    {
      digitalWrite(pinMotor1, LOW);
      digitalWrite(pinMotor2,LOW );
      digitalWrite(pinMotor3, HIGH);
      digitalWrite(LedYellow, HIGH);
      digitalWrite(LedGreen, HIGH);
      digitalWrite(pinSpindle, HIGH);
      //digitalWrite(LedSpindle, HIGH);
  
  
      Serial.println(" state is 3");
      
    }
    else if(st==4)
    {
      digitalWrite(pinMotor1, LOW);
      digitalWrite(pinMotor2,LOW );
      digitalWrite(pinMotor3, LOW);
      digitalWrite(LedYellow, HIGH);
      digitalWrite(LedGreen, HIGH);
      //digitalWrite(LedSpindle, LOW);
      digitalWrite(pinSpindle, LOW);
      Serial.println(" state is 4");
      
      cnt++;
      lcd.clear();
      lcd.setCursor(8, 0);
      // print message
      lcd.print(cnt);
    }

    
    int valueYellowLed = digitalRead(LedYellow);
    String dataYellowLed = String(valueYellowLed);

    int valueGreenLed = digitalRead(LedGreen);
    String dataGreenLed = String(valueGreenLed);

//    int valueSpindleLed = digitalRead(LedSpindle);
//    String dataSpindleLed = String(valueSpindleLed);

   int valueSpindle = digitalRead(pinSpindle);
   String dataSpindle = String(valueSpindle);

    int valueMotor1 = digitalRead(pinMotor1);
    String dataMotor1 = String(valueMotor1);

    int valueMotor2 = digitalRead(pinMotor2);
    String dataMotor2 = String(valueMotor2);

    int valueMotor3 = digitalRead(pinMotor3);
    String dataMotor3 = String(valueMotor3);

   
    String dataButtonRed = String(physicalbuttonRed);
    String dataButtonGreen = String(physicalbuttonGreen);
    
    String datatotal =dataGreenLed + "-" +dataButtonRed + "-" + dataYellowLed + "-" + cnt+ "-" + dataButtonGreen+ "-"+ dataSpindle+ "-"+ 
    dataMotor1+ "-"+ dataMotor2 + "-" +dataMotor3;
   
    if (datatotal != olddatatotal) {
      client.publish(topicPub, datatotal);
      olddatatotal = datatotal;
      delay(250);
  }     
}  
   
