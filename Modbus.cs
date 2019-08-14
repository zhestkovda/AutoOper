using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Text;
using System.IO;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.IO.Ports;
using System.Net.NetworkInformation;

namespace Modbus_TCP_Server
{
    public class Slave
    {
        // ------------------------------------------------------------------------
        // Constants for access
        public const byte fctReadCoils = 1;///RCS
        public const byte fctReadDiscreteInputs = 2;
        public const byte fctReadHoldingRegister = 3;///RHR
        public const byte fctReadInputRegister = 4;
        public const byte fctWriteSingleCoil = 5;
        public const byte fctWriteSingleRegister = 6;
        public const byte fctWriteMultipleCoils = 15; ///FMC
        public const byte fctWriteMultipleRegister = 16;///PMR
        public const byte fctReadWriteMultipleRegister = 23;
        private const int  NumberMBRegisters = 100;
        /// <summary>Constant for exception illegal function.</summary>
        public const byte excIllegalFunction = 1;
        /// <summary>Constant for exception illegal data address.</summary>
        public const byte excIllegalDataAdr = 2;
        /// <summary>Constant for exception illegal data value.</summary>
        public const byte excIllegalDataVal = 3;
        /// <summary>Constant for exception slave device failure.</summary>
        public const byte excSlaveDeviceFailure = 4;
        /// <summary>Constant for exception acknowledge.</summary>
        public const byte excAck = 5;
        /// <summary>Constant for exception slave is busy/booting up.</summary>
        public const byte excSlaveIsBusy = 6;
        /// <summary>Constant for exception gate path unavailable.</summary>
        public const byte excGatePathUnavailable = 10;
        /// <summary>Constant for exception not connected.</summary>
        public const byte excExceptionNotConnected = 253;
        /// <summary>Constant for exception connection lost.</summary>
        public const byte excExceptionConnectionLost = 254;
        /// <summary>Constant for exception response timeout.</summary>
        public const byte excExceptionTimeout = 255;
        /// <summary>Constant for exception wrong offset.</summary>
        public const byte excExceptionOffset = 128;
        /// <summary>Constant for exception send failt.</summary>
        public const byte excSendFailt = 100;

        // ------------------------------------------------------------------------
        public static ushort _timeout = 500;
        public static ushort _refresh = 10;
        private static bool _connected = false;
        public bool AnswerRDY = false;
        public TcpListener Listener;
        public TcpClient MyTcpClient;
        public NetworkStream networkStream;
        public byte[] InBuffer = new byte[64];
        public byte[] OutBuffer;

        public ushort Slave_ID;

        public Socket tcpSocket;
        public string IP;
        public int PORT;
        private ushort StartAddress;
        private ushort NumberRegs;
        private ushort NumberBytes;
        public ModbusRegister[] ModbusRegisters;
        public bool[] ModbusCoils; 
        public struct ModbusRegister
        {
            public byte LoByte;
            public byte HiByte;
        };

        // ------------------------------------------------------------------------
        /// <summary>Response data event. This event is called when new data arrives</summary>
        public delegate void ResponseData(ushort id, byte function, byte[] data);
        /// <summary>Exception data event. This event is called when the data is incorrect</summary>
        public delegate void ExceptionData(ushort id, byte function, byte exception);


        public int GetNumberMBRegisters()
        {
            return NumberMBRegisters;
        }
        // ------------------------------------------------------------------------
        /// <summary>Shows if a connection is active.</summary>
        public bool connected
        {
            get { return _connected; }
        }

        // ------------------------------------------------------------------------

        public Slave()
        {
            ModbusCoils = new bool[NumberMBRegisters];
            ModbusRegisters = new ModbusRegister[NumberMBRegisters];
        }        // ------------------------------------------------------------------------

