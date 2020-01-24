using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;

namespace UnityBluetooth
{
    public class SensorJSON : MonoBehaviour
    {
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
        public String retString;

        float cachedMoister = -1.0f;
        int frameCounter = 0;
        int framesToWait = 60;

        async void Start()
        {
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
            if (float.Parse(hSensors.moisture) < 0.5f)
            {
                rController.makeItRain = false;
            }
            Debug.Log("here");
            //await updateSensors();
            Task.Run( async() =>
            {
                while (true)
                {
                    Debug.Log("here");
                    retString = BluetoothManager.ReceiveData();
                    BluetoothManager.SendData(outPut.light);
                    //await Task.Delay(200);
                }
            });
        }
        async Task updateSensors()
        {
            while (true)
            {
                Debug.Log("here");
                hSensors = JsonUtility.FromJson<SensorObject>(BluetoothManager.ReceiveData());
            }
        }

        void Update()
        {
            //outPut.light = outp.hit;

            hSensors = JsonUtility.FromJson<SensorObject>(retString);
            if (hSensors != null)
            {
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
public class SensorObject
{
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