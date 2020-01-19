using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityBluetooth {
    public class SensorJSON : MonoBehaviour {
        public int sensorQuantity;
        [SerializeField]
        public SensorObject hSensors;
        public BluetoothCommunicator outCom;
        protected string jsonStringTest;
        public Dome plantDome;
        public RainController rController;
        public Particle mopart;
        public treeHit outp;
        [SerializeField]
        public OutputObject outPut;
        void Start() {
            hSensors = new SensorObject
            {
                humidity = "0",
                moisture = "0",
                temperature = "0",
                light = "0"
            };
            outPut = new OutputObject
            {
                light = "0"
            };
            jsonStringTest = JsonUtility.ToJson(hSensors);
            Debug.Log(jsonStringTest);
            if (float.Parse(hSensors.moisture) < 0.5f) {
                rController.makeItRain = false;
            }
        }


        void Update()
        {
            //outPut.light = outp.hit;
            BluetoothManager.SendData(outPut.light);
            hSensors = JsonUtility.FromJson<SensorObject>(BluetoothManager.ReceiveData());
            if (hSensors != null)
            {
                Debug.Log(jsonStringTest);
                if (float.Parse(hSensors.moisture) > 500)
                {
                    rController.makeItRain = true;
                }
                else
                {
                    rController.makeItRain = false;
                }
                if (float.Parse(hSensors.light) > 500)
                {
                    plantDome.daylight = true;
                    plantDome.light = float.Parse(hSensors.light);
                }
                else
                {
                    plantDome.daylight = false;
                    plantDome.light = float.Parse(hSensors.light);
                }
                if (float.Parse(hSensors.temperature) > 28)
                {
                    mopart.temp = 2;

                }
                else if (float.Parse(hSensors.temperature) < 24)
                {
                    mopart.temp = 1;
                }
                else
                {
                    mopart.temp = 0;

                }
            }
        }
    }
}


[Serializable]
public class SensorObject {
    public string humidity;
    public string moisture;
    public string temperature;
    public string light;
}
[Serializable]
public class OutputObject
{
    public string light;
}


