using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityBluetooth
{
    public class BluetoothCommunicator : MonoBehaviour
    {


        void Start()
        {
            // Get a list of serial port names.
            BluetoothManager.ConfigureBluetoothCommunication();
            StartCoroutine(OpenConnection());
            //string resultOpenConnection = BluetoothManager.OpenConnection();
            //Debug.Log(resultOpenConnection);
        }

        void Update()
        {
            //BluetoothManager.ReceiveData();
            //if (Input.GetKeyDown(KeyCode.Space)) {
            //    BluetoothManager.SendData();
            //}

        }


        IEnumerator OpenConnection()
        {
            if (BluetoothManager.serialPort != null)
            {
                if (BluetoothManager.serialPort.IsOpen)
                {
                    BluetoothManager.serialPort.Close();
                    Debug.Log("Closing port since was already open");
                }
                else
                {
                    BluetoothManager.serialPort.Open(); // opens connection // sets the timeout value before reporting
                    yield return new WaitForSeconds(5);
                    //BluetoothManager.serialPort.ReadTimeout = 1000; // sets the timeout value before reporting
                    Debug.Log("Port opened");
                }
            }
            else
            {
                if (BluetoothManager.serialPort.IsOpen)
                {
                    Debug.Log("port is already open");
                }
                else
                {
                    Debug.Log("port == null");
                }
            }
            //Debug.Log(resultOpenConnection);

        }


        /*
         * Close connection
         */
        public void OnDestroy()
        {
            Debug.Log("On destroy called");
            BluetoothManager.serialPort.Close();
        }
    }
}