        /// <summary>Start connection to Master.</summary>
        /// <param name="ip">IP adress of modbus slave.</param>
        /// <param name="port">Port number of modbus slave. Usually port 502 is used.</param>
        ///  
        private bool CheckAvailableServerPort(int port) 
        {
        bool isAvailable = true;
        IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
        IPEndPoint[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();

        foreach (IPEndPoint endpoint in tcpConnInfoArray) 
        {
            if (endpoint.Port == port) 
            {
                isAvailable = false;
                break;
            }
        }
        return isAvailable;
        }
//--------------------------------------------------------------------------------
        public void connect()
        {
            //while (true)
            //{

                Thread.BeginCriticalRegion();
                MakeConnection();
                Thread.EndCriticalRegion();
         
            //}
        }
//--------------------------------------------------------------------------------
        void MakeConnection()
        {
            lock (this)
            {
                //IPAddress _ip;
                //IP = "192.168.12.11";
                //if (IPAddress.TryParse(IP, out _ip) == false)
                //{
                //    IPHostEntry hst = Dns.GetHostEntry(IP);
                //    IP = hst.AddressList[0].ToString();
                //}
                if (CheckAvailableServerPort(PORT))
                {
                    //_connected = true;
                    //Listener = new TcpListener(PORT);// (IPep,PORT); //Listener = new TcpListener(PORT,IPAddress.Parse(IP));
                    Listener = new TcpListener(PORT);
                    try
                    {
                        Listener.Start();
                        MyTcpClient = Listener.AcceptTcpClient();
                        networkStream = MyTcpClient.GetStream();
                        networkStream.ReadTimeout = 7000;
                    }
                    catch (Exception e1)
                    {
                        _connected = false;
                        //MessageBox.Show("Ошибка сокета! " + e1.ToString());
                    }
                    
                    Thread.Sleep(30);
                    if ((MyTcpClient!=null) && (MyTcpClient.Connected))//(tcpSocket.Connected)
                    {
                        int readBytes = 0;
                        _connected = true;
                        while (_connected)
                        {
                            for (int j = 0; j < InBuffer.Length; j++)
                                InBuffer[j] = 0;
                            try
                            {
                                readBytes = networkStream.Read(InBuffer, 0, InBuffer.Length);//tcpSocket.Receive(InBuffer, 0, InBuffer.Length, SocketFlags.None);
                            }
                            catch
                            {
                                Thread.Sleep(30);
                                //CallBackMy1.callbackEventHandler1();
                                _connected = false;
                                //throw ex;  // any serious error occurr

                            }
                            try
                            {
                                if (readBytes > 0)
                                {
                                    ushort id_Request = InBuffer[6];
                                    Int16 function = Convert.ToInt16(InBuffer[7]);
                                    // ------------------------------------------------------------
                                    // Write response data
                                    if (id_Request == Slave_ID)
                                    {

                                        lock (this)
                                        {
                                            switch (function)
                                            {
                                                case fctReadHoldingRegister: //03
                                                    {
                                                        byte[] _adr = new byte[2];
                                                        _adr[0] = InBuffer[9];
                                                        _adr[1] = InBuffer[8];
                                                        StartAddress = BitConverter.ToUInt16(_adr, 0);
                                                        byte[] _nreg = new byte[2];
                                                        _nreg[0] = InBuffer[11];
                                                        _nreg[1] = InBuffer[10];
                                                        NumberRegs = BitConverter.ToUInt16(_nreg, 0);
                                                        OutBuffer = CreateAnswer_3(Slave_ID);
                                                        // tcpSocket.Send(OutBuffer, 0, OutBuffer.Length, SocketFlags.None);
                                                        break;
                                                    }
                                                case fctReadCoils: //01
                                                    {
                                                        byte[] _adr = new byte[2];
                                                        _adr[0] = InBuffer[9];
                                                        _adr[1] = InBuffer[8];
                                                        StartAddress = BitConverter.ToUInt16(_adr, 0);
                                                        byte[] _nreg = new byte[2];
                                                        _nreg[0] = InBuffer[11];
                                                        _nreg[1] = InBuffer[10];
                                                        NumberRegs = BitConverter.ToUInt16(_nreg, 0);

                                                        OutBuffer = CreateAnswer_1(Slave_ID);
                                                        //tcpSocket.Send(OutBuffer, 0, OutBuffer.Length, SocketFlags.None);
                                                        break;
                                                    }
                                                case fctWriteMultipleRegister: //16
                                                    {
                                                        byte[] _adr = new byte[2];
                                                        _adr[0] = InBuffer[9];
                                                        _adr[1] = InBuffer[8];
                                                        StartAddress = BitConverter.ToUInt16(_adr, 0);
                                                        byte[] _nreg = new byte[2];
                                                        _nreg[0] = InBuffer[11];
                                                        _nreg[1] = InBuffer[10];
                                                        NumberRegs = BitConverter.ToUInt16(_nreg, 0);
                                                        NumberBytes = Convert.ToUInt16(InBuffer[12]);
                                                        for (int i = 0; i < NumberRegs; i++)
                                                        {
                                                            ModbusRegisters[this.StartAddress + i].HiByte = InBuffer[13 + i * 2];
                                                            ModbusRegisters[this.StartAddress + i].LoByte = InBuffer[14 + i * 2];
                                                        }
                                                        OutBuffer = CreateAnswer_16(Slave_ID);
                                                        //tcpSocket.Send(OutBuffer, 0, OutBuffer.Length, SocketFlags.None);
                                                        break;
                                                    }
                                                case fctWriteMultipleCoils: //15
                                                    {
                                                        byte[] _adr = new byte[2];
                                                        _adr[0] = InBuffer[9];
                                                        _adr[1] = InBuffer[8];
                                                        StartAddress = BitConverter.ToUInt16(_adr, 0);
                                                        byte[] _nreg = new byte[2];
                                                        _nreg[0] = InBuffer[11];
                                                        _nreg[1] = InBuffer[10];
                                                        NumberRegs = BitConverter.ToUInt16(_nreg, 0); // количество записываемых бит
                                                        NumberBytes = Convert.ToUInt16(InBuffer[12]); // количество байт которое занимают эти  биты 

                                                        byte[] temp = new byte[NumberBytes];
                                                        for (int j = 0; j < this.NumberBytes; j++)
                                                        {
                                                            temp[this.NumberBytes - j - 1] = InBuffer[13 + j];
                                                        }


                                                        for (int i = 0; i < NumberRegs; i++)
                                                        {
                                                            bool temp1 = Convert.ToBoolean((temp[i / 8]) & ((byte)(1 << (i % 8))));
                                                            if (temp1)
                                                                ModbusCoils[i] = false;
                                                            else
                                                                ModbusCoils[i] = true;
                                                        }


                                                        OutBuffer = CreateAnswer_15(Slave_ID);
                                                        //tcpSocket.Send(OutBuffer, 0, OutBuffer.Length, SocketFlags.None);
                                                        break;
                                                    }

                                            } //switch
                                            networkStream.Write(OutBuffer, 0, OutBuffer.Length);
                                        }
                                    }   // if (id_Request == Slave_ID)
                                }       // if (readBytes > 0)
                                else
                                {
                                    //this.disconnect();
                                }
                            }           // try
                            catch
                            {
                                this.disconnect();
                            }
                        } //while(_connected)
                    }
                }
            }
        }
        // ------------------------------------------------------------------------
        /// <summary>Stop connection to slave.</summary>
        public void disconnect()
        {
            Dispose();
        }

        // ------------------------------------------------------------------------
        /// <summary>Destroy master instance.</summary>
        ~Slave()
        {
            Dispose();
        }

        // ------------------------------------------------------------------------
        /// <summary>Destroy master instance</summary>
        public void Dispose()
        {
            _connected = false;
            if (Listener != null)
            {
                Listener.Stop();
                Listener = null;
            }
            if (MyTcpClient != null)//(tcpSocket != null)
            {
                MyTcpClient = null;//tcpSocket = null;
            }
            if (networkStream != null)
                networkStream.Close();
            
        }
//-------------------------------------------------------------------------------------
        /*
        public void CallException(ushort id, byte function, byte exception)
        {
            if (tcpSocket == null) return;
            if (exception == excExceptionConnectionLost)
            {
                tcpSocket = null;
            }
            if (OnException != null) OnException(id, function, exception);
        }
         */
//-------------------------------------------------------------------------------------
            //Clear in/out buffers:
        public void DiscardOutBuffer()
        {
            //for(int i=0;i<64;i++)
            //    OutBuffer[i] = 0x00;
        }

        public void DiscardInBuffer()
        {
            //for (int i = 0; i < 64; i++)
            //    InBuffer[i] = 0x00;      
        }
//-------------------------------------------------------------------------------------
        // Create modbus header for write action
        public byte[] CreateAnswer_3(ushort id)
        {
            byte[] data = new byte[this.NumberRegs*2 + 9];
            data[0] = this.InBuffer[0];     //Transaction ID
            data[1] = this.InBuffer[1];     //Transaction ID

            data[2] = this.InBuffer[2];     //Protocol ID
            data[3] = this.InBuffer[3];     //Protocol ID

            data[4] = (byte)((3 + this.NumberRegs * 2) /255);     //N байт в ответе  !!!
            data[5] = (byte)((3 + this.NumberRegs * 2) %255);     //N байт в ответе  !!!

            data[6] = (byte)this.Slave_ID;				// Slave id high byte
            data[7] = 0x03;                             // RHR Function
            data[8] = (byte)(2*this.NumberRegs);        // количество байт в ответе
            for (int i = 0; i < this.NumberRegs; i++)
            {
                data[9 + i * 2] = (byte)this.ModbusRegisters[this.StartAddress + i].HiByte; //data[9 + i * 2] = (byte)this.ModbusRegisters[this.StartAddress + this.NumberRegs - i - 1].HiByte;
                data[10 + i * 2] = (byte)this.ModbusRegisters[this.StartAddress + i].LoByte; //data[10 + i * 2] = (byte)this.ModbusRegisters[this.StartAddress + this.NumberRegs - i - 1].LoByte;
            }
            return data;
        }
        //-------------------------------------------------------------------------------------
        // Create modbus header for write action
        public byte[] CreateAnswer_1(ushort id)
        {
            int n = (this.NumberRegs % 8 == 0) ? (this.NumberRegs / 8) : ((this.NumberRegs/8) +1);
            byte[] data = new byte[n + 9];
            byte[] temp = new byte[n];
            for (int j = this.StartAddress, i = 0; j < this.NumberRegs; j++, i++)
            {
                if (ModbusCoils[j])
                    temp[i / 8] = (byte)(1 << (i%8));
            }
            data[0] = this.InBuffer[0];     //Transaction ID
            data[1] = this.InBuffer[1];     //Transaction ID

            data[2] = this.InBuffer[2];     //Protocol ID
            data[3] = this.InBuffer[3];     //Protocol ID

            data[4] = (byte)((3 + n) / 255);       //N байт в ответе
            data[5] = (byte)((3 + n) % 255); ;     //N байт в ответе

            data[6] = (byte)this.Slave_ID;				// Slave id high byte
            data[7] = 0x01;                             // RCoil Function
            data[8] = (byte)(n);        // количество байт в ответе
            for (ushort i = 0; i < n; i++)
            {
                data[9 + i] = temp[n-i-1];
            }
            return data;
        }
//--------------------------------------------------------------------------------------------
        // Create modbus header for write action
        public byte[] CreateAnswer_16(ushort id)
        {
            byte[] data = new byte[12];
            data[0] = this.InBuffer[0];     //Transaction ID
            data[1] = this.InBuffer[1];     //Transaction ID

            data[2] = this.InBuffer[2];     //Protocol ID
            data[3] = this.InBuffer[3];     //Protocol ID

            data[4] = 0x00;     //N байт в ответе  !!!
            data[5] = 0x06;     //N байт в ответе  !!!

            data[6] = (byte)this.Slave_ID;				// Slave id high byte
            data[7] = 0x10;                             // 16 Function
            data[8] = InBuffer[8];
            data[9] = InBuffer[9];
            data[10] = InBuffer[10];
            data[11] = InBuffer[11];
            return data;
        }
//--------------------------------------------------------------------------------------------
        // Create modbus header for write action
        public byte[] CreateAnswer_15(ushort id)
        {
            byte[] data = new byte[12];
            data[0] = this.InBuffer[0];     //Transaction ID
            data[1] = this.InBuffer[1];     //Transaction ID

            data[2] = this.InBuffer[2];     //Protocol ID
            data[3] = this.InBuffer[3];     //Protocol ID

            data[4] = 0x00;     //N байт в ответе  !!!
            data[5] = 0x06;     //N байт в ответе  !!!

            data[6] = (byte)this.Slave_ID;				// Slave id high byte
            data[7] = 0x0f;                             // 15 Function
            data[8] = InBuffer[8];
            data[9] = InBuffer[9];
            data[10] = InBuffer[10];
            data[11] = InBuffer[11];
            return data;
        }
//--------------------------------------------------------------------------------------------
    }
}
