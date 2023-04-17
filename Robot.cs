using System;
using System.IO.Ports;

namespace LaneFollower
{
    public class Robot
    {
        //set bytes to be received by microchip
        public const byte CENTER = (byte)'C';
        public const byte LEFT = (byte)'l';
        public const byte FARLEFT = (byte)'L';
        public const byte RIGHT = (byte)'r';
        public const byte FARRIGHT = (byte)'R';
        public const byte STOP = (byte)'X';

        //declare new serial port
        SerialPort serialPort;

        public bool Online { get; private set; }
        public Robot(String port)
        {
            SetupSerialComms(port);
        }

        public void SetupSerialComms(String port)
        {
            try
            {
                serialPort = new SerialPort(port);
                serialPort.BaudRate = 9600;
                serialPort.DataBits = 8;
                serialPort.Parity = Parity.None;
                serialPort.StopBits = StopBits.Two;
                serialPort.Open();
                Online = true;
                Console.WriteLine("There's communication!");
            }
            catch
            {
                Online = false;
            }
        }

        public void Move(byte section)
        {
            try
            {
                if (Online)
                {
                    byte[] buffer = { section };
                    serialPort.Write(buffer, 0, 1);
                    Console.WriteLine(buffer); //print buffer to verify it worked
                }
            }
            catch
            {
                Online = false;
            }
        }

        public void Close()
        {
            serialPort.Close();
        }

    }
}