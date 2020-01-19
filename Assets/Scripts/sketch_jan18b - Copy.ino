#include <DHT.h>
#include <DHT_U.h>

/* Grove - Light Sensor demo v1.0
* 
* Signal wire to A0.
* By: http://www.seeedstudio.com
*/
#include <math.h>

#define LIGHT_SENSOR A5//Grove - Light Sensor is connected to A0 of Arduino
          //Connect the LED Grove module to Pin12, Digital 12
        //The treshold for which the LED should turn on. Setting it lower will make it go on at more light, higher for more darkness
float Rsensor; //Resistance of sensor in K
#define DHTPIN 4
#define DHTTYPE DHT22
DHT dht(DHTPIN,DHTTYPE);
void setup() 
{
    Serial.begin(9600); 
    pinMode(10, OUTPUT);    
    dht.begin();//Start the Serial connection
           //Set the LED on Digital 12 as an OUTPUT
}
void loop() 
{
  String li = "{ \"light\": ";
  String mo = "\"moisture\": ";
  String te = "\"temperature\": ";
  String hu = "\"humidity\": ";
  float light = analogRead(0);
  float most = analogRead(1);
  float h = dht.readHumidity();
  float t = dht.readTemperature();
  float f = dht.readTemperature(true);
  String main = li + light + "," + mo + most + "," + hu + h + "," + te + t + "}";
   
  Serial.println(main);
  if (Serial.available()){
    char in = Serial.read();
    if (in == '1'){
      digitalWrite(10, HIGH);
    }
    else {
      digitalWrite(10, LOW);
    }
    //digitalWrite(10, HIGH);
    //String test = Serial.readStringUntil('g');
  }
  
}
