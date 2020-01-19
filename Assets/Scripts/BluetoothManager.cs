using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Management;
using UnityEngine;

namespace UnityBluetooth
{
    public static class BluetoothManager
    {
        public static int BaudRate;
        public static string CommPort;
        private static string inBuff = "";
        public static SerialPort serialPort;


        public static void ConfigureBluetoothCommunication(int baudrate = 9600, string commport = "COM12")
        {
            if (baudrate == 0)
                baudrate = 115200;

            serialPort = new SerialPort
            {
                BaudRate = baudrate,
                PortName = commport,
                Handshake = Handshake.None,
                ReadTimeout = 1000000
            };
        }

        /*
         * Instantiate array to transmit of proper size, 
         * perform bitwise copy (i.e. get 4 bytes out of one int)
         * so that data is immediate in memory. Sent data over BT serial.
         */
        public static void SendData(string json)
        {
            try
            {
                //char[] result = { 'a' };
                serialPort.Write(json);
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
            }
        }

        /*
         * Read response data, every 2 bytes distinguishes values.
         * Bytes are converted to 16 bit integers.
         */
        public static string ReceiveData()
        {
            byte[] data = new byte[1024];
            //string test2 = serialPort.ReadTo("i");
            //int bytesRead = serialPort.Read(data, 0, data.Length);
            char latChar;
            String inBuff = "";//@"{""humidity"":""10"",""moisture"":""2"",""temperature"":"".2""}";
            if (serialPort.BytesToRead != 0)
            {
                inBuff = serialPort.ReadTo("}");
                inBuff += "}";
                //Debug.Log(income);
                //while (latChar != '}') {
                //   inBuff += latChar;
                //}
                //string test = serialPort.ReadTo("i");
                //Debug.Log(test);
            }
            return inBuff;
            //Debug.Log("HERE");
        }


        /*
         * Check if port is opened for serial comm,
         * if not open. If errors arise, debug them as found.
         */
        public static string OpenConnection()
        {
            string errorMessage = "";
            if (serialPort != null)
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                    errorMessage = "Closing port since was already open";
                }
                else
                {
                    serialPort.ReadTimeout = 500; // sets the timeout value before reporting
                    serialPort.WriteTimeout = 500;
                    serialPort.Open(); // opens connection // sets the timeout value before reporting
                    errorMessage = "Port opened";
                }
            }
            else
            {
                if (serialPort.IsOpen)
                {
                    errorMessage = "port is already open";
                }
                else
                {
                    errorMessage = "port == null";
                }
            }
            return errorMessage;
        }

    }

}
