using System;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.XPath;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ZedGraph;

namespace Modbus_TCP_Server
{
    public partial class Form1 : Form
    {
        private Slave MySlave;
        private Thread ServThread;          // поток работы модбас-сервера
        private int TimeFromMoskow;         // разница во времени с Москвой
        private int MoskowCurrHour = -1;    // текущий час в Москве
        public bool isMoskowDateChanged = false; // флаг изменения текущей даты в Москве
        public bool isAdminRules = false;   // права админа вкл/откл
        public bool isReadPGTPfromDB = false; // прочитан график измеренной мощности из  БД
        public double PGESyesterdayFACT =0;     // сумма фактической выработки ГЭС за вчерашний день
        public double PGEStodayFACT = 0;               // сумма фактической выработки ГЭС за сегодня
        public double PGESyesterdayPLAN =0;     // сумма плановой выработки ГЭС за вчерашний день
        public double PGEStodayPLAN = 0;               // сумма плановой выработки ГЭС за сегодня
        
        private double[] GTP0;
        private double[] GTP1;
        private double[] GTP2;
        private double[] GTP3;
        private double[] GTP4;
        private double[] GTP5;
        private double[] GTP6;
        private double[] GTP7;
        private double[] GTP8;
        private double[] GTP9;
        private double[] GTP10;
                       
        private double[] GTP1_final;
        private double[] GTP2_final;
        private double[] GTP3_final;
        private double[] GTP4_final;
        private double[] GTP5_final;
        private double[] GTP6_final;
        private double[] GTP7_final;
        private double[] GTP8_final;
        private double[] GTP9_final;
        private double[] GTP10_final;

        // словарь Время = мощность
        private Dictionary <int, double> GTP1_DB;
        private Dictionary <int, double> GTP2_DB;
        private Dictionary <int, double> GTP3_DB;
        private Dictionary <int, double> GTP4_DB;
        private Dictionary <int, double> GTP5_DB;
        private Dictionary <int, double> GTP6_DB;
        private Dictionary <int, double> GTP7_DB;
        private Dictionary <int, double> GTP8_DB;
        private Dictionary <int, double> GTP9_DB;
        private Dictionary <int, double> GTP10_DB;

        private ArrayList TimeReperPoints1;
        private ArrayList TimeReperPoints2;
        private ArrayList TimeReperPoints3;
        private ArrayList TimeReperPoints4;
        private ArrayList TimeReperPoints5;
        private ArrayList TimeReperPoints6;
        private ArrayList TimeReperPoints7;
        private ArrayList TimeReperPoints8;
        private ArrayList TimeReperPoints9;
        private ArrayList TimeReperPoints10;

        private Dictionary<int, double> PGES;
        private Dictionary<int, double> PGTP1;
        private Dictionary<int, double> PGTP2;
        private Dictionary<int, double> PGTP3;
        private Dictionary<int, double> PGTP4;
        private Dictionary<int, double> PGTP5;
        private Dictionary<int, double> PGTP6;
        private Dictionary<int, double> PGTP7;
        private Dictionary<int, double> PGTP8;
        private Dictionary<int, double> PGTP9;
        private Dictionary<int, double> PGTP10;

        private int pbrY; //Год в ПБР
        private int pbrM; //Месяц в ПБР
        private int pbrD; //День в ПБР
        private int pbrHour; //Час в ПБР
        private int pbrMin; //Минута в ПБР
        private int LastpbrHour; // час в последнем принятом ПБР
        private int LastpbrD;    // день в последнем принятом ПБР   
        private int LastpbrM;    // месяц в последнем принятом ПБР 
        private int LastpbrY;    // год в последнем принятом ПБР 
        private int CurrSec = 0;// текущая секунда из Овации
        private int CurrMin = 0;// текущая минута из Овации
        private int CurrHour = 0;// текущий час из Овации
        private int CurrDay = 0;// текущий день из Овации
        private int CurrMonth = 0;// текущий месяц из Овации
        private int CurrYear = 0;// текущий год из Овации
        private int Today;// текущий день

        private Vyrabotka vyr0 = new Vyrabotka();
        private Vyrabotka vyr1 = new Vyrabotka();
        private Vyrabotka vyr2 = new Vyrabotka();
        private Vyrabotka vyr3 = new Vyrabotka();
        private Vyrabotka vyr4 = new Vyrabotka();
        private Vyrabotka vyr5 = new Vyrabotka();
        private Vyrabotka vyr6 = new Vyrabotka();
        private Vyrabotka vyr7 = new Vyrabotka();
        private Vyrabotka vyr8 = new Vyrabotka();
        private Vyrabotka vyr9 = new Vyrabotka();
        private Vyrabotka vyr10 = new Vyrabotka();

        private string IniFileName = "AutoOper.ini";
        private string PbrDirName;
        private string ArchDirName;
        private string LogFileName;
        private double ForbiddenZone;
        private bool isIniFile = false;
        private bool isActualDate = false; //получили актуальное время
        private bool pbrWithData = false;  // есть ли актуальные данные в ПБРе 
        private bool getDBdata = false;    // получили данные из БД
        private bool Permit = false;       //разрешение на редактирование параметров интерполяции   
        private bool isDateChanged = false;    // флаг смены даты
        private bool isGTP1 = false;    // наличие ГТП-1
        private bool isGTP2 = false;    // наличие ГТП-2
        private bool isGTP3 = false;    // наличие ГТП-3
        private bool isGTP4 = false;    // наличие ГТП-4
        private bool isGTP5 = false;    // наличие ГТП-5
        private bool isGTP6 = false;    // наличие ГТП-6
        private bool isGTP7 = false;    // наличие ГТП-7
        private bool isGTP8 = false;    // наличие ГТП-8
        private bool isGTP9 = false;    // наличие ГТП-9
        private bool isGTP10 = false;    // наличие ГТП-10

        private int GTP1_ID;    // ID ГТП-1
        private int GTP2_ID;    // ID ГТП-2
        private int GTP3_ID;    // ID ГТП-3
        private int GTP4_ID;    // ID ГТП-4
        private int GTP5_ID;    // ID ГТП-5
        private int GTP6_ID;    // ID ГТП-6
        private int GTP7_ID;    // ID ГТП-7
        private int GTP8_ID;    // ID ГТП-8
        private int GTP9_ID;    // ID ГТП-9
        private int GTP10_ID;    // ID ГТП-10

        private int LocalName;    //имя компа на котором запущен АвтоОператор
        private int RUSATime;   //время РУСА в секундах
        private int TimeStep;   //шаг по времени
        private int PowerStep;  //шаг по мощности
        private double GTPIsOk = 1;//статус АвтоОператора
        private Ini.IniFile ini;
        private SqlConnection DBConn;
        private string DBIP;
        private string DBLogin;
        private string DBPassword;
        private string DBName;
        private int GlobalTimeStep;
        Form f2;
        // ------------------------------------------------------------------------
        public Form1()
        {
            //CallBackMy.callbackEventHandler = new CallBackMy.callbackEvent(this.ClearGraphs);
            //CallBackMy1.callbackEventHandler1 = new CallBackMy1.callbackEvent(this.ClearServThread);
            InitializeComponent();
        }
        // ------------------------------------------------------------------------
        void ClearGraphs(string param)
        {
            //чистим все графики
            ClearGraph(zg0);
            ClearGraph(zg1);
            ClearGraph(zg2);
            ClearGraph(zg3);
            ClearGraph(zg4);
            ClearGraph(zg5);
            ClearGraph(zg6);
            ClearGraph(zg7);
            ClearGraph(zg8);
            ClearGraph(zg9);
            ClearGraph(zg10);
            
            PGES.Clear();
            PGTP1.Clear();
            PGTP2.Clear();
            PGTP3.Clear();
            PGTP4.Clear();
            PGTP5.Clear();
            PGTP6.Clear();
            PGTP7.Clear();
            PGTP8.Clear();
            PGTP9.Clear();
            PGTP10.Clear();


            for(int i=0; i<GTP0.Length; i++)
            {
                GTP0[i] = 0;          
            }

            if(isGTP1)
            {
                for(int i=0; i<GTP1.Length; i++)
                {
                    GTP1[i] = 0;          
                }       
                for(int i=0; i<GTP1_final.Length; i++)
                {
                    GTP1_final[i] = 0;          
                } 
            }
            if (isGTP2)
            {
                for (int i = 0; i < GTP2.Length; i++)
                {
                    GTP2[i] = 0;
                }
                for (int i = 0; i < GTP2_final.Length; i++)
                {
                    GTP2_final[i] = 0;
                }
            }
            if (isGTP3)
            {
                for (int i = 0; i < GTP3.Length; i++)
                {
                    GTP3[i] = 0;
                }
                for (int i = 0; i < GTP3_final.Length; i++)
                {
                    GTP3_final[i] = 0;
                }
            }
            if (isGTP4)
            {
                for (int i = 0; i < GTP4.Length; i++)
                {
                    GTP4[i] = 0;
                }
                for (int i = 0; i < GTP4_final.Length; i++)
                {
                    GTP4_final[i] = 0;
                }
            }
            if (isGTP5)
            {
                for (int i = 0; i < GTP5.Length; i++)
                {
                    GTP5[i] = 0;
                }
                for (int i = 0; i < GTP5_final.Length; i++)
                {
                    GTP5_final[i] = 0;
                }
            }
            if (isGTP6)
            {
                for (int i = 0; i < GTP6.Length; i++)
                {
                    GTP6[i] = 0;
                }
                for (int i = 0; i < GTP6_final.Length; i++)
                {
                    GTP6_final[i] = 0;
                }
            }
            if (isGTP7)
            {
                for (int i = 0; i < GTP7.Length; i++)
                {
                    GTP7[i] = 0;
                }
                for (int i = 0; i < GTP7_final.Length; i++)
                {
                    GTP7_final[i] = 0;
                }
            }
            if (isGTP8)
            {
                for (int i = 0; i < GTP8.Length; i++)
                {
                    GTP8[i] = 0;
                }
                for (int i = 0; i < GTP8_final.Length; i++)
                {
                    GTP8_final[i] = 0;
                }
            }
            if (isGTP9)
            {
                for (int i = 0; i < GTP9.Length; i++)
                {
                    GTP9[i] = 0;
                }
                for (int i = 0; i < GTP9_final.Length; i++)
                {
                    GTP9_final[i] = 0;
                }
            }
            if (isGTP10)
            {
                for (int i = 0; i < GTP10.Length; i++)
                {
                    GTP10[i] = 0;
                }
                for (int i = 0; i < GTP10_final.Length; i++)
                {
                    GTP10_final[i] = 0;
                }
            }
        }
        // ------------------------------------------------------------------------
        // Connect
        public void ServStart()
        {
            if (!MySlave.connected)
                MySlave.connect();
            else
                MySlave.disconnect();
        }
        //------------------------------------------------------------------------
        /*
        private void button2_Click(object sender, EventArgs e)
        {
            if (!MySlave.connected && MySlave.Listener == null) //connect
            {
                MySlave.Slave_ID = Convert.ToByte(textBox2.Text);
                MySlave.IP = textBox1.Text;
                MySlave.PORT = Convert.ToInt32(textBox3.Text);
                //Clear in/out buffers:
                MySlave.DiscardOutBuffer();
                MySlave.DiscardInBuffer();
                try
                {
                        ServThread = new Thread(new ThreadStart(ServStart));
                        ServThread.Priority = ThreadPriority.Highest;             // установка приоритета потока
                        ServThread.IsBackground = true;
                        Thread.Sleep(0);
                        ServThread.Start();//                                      // запустили поток. Стартовая функция
                        button2.Text = "Разъединить";
                        toolStripStatusLabel2.BackColor = Color.Green;
                        toolStripStatusLabel2.Text = "Соединение установлено";               
                }
                catch
                {
                    button2.Text = "Подключить";
                }
            }
            else  //disconnect
            {                            
                MySlave.disconnect();
                ServThread.Abort();
                button2.Text = "Подключить";
                toolStripStatusLabel2.BackColor = Color.Red;
                toolStripStatusLabel2.Text = "Соединение отсутствует";
            }
        }
          */
        //-------------------------------------------------------------------
        /*
        private void button1_Click(object sender, EventArgs e) // открыть ПБР 
        {// Открыть XML-документ
            openFileDialog2.Title = "Выбор ПБР";
            openFileDialog2.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                XmlFile = openFileDialog2.FileName;
                //label4.Text = openFileDialog2.FileName;
            }
        }
        //--------------------------------------------------------------------
        private void button3_Click(object sender, EventArgs e)
        {// Открыть XSLT-шаблон
            openFileDialog1.Title = "XSLP-шаблон";
            openFileDialog1.Filter = "XSLT files (*.xsl)|*.xsl|All files (*.*)|*.*";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                XsltFile = openFileDialog1.FileName;
                //label5.Text = openFileDialog1.FileName;
            }
        }
        */
        //----------------------------------------------------------------------------------
        void MBRegisterWriteValue(int RegNumber, double Value)
        {
            lock (this)
            {
                byte[] bBytes = new byte[4];
                bBytes = BitConverter.GetBytes((float)Value);
                MySlave.ModbusRegisters[RegNumber - 1].HiByte = bBytes[3];
                MySlave.ModbusRegisters[RegNumber - 1].LoByte = bBytes[2];
                MySlave.ModbusRegisters[RegNumber].HiByte = bBytes[1];
                MySlave.ModbusRegisters[RegNumber].LoByte = bBytes[0];
            }
        }
        //----------------------------------------------------------------------------------
        float MBRegisterReadValue(int RegNumber)
        {
            float value;
            byte[] data = new byte[4];

            data[3] = MySlave.ModbusRegisters[RegNumber - 1].HiByte;
            data[2] = MySlave.ModbusRegisters[RegNumber - 1].LoByte;
            data[1] = MySlave.ModbusRegisters[RegNumber].HiByte;
            data[0] = MySlave.ModbusRegisters[RegNumber].LoByte;
            value = BitConverter.ToSingle(data, 0);
            return value;
        }
        //----------------------------------------------------------------------------------
        void ClearServThread()
        {
            if (MySlave.Listener != null)
            {
                MySlave.Listener.Stop();
                MySlave.Listener = null;
            }

            ServThread = new Thread(new ThreadStart(ServStart));
            ServThread.Priority = ThreadPriority.Highest;             // установка приоритета потока
            ServThread.IsBackground = true;
            Thread.Sleep(0);
            ServThread.Start();// 
        }
        //----------------------------------------------------------------------------------
        private void timer1_Tick(object sender, EventArgs e) // работа по таймеру
        {
            DateTime dt = DateTime.Now;
            
            //заполняем переменные АвтоОператора данными из регистров Модбас
            ForbiddenZone = Convert.ToDouble(textBox1.Text);
            if (MySlave.connected)
            {
                CurrSec = Convert.ToInt32(MBRegisterReadValue(3)); // текущая секунда в сутках 1....86399
                CurrYear = Convert.ToInt32(MBRegisterReadValue(5));
                CurrMonth = Convert.ToInt32(MBRegisterReadValue(7));
                CurrDay = Convert.ToInt32(MBRegisterReadValue(9));
            }
            else
            {
                CurrSec = dt.Hour * 3600 + dt.Minute * 60 + dt.Second; // текущая секунда в сутках 1....86399
                CurrYear = dt.Year;
                CurrMonth = dt.Month;
                CurrDay = dt.Day;
            }

            if (CurrSec < 0)
                CurrSec = 0;
            if (CurrSec > 172800)
                CurrSec = 172800;

            CurrMin = (CurrSec % 3600) / 60;
            CurrHour = CurrSec / 3600;

            if ((CurrYear != 0) && (CurrMonth != 0) && ((MySlave.connected && isAdminRules) || !isAdminRules )  )
                isActualDate = true;
            else
                isActualDate = false;
            if (((CurrYear.ToString()).Length <= 2) && isActualDate) // формируем 4-хзначную запись текущего года
                CurrYear += 2000;


            if (CurrDay != Today)
            {
                isDateChanged = true;
            }
            else
            {
                isDateChanged = false;
            }
            Today = CurrDay;

            if ((24 + (CurrSec / 3600) - TimeFromMoskow) % 24 != MoskowCurrHour)
            {
                if ((CurrSec / 3600) - TimeFromMoskow == 0)
                {
                    // обнуляем массив выработок при начале новых суток в Москве
                    PGESyesterdayFACT = 0;
                    PGEStodayFACT = 0;
                    PGESyesterdayPLAN = 0;
                    PGEStodayPLAN = 0;
                    GTPOutputToNull();
                    PGES.Clear();
                }
            }
            MoskowCurrHour = (24 + (CurrSec / 3600) - TimeFromMoskow) % 24;

            // читаем данные в начале новых суток 1 раз
            // обнуляем массивы выработок
            if ((isDateChanged || (!getDBdata)) && (isActualDate))
                if (DBConn.State == ConnectionState.Open)
                {
                    PGESyesterdayFACT = vyr0.GetFactVyr();// PGEStodayFACT;
                    PGEStodayFACT = 0;
                    PGESyesterdayPLAN = vyr0.GetPlanVyr();// PGEStodayPLAN;
                    PGEStodayPLAN = 0;

                    PGES.Clear();
                    lock (this)
                    {
                        DBReadData();
                    }
                }


            if (isActualDate)
            {
                if (!isReadPGTPfromDB)
                {
                    ReadPGTPfromDB();
                    isReadPGTPfromDB = true;
                }
                if (!PGES.Keys.Contains<int>(CurrSec))
                {
                    PGES.Add(CurrSec, Convert.ToDouble(MBRegisterReadValue(51)));
                    if(isAdminRules)
                        WritePGTPtoDB(CurrSec, Convert.ToDouble(MBRegisterReadValue(51)), 0);
                }

                if (!PGTP1.Keys.Contains<int>(CurrSec) && isGTP1)
                    PGTP1.Add(CurrSec, Convert.ToDouble(MBRegisterReadValue(57)));

                if (!PGTP2.Keys.Contains<int>(CurrSec) && isGTP2)
                    PGTP2.Add(CurrSec, Convert.ToDouble(MBRegisterReadValue(59)));

                if (!PGTP3.Keys.Contains<int>(CurrSec) && isGTP3)
                    PGTP3.Add(CurrSec, Convert.ToDouble(MBRegisterReadValue(61)));

                if (!PGTP4.Keys.Contains<int>(CurrSec) && isGTP4)
                    PGTP4.Add(CurrSec, Convert.ToDouble(MBRegisterReadValue(63)));

                if (!PGTP5.Keys.Contains<int>(CurrSec) && isGTP5)
                    PGTP5.Add(CurrSec, Convert.ToDouble(MBRegisterReadValue(65)));

                if (!PGTP6.Keys.Contains<int>(CurrSec) && isGTP6)
                    PGTP6.Add(CurrSec, Convert.ToDouble(MBRegisterReadValue(67)));

                if (!PGTP7.Keys.Contains<int>(CurrSec) && isGTP7)
                    PGTP7.Add(CurrSec, Convert.ToDouble(MBRegisterReadValue(69)));

                if (!PGTP8.Keys.Contains<int>(CurrSec) && isGTP8)
                    PGTP8.Add(CurrSec, Convert.ToDouble(MBRegisterReadValue(71)));

                if (!PGTP9.Keys.Contains<int>(CurrSec) && isGTP9)
                    PGTP9.Add(CurrSec, Convert.ToDouble(MBRegisterReadValue(73)));

                if (!PGTP10.Keys.Contains<int>(CurrSec) && isGTP10)
                    PGTP10.Add(CurrSec, Convert.ToDouble(MBRegisterReadValue(75)));
            }
            try
            {
                if (CurrSec > 10 && MySlave.connected)
                {
                    //заполняем регистры Модбас полученными данными
                    MBRegisterWriteValue(1, (MBRegisterReadValue(1) + 1) % 60); //Пила в Овацию
                    if (isGTP1)
                    {
                        MBRegisterWriteValue(11, GTP1_final[CurrSec]);       //Задание на ГТП-1
                        MBRegisterWriteValue(31, GTP1_final[(CurrSec + RUSATime) % 172800]);
                    }
                    if (isGTP2)
                    {
                        MBRegisterWriteValue(13, GTP2_final[CurrSec]);       //Задание на ГТП-2
                        MBRegisterWriteValue(33, GTP2_final[(CurrSec + RUSATime) % 172800]);       //Задание на ГТП-2 РУСА
                    }
                    if (isGTP3)
                    {
                        MBRegisterWriteValue(15, GTP3_final[CurrSec]);       //Задание на ГТП-3
                        MBRegisterWriteValue(35, GTP3_final[(CurrSec + RUSATime) % 172800]);       //Задание на ГТП-3 РУСА
                    }
                    if (isGTP4)
                    {
                        MBRegisterWriteValue(17, GTP4_final[CurrSec]);       //Задание на ГТП-4
                        MBRegisterWriteValue(37, GTP4_final[(CurrSec + RUSATime) % 172800]);       //Задание на ГТП-4 РУСА
                    }
                    if (isGTP5)
                    {
                        MBRegisterWriteValue(19, GTP5_final[CurrSec]);       //Задание на ГТП-5
                        MBRegisterWriteValue(39, GTP5_final[(CurrSec + RUSATime) % 172800]);       //Задание на ГТП-5 РУСА
                    }
                    if (isGTP6)
                    {
                        MBRegisterWriteValue(21, GTP6_final[CurrSec]);       //Задание на ГТП-6
                        MBRegisterWriteValue(41, GTP6_final[(CurrSec + RUSATime) % 172800]);       //Задание на ГТП-6 РУСА
                    }
                    if (isGTP7)
                    {
                        MBRegisterWriteValue(23, GTP7_final[CurrSec]);       //Задание на ГТП-7
                        MBRegisterWriteValue(43, GTP7_final[(CurrSec + RUSATime) % 172800]);       //Задание на ГТП-7 РУСА
                    }
                    if (isGTP8)
                    {
                        MBRegisterWriteValue(25, GTP8_final[CurrSec]);       //Задание на ГТП-8
                        MBRegisterWriteValue(45, GTP8_final[(CurrSec + RUSATime) % 172800]);       //Задание на ГТП-8 РУСА
                    }
                    if (isGTP9)
                    {
                        MBRegisterWriteValue(27, GTP9_final[CurrSec]);       //Задание на ГТП-9
                        MBRegisterWriteValue(47, GTP9_final[(CurrSec + RUSATime) % 172800]);       //Задание на ГТП-9 РУСА
                    }
                    if (isGTP10)
                    {
                        MBRegisterWriteValue(29, GTP10_final[CurrSec]);       //Задание на ГТП-10
                        MBRegisterWriteValue(49, GTP10_final[(CurrSec + RUSATime) % 172800]);       //Задание на ГТП-10 РУСА
                    }

                    MBRegisterWriteValue(53, GTPIsOk);       //Статус АвтоОператора
                    MBRegisterWriteValue(55, LocalName);       //IP-адрес компа на котором крутится АвтоОператор
                }
            }
            catch (Exception ex)
            {
                WriteLog(CurrSec, "Ошибка записи данных в регистры Modbus.");
                GTPIsOk = 0;
            }
            //переписываем значения всех аналоговых регистров Modbus в таблицу
            try
            {
                byte[] data = new byte[4];
                for (int i = 1; i < MySlave.GetNumberMBRegisters(); i += 2)
                {
                    dataGridView1.Rows[i-1].Cells[1].Value = Convert.ToString(MBRegisterReadValue(i));
                }
            }
            catch
            {
                WriteLog(CurrSec, "Ошибка перезаписи регистров Modbus в таблицу.");
            }


            // проверяем подключение к Овации при запущенном потоке ServerThread
            if ((ServThread == null) || ((!MySlave.connected) && (ServThread != null)))
            {
                if (MySlave.Listener != null)
                {
                    MySlave.Listener.Stop();
                    MySlave.Listener = null;
                }

                ServThread = new Thread(new ThreadStart(ServStart));
                ServThread.Priority = ThreadPriority.Highest;             // установка приоритета потока
                ServThread.IsBackground = true;
                Thread.Sleep(0);
                ServThread.Start();//
            }

            if (MySlave.connected)
            {
                toolStripStatusLabel2.BackColor = Color.Green;
                toolStripStatusLabel2.Text = "Соединение установлено";
            }
            else 
            {
                toolStripStatusLabel2.BackColor = Color.Red;
                toolStripStatusLabel2.Text = "Соединение отсутствует";
            }

            label1.Text = (isGTP1) ? Convert.ToString(Math.Round(GTP1_final[CurrSec],1)) :"0";
            label2.Text = (isGTP2) ? Convert.ToString(Math.Round(GTP2_final[CurrSec],1)) :"0";
            label3.Text = (isGTP3) ? Convert.ToString(Math.Round(GTP3_final[CurrSec],1)) :"0";
            label13.Text = (isGTP4) ? Convert.ToString(Math.Round(GTP4_final[CurrSec],1)) :"0";
            label14.Text = (isGTP5) ? Convert.ToString(Math.Round(GTP5_final[CurrSec],1)) :"0";
            label15.Text = (isGTP6) ? Convert.ToString(Math.Round(GTP6_final[CurrSec],1)) :"0";
            label16.Text = (isGTP7) ? Convert.ToString(Math.Round(GTP7_final[CurrSec],1)) :"0";
            label17.Text = (isGTP8) ? Convert.ToString(Math.Round(GTP8_final[CurrSec],1)) :"0";
            label18.Text = (isGTP9) ? Convert.ToString(Math.Round(GTP9_final[CurrSec],1)) :"0";
            label19.Text = (isGTP10) ? Convert.ToString(Math.Round(GTP10_final[CurrSec], 1)) : "0";

            label20.Text = Convert.ToString(Math.Round(GTP0[CurrSec],1));

            // расчёт выработки
            if (CurrSec > 0 && isActualDate)
                try
                {
                    // вычисляем динамическую фактичеcкую выработку методом трапеций
                    vyr0.ToNULL();

                    if ( CurrSec > TimeFromMoskow*3600)
                    // время с 02:00 по 24:00
                    {                       
                        for (int i = 0; i < PGES.Keys.Count - 1; i++)
                        {
                            if (PGES.Keys.ToArray()[i] >= TimeFromMoskow * 3600)
                            {
                                PGEStodayFACT = Math.Abs(0.5 * ((PGES.Keys.ToArray()[i + 1] - PGES.Keys.ToArray()[i]) / 3600.0) * (PGES.Values.ToArray()[i] + PGES.Values.ToArray()[i + 1]));
                            }
                            vyr0.AddFactVyr(PGEStodayFACT);
                        }
                    }
                    else
                    // время с 00:00 по 02:00
                    {
                        for (int i = 0; i < PGES.Keys.Count - 1; i++)
                        {
                            if (PGES.Keys.ToArray()[i + 1] <= TimeFromMoskow * 3600)
                            {
                                PGEStodayFACT = Math.Abs(0.5 * ((PGES.Keys.ToArray()[i + 1] - PGES.Keys.ToArray()[i]) / 3600.0) * (PGES.Values.ToArray()[i] + PGES.Values.ToArray()[i + 1]));
                            }
                            vyr0.AddFactVyr(PGEStodayFACT);
                        }
                        vyr0.AddFactVyr(PGESyesterdayFACT);
                    }
                    

                    // вычисляем динамическую плановую выработку методом трапеций
                    if (CurrSec > TimeFromMoskow * 3600)
                    // время с 02:00 по 24:00
                    {
                        for (int i = TimeFromMoskow * 3600; i < CurrSec - 1; i++)
                        {
                            PGEStodayPLAN = Math.Abs(0.5 * (1.0 / 3600.0) * (GTP0[i] + GTP0[i + 1]));
                            vyr0.AddPlanVyr(PGEStodayPLAN);
                        }
                    }
                    else
                    // время с 00:00 по 02:00
                    {
                        for (int i = 0; i < TimeFromMoskow * 3600 - 1; i++)
                        {
                            PGEStodayPLAN = Math.Abs(0.5 * (1.0 / 3600.0) * (GTP0[i] + GTP0[i + 1]));
                            vyr0.AddPlanVyr(PGEStodayPLAN);
                        }
                        vyr0.AddPlanVyr(PGESyesterdayPLAN);
                    }

                    //for (int i = 0; i < CurrSec - 1; i++)
                    //{
                    //    vyr0.AddPlanVyr(Math.Abs( 0.5 * (1.0 / 3600.0) * (GTP0[i] + GTP0[i+1])  ));
                    //}
                }
                catch
                {;}
        }

        //--------------------------------------------------------------------------------
        private void Form1_Load(object sender, EventArgs e)
        {
            MySlave = new Slave();
            MBRegisterWriteValue(1, 0.0);
            ForbiddenZone = Convert.ToDouble(textBox1.Text);
            DateTime dt = DateTime.Now;
            CurrYear = dt.Year;
            CurrMonth = dt.Month;
            CurrDay = dt.Day;
            CurrSec = dt.Second + 60 * dt.Minute + 3600 * dt.Hour;
            
            // таблица регистров 40000 - ...
            dataGridView1.Rows.Add(MySlave.GetNumberMBRegisters());
            for (int k = 0; k < MySlave.GetNumberMBRegisters(); k++)
            {
                dataGridView1.Rows[k].Cells[0].Value = Convert.ToString(40000 + k+1);
                dataGridView1.Rows[k].Cells[2].Value = "Float";
            }
            dataGridView1.Rows[0].Cells[3].Value = "OUT--->Счётчик секунд";
            dataGridView1.Rows[2].Cells[3].Value = "IN<----Номер текущей секунды в сутках";
            dataGridView1.Rows[4].Cells[3].Value = "IN<----Год";
            dataGridView1.Rows[6].Cells[3].Value = "IN<----Месяц";
            dataGridView1.Rows[8].Cells[3].Value = "IN<----День";


            dataGridView1.Rows[10].Cells[3].Value = "OUT--->ГТП-1";
            dataGridView1.Rows[12].Cells[3].Value = "OUT--->ГТП-2";
            dataGridView1.Rows[14].Cells[3].Value = "OUT--->ГТП-3";
            dataGridView1.Rows[16].Cells[3].Value = "OUT--->ГТП-4";
            dataGridView1.Rows[18].Cells[3].Value = "OUT--->ГТП-5";
            dataGridView1.Rows[20].Cells[3].Value = "OUT--->ГТП-6";
            dataGridView1.Rows[22].Cells[3].Value = "OUT--->ГТП-7";
            dataGridView1.Rows[24].Cells[3].Value = "OUT--->ГТП-8";
            dataGridView1.Rows[26].Cells[3].Value = "OUT--->ГТП-9";
            dataGridView1.Rows[28].Cells[3].Value = "OUT--->ГТП-10";

            dataGridView1.Rows[30].Cells[3].Value = "OUT--->ГТП-1 РУСА";
            dataGridView1.Rows[32].Cells[3].Value = "OUT--->ГТП-2 РУСА";
            dataGridView1.Rows[34].Cells[3].Value = "OUT--->ГТП-3 РУСА";
            dataGridView1.Rows[36].Cells[3].Value = "OUT--->ГТП-4 РУСА";
            dataGridView1.Rows[38].Cells[3].Value = "OUT--->ГТП-5 РУСА";
            dataGridView1.Rows[40].Cells[3].Value = "OUT--->ГТП-6 РУСА";
            dataGridView1.Rows[42].Cells[3].Value = "OUT--->ГТП-7 РУСА";
            dataGridView1.Rows[44].Cells[3].Value = "OUT--->ГТП-8 РУСА";
            dataGridView1.Rows[46].Cells[3].Value = "OUT--->ГТП-9 РУСА";
            dataGridView1.Rows[48].Cells[3].Value = "OUT--->ГТП-10 РУСА";

            dataGridView1.Rows[50].Cells[3].Value = "IN<----Текущая нагрузка ГЭС";
            dataGridView1.Rows[52].Cells[3].Value = "OUT--->Статус ГТП";
            dataGridView1.Rows[54].Cells[3].Value = "OUT--->Номер данного АРМа";

            dataGridView1.Rows[56].Cells[3].Value = "IN<----Текущая нагрузка ГТП-1";
            dataGridView1.Rows[58].Cells[3].Value = "IN<----Текущая нагрузка ГТП-2";
            dataGridView1.Rows[60].Cells[3].Value = "IN<----Текущая нагрузка ГТП-3";
            dataGridView1.Rows[62].Cells[3].Value = "IN<----Текущая нагрузка ГТП-4";
            dataGridView1.Rows[64].Cells[3].Value = "IN<----Текущая нагрузка ГТП-5";
            dataGridView1.Rows[66].Cells[3].Value = "IN<----Текущая нагрузка ГТП-6";
            dataGridView1.Rows[68].Cells[3].Value = "IN<----Текущая нагрузка ГТП-7";
            dataGridView1.Rows[70].Cells[3].Value = "IN<----Текущая нагрузка ГТП-8";
            dataGridView1.Rows[72].Cells[3].Value = "IN<----Текущая нагрузка ГТП-9";
            dataGridView1.Rows[74].Cells[3].Value = "IN<----Текущая нагрузка ГТП-10";

            toolStripStatusLabel2.BackColor = Color.Red;
            toolStripStatusLabel2.Text = "Соединение отсутствует";
            tabControl1.SelectedIndex = 1;
            comboBox1.Enabled = false;
            comboBox2.Enabled = false;
            comboBox4.Enabled = false;
            textBox5.BackColor = Color.White;
            textBox6.BackColor = Color.White;
            textBox5.ForeColor = Color.Black;
            textBox6.ForeColor = Color.Black;

            label1.Location = new Point(Convert.ToInt32(tabControl1.Width * 0.5), Convert.ToInt32(tabPage3.Top - 30));//ГТП-1
            label2.Location = new Point(Convert.ToInt32(tabControl1.Width * 0.5), Convert.ToInt32(tabPage3.Top - 30));//ГТП-2
            label3.Location = new Point(Convert.ToInt32(tabControl1.Width * 0.5), Convert.ToInt32(tabPage4.Top - 30));//ГТП-3
            label13.Location = new Point(Convert.ToInt32(tabControl1.Width * 0.5), Convert.ToInt32(tabPage5.Top - 30));//ГТП-4
            label14.Location = new Point(Convert.ToInt32(tabControl1.Width * 0.5), Convert.ToInt32(tabPage6.Top - 30));//ГТП-5
            label15.Location = new Point(Convert.ToInt32(tabControl1.Width * 0.5), Convert.ToInt32(tabPage7.Top - 30));//ГТП-6
            label16.Location = new Point(Convert.ToInt32(tabControl1.Width * 0.5), Convert.ToInt32(tabPage8.Top - 30));//ГТП-7
            label17.Location = new Point(Convert.ToInt32(tabControl1.Width * 0.5), Convert.ToInt32(tabPage9.Top - 30));//ГТП-8
            label18.Location = new Point(Convert.ToInt32(tabControl1.Width * 0.5), Convert.ToInt32(tabPage10.Top - 30));//ГТП-9
            label19.Location = new Point(Convert.ToInt32(tabControl1.Width * 0.5), Convert.ToInt32(tabPage11.Top - 30));//ГТП-10

            label20.Location = new Point(Convert.ToInt32(tabControl1.Width * 0.5), Convert.ToInt32(tabPage12.Top - 30));//ГТП-ГЭС

            RUSATime = Convert.ToInt32(comboBox3.Text);
            // проверяем наличие ini-файла в текущей директории
            DirectoryInfo MyDir = new DirectoryInfo(Application.StartupPath);
            FileInfo[] MyFiles = MyDir.GetFiles();
            foreach (FileInfo f in MyFiles)
                if (f.Name == IniFileName)
                    isIniFile = true;

            if(isIniFile)
            {
                // читаем конфигурационнный файл
                ini = new Ini.IniFile(Application.StartupPath + "\\" + IniFileName);

                MySlave.PORT = Convert.ToInt32(ini.IniReadValue("Connection", "Port"));
                MySlave.Slave_ID = Convert.ToUInt16(ini.IniReadValue("Connection", "SlaveID"));

                DBIP = ini.IniReadValue("DataBase", "DBIP");
                DBLogin = ini.IniReadValue("DataBase", "DBLogin");
                DBPassword = ini.IniReadValue("DataBase", "DBPassword");
                DBName = ini.IniReadValue("DataBase", "DBName");

                PbrDirName = ini.IniReadValue("Paths", "PBR");
                ArchDirName = ini.IniReadValue("Paths", "ARCH");
                LogFileName = ini.IniReadValue("Paths", "LOG");

                TimeStep = Convert.ToInt32(ini.IniReadValue("Approx", "TimeStep"));
                comboBox2.Text = ini.IniReadValue("Approx", "TimeStep");
                PowerStep = Convert.ToInt32(ini.IniReadValue("Approx", "PowerStep"));
                comboBox4.Text = ini.IniReadValue("Approx", "PowerStep");
                comboBox1.SelectedIndex = Convert.ToInt32(ini.IniReadValue("Approx", "SelectedIndex"));

                isGTP1 = Convert.ToBoolean(ini.IniReadValue("GTP", "GTP1"));
                isGTP2 = Convert.ToBoolean(ini.IniReadValue("GTP", "GTP2"));
                isGTP3 = Convert.ToBoolean(ini.IniReadValue("GTP", "GTP3"));
                isGTP4 = Convert.ToBoolean(ini.IniReadValue("GTP", "GTP4"));
                isGTP5 = Convert.ToBoolean(ini.IniReadValue("GTP", "GTP5"));
                isGTP6 = Convert.ToBoolean(ini.IniReadValue("GTP", "GTP6"));
                isGTP7 = Convert.ToBoolean(ini.IniReadValue("GTP", "GTP7"));
                isGTP8 = Convert.ToBoolean(ini.IniReadValue("GTP", "GTP8"));
                isGTP9 = Convert.ToBoolean(ini.IniReadValue("GTP", "GTP9"));
                isGTP10 = Convert.ToBoolean(ini.IniReadValue("GTP", "GTP10"));

                tabPage2.Text = ini.IniReadValue("GTPName", "GTP1");
                tabPage3.Text = ini.IniReadValue("GTPName", "GTP2");
                tabPage4.Text = ini.IniReadValue("GTPName", "GTP3");
                tabPage5.Text = ini.IniReadValue("GTPName", "GTP4");
                tabPage6.Text = ini.IniReadValue("GTPName", "GTP5");
                tabPage7.Text = ini.IniReadValue("GTPName", "GTP6");
                tabPage8.Text = ini.IniReadValue("GTPName", "GTP7");
                tabPage9.Text = ini.IniReadValue("GTPName", "GTP8");
                tabPage10.Text = ini.IniReadValue("GTPName", "GTP9");
                tabPage11.Text = ini.IniReadValue("GTPName", "GTP10");

                GTP1_ID = Convert.ToInt32(ini.IniReadValue("GTPID", "GTP1_ID"));
                GTP2_ID = Convert.ToInt32(ini.IniReadValue("GTPID", "GTP2_ID"));
                GTP3_ID = Convert.ToInt32(ini.IniReadValue("GTPID", "GTP3_ID"));
                GTP4_ID = Convert.ToInt32(ini.IniReadValue("GTPID", "GTP4_ID"));
                GTP5_ID = Convert.ToInt32(ini.IniReadValue("GTPID", "GTP5_ID"));
                GTP6_ID = Convert.ToInt32(ini.IniReadValue("GTPID", "GTP6_ID"));
                GTP7_ID = Convert.ToInt32(ini.IniReadValue("GTPID", "GTP7_ID"));
                GTP8_ID = Convert.ToInt32(ini.IniReadValue("GTPID", "GTP8_ID"));
                GTP9_ID = Convert.ToInt32(ini.IniReadValue("GTPID", "GTP9_ID"));
                GTP10_ID = Convert.ToInt32(ini.IniReadValue("GTPID", "GTP10_ID"));

                TimeFromMoskow = Convert.ToInt32(ini.IniReadValue("Time", "TimeFromMoskow"));

                isAdminRules = Convert.ToBoolean(ini.IniReadValue("Rules", "Admin"));
            }

            PGES = new Dictionary<int,double>(); // измеренная мощность гэс
            PGTP1 = new Dictionary<int, double>(); // измеренная мощность РГЕ-1
            PGTP2 = new Dictionary<int, double>(); // измеренная мощность РГЕ-1
            PGTP3 = new Dictionary<int, double>(); // измеренная мощность РГЕ-1
            PGTP4 = new Dictionary<int, double>(); // измеренная мощность РГЕ-1
            PGTP5 = new Dictionary<int, double>(); // измеренная мощность РГЕ-1
            PGTP6 = new Dictionary<int, double>(); // измеренная мощность РГЕ-1
            PGTP7 = new Dictionary<int, double>(); // измеренная мощность РГЕ-1
            PGTP8 = new Dictionary<int, double>(); // измеренная мощность РГЕ-1
            PGTP9 = new Dictionary<int, double>(); // измеренная мощность РГЕ-1
            PGTP10 = new Dictionary<int, double>(); // измеренная мощность РГЕ-1
            GTP0 = new double[172801]; // суммарная мощность ГЭС

            if (isGTP1)
            {
                GTP1 = new double[172801];
                GTP1_final = new double[172801];
                TimeReperPoints1 = new ArrayList();
                GTP1_DB = new Dictionary<int, double>();
                checkBox1.Checked = true;
            }
            else
            {
                tabControl1.TabPages.Remove(tabPage2);
            }
            if (isGTP2)
            {
                GTP2 = new double[172801];
                GTP2_final = new double[172801];
                TimeReperPoints2 = new ArrayList();
                GTP2_DB = new Dictionary<int, double>();
                checkBox2.Checked = true;
            }
            else
            {
                tabControl1.TabPages.Remove(tabPage3);
            }
            if (isGTP3)
            {
                GTP3 = new double[172801];
                GTP3_final = new double[172801];
                TimeReperPoints3 = new ArrayList();
                GTP3_DB = new Dictionary<int, double>();
                checkBox3.Checked = true;
            }
            else
            {
                tabControl1.TabPages.Remove(tabPage4);
            }
            if (isGTP4)
            {
                GTP4 = new double[172801];
                GTP4_final = new double[172801];
                TimeReperPoints4 = new ArrayList();
                GTP4_DB = new Dictionary<int, double>();
                checkBox4.Checked = true;
            }
            else
            {
                tabControl1.TabPages.Remove(tabPage5);
            }
            if (isGTP5)
            {
                GTP5 = new double[172801];
                GTP5_final = new double[172801];
                TimeReperPoints5 = new ArrayList();
                GTP5_DB = new Dictionary<int, double>();
                checkBox5.Checked = true;
            }
            else
            {
                tabControl1.TabPages.Remove(tabPage6);
            }
            if (isGTP6)
            {
                GTP6 = new double[172801];
                GTP6_final = new double[172801];
                TimeReperPoints6 = new ArrayList();
                GTP6_DB = new Dictionary<int, double>();
                checkBox6.Checked = true;
            }
            else
            {
                tabControl1.TabPages.Remove(tabPage7);
            }
            if (isGTP7)
            {
                GTP7 = new double[172801];
                GTP7_final = new double[172801];
                TimeReperPoints7 = new ArrayList();
                GTP7_DB = new Dictionary<int, double>();
                checkBox7.Checked = true;
            }
            else
            {
                tabControl1.TabPages.Remove(tabPage8);
            }
            if (isGTP8)
            {
                GTP8 = new double[172801];
                GTP8_final = new double[172801];
                TimeReperPoints8 = new ArrayList();
                GTP8_DB = new Dictionary<int, double>();
                checkBox8.Checked = true;
            }
            else
            {
                tabControl1.TabPages.Remove(tabPage9);
            }
            if (isGTP9)
            {
                GTP9 = new double[172801];
                GTP9_final = new double[172801];
                TimeReperPoints9 = new ArrayList();
                GTP9_DB = new Dictionary<int, double>();
                checkBox9.Checked = true;
            }
            else
            {
                tabControl1.TabPages.Remove(tabPage10);
            }
            if (isGTP10)
            {
                GTP10 = new double[172801];
                GTP10_final = new double[172801];
                TimeReperPoints10 = new ArrayList();
                GTP10_DB = new Dictionary<int, double>();
                checkBox10.Checked = true;
            }
            else
            {
                tabControl1.TabPages.Remove(tabPage11);
            }

            // соединяемся с БД
            
            DBConn = new SqlConnection("server=" + DBIP + ";user id =" + DBLogin + ";password=" + DBPassword + ";database=" + DBName + ";connection timeout=15");
            try
            {
                DBConn.Open();
                WriteLog(CurrSec, "Соединение с базой данных успешно установлено.");
            }
            catch
            {
                WriteLog(CurrSec, "Ошибка соединения с базой данных.");
                GTPIsOk = 0;
            }
            // получаем имя сервера
            if (Environment.MachineName.StartsWith("drop") || Environment.MachineName.StartsWith("Drop") || Environment.MachineName.StartsWith("DROP"))
                LocalName = Convert.ToInt32(Environment.MachineName.Substring(4,3));
            timer1.Enabled = true;///запускаем основной таймер программы !!!!!!!   
            UpdateVyr();
        }
        //--------------------------------------------------------------------------------
        private void WriteLog(int TimeInSec, string Data)
        {
            StreamWriter log;
            if (!File.Exists(LogFileName))
            {
                log = new StreamWriter(LogFileName);
            }
            else
            {
                FileInfo fi = new FileInfo(LogFileName);
                if (fi.Length > 100000000) //>100 Мб
                {
                    fi.MoveTo(ArchDirName);
                    log = new StreamWriter(LogFileName);
                }
                log = File.AppendText(LogFileName);
            }

            // Write to the file:
            log.WriteLine(GetDate(TimeInSec) + " -------- " + Data);
            log.WriteLine();

            // Close the stream:
            log.Close();
        }
        //--------------------------------------------------------------------------------
        string GetCurrentSeason()
        {
            DateTime dt = DateTime.Now;
            if (isActualDate)
            {
                bool I = dt.IsDaylightSavingTime();
                int t1 = CurrYear * 2;
                if (I)
                    t1 += 1;
                else
                    if (CurrMonth > 6)
                        t1 += 2;
                return Convert.ToString(t1);
            }
            else
                return "0";
        }
        //------------------------------------------------------------------------------
        string GetDate(int second)
        {
            string date;
            int t3 = CurrDay;
            if ((second >= 86400) && (second < 172800))
                t3 = GetNextDay(CurrDay, CurrMonth, CurrYear);
            if (second >= 172800)
                t3 = GetNextDay(GetNextDay(CurrDay, CurrMonth, CurrYear), CurrMonth, CurrYear);
            return date = Convert.ToString(CurrYear) + "-" + Convert.ToString(CurrMonth).PadLeft(2, '0') + "-" + Convert.ToString(t3).PadLeft(2, '0') + " " +
                           Convert.ToString((second / 3600) % 24).PadLeft(2, '0') + ":" + Convert.ToString((second % 3600) / 60).PadLeft(2, '0') + ":" + Convert.ToString(second % 60).PadLeft(2, '0') + ".000";
        
        }
        //------------------------------------------------------------------------------
        void DBWriteTRP_each(int tempSec, ArrayList TimeReperPoints, int Item, double[] GTP)
        {
                    SqlCommand DBCommand;
                    string dbPARNUMBER;
                    string dbOBJECT = "0";
                    string dbITEM;
                    string dbVALUE0;
                    string dbVALUE1;
                    string dbOBJTYPE = "0";
                    string dbDATA_DATE;
                    string dbP2KStatus = "0";
                    string dbRcvStamp = "GETDATE()";
                    string dbSEASON = GetCurrentSeason();

                    int sec1 = Convert.ToInt32( (TimeReperPoints.Count>0) ? TimeReperPoints[0] : 0);
                    int sec2 = Convert.ToInt32( (TimeReperPoints.Count>0) ? TimeReperPoints1[TimeReperPoints.Count - 1] : 0);
                    if (sec1 < tempSec) // чтобы не удалить прошлые записи в базе данных
                        sec1 = tempSec;
                    if (sec1 < sec2)
                    {
                        string t1 = GetDate(sec1);
                        string t2 = GetDate(sec2);
                        dbITEM = Convert.ToString(Item);

                        // удаление данных из БД c времени  t1 по t2
                        DBCommand = new SqlCommand("DELETE FROM DATA" +
                              " WHERE  (PARNUMBER=301) AND ITEM = " + dbITEM + " AND (DATA_DATE BETWEEN '" + t1 + "' AND '" + t2 + "')", DBConn);
                        DBCommand.ExecuteNonQuery();
                        //--------------------------------------------------------------------------
                        //пишем в базу массив TimeReperPoints
                        dbPARNUMBER = "301";
                        // запись   
                        for (int i = 0; i < TimeReperPoints.Count; i++)
                        {
                            int tempTime = Convert.ToInt32(TimeReperPoints[i]);
                            if (tempTime >= tempSec)  //if (tempTime >= 3600 * (tempSec / 3600))//
                            {
                                dbVALUE0 = dbVALUE1 = Convert.ToString(GTP[Convert.ToInt32(TimeReperPoints[i])]).Replace(',', '.');

                                dbDATA_DATE = GetDate(tempTime);

                                DBCommand = new SqlCommand("INSERT INTO DATA (PARNUMBER,OBJECT,ITEM,VALUE0,VALUE1,OBJTYPE,DATA_DATE,P2KStatus,RcvStamp,SEASON)" +
                                    " VALUES (" + dbPARNUMBER + "," + dbOBJECT + "," + dbITEM + "," + dbVALUE0 + "," + dbVALUE1 + "," + dbOBJTYPE + ",'" + dbDATA_DATE + "'," + dbP2KStatus + "," + dbRcvStamp + "," + dbSEASON + ")", DBConn);
                                DBCommand.ExecuteNonQuery();
                            }
                        }
                    }
        }
        //------------------------------------------------------------------------------
        void DBWriteTRP() // запись в  БД массива TimrReperPoints[]
        {
            if (isActualDate)
            {
                try
                {
                    int tempSec = CurrSec; // запоминаем текущую секунду в сутках !!!! 
                    if (isGTP1)
                    {
                        DBWriteTRP_each(tempSec, TimeReperPoints1, 1, GTP1);
                    }
                    if (isGTP2)
                    {
                        DBWriteTRP_each(tempSec, TimeReperPoints2, 2, GTP2);
                    }
                    if (isGTP3)
                    {
                        DBWriteTRP_each(tempSec, TimeReperPoints3, 3, GTP3);
                    }
                    if (isGTP4)
                    {
                        DBWriteTRP_each(tempSec, TimeReperPoints4, 4, GTP4);
                    }
                    if (isGTP5)
                    {
                        DBWriteTRP_each(tempSec, TimeReperPoints5, 5, GTP5);
                    }
                    if (isGTP6)
                    {
                        DBWriteTRP_each(tempSec, TimeReperPoints6, 6, GTP6);
                    }
                    if (isGTP7)
                    {
                        DBWriteTRP_each(tempSec, TimeReperPoints7, 7, GTP7);
                    }
                    if (isGTP8)
                    {
                        DBWriteTRP_each(tempSec, TimeReperPoints8, 8, GTP8);
                    }
                    if (isGTP9)
                    {
                        DBWriteTRP_each(tempSec, TimeReperPoints9, 9, GTP9);
                    }
                    if (isGTP10)
                    {
                        DBWriteTRP_each(tempSec, TimeReperPoints10, 10, GTP10);
                    }
                    WriteLog(CurrSec, "Функция DBWriteTRP_each() выполнена успешно.");
                    GTPIsOk = 1;
                }
                catch (SqlException ex)
                {
                    WriteLog(CurrSec, "Ошибка выполнения функции DBWriteTRP_each().");
                    GTPIsOk = 0;
                }
            }
        }        
        //------------------------------------------------------------------------------
        void DBWriteData_each(int tempSec, Dictionary<int, double> GTP_DB, ArrayList TimeReperPoints, double[] GTP, int Item)
        {
                    SqlCommand DBCommand;
                    string dbPARNUMBER;
                    string dbOBJECT = "0";
                    string dbITEM;
                    string dbVALUE0;
                    string dbVALUE1;
                    string dbOBJTYPE = "0";
                    string dbDATA_DATE;
                    string dbP2KStatus = "0";
                    string dbRcvStamp = "GETDATE()";
                    string dbSEASON = GetCurrentSeason();

                    int sec1 = (TimeReperPoints.Count > 0) ? Convert.ToInt32(TimeReperPoints[0]) : 0;//(GTP_DB.Count > 0) ? GTP_DB.Keys.Min() : 0;
                    int sec2 = (TimeReperPoints.Count > 0) ? Convert.ToInt32(TimeReperPoints[TimeReperPoints.Count - 1]) : 0;//(GTP_DB.Count > 0) ? GTP_DB.Keys.Max() : 0;
                    if (sec1 < tempSec) // чтобы не удалить прошлые записи в базе данных
                        sec1 = tempSec;
                    if (sec1 < sec2)
                    {
                        string t1 = GetDate(sec1);
                        string t2 = GetDate(sec2);
                        dbITEM = Convert.ToString(Item);
                        // удаление данных из БД c времени  t1 по t2
                        DBCommand = new SqlCommand("DELETE FROM DATA" +
                              " WHERE ITEM = " + dbITEM + " AND  (PARNUMBER=300 OR PARNUMBER=301) AND (DATA_DATE BETWEEN '" + t1 + "' AND '" + t2 + "')", DBConn);
                        DBCommand.ExecuteNonQuery();

                        // запись  
                        //пишем в базу массив GTPx_DB[]  c текущего момента времени и до 48 ч
                        dbPARNUMBER = "300";
                        foreach (var i in GTP_DB)
                        {
                            int tempTime = i.Key;
                            if (tempTime >= tempSec)  //if (tempTime >= 3600 * (tempSec) / 3600) //
                            {
                                dbVALUE0 = dbVALUE1 = Convert.ToString(i.Value).Replace(',', '.');
                                dbDATA_DATE = GetDate(tempTime);

                                DBCommand = new SqlCommand("INSERT INTO DATA (PARNUMBER,OBJECT,ITEM,VALUE0,VALUE1,OBJTYPE,DATA_DATE,P2KStatus,RcvStamp,SEASON)" +
                                    " VALUES (" + dbPARNUMBER + "," + dbOBJECT + "," + dbITEM + "," + dbVALUE0 + "," + dbVALUE1 + "," + dbOBJTYPE + ",'" + dbDATA_DATE + "'," + dbP2KStatus + "," + dbRcvStamp + "," + dbSEASON + ")", DBConn);
                                DBCommand.ExecuteNonQuery();
                            }
                        }
                        //--------------------------------------------------------------------------
                        //пишем в базу массив TimeReperPoints
                        dbPARNUMBER = "301";
                        // запись   
                        for (int i = 0; i < TimeReperPoints.Count; i++)
                        {
                            int tempTime = Convert.ToInt32(TimeReperPoints[i]);
                            if (tempTime >= tempSec)
                            {
                                dbVALUE0 = dbVALUE1 = Convert.ToString(GTP[Convert.ToInt32(TimeReperPoints[i])]).Replace(',', '.');
                                dbDATA_DATE = GetDate(tempTime);

                                DBCommand = new SqlCommand("INSERT INTO DATA (PARNUMBER,OBJECT,ITEM,VALUE0,VALUE1,OBJTYPE,DATA_DATE,P2KStatus,RcvStamp,SEASON)" +
                                    " VALUES (" + dbPARNUMBER + "," + dbOBJECT + "," + dbITEM + "," + dbVALUE0 + "," + dbVALUE1 + "," + dbOBJTYPE + ",'" + dbDATA_DATE + "'," + dbP2KStatus + "," + dbRcvStamp + "," + dbSEASON + ")", DBConn);
                                DBCommand.ExecuteNonQuery();
                            }
                        }
                    }
                    // очистка памяти !!!!!
                    TimeReperPoints.Clear();
                    GTP_DB.Clear();
        }
        //------------------------------------------------------------------------------
        void DBWriteData() // запись БД
        { 
            if (isActualDate)
            {
                int tempSec = CurrSec; // запоминаем текущую секунду в сутках !!!!
                try
                {
                    if (isGTP1)
                    {
                        DBWriteData_each(tempSec, GTP1_DB, TimeReperPoints1, GTP1, 1);
                    }
                    if (isGTP2)
                    {
                        DBWriteData_each(tempSec, GTP2_DB, TimeReperPoints2, GTP2, 2);
                    }
                    if (isGTP3)
                    {
                        DBWriteData_each(tempSec, GTP3_DB, TimeReperPoints3, GTP3, 3);
                    }
                    if (isGTP4)
                    {
                        DBWriteData_each(tempSec, GTP4_DB, TimeReperPoints4, GTP4, 4);
                    }
                    if (isGTP5)
                    {
                        DBWriteData_each(tempSec, GTP5_DB, TimeReperPoints5, GTP5, 5);
                    }
                    if (isGTP6)
                    {
                        DBWriteData_each(tempSec, GTP6_DB, TimeReperPoints6, GTP6, 6);
                    }
                    if (isGTP7)
                    {
                        DBWriteData_each(tempSec, GTP7_DB, TimeReperPoints7, GTP7, 7);
                    }
                    if (isGTP8)
                    {
                        DBWriteData_each(tempSec, GTP8_DB, TimeReperPoints8, GTP8, 8);
                    }
                    if (isGTP9)
                    {
                        DBWriteData_each(tempSec, GTP9_DB, TimeReperPoints9, GTP9, 9);
                    }
                    if (isGTP10)
                    {
                        DBWriteData_each(tempSec, GTP10_DB, TimeReperPoints10, GTP10, 10);
                    }
                    WriteLog(CurrSec, "Функция DBWriteData_each() выполнена успешно.");
                    GTPIsOk = 1;
                }
                    catch (SqlException ex)
                {
                    WriteLog(CurrSec, "Ошибка выполнения функции DBWriteData_each().");
                    GTPIsOk = 0;
                }
            }

        }
        //--------------------------------------------------------------------------------
        void DBReadTRP_each(ArrayList TimeReperPoints, double[] GTP, double[] GTP_final, int Item)
        {
                    SqlDataReader DBReader;
                    SqlCommand DBCommand;

                    TimeReperPoints.Clear();
                    for (int i = 0; i < 172801; i++)
                    {
                        GTP[i] = 0;
                        GTP_final[i] = 0;
                    }

                    string datetimeSTART = GetDate(0);
                    DBCommand = new SqlCommand("SELECT * FROM DATA WHERE DATA_DATE BETWEEN '" + datetimeSTART +
                            "' AND DATEADD(HOUR,48,'" + datetimeSTART + "') AND (PARNUMBER = 301) AND ITEM = " + Convert.ToString(Item)
                            + "ORDER BY DATA_DATE", DBConn);
                    DBReader = DBCommand.ExecuteReader();
                    while (DBReader.Read())
                    {
                        string dt = Convert.ToString(DBReader.GetDateTime(6));
                        char[] sep = { '-', ' ', ':', '.' };
                        string[] tempLine = dt.Split(sep);
                        int d = Convert.ToInt32(tempLine[0]);
                        int m = Convert.ToInt32(tempLine[1]);
                        int y = Convert.ToInt32(tempLine[2]);
                        int hh = Convert.ToInt32(tempLine[3]);
                        int mm = Convert.ToInt32(tempLine[4]);
                        int ss = Convert.ToInt32(tempLine[5]);
                        int index = 0;
                        //проверка дня: сегодня
                        if ((d == CurrDay) && (m == CurrMonth) && (y == CurrYear))
                        {
                            index = ss + 60 * mm + 3600 * hh;
                        }
                        //проверка дня: завтра
                        if (d == GetNextDay(CurrDay, CurrMonth, CurrYear))
                        {
                            index = 86400 + ss + 60 * mm + 3600 * hh;
                        }
                        //проверка дня: 2 часа на послезавтра
                        if ((d == GetNextDay(GetNextDay(CurrDay, CurrMonth, CurrYear), CurrMonth, CurrYear)) && (pbrHour <= 2))
                        {
                            //int time = 172800 + ss + 60 * mm + 3600 * hh;
                            //if (time <= 172800)
                            //{
                            //    index = time;
                            //}
                            //else
                            //{
                            index = 172800;
                            //}
                        }

                        TimeReperPoints.Add(index); //ФОРМИРУЕМ МАССИВ РЕПЕРНЫХ ТОЧЕК ЗА 48 ЧАСОВ
                        GTP[index] = DBReader.GetDouble(3);
                        GTP_final[index] = DBReader.GetDouble(3);
                    }
                    DBReader.Close();
                    DBCommand.Dispose();

                    TimeReperPoints.Sort();

                    // заносим считанные из БД данные в  массивы GTP[] линейно
                    for (int j = 0; j < TimeReperPoints.Count - 1; j++)
                    {
                        double k = (GTP[Convert.ToInt32(TimeReperPoints[j + 1])] - GTP[Convert.ToInt32(TimeReperPoints[j])]) / (Convert.ToDouble(TimeReperPoints[j + 1]) - Convert.ToDouble(TimeReperPoints[j]));
                        double b = GTP[Convert.ToInt32(TimeReperPoints[j])] - k * Convert.ToDouble(TimeReperPoints[j]);
                        for (int i = Convert.ToInt32(TimeReperPoints[j]); i < Convert.ToInt32(TimeReperPoints[j + 1]); i++)
                            GTP[i] = k * i + b;
                    }
        }    
        //-------------------------------------------------------------------------------
        void DBReadTRP()       // чтение  из БД только TimeReperPoints 
        {
            if (isActualDate)
            {
                //getDBdata = true;
                try
                {
                    if (isGTP1)
                    {
                        DBReadTRP_each(TimeReperPoints1, GTP1, GTP1_final, 1);
                    }
                    if (isGTP2)
                    {
                        DBReadTRP_each(TimeReperPoints2, GTP2, GTP2_final, 2);
                    }
                    if (isGTP3)
                    {
                        DBReadTRP_each(TimeReperPoints3, GTP3, GTP3_final, 3);
                    }
                    if (isGTP4)
                    {
                        DBReadTRP_each(TimeReperPoints4, GTP4, GTP4_final, 4);
                    }
                    if (isGTP5)
                    {
                        DBReadTRP_each(TimeReperPoints5, GTP5, GTP5_final, 5);
                    }
                    if (isGTP6)
                    {
                        DBReadTRP_each(TimeReperPoints6, GTP6, GTP6_final, 6);
                    }
                    if (isGTP7)
                    {
                        DBReadTRP_each(TimeReperPoints7, GTP7, GTP7_final, 7);
                    }
                    if (isGTP8)
                    {
                        DBReadTRP_each(TimeReperPoints8, GTP8, GTP8_final, 8);
                    }
                    if (isGTP9)
                    {
                        DBReadTRP_each(TimeReperPoints9, GTP9, GTP9_final, 9);
                    }
                    if (isGTP10)
                    {
                        DBReadTRP_each(TimeReperPoints10, GTP10, GTP10_final, 10);
                    }

                    WriteLog(CurrSec, "Функция DBReadTRP_each() выполнена успешно.");
                    GTPIsOk = 1;
                }
                catch (Exception ex)
                {
                    WriteLog(CurrSec, "Ошибка выполнения функции DBReadTRP_each().");
                    GTPIsOk = 0;
                }
            }
        }
        //-----------------------------------------------------------------------------------
        void DBReadData_each(ArrayList TimeReperPoints, double[] GTP, double[] GTP_final, Dictionary<int, double> GTP_DB, int Item)
        { 
                 SqlDataReader DBReader;
                 SqlCommand DBCommand; 
                 TimeReperPoints.Clear();//301
                 GTP_DB.Clear();//300  
                 for (int i = 0; i < 172801; i++)
                 {
                     GTP[i] = 0.0;
                     GTP_final[i] = 0.0;
                 }

                      

                 string datetimeSTART = GetDate(0);   
                 DBCommand = new SqlCommand("SELECT * FROM DATA WHERE DATA_DATE BETWEEN '" + datetimeSTART +
                         "' AND DATEADD(SECOND,172800,'" + datetimeSTART + "') AND (PARNUMBER = 300 OR PARNUMBER = 301) AND ITEM = " + Convert.ToString(Item)
                         + "ORDER BY DATA_DATE", DBConn);
                 DBReader = DBCommand.ExecuteReader();
                 while (DBReader.Read())
                 {
                     string dt = Convert.ToString(DBReader.GetDateTime(6));
                     char[] sep = { '-', ' ', ':', '.' };
                     string[] tempLine = dt.Split(sep);
                     int d = Convert.ToInt32(tempLine[0]);
                     int m = Convert.ToInt32(tempLine[1]);
                     int y = Convert.ToInt32(tempLine[2]);
                     int hh = Convert.ToInt32(tempLine[3]);
                     int mm = Convert.ToInt32(tempLine[4]);
                     int ss = Convert.ToInt32(tempLine[5]);
                     int index = 0;
                        //проверка дня: сегодня
                     if ((d == CurrDay) && (m == CurrMonth) && (y == CurrYear))
                     {
                         index = ss + 60 * mm + 3600 * hh;
                     }
                     //проверка дня: завтра
                     if (d == GetNextDay(CurrDay, CurrMonth, CurrYear))
                     {
                         index = 86400 + ss + 60 * mm + 3600 * hh;                                   
                     }
                     //проверка дня: 2 часа на послезавтра
                     if ((d == GetNextDay(GetNextDay(CurrDay, CurrMonth, CurrYear), CurrMonth, CurrYear)) && (pbrHour <= 2))
                     {
                         //int time = 172800 + ss + 60 * mm + 3600 * hh;
                         //if (time <= 172800)
                         //{
                         //    index = time;
                         //}
                         //else
                         //{
                         index = 172800;
                         //}
                     }
                                    
                     if (DBReader.GetInt32(0) == 300) // читаем GTP_DB
                     {
                                     GTP_DB.Add(index, DBReader.GetDouble(3));
                                     GTP_final[index] = DBReader.GetDouble(3); // GTP_DB[index];
                     }
                     if (DBReader.GetInt32(0) == 301) //читаем TimeReperPoints
                     {
                                     TimeReperPoints.Add(index); //ФОРМИРУЕМ МАССИВ РЕПЕРНЫХ ТОЧЕК ЗА 48 ЧАСОВ
                                     GTP[index] = DBReader.GetDouble(3);
                     }         
                 }
                 DBReader.Close();
                 DBCommand.Dispose();

                 TimeReperPoints.Sort();
                 GTP_DB.OrderBy(x => x.Key);
                 //заносим считанные из БД данные в  массивы
                 // заполняем массив GTP[] линейно
                 for (int j = 0; j < TimeReperPoints.Count - 1; j++)
                 {
                    double k = (GTP[Convert.ToInt32(TimeReperPoints[j + 1])] - GTP[Convert.ToInt32(TimeReperPoints[j])]) / (Convert.ToDouble(TimeReperPoints[j + 1]) - Convert.ToDouble(TimeReperPoints[j]));
                    double b = GTP[Convert.ToInt32(TimeReperPoints[j])] - k * Convert.ToDouble(TimeReperPoints[j]);
                    for (int i = Convert.ToInt32(TimeReperPoints[j]); i < Convert.ToInt32(TimeReperPoints[j + 1]); i++)
                        GTP[i] = k * i + b;
                 }
                
                 // запоняем массив GTP_final[] маленькими ступеньками
                 int[] KeyArray = new int[GTP_DB.Count];
                 GTP_DB.Keys.CopyTo(KeyArray, 0);
                 for (int j = 0; j < GTP_DB.Count - 1; j++)
                 {
                     for (int i = KeyArray[j]; i < KeyArray[j + 1]; i++)
                         GTP_final[i] = GTP_final[KeyArray[j]];
                 }

                 // защита от позднего прихода графика ПБР!!!!
                 if (Convert.ToInt32(TimeReperPoints[TimeReperPoints.Count - 1]) < 172800)
                     for (int i = Convert.ToInt32(TimeReperPoints[TimeReperPoints.Count - 1]); i < 172800; i++)
                     {
                         GTP_final[i+1] = GTP_final[i];
                     }


                 //освобождаем память
                 TimeReperPoints.Clear();//301
                 GTP_DB.Clear();//300  
        }
        //-----------------------------------------------------------------------------------
        void DBReadData()       // чтение БД 
        {
            if (isActualDate)
            {
                getDBdata = true;
                try
                {
                    if (isGTP1)
                    {
                        DBReadData_each(TimeReperPoints1, GTP1, GTP1_final, GTP1_DB, 1);
                    }
                    if (isGTP2)
                    {
                        DBReadData_each(TimeReperPoints2, GTP2, GTP2_final, GTP2_DB, 2);
                    }
                    if (isGTP3)
                    {
                        DBReadData_each(TimeReperPoints3, GTP3, GTP3_final, GTP3_DB, 3);
                    }
                    if (isGTP4)
                    {
                        DBReadData_each(TimeReperPoints4, GTP4, GTP4_final, GTP4_DB, 4);
                    }
                    if (isGTP5)
                    {
                        DBReadData_each(TimeReperPoints5, GTP5, GTP5_final, GTP5_DB, 5);
                    }
                    if (isGTP6)
                    {
                        DBReadData_each(TimeReperPoints6, GTP6, GTP6_final, GTP6_DB, 6);
                    }
                    if (isGTP7)
                    {
                        DBReadData_each(TimeReperPoints7, GTP7, GTP7_final, GTP7_DB, 7);
                    }
                    if (isGTP8)
                    {
                        DBReadData_each(TimeReperPoints8, GTP8, GTP8_final, GTP8_DB, 8);
                    }
                    if (isGTP9)
                    {
                        DBReadData_each(TimeReperPoints9, GTP9, GTP9_final, GTP9_DB, 9);
                    }
                    if (isGTP10)
                    {
                        DBReadData_each(TimeReperPoints10, GTP10, GTP10_final, GTP10_DB, 10);
                    }
                    
                    WriteLog(CurrSec, "Функции DBReadData_each() выполнена успешно.");
                    DrawGTPS();
                    GTPIsOk = 1;
                }
                catch (Exception ex)
                {
                    WriteLog(CurrSec, "Ошибка выполнения функции DBReadData_each().");
                    GTPIsOk = 0;
                }
            }
        }
        //----------------------------------------------------------------------------------
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //закрываем сетевое соединение
            if (ServThread != null)
                ServThread.Abort();
            

            if (!isIniFile)
            {
                ini = new Ini.IniFile(Application.StartupPath + "\\" + IniFileName);
            }
            // пишем конфигурационнный файл

            ini.IniWriteValue("Approx", "TimeStep", comboBox2.Text);
            ini.IniWriteValue("Approx", "PowerStep", comboBox4.Text);
            ini.IniWriteValue("Approx", "SelectedIndex", Convert.ToString(comboBox1.SelectedIndex));

            //закрываем соединение с БД
            try
            {
                if (DBConn != null)
                DBConn.Close();
            }
            catch (Exception ex)
            {
                WriteLog(CurrSec, "Ошибка завершения работы с базой данных.");
            }

        }
        //------------------------------------------------------------------------------------
        private void ReadPBR(string f)
        {
            timer1.Enabled = false; // чтобы избежать коллизий
            timer2.Enabled = false;
            timer3.Enabled = false;

            ParsePBR(f); // заполнение TimeReperPoints[] новыми значениями
            if (pbrWithData && isAdminRules && (DBConn.State == ConnectionState.Open))
            {
                DBWriteTRP();// запись в БД TimeReperPoints[]
                Thread.Sleep(200);
                DBReadTRP(); // чтение из БД TimeReperPoints[] на 48 часов, заполнение массивов GTP1[]
                Thread.Sleep(200);
                lock (this)
                {
                    MakeInterpolation(); // производим интерполяцию, заполняем массив GTP1_DB[]
                }
                Thread.Sleep(200);
                DBWriteData();// пишем в базу сформированные массивы GTP1_DB[] и TimeReperPoints1[]
                Thread.Sleep(200);
                DrawGTPS();
                
            }

            timer1.Enabled = true;
            timer2.Enabled = true;
            timer3.Enabled = true;
        }
        //----------------------------------------------------------------------------
        void FillArrays(int pbrD, int pbrM, int pbrY, ArrayList TimeReperPoints, double[] GTP, double[] GTP_final, double power)
        {
            //проверка дня: сегодня
            if ((pbrD == CurrDay) && (pbrM == CurrMonth) && (pbrY == CurrYear))
            {
                TimeReperPoints.Add(3600 * pbrHour + 60 * pbrMin);
                GTP[3600 * pbrHour + 60 * pbrMin] = power;
                GTP_final[3600 * pbrHour + 60 * pbrMin] = power;
            }
            //проверка дня: завтра
            if (pbrD == GetNextDay(CurrDay, CurrMonth, CurrYear))
            {
                TimeReperPoints.Add(86400 + 3600 * pbrHour + 60 * pbrMin);
                GTP[86400 + 3600 * pbrHour + 60 * pbrMin] = power;
                GTP_final[86400 + 3600 * pbrHour + 60 * pbrMin] = power;
            }
            //проверка дня: 2 часа на послезавтра
            //if ((pbrD == GetNextDay(GetNextDay(CurrDay, CurrMonth, CurrYear), CurrMonth, CurrYear)) && (pbrHour <= 2))
            //{
                //int time = 172800 + 3600 * pbrHour + 60 * pbrMin;
                //if (time <= 172800)
                //{
                //    TimeReperPoints.Add(time);
                //    GTP[time] = power;
                //    GTP_final[time] = power;
                //}
                //else
                //{
                //TimeReperPoints.Add(172800);
                //GTP[172800] = power;
                //GTP_final[172800] = power;
                //}
            //}     
        }
        //----------------------------------------------------------------------------
        private void ParsePBR(string PBRFile)
        {
            try
            {                
                string[] lines = System.IO.File.ReadAllLines(PBRFile);

                if(isGTP1)
                    TimeReperPoints1.Clear();
                if (isGTP2)
                    TimeReperPoints2.Clear();
                if (isGTP3)
                    TimeReperPoints3.Clear();
                if (isGTP4)
                    TimeReperPoints4.Clear();
                if (isGTP5)
                    TimeReperPoints5.Clear();
                if (isGTP6)
                    TimeReperPoints6.Clear();
                if (isGTP7)
                    TimeReperPoints7.Clear();
                if (isGTP8)
                    TimeReperPoints8.Clear();
                if (isGTP9)
                    TimeReperPoints9.Clear();
                if (isGTP10)
                    TimeReperPoints10.Clear();

                pbrWithData = false;
                //анализ прихода более нового ПБР чем прежний: по дате и первому часу в задании
                //char separ1 = ';';
                //string[] temporaryLine = lines[0].Split(separ1);
                //pbrY = Convert.ToInt32(temporaryLine[1].Substring(0, 4));
                //pbrM = Convert.ToInt32(temporaryLine[1].Substring(4, 2));
                //pbrD = Convert.ToInt32(temporaryLine[1].Substring(6, 2));
                //pbrHour = Convert.ToInt32(temporaryLine[1].Substring(9, 2)) + TimeFromMoskow;
                //if (pbrHour > 24)
                //{
                //    pbrHour = pbrHour - 24;
                //    pbrD = GetNextDay(pbrD, pbrM, pbrY);
                //}
                //if ((pbrY >= LastpbrY) && (pbrM >= LastpbrM) && (pbrD >= LastpbrD) && (pbrHour >= LastpbrHour))
                //{
                    // если этот ПБР начинается с последующей даты по сравнению с предыдущим принятым
                    //LastpbrY = pbrY;
                    //LastpbrM = pbrM;
                    //LastpbrD = pbrD;
                    //LastpbrHour = pbrHour;
                
                    foreach (string line in lines)
                    {
                        char sep1 = ';';
                        string[] tempLine = line.Split(sep1);
                        pbrY = Convert.ToInt32(tempLine[1].Substring(0, 4));
                        pbrM = Convert.ToInt32(tempLine[1].Substring(4, 2));
                        pbrD = Convert.ToInt32(tempLine[1].Substring(6, 2));
                        pbrHour = Convert.ToInt32(tempLine[1].Substring(9, 2)) + TimeFromMoskow;
                        if (pbrHour > 24)
                        {
                            pbrHour = pbrHour - 24;
                            pbrD = GetNextDay(pbrD, pbrM, pbrY);
                        }
                        pbrMin = Convert.ToInt32(tempLine[1].Substring(11, 2));


                        if(Convert.ToInt32(tempLine[0]) == GTP1_ID)
                                {
                                    if(isGTP1)
                                        FillArrays(pbrD, pbrM, pbrY, TimeReperPoints1, GTP1, GTP1_final, Convert.ToDouble(tempLine[2]));
                                }
                        if(Convert.ToInt32(tempLine[0]) == GTP2_ID)
                                {
                                    if (isGTP2)
                                        FillArrays(pbrD, pbrM, pbrY, TimeReperPoints2, GTP2, GTP2_final, Convert.ToDouble(tempLine[2]));
                                }
                        if(Convert.ToInt32(tempLine[0]) == GTP3_ID)
                                {
                                    if (isGTP3)
                                        FillArrays(pbrD, pbrM, pbrY, TimeReperPoints3, GTP3, GTP3_final, Convert.ToDouble(tempLine[2]));
                                }
                        if(Convert.ToInt32(tempLine[0]) == GTP4_ID)
                                {
                                    if (isGTP4)
                                        FillArrays(pbrD, pbrM, pbrY, TimeReperPoints4, GTP4, GTP4_final, Convert.ToDouble(tempLine[2]));
                                }
                        if(Convert.ToInt32(tempLine[0]) == GTP5_ID)
                                {
                                    if (isGTP5)
                                        FillArrays(pbrD, pbrM, pbrY, TimeReperPoints5, GTP5, GTP5_final, Convert.ToDouble(tempLine[2]));
                                }
                        if(Convert.ToInt32(tempLine[0]) == GTP6_ID)
                                {
                                    if (isGTP6)
                                        FillArrays(pbrD, pbrM, pbrY, TimeReperPoints6, GTP6, GTP6_final, Convert.ToDouble(tempLine[2]));
                                }
                        if(Convert.ToInt32(tempLine[0]) == GTP7_ID)
                                {
                                    if (isGTP7)
                                        FillArrays(pbrD, pbrM, pbrY, TimeReperPoints7, GTP7, GTP7_final, Convert.ToDouble(tempLine[2]));
                                }
                        if(Convert.ToInt32(tempLine[0]) == GTP8_ID)
                                {
                                    if (isGTP8)
                                        FillArrays(pbrD, pbrM, pbrY, TimeReperPoints8, GTP8, GTP8_final, Convert.ToDouble(tempLine[2]));
                                }
                        if(Convert.ToInt32(tempLine[0]) == GTP9_ID)
                                {
                                    if (isGTP9)
                                        FillArrays(pbrD, pbrM, pbrY, TimeReperPoints9, GTP9, GTP9_final, Convert.ToDouble(tempLine[2]));
                                }
                        if (Convert.ToInt32(tempLine[0]) == GTP10_ID)
                                {
                                    if (isGTP10)
                                        FillArrays(pbrD, pbrM, pbrY, TimeReperPoints10, GTP10, GTP10_final, Convert.ToDouble(tempLine[2]));
                                }
                    } //for
                    WriteLog(CurrSec, "Полученный ПБР-файла успешно разобран.");

                    //защита от позднего прихода следующего графика на сутки !!!!
                    //if (isGTP1)
                    //{
                    //    GTP1[172800] = GTP1_final[172800] = GTP1[ Convert.ToInt32(TimeReperPoints1[TimeReperPoints1.Count - 1]) ];
                    //    if (!TimeReperPoints1.Contains(172800))
                    //        TimeReperPoints1.Add(172800);
                    //}
                    //if (isGTP2)
                    //{
                    //    GTP2[172800] = GTP2_final[172800] = GTP2[Convert.ToInt32(TimeReperPoints2[TimeReperPoints2.Count - 1])];
                    //    if (!TimeReperPoints2.Contains(172800))
                    //        TimeReperPoints2.Add(172800);
                    //}
                    //if (isGTP3)
                    //{
                    //    GTP3[172800] = GTP3_final[172800] = GTP3[Convert.ToInt32(TimeReperPoints3[TimeReperPoints3.Count - 1])];
                    //    if (!TimeReperPoints3.Contains(172800))
                    //        TimeReperPoints3.Add(172800);
                    //}
                    //if (isGTP4)
                    //{
                    //    GTP4[172800] = GTP4_final[172800] = GTP4[Convert.ToInt32(TimeReperPoints4[TimeReperPoints4.Count - 1])];
                    //    if (!TimeReperPoints4.Contains(172800))
                    //        TimeReperPoints4.Add(172800);
                    //}
                    //if (isGTP5)
                    //{
                    //    GTP5[172800] = GTP5_final[172800] = GTP5[Convert.ToInt32(TimeReperPoints5[TimeReperPoints5.Count - 1])];
                    //    if (!TimeReperPoints5.Contains(172800))
                    //        TimeReperPoints5.Add(172800);
                    //}
                    //if (isGTP6)
                    //{
                    //    GTP6[172800] = GTP6_final[172800] = GTP6[Convert.ToInt32(TimeReperPoints6[TimeReperPoints6.Count - 1])];
                    //    if (!TimeReperPoints6.Contains(172800))
                    //        TimeReperPoints6.Add(172800);
                    //}
                    //if (isGTP7)
                    //{
                    //    GTP7[172800] = GTP7_final[172800] = GTP7[Convert.ToInt32(TimeReperPoints7[TimeReperPoints7.Count - 1])];
                    //    if (!TimeReperPoints7.Contains(172800))
                    //        TimeReperPoints7.Add(172800);
                    //}
                    //if (isGTP8)
                    //{
                    //    GTP8[172800] = GTP8_final[172800] = GTP8[Convert.ToInt32(TimeReperPoints8[TimeReperPoints8.Count - 1])];
                    //    if (!TimeReperPoints8.Contains(172800))
                    //        TimeReperPoints8.Add(172800);
                    //}
                    //if (isGTP9)
                    //{
                    //    GTP9[172800] = GTP9_final[172800] = GTP9[Convert.ToInt32(TimeReperPoints9[TimeReperPoints9.Count - 1])];
                    //    if (!TimeReperPoints9.Contains(172800))
                    //        TimeReperPoints9.Add(172800);
                    //}
                    //if (isGTP10)
                    //{
                    //    GTP10[172800] = GTP10_final[172800] = GTP10[Convert.ToInt32(TimeReperPoints10[TimeReperPoints10.Count - 1])];
                    //    if (!TimeReperPoints10.Contains(172800))
                    //        TimeReperPoints10.Add(172800);
                    //}
                    // проверка ПБР-файла на наличие данных
                    if (lines.Length != 0)
                    {
                        if (((isGTP1 ? TimeReperPoints1.Count : 0) != 0) || ((isGTP2 ? TimeReperPoints2.Count : 0) != 0) ||
                            ((isGTP3 ? TimeReperPoints3.Count : 0) != 0) || ((isGTP4 ? TimeReperPoints4.Count : 0) != 0) ||
                            ((isGTP5 ? TimeReperPoints5.Count : 0) != 0) || ((isGTP6 ? TimeReperPoints6.Count : 0) != 0) ||
                            ((isGTP7 ? TimeReperPoints7.Count : 0) != 0) || ((isGTP8 ? TimeReperPoints8.Count : 0) != 0) ||
                            ((isGTP9 ? TimeReperPoints9.Count : 0) != 0) || ((isGTP10 ? TimeReperPoints10.Count : 0) != 0))
                            pbrWithData = true;
                    }
            }
            catch
            {
                WriteLog(CurrSec, "Ошибка функции разбора ПБР-файла.");
            }
        }
        //----------------------------------------------------------------------------
        int GetNextDay(int day, int month, int year)
        {
            int[] days = new int[12];
            days[0] = 31;
            if ((year % 4) == 0)
                days[1] = 29;
            else
                days[1] = 28;
            days[2] = 31;
            days[3] = 30;
            days[4] = 31;
            days[5] = 30;
            days[6] = 31;
            days[7] = 31;
            days[8] = 30;
            days[9] = 31;
            days[10] = 30;
            days[11] = 31;
            int m = month;
            if (m < 1 || m > 12)
                m = 1;
            if (day < days[m - 1])
                return day + 1;
            else
                return 1;
        }
        //----------------------------------------------------------------------------
        public void CheckAndCreateDir(string path)
        {
            if (!(Directory.Exists(path)))
            {
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch (Exception error)
                {
                    WriteLog(CurrSec, "Ошибка создания папки.");
                }
            }

        }
        //----------------------------------------------------------------------------
        private void timer2_Tick(object sender, EventArgs e)
        {
            UpdateVyr();
            // проверяем наличие csv-файла в текущей директории
            DirectoryInfo PbrDir = new DirectoryInfo(PbrDirName);
            DirectoryInfo ArchDir = new DirectoryInfo(ArchDirName);
            CheckAndCreateDir(PbrDirName);
            CheckAndCreateDir(ArchDirName); 

            FileInfo[] PBRFiles = PbrDir.GetFiles("*.csv");

            foreach (FileInfo PBRFile in PBRFiles)
            {
                //парсим ПБР-файл, а затем перемещаем его в архивную папку
                ReadPBR(PBRFile.FullName);
                try
                {
                    string path = ArchDirName +  "\\" + PBRFile.Name;
                    FileInfo[] ArchFiles = ArchDir.GetFiles();
                    foreach (FileInfo ArchFile in ArchFiles)
                    {
                        if (ArchFile.Name == PBRFile.Name)
                            ArchFile.Delete();
                    }
                    PBRFile.MoveTo(path);
                }
                catch(Exception ex)
                {
                    WriteLog(CurrSec, "Ошибка работы с *.csv файлом.");
                }
            }
        }
        //-----------------------------------------------------------------------------
        private void DrawGTPS()
        {
            // запоняем массив GTP0[]
            for (int i = 0; i < 172801; i++)
            {
                GTP0[i] = ((isGTP1) ? GTP1[i] : 0.0) + ((isGTP2) ? GTP2[i] : 0.0) + 
                          ((isGTP3) ? GTP3[i] : 0.0) + ((isGTP4) ? GTP4[i] : 0.0) + 
                          ((isGTP5) ? GTP5[i] : 0.0) + ((isGTP6) ? GTP6[i] : 0.0) + 
                          ((isGTP7) ? GTP7[i] : 0.0) + ((isGTP8) ? GTP8[i] : 0.0) + 
                          ((isGTP9) ? GTP9[i] : 0.0) + ((isGTP10) ? GTP10[i] : 0.0);
            }
            
            DrawGTP(zg0, GTP0, 0, Color.Blue, 1, 1, PGES);

            if (isGTP1)
            {
                DrawGTP(zg1, GTP1, 1, Color.Blue, 0, 1, PGTP1);
                DrawGTP(zg1, GTP1_final, 1, Color.Red, 1, 0, PGTP1);
            }
            if (isGTP2)
            {
                DrawGTP(zg2, GTP2, 2, Color.Blue, 0, 1, PGTP2);
                DrawGTP(zg2, GTP2_final, 2, Color.Red, 1, 0, PGTP2);
            }
            if (isGTP3)
            {
                DrawGTP(zg3, GTP3, 3, Color.Blue, 0, 1, PGTP3);
                DrawGTP(zg3, GTP3_final, 3, Color.Red, 1, 0, PGTP3);
            }
            if (isGTP4)
            {
                DrawGTP(zg4, GTP4, 4, Color.Blue, 0, 1, PGTP4);
                DrawGTP(zg4, GTP4_final, 4, Color.Red, 1, 0, PGTP4);
            }
            if (isGTP5)
            {
                DrawGTP(zg5, GTP5, 5, Color.Blue, 0, 1, PGTP5);
                DrawGTP(zg5, GTP5_final, 5, Color.Red, 1, 0, PGTP5);
            }
            if (isGTP6)
            {
                DrawGTP(zg6, GTP6, 6, Color.Blue, 0, 1, PGTP6);
                DrawGTP(zg6, GTP6_final, 6, Color.Red, 1, 0, PGTP6);
            }
            if (isGTP7)
            {
                DrawGTP(zg7, GTP7, 7, Color.Blue, 0, 1, PGTP7);
                DrawGTP(zg7, GTP7_final, 7, Color.Red, 1, 0, PGTP7);
            }
            if (isGTP8)
            {
                DrawGTP(zg8, GTP8, 8, Color.Blue, 0, 1, PGTP8);
                DrawGTP(zg8, GTP8_final, 8, Color.Red, 1, 0, PGTP8);
            }
            if (isGTP9)
            {
                DrawGTP(zg9, GTP9, 9, Color.Blue, 0, 1, PGTP9);
                DrawGTP(zg9, GTP9_final, 9, Color.Red, 1, 0, PGTP9);
            }
            if (isGTP10)
            {
                DrawGTP(zg10, GTP10, 10, Color.Blue, 0, 1, PGTP10);
                DrawGTP(zg10, GTP10_final, 10, Color.Red, 1, 0, PGTP10);
            }
        }
        //----------------------------------------------------------------------------
        string XAxis_ScaleFormatEvent(GraphPane pane, Axis axis, double val, int index)
        {
            int time = Convert.ToInt32(val);
            if (time < 0)
                time = 24 + (time % 24);
            else
                time = time % 24;
            return Convert.ToString(time); 
        }
        //----------------------------------------------------------------------------
        private void DrawGTP(ZedGraphControl zgc, double[] GTP, int GTPNumber, Color col, int DrawFill, int bClear, Dictionary<int,double> PReal)
        {
            GraphPane myPane = null;
            double maxValue = 0;
            double minValue = 0;
            zgc.Refresh();
            switch (GTPNumber)
            {
                case 0: { myPane = zg0.GraphPane;break;  }
                case 1: { myPane = zg1.GraphPane; break; }
                case 2: { myPane = zg2.GraphPane; break; }
                case 3: { myPane = zg3.GraphPane; break; }
                case 4: { myPane = zg4.GraphPane; break; }
                case 5: { myPane = zg5.GraphPane; break; }
                case 6: { myPane = zg6.GraphPane; break; }
                case 7: { myPane = zg7.GraphPane; break; }
                case 8: { myPane = zg8.GraphPane; break; }
                case 9: { myPane = zg9.GraphPane; break; }
                case 10: { myPane = zg10.GraphPane; break; }
            }
            if (bClear == 1)
            {
                zgc.GraphPane.CurveList.Clear();
            }
            myPane.GraphObjList.Clear();
            // Set the titles and axis labels
            switch(GTPNumber)
            {
                case 0: { myPane.Title.Text = "График нагрузки ГЭС";break; }
                case 1: { myPane.Title.Text = tabPage2.Text;break; }
                case 2: { myPane.Title.Text = tabPage3.Text;break; }
                case 3: { myPane.Title.Text = tabPage4.Text;break; }
                case 4: { myPane.Title.Text = tabPage5.Text;break; }
                case 5: { myPane.Title.Text = tabPage6.Text;break; }
                case 6: { myPane.Title.Text = tabPage7.Text;break; }
                case 7: { myPane.Title.Text = tabPage8.Text;break; }
                case 8: { myPane.Title.Text = tabPage9.Text;break; }
                case 9: { myPane.Title.Text = tabPage10.Text;break; }
                case 10: { myPane.Title.Text = tabPage11.Text;break; }
            
            }
            myPane.XAxis.Title.Text = "Время, ч";
            myPane.YAxis.Title.Text = "Мощность, МВт";
            myPane.XAxis.Type = AxisType.Linear;
            myPane.Legend.IsVisible = false;
            PointPairList MyList = new PointPairList();
            myPane.XAxis.ScaleFormatEvent += new Axis.ScaleFormatHandler(XAxis_ScaleFormatEvent);

            for (int i = 0; i < 172801; i = i + 30)
            {
                double ttt = Convert.ToDouble(i)/ 3600.0;
                MyList.Add(ttt, GTP[i]);
                if (GTP[i] > maxValue)
                    maxValue = GTP[i];
                if (GTP[i] < minValue)
                    minValue = GTP[i];
            }
            foreach (var i in PReal)
            {
                if (i.Value > maxValue)
                    maxValue = i.Value;
                if (i.Value < minValue)
                    minValue = i.Value;
            }
            LineItem myCurve = myPane.AddCurve("ГТП-" + Convert.ToString(GTPNumber), MyList, col, SymbolType.None);
            myCurve.Line.Width = 2;
            
            
            double x1 = 24.0;
            //double x2 = 48.0;
            double x3 = Convert.ToDouble(CurrSec) / 3600;
            //рисуем линии смены дат
            LineObj line1 = new LineObj(Color.Black, x1, minValue, x1, ((maxValue / 50) + 1) * 50);
            //LineObj line2 = new LineObj(Color.Black, x2, minValue, x2, ((maxValue / 50) + 1) * 50);
            //рисуем линию текущей нагрузки
            LineObj line3 = new LineObj(Color.Red, x3, minValue, x3, ((maxValue / 50) + 1) * 50);
            line1.Line.Width = 3;
            //line2.Line.Width = 3;
            line3.Line.Width = 3;
            myPane.GraphObjList.Add(line1);
            //myPane.GraphObjList.Add(line2);
            myPane.GraphObjList.Add(line3);
            TextObj text1 = new TextObj("Сегодня", 10.0, myPane.Rect.Top - 0.1*myPane.Rect.Height);
            TextObj text2 = new TextObj("Завтра", 34.0, myPane.Rect.Top - 0.1 * myPane.Rect.Height);
            text1.FontSpec.Border.IsVisible = true;
            text2.FontSpec.Border.IsVisible = true;
            text1.FontSpec.Border.Width = 2;
            text2.FontSpec.Border.Width = 2;
            text1.ZOrder = ZOrder.A_InFront;
            text2.ZOrder = ZOrder.A_InFront;
            myPane.GraphObjList.Add(text1);
            myPane.GraphObjList.Add(text2);
            //подписываем ось X
            /*
            for (int i = 1; i < 50;i++ )
            {
                TextObj TEMPtext = new TextObj(Convert.ToString(i % 24), Convert.ToDouble(i), 0.0);
                TEMPtext.FontSpec.Border.IsVisible = false;
                TEMPtext.FontSpec.Size = 10;
                myPane.GraphObjList.Add(TEMPtext);
            }
            */
            if (DrawFill == 1)
            {
                myCurve.Line.Fill = new Fill(Color.YellowGreen);
            }
            // рисуем измеренную мощность в графике нагрузки ГЭС
            LineObj linePReal;
            for (int k = 0; k < PReal.Count - 1 ;k++ )
                {
                    double t1 = Convert.ToDouble(PReal.Keys.ToArray()[k]) / 3600.0;
                    double t2 = Convert.ToDouble(PReal.Keys.ToArray()[k + 1]) / 3600.0;
                    if(k+1 < CurrSec)
                    {
                        linePReal = new LineObj(Color.Orange, t1, PReal.Values.ToArray()[k], t2, PReal.Values.ToArray()[k + 1]);
                        linePReal.Line.Width = 3;
                        myPane.GraphObjList.Add(linePReal);
                    }
                }
            
            
            // Устанавливаем интересующий нас интервал по оси X
            myPane.XAxis.Scale.Min = 0;
            myPane.XAxis.Scale.Max = 48;
            
            // Устанавливаем интересующий нас интервал по оси Y
            myPane.YAxis.Scale.Min = minValue;
            myPane.YAxis.Scale.Max = ((maxValue / 50) + 1) * 50;
            if (minValue == maxValue)
                myPane.YAxis.Scale.Max = minValue + 50;
            myPane.XAxis.MinorTic.IsAllTics = true;
            myPane.XAxis.Scale.MinorStep = 0.5;
            myPane.XAxis.Scale.MajorStep = 1.0;
            // Включаем отображение сетки напротив крупных рисок по оси X
            myPane.XAxis.MajorGrid.IsVisible = true;

            
            // Задаем вид пунктирной линии для крупных рисок по оси X:
            // Длина штрихов равна 10 пикселям, ... 
            myPane.XAxis.MajorGrid.DashOn = 10;
            myPane.XAxis.MinorGrid.DashOn = 20;
            // затем 5 пикселей - пропуск
            myPane.XAxis.MajorGrid.DashOff = 20;
            myPane.XAxis.MinorGrid.DashOff = 20;
            
            // Включаем отображение сетки напротив крупных рисок по оси Y
            myPane.YAxis.MajorGrid.IsVisible = true;

            // Аналогично задаем вид пунктирной линии для крупных рисок по оси Y
            myPane.YAxis.MajorGrid.DashOn = 10;
            myPane.YAxis.MinorGrid.DashOn = 20;

            myPane.YAxis.MajorGrid.DashOff = 20;
            myPane.YAxis.MinorGrid.DashOff = 20;

            // Включаем отображение сетки напротив мелких рисок по оси X
            myPane.YAxis.MinorGrid.IsVisible = true;

            // Задаем вид пунктирной линии для крупных рисок по оси Y: 
            // Длина штрихов равна одному пикселю, ... 
            myPane.YAxis.MinorGrid.DashOn = 1;

            // затем 2 пикселя - пропуск
            myPane.YAxis.MinorGrid.DashOff = 2;

            // Включаем отображение сетки напротив мелких рисок по оси Y
            myPane.XAxis.MinorGrid.IsVisible = true;

            // Аналогично задаем вид пунктирной линии для крупных рисок по оси Y
            myPane.XAxis.MinorGrid.DashOn = 1;
            myPane.XAxis.MinorGrid.DashOff = 2;

            // Calculate the Axis Scale Ranges
            zgc.AxisChange();
            zgc.Invalidate();
        }
        //--------------------------------------------------------------------------
        private void SetSizeZG()
        {
            zg0.Location = new Point(11, 34);
            zg1.Location = new Point(11, 34);
            zg2.Location = new Point(11, 34);
            zg3.Location = new Point(11, 34);
            zg4.Location = new Point(11, 34);
            zg5.Location = new Point(11, 34);
            zg6.Location = new Point(11, 34);
            zg7.Location = new Point(11, 34);
            zg8.Location = new Point(11, 34);
            zg9.Location = new Point(11, 34);
            zg10.Location = new Point(11, 34);
            // Leave a small margin around the outside of the control
            tabControl1.Size = new Size(this.ClientSize.Width-5, this.ClientSize.Height - 120);
            dataGridView1.Size = new Size(714, this.tabControl1.Height - 50);

            label1.Location = new Point(Convert.ToInt32(tabControl1.Width * 0.5), Convert.ToInt32(tabPage3.Top - 30));//ГТП-1
            label2.Location = new Point(Convert.ToInt32(tabControl1.Width * 0.5), Convert.ToInt32(tabPage3.Top - 30));//ГТП-2
            label3.Location = new Point(Convert.ToInt32(tabControl1.Width * 0.5), Convert.ToInt32(tabPage4.Top - 30));//ГТП-3
            label13.Location = new Point(Convert.ToInt32(tabControl1.Width * 0.5), Convert.ToInt32(tabPage5.Top - 30));//ГТП-4
            label14.Location = new Point(Convert.ToInt32(tabControl1.Width * 0.5), Convert.ToInt32(tabPage6.Top - 30));//ГТП-5
            label15.Location = new Point(Convert.ToInt32(tabControl1.Width * 0.5), Convert.ToInt32(tabPage7.Top - 30));//ГТП-6
            label16.Location = new Point(Convert.ToInt32(tabControl1.Width * 0.5), Convert.ToInt32(tabPage8.Top - 30));//ГТП-7
            label17.Location = new Point(Convert.ToInt32(tabControl1.Width * 0.5), Convert.ToInt32(tabPage9.Top - 30));//ГТП-8
            label18.Location = new Point(Convert.ToInt32(tabControl1.Width * 0.5), Convert.ToInt32(tabPage10.Top - 30));//ГТП-9
            label19.Location = new Point(Convert.ToInt32(tabControl1.Width * 0.5), Convert.ToInt32(tabPage11.Top - 30));//ГТП-10

            label20.Location = new Point(Convert.ToInt32(tabControl1.Width * 0.5), Convert.ToInt32(tabPage12.Top - 30));//ГТП-ГЭС

            zg0.Size = new Size(this.tabControl1.Width - 20, this.tabControl1.Height - 50);
            zg0.AxisChange();
            zg0.Invalidate();
            zg1.Size = new Size(this.tabControl1.Width - 20, this.tabControl1.Height - 50);
            zg1.AxisChange();
            zg1.Invalidate();
            zg2.Size = new Size(this.tabControl1.Width - 20, this.tabControl1.Height - 50);
            zg2.AxisChange();
            zg2.Invalidate();
            zg3.Size = new Size(this.tabControl1.Width - 20, this.tabControl1.Height - 50);
            zg3.AxisChange();
            zg3.Invalidate();
            zg4.Size = new Size(this.tabControl1.Width - 20, this.tabControl1.Height - 50);
            zg4.AxisChange();
            zg4.Invalidate();
            zg5.Size = new Size(this.tabControl1.Width - 20, this.tabControl1.Height - 50);
            zg5.AxisChange();
            zg5.Invalidate();
            zg6.Size = new Size(this.tabControl1.Width - 20, this.tabControl1.Height - 50);
            zg6.AxisChange();
            zg6.Invalidate();
            zg7.Size = new Size(this.tabControl1.Width - 20, this.tabControl1.Height - 50);
            zg7.AxisChange();
            zg7.Invalidate();
            zg8.Size = new Size(this.tabControl1.Width - 20, this.tabControl1.Height - 50);
            zg8.AxisChange();
            zg8.Invalidate();
            zg9.Size = new Size(this.tabControl1.Width - 20, this.tabControl1.Height - 50);
            zg9.AxisChange();
            zg9.Invalidate();
            zg10.Size = new Size(this.tabControl1.Width - 20, this.tabControl1.Height - 50);
            zg10.AxisChange();
            zg10.Invalidate();
        }
        //--------------------------------------------------------------------------
        private void Form1_Resize(object sender, EventArgs e)
        {
            SetSizeZG();
        }
        //--------------------------------------------------------------------------
        private string zg1_PointValueEvent(ZedGraphControl sender, GraphPane pane, CurveItem curve, int iPt)
        {
            string result;
            PointPair point = curve[iPt];
            result = string.Format("Время: {0}\nМощность: {1:F3}", Convert.ToString(Math.Floor(point.X % 24)).PadLeft(2,'0') + ":" + Convert.ToString(Math.Floor((point.X % 1) *60)).PadLeft(2,'0'), point.Y);
            return result;
        }
        //--------------------------------------------------------------------------
        private string zg2_PointValueEvent(ZedGraphControl sender, GraphPane pane, CurveItem curve, int iPt)
        {
            string result;
            PointPair point = curve[iPt];
            result = string.Format("Время: {0}\nМощность: {1:F3}", Convert.ToString(Math.Floor(point.X % 24)).PadLeft(2, '0') + ":" + Convert.ToString(Math.Floor((point.X % 1) * 60)).PadLeft(2, '0'), point.Y);
            return result;
        }
        //--------------------------------------------------------------------------
        private string zg3_PointValueEvent(ZedGraphControl sender, GraphPane pane, CurveItem curve, int iPt)
        {
            string result;
            PointPair point = curve[iPt];
            result = string.Format("Время: {0}\nМощность: {1:F3}", Convert.ToString(Math.Floor(point.X % 24)).PadLeft(2, '0') + ":" + Convert.ToString(Math.Floor((point.X % 1) * 60)).PadLeft(2, '0'), point.Y);
            return result;
        }
        //------------------------------------------------------------------------
        private void MakeTimeAppr(ArrayList TimeReperPoints, double[] GTP_final, Dictionary<int, double> GTP_DB)
        {
            // ищем ближайшую реперную точку
            int StartPoint;
            int EndPoint;
            double IdealSquare;         // плановая выработка
            double InternalPowerStep;   // внутренняя ступенька по мощности в МВт
            double InternalStepNumber;  // количество внутренних ступенек по мощности
            int tempTimeStep;           // локальная переменная с величиной шага по времени
            GlobalTimeStep = 30;        // по умолчанию
            for (int j = 0; j < TimeReperPoints.Count - 1; j++)  // в данном цикле обрабатывается каждый скачок
            {
                StartPoint = Convert.ToInt32(TimeReperPoints[j]);
                EndPoint = Convert.ToInt32(TimeReperPoints[j + 1]);
               //if (StartPoint > CurrSec)
               //{
                    if (GTP_final[StartPoint] == GTP_final[EndPoint])
                        for (int k = StartPoint + 1; k < EndPoint; k++)
                            GTP_final[k] = GTP_final[StartPoint];
                    else
                    {
                        IdealSquare = 0.5 * Math.Abs(GTP_final[EndPoint] - GTP_final[StartPoint]) * (Convert.ToDouble(EndPoint - StartPoint) / 3600.0);

                        tempTimeStep = TimeStep; //период набора мощности для участка 0-35 МВт
                        // ищем наименьший  шаг по времени
                        //if (GlobalTimeStep < TimeStep)
                        //    GlobalTimeStep = TimeStep;
                        InternalStepNumber = (EndPoint - StartPoint) / tempTimeStep;
                        InternalPowerStep = (GTP_final[EndPoint] - GTP_final[StartPoint]) / InternalStepNumber;

                        if ((GTP_final[StartPoint] == 0) && (GTP_final[EndPoint] >= ForbiddenZone))
                        {
                            // если есть переход через границу 35 МВт снизу вверх
                            // ищем целое количество ступенек n которое успеем сделать после 35 МВт
                            // n+1 - добаляем ещё одну "нестандартную по высоте ступеньку"
                            int MidPoint;//серединка большой ступеньки 0->35
                            int NormStepsNumber; // количество "нормальных" ступенек которое успеем сделать
                            int StartNormStepTime; //время начала формирования нормальных ступенек; StartNormStepTime - tempTimeStep - время формирования нестандартной ступеньки 
                            double SmallPowerStep;// величина "нестандартной" ступеньки
                            NormStepsNumber = Convert.ToInt32(Math.Floor((GTP_final[EndPoint] - ForbiddenZone) / Math.Abs(InternalPowerStep)));
                            StartNormStepTime = EndPoint - NormStepsNumber * tempTimeStep;
                            SmallPowerStep = GTP_final[EndPoint] - NormStepsNumber * InternalPowerStep - ForbiddenZone;
                            //-----------------------------------------------------------------
                            if (SmallPowerStep == 0) // строим график без маленькой ступеньки
                            {
                                // большая ступенька перехода 0 ->35
                                MidPoint = (StartPoint + StartNormStepTime) / 2;
                                for (int h = StartPoint; (h < StartNormStepTime) && (h <= 172800); h++)
                                {
                                    if (h < MidPoint)
                                    {
                                        GTP_final[h] = 0;
                                    }
                                    else
                                    {
                                        GTP_final[h] = ForbiddenZone;
                                    }
                                }
                                // норамльные ступеньки
                                for (int i = StartNormStepTime; i < EndPoint; i++)
                                {
                                    if ((i - StartNormStepTime) % tempTimeStep == 0)
                                    {
                                        for (int k = i; (k < i + (tempTimeStep / 2)) && (k <= 172800); k++)
                                            GTP_final[k] = GTP_final[k - 1];
                                        for (int k = i + (tempTimeStep / 2); (k < i + tempTimeStep) && (k <= 172800); k++)
                                            GTP_final[k] = GTP_final[i] + InternalPowerStep;
                                    }
                                }
                            }
                            //------------------------------------------------------------------
                            else   // строим график с маленькой ступенькой
                            {
                                // большая ступенька перехода 0 ->35
                                MidPoint = (StartPoint + StartNormStepTime - tempTimeStep) / 2;
                                for (int h = StartPoint; (h < StartNormStepTime - tempTimeStep) && (h <= 172800); h++)
                                {
                                    if (h < MidPoint)
                                    {
                                        GTP_final[h] = 0;
                                    }
                                    else
                                    {
                                        GTP_final[h] = ForbiddenZone;
                                    }
                                }

                                // собственно сама маленькая ступенька
                                for (int i = StartNormStepTime - tempTimeStep; i < StartNormStepTime; i++)
                                {
                                    if ((i - (StartNormStepTime - tempTimeStep)) % tempTimeStep == 0)
                                    {
                                        for (int k = i; (k < i + (tempTimeStep / 2)) && (k <= 172800); k++)
                                            GTP_final[k] = GTP_final[k - 1];
                                        for (int k = i + (tempTimeStep / 2); (k < i + tempTimeStep) && (k <= 172800); k++)
                                            GTP_final[k] = GTP_final[i] + SmallPowerStep;
                                    }
                                }

                                // норамльные ступеньки
                                for (int i = StartNormStepTime; i < EndPoint; i++)
                                {
                                    if ((i - StartNormStepTime) % tempTimeStep == 0)
                                    {
                                        for (int k = i; (k < i + (tempTimeStep / 2)) && (k <= 172800); k++)
                                            GTP_final[k] = GTP_final[k - 1];
                                        for (int k = i + (tempTimeStep / 2); (k < i + tempTimeStep) && (k <= 172800); k++)
                                            GTP_final[k] = GTP_final[i] + InternalPowerStep;
                                    }
                                }
                            }
                            //----------------------------------------------------------------
                        }
                        else
                        {
                            if ((GTP_final[EndPoint] == 0) && (GTP_final[StartPoint] >= ForbiddenZone))
                            {
                                //если есть переход через границу 35 МВт сверху вниз
                                // ищем целое количество ступенек n которое успеем сделать до 35 МВт
                                int MidPoint;//серединка большой ступеньки 35->0
                                int NormStepsNumber; // количество "нормальных" ступенек которое успеем сделать
                                int StartSmallStepTime; // время формирования нестандартной ступеньки 
                                double SmallPowerStep;// величина "нестандартной" ступеньки
                                NormStepsNumber = Convert.ToInt32(Math.Floor((GTP_final[StartPoint] - ForbiddenZone) / Math.Abs(InternalPowerStep)));
                                SmallPowerStep = GTP_final[StartPoint] - NormStepsNumber * Math.Abs(InternalPowerStep) - ForbiddenZone;
                                StartSmallStepTime = StartPoint + NormStepsNumber * tempTimeStep;
                                //-----------------------------------------------------------------
                                if (SmallPowerStep == 0) // строим график без маленькой ступеньки
                                {
                                    // нормальные ступеньки
                                    for (int i = StartPoint + 1; i < StartSmallStepTime; i++)
                                    {
                                        if ((i - (StartPoint + 1)) % tempTimeStep == 0)
                                        {
                                            for (int k = i; (k < i + (tempTimeStep / 2)) && (k <= 172800); k++)
                                                GTP_final[k] = GTP_final[k - 1];
                                            for (int k = i + (tempTimeStep / 2); (k < i + tempTimeStep) && (k <= 172800); k++)
                                                GTP_final[k] = GTP_final[i] + InternalPowerStep;
                                        }
                                    }
                                    // большая ступенька перехода 35 ->0
                                    MidPoint = (EndPoint + StartSmallStepTime) / 2;
                                    for (int h = StartSmallStepTime; (h < EndPoint) && (h <= 172800); h++)
                                    {
                                        if (h < MidPoint)
                                        {
                                            GTP_final[h] = GTP_final[h - 1];
                                        }
                                        else
                                        {
                                            GTP_final[h] = 0;
                                        }
                                    }
                                }
                                //------------------------------------------------------------
                                else   // строим график с маленькой ступенькой
                                {
                                    // нормальные ступеньки
                                    for (int i = StartPoint + 1; i < StartSmallStepTime; i++)
                                    {
                                        if ((i - (StartPoint + 1)) % tempTimeStep == 0)
                                        {
                                            for (int k = i; (k < i + (tempTimeStep / 2)) && (k <= 172800); k++)
                                                GTP_final[k] = GTP_final[k - 1];
                                            for (int k = i + (tempTimeStep / 2); (k < i + tempTimeStep) && (k <= 172800); k++)
                                                GTP_final[k] = GTP_final[i] + InternalPowerStep;
                                        }
                                    }

                                    // собственно сама маленькая ступенька
                                    for (int i = StartSmallStepTime; i < StartSmallStepTime + tempTimeStep; i++)
                                    {
                                        if ((i - StartSmallStepTime) % tempTimeStep == 0)
                                        {
                                            for (int k = i; (k < i + (tempTimeStep / 2)) && (k <= 172800); k++)
                                                GTP_final[k] = GTP_final[k - 1];
                                            for (int k = i + (tempTimeStep / 2); (k < i + tempTimeStep) && (k <= 172800); k++)
                                                GTP_final[k] = GTP_final[i] - SmallPowerStep;  //!!!!!!!!!!!!!
                                        }
                                    }

                                    // большая ступенька перехода 35 ->0
                                    MidPoint = (EndPoint + StartSmallStepTime + tempTimeStep) / 2;
                                    for (int h = StartSmallStepTime + tempTimeStep; (h < EndPoint) && (h <= 172800); h++)
                                    {
                                        if (h < MidPoint)
                                        {
                                            GTP_final[h] = GTP_final[h - 1];
                                        }
                                        else
                                        {
                                            GTP_final[h] = 0;
                                        }
                                    }
                                }
                            }
                            else // нет перехода через границу 35 мвт !!!!!!!!!!!!!!!!)))))))))
                            {
                                for (int i = StartPoint + 1; i < EndPoint; i++)
                                {
                                    if ((i - (StartPoint + 1)) % tempTimeStep == 0)
                                    {
                                        for (int k = i; (k < i + (tempTimeStep / 2)) && (k <= 172800); k++)
                                            GTP_final[k] = GTP_final[k - 1];
                                        for (int k = i + (tempTimeStep / 2); (k < i + tempTimeStep) && (k <= 172800); k++)
                                            GTP_final[k] = GTP_final[i] + InternalPowerStep;
                                    }
                                }
                            }
                        }
                    }

                    // дискретизация графика
                    if ( !GTP_DB.ContainsKey(StartPoint) )
                        GTP_DB.Add(StartPoint, GTP_final[StartPoint]);
                    for (int i = StartPoint + 1; i < EndPoint; i++)
                    {
                        if (GTP_final[i] != GTP_final[i-1])
                            GTP_DB.Add(i, GTP_final[i]);
                    }
                    if (!GTP_DB.ContainsKey(EndPoint))
                        GTP_DB.Add(EndPoint, GTP_final[EndPoint]);
            } //for (int j = 0; j < TimeReperPoints.Count - 1; j++)
            // защита от позднего прихода графика!!!  

            if (Convert.ToInt32(TimeReperPoints[TimeReperPoints.Count - 1]) < 172800)
                for (int i = 1 + Convert.ToInt32(TimeReperPoints[TimeReperPoints.Count - 1]); i <= 172800; i++)
                {
                    GTP_final[i] = GTP_final[i-1];
                }
        }
        //-------------------------------------------------------------------------
        private void MakePowerAppr(ArrayList TimeReperPoints, double[] GTP_final, Dictionary<int, double> GTP_DB)
        {
            // ищем ближайшую реперную точку
            int StartPoint;
            int EndPoint;
            double IdealSquare;         // плановая выработка
            int InternalTimeStep;   // внутренняя ступенька по времени в сек.
            int InternalStepNumber;  // количество внутренних ступенек 
            double tempPowerStep;           // локальная переменная с величиной шага по мощности
            GlobalTimeStep = 30;            // по умолчанию
            // заполняем полностью массив GTP_final[], а затем из него берем выборки с периодом GlobalTimeStep/2 и добавляем их в массив GTP_DB
            for (int j = 0; j < TimeReperPoints.Count - 1; j++)  // в данном цикле обрабатывается каждый скачок
            {
                StartPoint = Convert.ToInt32(TimeReperPoints[j]);
                EndPoint = Convert.ToInt32(TimeReperPoints[j + 1]);
                //if (StartPoint > CurrSec)
                //{
                // ищем наименьший  шаг по времени
                if (GTP_final[StartPoint] == GTP_final[EndPoint])
                    for (int k = StartPoint + 1; k < EndPoint; k++)
                        GTP_final[k] = GTP_final[StartPoint];
                else
                {
                    tempPowerStep = Convert.ToDouble(PowerStep); //величина ступени подъема мощности
                    InternalStepNumber = Convert.ToInt32(Math.Ceiling(Math.Abs((GTP_final[EndPoint] - GTP_final[StartPoint]) / tempPowerStep)));
                    InternalTimeStep = Convert.ToInt32(Math.Floor(((decimal)EndPoint - (decimal)StartPoint) / (decimal)InternalStepNumber));
                    // ищем наименьший  шаг по времени
                    //if (GlobalTimeStep < InternalTimeStep)
                    //    GlobalTimeStep = InternalTimeStep;
                    IdealSquare = 0.5 * Math.Abs(GTP_final[EndPoint] - GTP_final[StartPoint]) * (Convert.ToDouble(EndPoint - StartPoint) / 3600.0);
                    if ((GTP_final[StartPoint] == 0) && (GTP_final[EndPoint] >= ForbiddenZone)) 
                    {
                        // если есть переход через границу 35 МВт снизу вверх
                        int MidPoint;//серединка большой ступеньки 0->35
                        int NormStepsNumber; // количество "нормальных" ступенек которое успеем сделать
                        int StartNormStepTime; //время начала формирования нормальных ступенек; StartNormStepTime - tempTimeStep - время формирования нестандартной ступеньки 
                        double SmallPowerStep;// величина "нестандартной" ступеньки
                        NormStepsNumber = Convert.ToInt32(Math.Floor((GTP_final[EndPoint] - ForbiddenZone) / Math.Abs(tempPowerStep)));
                        StartNormStepTime = EndPoint - NormStepsNumber * InternalTimeStep;
                        SmallPowerStep = GTP_final[EndPoint] - NormStepsNumber * tempPowerStep - ForbiddenZone;
                        //-----------------------------------------------------------------
                        if (SmallPowerStep == 0) // строим график без маленькой ступеньки
                        {
                            // большая ступенька перехода 0 ->35
                            MidPoint = (StartPoint + StartNormStepTime) / 2;
                            for (int h = StartPoint; (h < StartNormStepTime) && (h <= 172800); h++)
                            {
                                if (h < MidPoint)
                                {
                                    GTP_final[h] = 0;
                                }
                                else
                                {
                                    GTP_final[h] = ForbiddenZone;
                                }
                            }
                            // норамльные ступеньки
                            for (int i = StartNormStepTime; i < EndPoint; i++)
                            {
                                if ((i - StartNormStepTime) % InternalTimeStep == 0)
                                {
                                    for (int k = i; (k < i + (InternalTimeStep / 2)) && (k <=172800) ; k++)
                                        GTP_final[k] = GTP_final[k - 1];
                                    for (int k = i + (InternalTimeStep / 2); (k < i + InternalTimeStep) && (k <= 172800); k++)
                                        GTP_final[k] = GTP_final[i] + tempPowerStep;
                                }
                            }
                        }
                        //------------------------------------------------------------------
                        else   // строим график с маленькой ступенькой
                        {
                            // большая ступенька перехода 0 ->35
                            MidPoint = (StartPoint + StartNormStepTime - InternalTimeStep) / 2;
                            for (int h = StartPoint; (h < StartNormStepTime - InternalTimeStep) && (h <= 172800); h++)
                            {
                                if (h < MidPoint)
                                {
                                    GTP_final[h] = 0;
                                }
                                else
                                {
                                    GTP_final[h] = ForbiddenZone;
                                }
                            }

                            // собственно сама маленькая ступенька
                            for (int i = StartNormStepTime - InternalTimeStep; i < StartNormStepTime; i++)
                            {
                                if ((i - (StartNormStepTime - InternalTimeStep)) % InternalTimeStep == 0)
                                {
                                    for (int k = i; (k < i + (InternalTimeStep / 2)) && (k <= 172800); k++)
                                        GTP_final[k] = GTP_final[k - 1];
                                    for (int k = i + (InternalTimeStep / 2); (k < i + InternalTimeStep) && (k <= 172800); k++)
                                        GTP_final[k] = GTP_final[i] + SmallPowerStep;
                                }
                            }

                            // норамльные ступеньки
                            for (int i = StartNormStepTime; i < EndPoint; i++)
                            {
                                if ((i - StartNormStepTime) % InternalTimeStep == 0)
                                {
                                    for (int k = i; (k < i + (InternalTimeStep / 2)) && (k <= 172800); k++)
                                        GTP_final[k] = GTP_final[k - 1];
                                    for (int k = i + (InternalTimeStep / 2); (k < i + InternalTimeStep) && (k <= 172800); k++)
                                        GTP_final[k] = GTP_final[i] + tempPowerStep;
                                }
                            }
                        }
                        //------------------------------------------------------------------------
                    }
                    else   
                    {
                        if ((GTP_final[EndPoint] == 0) && (GTP_final[StartPoint] >= ForbiddenZone))
                        {
                            // если есть переход через границу 35 МВт сверху вниз
                            int MidPoint;//серединка большой ступеньки 35->0
                            int NormStepsNumber; // количество "нормальных" ступенек которое успеем сделать
                            int StartSmallStepTime; // время формирования нестандартной ступеньки 
                            double SmallPowerStep;// величина "нестандартной" ступеньки
                            NormStepsNumber = Convert.ToInt32(Math.Floor((GTP_final[StartPoint] - ForbiddenZone) / Math.Abs(tempPowerStep)));
                            SmallPowerStep = GTP_final[StartPoint] - NormStepsNumber * Math.Abs(tempPowerStep) - ForbiddenZone;
                            StartSmallStepTime = StartPoint + NormStepsNumber * InternalTimeStep;
                            //-----------------------------------------------------------------
                            if (SmallPowerStep == 0) // строим график без маленькой ступеньки
                            {
                                // нормальные ступеньки
                                for (int i = StartPoint + 1; i < StartSmallStepTime; i++)
                                {
                                    if ((i - (StartPoint + 1)) % InternalTimeStep == 0)
                                    {
                                        for (int k = i; (k < i + (InternalTimeStep / 2)) && (k <= 172800); k++)
                                            GTP_final[k] = GTP_final[k - 1];
                                        for (int k = i + (InternalTimeStep / 2); (k < i + InternalTimeStep) && (k <= 172800); k++)
                                            GTP_final[k] = GTP_final[i] - tempPowerStep;        //!!!
                                    }
                                }
                                // большая ступенька перехода 35 ->0
                                MidPoint = (EndPoint + StartSmallStepTime) / 2;
                                for (int h = StartSmallStepTime; (h < EndPoint) && (h <= 172800); h++)
                                {
                                    if (h < MidPoint)
                                    {
                                        GTP_final[h] = GTP_final[h - 1];
                                    }
                                    else
                                    {
                                        GTP_final[h] = 0;
                                    }
                                }
                            }
                            //------------------------------------------------------------
                            else   // строим график с маленькой ступенькой
                            {
                                // нормальные ступеньки
                                for (int i = StartPoint + 1; i < StartSmallStepTime; i++)
                                {
                                    if ((i - (StartPoint + 1)) % InternalTimeStep == 0)
                                    {
                                        for (int k = i; (k < i + (InternalTimeStep / 2)) && (k <= 172800); k++)
                                            GTP_final[k] = GTP_final[k - 1];
                                        for (int k = i + (InternalTimeStep / 2); (k < i + InternalTimeStep) && (k <=172800); k++)
                                            GTP_final[k] = GTP_final[i] - tempPowerStep;  //!!!!
                                    }
                                }

                                // собственно сама маленькая ступенька
                                for (int i = StartSmallStepTime; i < StartSmallStepTime + InternalTimeStep; i++)
                                {
                                    if ((i - StartSmallStepTime) % InternalTimeStep == 0)
                                    {
                                        for (int k = i; (k < i + (InternalTimeStep / 2)) && (k <= 172800); k++)
                                            GTP_final[k] = GTP_final[k - 1];
                                        for (int k = i + (InternalTimeStep / 2); (k < i + InternalTimeStep) && (k <= 172800); k++)
                                            GTP_final[k] = GTP_final[i] - SmallPowerStep; //!!!!!!!
                                    }
                                }

                                // большая ступенька перехода 35 ->0
                                MidPoint = (EndPoint + StartSmallStepTime + InternalTimeStep) / 2;
                                for (int h = StartSmallStepTime + InternalTimeStep; (h < EndPoint) && (h <= 172800); h++)
                                {
                                    if (h < MidPoint)
                                    {
                                        GTP_final[h] = GTP_final[h - 1];
                                    }
                                    else
                                    {
                                        GTP_final[h] = 0;
                                    }
                                }
                            } 

                        }
                        else
                        {
                            // нет перехода через границу 35 мвт
                            for (int i = StartPoint + 1; i < EndPoint; i++)
                            {
                                if ((i - (StartPoint + 1)) % InternalTimeStep == 0)
                                {
                                    if (GTP_final[EndPoint] > GTP_final[StartPoint]) 
                                    {
                                        // ступенька положительная
                                        if (GTP_final[i - 1] + tempPowerStep <= GTP_final[EndPoint])
                                        {
                                            for (int k = i; (k < i + (InternalTimeStep / 2)) && (k <= 172800); k++)
                                                GTP_final[k] = GTP_final[k - 1];
                                            for (int k = i + (InternalTimeStep / 2); (k < i + InternalTimeStep) && (k <= 172800); k++)
                                                GTP_final[k] = GTP_final[i] + tempPowerStep;
                                        }
                                        else
                                        {
                                            for (int k = i; (k < i + (InternalTimeStep / 2)) && (k <= 172800); k++)
                                                GTP_final[k] = GTP_final[k - 1];
                                            for (int k = i + (InternalTimeStep / 2); (k < i + InternalTimeStep) && (k <= 172800); k++)
                                                GTP_final[k] = GTP_final[EndPoint];
                                        }
                                    }
                                    else  // ступенька отрицательная
                                    {
                                        if (GTP_final[i - 1] - tempPowerStep >= GTP_final[EndPoint])
                                        {
                                            for (long k = i; (k < i + (InternalTimeStep / 2)) && (k <= 172800); k++)
                                                GTP_final[k] = GTP_final[k - 1];
                                            for (long k = i + (InternalTimeStep / 2); (k < i + InternalTimeStep) && (k <= 172800); k++)
                                                GTP_final[k] = GTP_final[i] - tempPowerStep; //!!!!
                                        }
                                        else
                                        {
                                            for (long k = i; (k < i + (InternalTimeStep / 2)) && (k <= 172800); k++)
                                                GTP_final[k] = GTP_final[k - 1];
                                            for (long k = i + (InternalTimeStep / 2); (k < i + InternalTimeStep) && (k <= 172800); k++)
                                                GTP_final[k] = GTP_final[EndPoint];
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // дискретизация графика
                if (!GTP_DB.ContainsKey(StartPoint))
                    GTP_DB.Add(StartPoint, GTP_final[StartPoint]);
                for (int i = StartPoint + 1; i < EndPoint; i++)
                {
                    if (GTP_final[i] != GTP_final[i - 1])
                        GTP_DB.Add(i, GTP_final[i]);
                }
                if (!GTP_DB.ContainsKey(EndPoint))
                    GTP_DB.Add(EndPoint, GTP_final[EndPoint]); 

            }
            // защита от позднего прихода графика!!!  
            if (Convert.ToInt32(TimeReperPoints[TimeReperPoints.Count - 1]) < 172800)
                for (int i = 1 + Convert.ToInt32(TimeReperPoints[TimeReperPoints.Count - 1]); i <= 172800; i++)
                {
                    GTP_final[i] = GTP_final[i - 1];
                }
        }
        //-------------------------------------------------------------------------
        private void MakeLinearAppr(ArrayList TimeReperPoints, double[] GTP_final, Dictionary<int, double> GTP_DB)
        {
            GlobalTimeStep = 60;//!!!! по умолчанию для линейного графика
            double IdealSquare;// плановая выработка
            for (int i = 0; i < TimeReperPoints.Count - 1; i++)  // в данном цикле обрабатывается каждый скачок
            {
                // ищем ближайшую реперную точку
                int StartPoint;
                int EndPoint;     
                StartPoint = Convert.ToInt32(TimeReperPoints[i]);
                EndPoint = Convert.ToInt32(TimeReperPoints[i + 1]);
                //if (StartPoint > CurrSec)
                //{
                    IdealSquare = 0.5 * Math.Abs(GTP_final[EndPoint] - GTP_final[StartPoint]) * (Convert.ToDouble(EndPoint - StartPoint));
               
                    if (GTP_final[StartPoint] == GTP_final[EndPoint])
                        for (int j = StartPoint + 1; j < EndPoint; j++)
                            GTP_final[j] = GTP_final[StartPoint];
                    else
                    {
                        if ((GTP_final[StartPoint] == 0) && (GTP_final[EndPoint] >= ForbiddenZone))
                        {
                            // если есть переход через границу 35 МВт снизу вверх
                            int MidPoint;//точка излома графика
                            double PoluSummaOsnov = 0.5 * (ForbiddenZone + GTP_final[EndPoint]);
                            MidPoint = Convert.ToInt32(Math.Floor(EndPoint - (IdealSquare / PoluSummaOsnov)));

                            //строим нулевой участок
                            for (int j = StartPoint; (j < MidPoint) && (j <= 172800); j++)
                                GTP_final[j] = 0.0;

                            // строим наклонный участок
                            double k = (GTP_final[EndPoint] - ForbiddenZone) / (EndPoint - MidPoint);
                            double b = ForbiddenZone - k * MidPoint;
                            for (int j = MidPoint; (j < EndPoint) && (j <= 172800); j++)
                                GTP_final[j] = k * j + b;
                        }
                        else
                        {
                            if ((GTP_final[EndPoint] == 0) && (GTP_final[StartPoint] >= ForbiddenZone))
                            {
                                // если есть переход через границу 35 МВт сверху вниз
                                int MidPoint;//точка излома графика
                                double PoluSummaOsnov = 0.5 * (ForbiddenZone + GTP_final[StartPoint]);
                                MidPoint = Convert.ToInt32(Math.Floor((IdealSquare / PoluSummaOsnov) + StartPoint));

                                // строим наклонный участок
                                double k = (ForbiddenZone - GTP_final[StartPoint]) / (MidPoint - StartPoint);
                                double b = GTP_final[StartPoint] - k * StartPoint;
                                for (int j = StartPoint; (j < MidPoint) && (j <= 172800); j++)
                                    GTP_final[j] = k * j + b;

                                //строим нулевой участок
                                for (int j = MidPoint; (j < EndPoint) && (j <= 172800); j++)
                                    GTP_final[j] = 0.0;
                            }
                            else
                            {
                                // если нет перехода через границу 35 МВт
                                //строим нулевой участок
                                double k = (GTP_final[EndPoint] - GTP_final[StartPoint]) / (EndPoint - StartPoint);
                                double b = GTP_final[StartPoint] - k * StartPoint;
                                for (int j = StartPoint; (j < EndPoint) && (j <= 172800); j++)
                                    GTP_final[j] = k * j + b;
                            }
                   
                    }
                }
                //for (int m = StartPoint; m < EndPoint; m += GlobalTimeStep / 2)
                //{
                //    GTP_DB.Add(m, GTP_final[m]);
                //}
                    // дискретизация графика
                    if (!GTP_DB.ContainsKey(StartPoint))
                        GTP_DB.Add(StartPoint, GTP_final[StartPoint]);
                    for (int m = StartPoint + 1; m < EndPoint; m++)
                    {
                        if (GTP_final[m] != GTP_final[m - 1])
                            GTP_DB.Add(m, GTP_final[m]);
                    }
                    if (!GTP_DB.ContainsKey(EndPoint))
                        GTP_DB.Add(EndPoint, GTP_final[EndPoint]);   
            }
            // защита от позднего прихода графика!!!  
            if (Convert.ToInt32(TimeReperPoints[TimeReperPoints.Count - 1]) < 172800)
                for (int i = 1 + Convert.ToInt32(TimeReperPoints[TimeReperPoints.Count - 1]); i <= 172800; i++)
                {
                    GTP_final[i] = GTP_final[i - 1];
                }
        }
        //--------------------------------------------------------------------------
        void MakeInterpolation()
        {
            if (isGTP1)
                GTP1_DB.Clear();
            if (isGTP2)
                GTP2_DB.Clear();
            if (isGTP3)
                GTP3_DB.Clear();
            if (isGTP4)
                GTP4_DB.Clear();
            if (isGTP5)
                GTP5_DB.Clear();
            if (isGTP6)
                GTP6_DB.Clear();
            if (isGTP7)
                GTP7_DB.Clear();
            if (isGTP8)
                GTP8_DB.Clear();
            if (isGTP9)
                GTP9_DB.Clear();
            if (isGTP10)
                GTP10_DB.Clear();
            try
            {
                switch (comboBox1.SelectedIndex)
                {
                    case 0:
                        {
                            // моделируем с шагом по времени
                            if (isGTP1)
                                MakeTimeAppr(TimeReperPoints1, GTP1_final, GTP1_DB);
                            if (isGTP2)
                                MakeTimeAppr(TimeReperPoints2, GTP2_final, GTP2_DB);
                            if (isGTP3)
                                MakeTimeAppr(TimeReperPoints3, GTP3_final, GTP3_DB);
                            if (isGTP4)
                                MakeTimeAppr(TimeReperPoints4, GTP4_final, GTP4_DB);
                            if (isGTP5)
                                MakeTimeAppr(TimeReperPoints5, GTP5_final, GTP5_DB);
                            if (isGTP6)
                                MakeTimeAppr(TimeReperPoints6, GTP6_final, GTP6_DB);
                            if (isGTP7)
                                MakeTimeAppr(TimeReperPoints7, GTP7_final, GTP7_DB);
                            if (isGTP8)
                                MakeTimeAppr(TimeReperPoints8, GTP8_final, GTP8_DB);
                            if (isGTP9)
                                MakeTimeAppr(TimeReperPoints9, GTP9_final, GTP9_DB);
                            if (isGTP10)
                                MakeTimeAppr(TimeReperPoints10, GTP10_final, GTP10_DB);
                            break;
                        }
                    case 1:
                        {
                            // моделируем с шагом по мощности
                            if (isGTP1)
                                MakePowerAppr(TimeReperPoints1, GTP1_final, GTP1_DB);
                            if (isGTP2)
                                MakePowerAppr(TimeReperPoints2, GTP2_final, GTP2_DB);
                            if (isGTP3)
                                MakePowerAppr(TimeReperPoints3, GTP3_final, GTP3_DB);
                            if (isGTP4)
                                MakePowerAppr(TimeReperPoints4, GTP4_final, GTP4_DB);
                            if (isGTP5)
                                MakePowerAppr(TimeReperPoints5, GTP5_final, GTP5_DB);
                            if (isGTP6)
                                MakePowerAppr(TimeReperPoints6, GTP6_final, GTP6_DB);
                            if (isGTP7)
                                MakePowerAppr(TimeReperPoints7, GTP7_final, GTP7_DB);
                            if (isGTP8)
                                MakePowerAppr(TimeReperPoints8, GTP8_final, GTP8_DB);
                            if (isGTP9)
                                MakePowerAppr(TimeReperPoints9, GTP9_final, GTP9_DB);
                            if (isGTP10)
                                MakePowerAppr(TimeReperPoints10, GTP10_final, GTP10_DB);
                            break;
                        }
                    case 2:
                        {
                            // моделируем линейный набор мощности
                            if (isGTP1)
                                MakeLinearAppr(TimeReperPoints1, GTP1_final, GTP1_DB);
                            if (isGTP2)
                                MakeLinearAppr(TimeReperPoints2, GTP2_final, GTP2_DB);
                            if (isGTP3)
                                MakeLinearAppr(TimeReperPoints3, GTP3_final, GTP3_DB);
                            if (isGTP4)
                                MakeLinearAppr(TimeReperPoints4, GTP4_final, GTP4_DB);
                            if (isGTP5)
                                MakeLinearAppr(TimeReperPoints5, GTP5_final, GTP5_DB);
                            if (isGTP6)
                                MakeLinearAppr(TimeReperPoints6, GTP6_final, GTP6_DB);
                            if (isGTP7)
                                MakeLinearAppr(TimeReperPoints7, GTP7_final, GTP7_DB);
                            if (isGTP8)
                                MakeLinearAppr(TimeReperPoints8, GTP8_final, GTP8_DB);
                            if (isGTP9)
                                MakeLinearAppr(TimeReperPoints9, GTP9_final, GTP9_DB);
                            if (isGTP10)
                                MakeLinearAppr(TimeReperPoints10, GTP10_final, GTP10_DB);
                            break;
                        }
                }
                GTPIsOk = 1;
            }
            catch (Exception ex)
            {
                WriteLog(CurrSec, "Ошибка выполнения функции интерполяции.");
                GTPIsOk = 0;
            }
        }
        //--------------------------------------------------------------------------
        private void button9_Click(object sender, EventArgs e) // очистить график ГТП-1
        {
            ClearGraph(zg1);
        }
        //--------------------------------------------------------------------------
        private void button10_Click(object sender, EventArgs e) // очистить график ГТП-2
        {
            ClearGraph(zg2);
        }
        //--------------------------------------------------------------------------
        private void button11_Click(object sender, EventArgs e) // очистить график ГТП-3
        {
            ClearGraph(zg3);
        }
        //--------------------------------------------------------------------------
        private void comboBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            if (Permit)
            {
                switch (comboBox1.SelectedIndex)
                {
                    case 0:
                        {
                            comboBox2.Enabled = true;
                            comboBox4.Enabled = false;
                            break;
                        }
                    case 1:
                        {
                            comboBox2.Enabled = false;
                            comboBox4.Enabled = true;
                            break;
                        }
                    case 2:
                        {
                            comboBox2.Enabled = false;
                            comboBox4.Enabled = false;
                            break;
                        }
                }
            }
        }
        //--------------------------------------------------------------------------
        private void comboBox2_SelectedValueChanged(object sender, EventArgs e)
        {
            TimeStep = Convert.ToInt32(comboBox2.Text);
        }
        //------------------------------------------------------------------------
        private void tabControl1_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                Array a = (Array)e.Data.GetData(DataFormats.FileDrop);

                if (a != null)
                {
                    // Extract string from first array element
                    // (ignore all files except first if number of files are dropped).
                    string s = a.GetValue(0).ToString();
                    ReadPBR(s);
                }
            }
            catch (Exception ex)
            {
                WriteLog(CurrSec, "Ошибка обработки Drag&Drop файла на форму.");
            }
        }
        //-----------------------------------------------------------------------
        private void tabControl1_DragEnter(object sender, DragEventArgs e)
        {
            // Check if the Dataformat of the data can be accepted
            // (we only accept file drops from Explorer, etc.)
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy; // Okay
            else
                e.Effect = DragDropEffects.None; // Unknown data, ignore it

        }
        //-----------------------------------------------------------------------
        private string zg4_PointValueEvent(ZedGraphControl sender, GraphPane pane, CurveItem curve, int iPt)
        {
            string result;
            PointPair point = curve[iPt];
            result = string.Format("Время: {0}\nМощность: {1:F3}", Convert.ToString(Math.Floor(point.X % 24)).PadLeft(2, '0') + ":" + Convert.ToString(Math.Floor((point.X % 1) * 60)).PadLeft(2, '0'), point.Y);
            return result;
        }
        //-----------------------------------------------------------------------
        private string zg5_PointValueEvent(ZedGraphControl sender, GraphPane pane, CurveItem curve, int iPt)
        {
            string result;
            PointPair point = curve[iPt];
            result = string.Format("Время: {0}\nМощность: {1:F3}", Convert.ToString(Math.Floor(point.X % 24)).PadLeft(2, '0') + ":" + Convert.ToString(Math.Floor((point.X % 1) * 60)).PadLeft(2, '0'), point.Y);
            return result;
        }
        //-----------------------------------------------------------------------
        private string zg6_PointValueEvent(ZedGraphControl sender, GraphPane pane, CurveItem curve, int iPt)
        {
            string result;
            PointPair point = curve[iPt];
            result = string.Format("Время: {0}\nМощность: {1:F3}", Convert.ToString(Math.Floor(point.X % 24)).PadLeft(2, '0') + ":" + Convert.ToString(Math.Floor((point.X % 1) * 60)).PadLeft(2, '0'), point.Y);
            return result;
        }
        //-----------------------------------------------------------------------
        private string zg7_PointValueEvent(ZedGraphControl sender, GraphPane pane, CurveItem curve, int iPt)
        {
            string result;
            PointPair point = curve[iPt];
            result = string.Format("Время: {0}\nМощность: {1:F3}", Convert.ToString(Math.Floor(point.X % 24)).PadLeft(2, '0') + ":" + Convert.ToString(Math.Floor((point.X % 1) * 60)).PadLeft(2, '0'), point.Y);
            return result;
        }
        //-----------------------------------------------------------------------
        private string zg8_PointValueEvent(ZedGraphControl sender, GraphPane pane, CurveItem curve, int iPt)
        {
            string result;
            PointPair point = curve[iPt];
            result = string.Format("Время: {0}\nМощность: {1:F3}", Convert.ToString(Math.Floor(point.X % 24)).PadLeft(2, '0') + ":" + Convert.ToString(Math.Floor((point.X % 1) * 60)).PadLeft(2, '0'), point.Y);
            return result;
        }
        //-----------------------------------------------------------------------
        private string zg9_PointValueEvent(ZedGraphControl sender, GraphPane pane, CurveItem curve, int iPt)
        {
            string result;
            PointPair point = curve[iPt];
            result = string.Format("Время: {0}\nМощность: {1:F3}", Convert.ToString(Math.Floor(point.X % 24)).PadLeft(2, '0') + ":" + Convert.ToString(Math.Floor((point.X % 1) * 60)).PadLeft(2, '0'), point.Y);
            return result;
        }
        //-----------------------------------------------------------------------
        private string zg10_PointValueEvent(ZedGraphControl sender, GraphPane pane, CurveItem curve, int iPt)
        {
            string result;
            PointPair point = curve[iPt];
            result = string.Format("Время: {0}\nМощность: {1:F3}", Convert.ToString(Math.Floor(point.X % 24)).PadLeft(2, '0') + ":" + Convert.ToString(Math.Floor((point.X % 1) * 60)).PadLeft(2, '0'), point.Y);
            return result;
        }
        //-----------------------------------------------------------------------
        private string zg0_PointValueEvent(ZedGraphControl sender, GraphPane pane, CurveItem curve, int iPt)
        {
            string result;
            PointPair point = curve[iPt];
            result = string.Format("Время: {0}\nМощность: {1:F3}", Convert.ToString(Math.Floor(point.X % 24)).PadLeft(2, '0') + ":" + Convert.ToString(Math.Floor((point.X % 1) * 60)).PadLeft(2, '0'), point.Y);
            return result;
        }
        //-----------------------------------------------------------------------
        //-----------------------------------------------------------------------
        private void UpdateGraph(ZedGraphControl zg, double[] GTP, Dictionary<int,double> PReal)
        {
            double maxValue = 0;
            double minValue = 0;
            for (int i = 1; i < 172801; i++)
            {
                if (GTP[i] > maxValue)
                    maxValue = GTP[i];
                if (GTP[i] < minValue)
                    minValue = GTP[i];
            }
            foreach (var i in PReal)
            {
                if (i.Value > maxValue)
                    maxValue = i.Value;
                if (i.Value < minValue)
                    minValue = i.Value;
            }
            // Устанавливаем интересующий нас интервал по оси X
            zg.GraphPane.XAxis.Scale.Min = 0;
            zg.GraphPane.XAxis.Scale.Max = 48;
            // Устанавливаем интересующий нас интервал по оси Y
            zg.GraphPane.YAxis.Scale.Min = minValue;
            zg.GraphPane.YAxis.Scale.Max = ((maxValue / 50) + 1) * 50;
            if (minValue == maxValue)
                zg.GraphPane.YAxis.Scale.Max = minValue + 50;
            zg.AxisChange();
            zg.Invalidate();
        
        }
        //-----------------------------------------------------------------------
        private void ClearGraph(ZedGraphControl zg)
        {
            zg.GraphPane.CurveList.Clear();
            zg.AxisChange();
            zg.Invalidate();
        }
        //----------------------------------------------------------------------
        private void button1_Click(object sender, EventArgs e) // очистить график ГТП-4
        {
            ClearGraph(zg4);
        }
        //---------------------------------------------------------------------
        private void button4_Click(object sender, EventArgs e) // очистить график ГТП-5
        {
            ClearGraph(zg5);
        }
        //---------------------------------------------------------------------
        private void button13_Click(object sender, EventArgs e)// очистить график ГТП-6
        {
            ClearGraph(zg6);
        }
        //---------------------------------------------------------------------
        private void button15_Click(object sender, EventArgs e)// очистить график ГТП-7
        {
            ClearGraph(zg7);
        }
        //---------------------------------------------------------------------
        private void button17_Click(object sender, EventArgs e)// очистить график ГТП-8
        {
            ClearGraph(zg8);
        }
        //---------------------------------------------------------------------
        private void button19_Click(object sender, EventArgs e)// очистить график ГТП-9
        {
            ClearGraph(zg9);
        }
        //---------------------------------------------------------------------
        private void button21_Click(object sender, EventArgs e)// очистить график ГТП-10
        {
            ClearGraph(zg10);
        }
        //---------------------------------------------------------------------
        private void button23_Click(object sender, EventArgs e)// очистить график ГТП-0
        {
            ClearGraph(zg0);
        }
        //---------------------------------------------------------------------
        private void comboBox4_SelectedValueChanged(object sender, EventArgs e)
        {
            PowerStep = Convert.ToInt32(comboBox4.Text);
        }
        //---------------------------------------------------------------------
        private void button25_Click(object sender, EventArgs e) //ГЛОБАЛЬНОЕ ОЧИСТИТЬ
        {
                f2 = new Form2();
                f2.Owner = this;    
                f2.Show();
        }
        //-------------------------------------------------------------------------
        private void textBox4_KeyDown_1(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Permit = (textBox4.Text == "wdpf");
                textBox4.Clear();
            }
            if (Permit)
            {
                comboBox3.Enabled = true;
                textBox1.Enabled = true;
                switch (comboBox1.SelectedIndex)
                {
                    case 0:
                        {
                            comboBox2.Enabled = true;
                            comboBox4.Enabled = false;
                            break;
                        }
                    case 1:
                        {
                            comboBox2.Enabled = false;
                            comboBox4.Enabled = true;
                            break;
                        }
                    case 2:
                        {
                            comboBox2.Enabled = false;
                            comboBox4.Enabled = false;
                            break;
                        }
                }
                comboBox1.Enabled = true;
                tabPage1.IsAccessible = true;
                WriteLog(CurrSec, "Получен доступ к редактированию параметров АвтоОператора.");
            }
            else
            {
                comboBox3.Enabled = false;
                textBox1.Enabled = false;
                comboBox2.Enabled = false;
                comboBox4.Enabled = false;
                comboBox1.Enabled = false;
                tabPage1.IsAccessible = false;
            }
        }
        //-------------------------------------------------------------------------------------
        private void button26_Click(object sender, EventArgs e)
        {
            if (isGTP1)
            {
                UpdateGraph(zg1, GTP1, PGTP1);
            }
            if (isGTP2)
            {
                UpdateGraph(zg2, GTP2, PGTP2);
            }
            if (isGTP3)
            {
                UpdateGraph(zg3, GTP3, PGTP3);
            }
            if (isGTP4)
            {
                UpdateGraph(zg4, GTP4, PGTP4);
            }
            if (isGTP5)
            {
                UpdateGraph(zg5, GTP5, PGTP5);
            }
            if (isGTP6)
            {
                UpdateGraph(zg6, GTP6, PGTP6);
            }
            if (isGTP7)
            {
                UpdateGraph(zg7, GTP7, PGTP7);
            }
            if (isGTP8)
            {
                UpdateGraph(zg8, GTP8, PGTP8);
            }
            if (isGTP9)
            {
                UpdateGraph(zg9, GTP9, PGTP9);
            }
            if (isGTP10)
            {
                UpdateGraph(zg10, GTP10, PGTP10);
            }

            UpdateGraph(zg0, GTP0, PGES);
        }
        //-------------------------------------------------------------------------------------
        private void comboBox3_TextChanged_1(object sender, EventArgs e)
        {
            RUSATime = Convert.ToInt32(comboBox3.Text);
        }
        //-------------------------------------------------------------------------------------
        private void timer3_Tick(object sender, EventArgs e)
        {
            if (isActualDate && !isAdminRules && (DBConn.State == ConnectionState.Open))
            {
                lock (this)
                {
                    DBReadData();
                }
            }
            DrawGTPS();
            
        }
        //---------------------------------------------------------------------
        public void UpdateVyr()
        {
            label32.Text = "Выработка ПЛАН = " + Convert.ToString(Math.Round(vyr0.GetPlanVyr(), 1)) + " МВт*ч\nВыработка ФАКТ = " +
                Convert.ToString(Math.Round(vyr0.GetFactVyr(), 1)) + " МВт*ч\nРазница: " + Convert.ToString(Math.Round(vyr0.GetDeltaVyr(), 1)) + " МВт*ч";
            label32.Location = new Point(Convert.ToInt32(tabPage12.Left + 9), Convert.ToInt32(tabPage12.Top + 5));//ГЭС
            /*
            label1.Text = (isGTP1) ? Convert.ToString(Math.Round(GTP1_final[CurrSec], 1)) : "0";
            label2.Text = (isGTP2) ? Convert.ToString(Math.Round(GTP2_final[CurrSec], 1)) : "0";
            label3.Text = (isGTP3) ? Convert.ToString(Math.Round(GTP3_final[CurrSec], 1)) : "0";
            label13.Text = (isGTP4) ? Convert.ToString(Math.Round(GTP4_final[CurrSec], 1)) : "0";
            label14.Text = (isGTP5) ? Convert.ToString(Math.Round(GTP5_final[CurrSec], 1)) : "0";
            label15.Text = (isGTP6) ? Convert.ToString(Math.Round(GTP6_final[CurrSec], 1)) : "0";
            label16.Text = (isGTP7) ? Convert.ToString(Math.Round(GTP7_final[CurrSec], 1)) : "0";
            label17.Text = (isGTP8) ? Convert.ToString(Math.Round(GTP8_final[CurrSec], 1)) : "0";
            label18.Text = (isGTP9) ? Convert.ToString(Math.Round(GTP9_final[CurrSec], 1)) : "0";
            label19.Text = (isGTP10) ? Convert.ToString(Math.Round(GTP10_final[CurrSec], 1)) : "0";
            */
            
        }
        //---------------------------------------------------------------------
        private void GTPOutputToNull()
        {
            vyr0.ToNULL();
            vyr1.ToNULL();
            vyr2.ToNULL();
            vyr3.ToNULL();
            vyr4.ToNULL();
            vyr5.ToNULL();
            vyr6.ToNULL();
            vyr7.ToNULL();
            vyr8.ToNULL();
            vyr9.ToNULL();
            vyr10.ToNULL();

            PGES.Clear();
            PGTP1.Clear();
            PGTP2.Clear();
            PGTP3.Clear();
            PGTP3.Clear();
            PGTP4.Clear();
            PGTP5.Clear();
            PGTP6.Clear();
            PGTP7.Clear();
            PGTP8.Clear();
            PGTP9.Clear();
            PGTP10.Clear();
        }
        //---------------------------------------------------------------------
        private void timer4_Tick(object sender, EventArgs e) // вывод  системного времени 
        {
            DateTime dt = DateTime.Now;
            //выводим Овационную дату     
            textBox6.Text = Convert.ToString(dt.Day).PadLeft(2, '0') + "/" + Convert.ToString(dt.Month).PadLeft(2, '0') + "/" + Convert.ToString(dt.Year).PadLeft(2, '0');

            //выводим Овационное время      
            textBox5.Text = Convert.ToString(dt.Hour).PadLeft(2, '0') + ":" + Convert.ToString(dt.Minute).PadLeft(2, '0') + ":" + Convert.ToString(dt.Second).PadLeft(2, '0');
        }
        //---------------------------------------------------------------------
        private void WritePGTPtoDB(int CSec, double Power, int Item) // пишем графики измеренных значений в Базу 
        {
            SqlCommand DBCommand;
            string dbPARNUMBER;
            string dbOBJECT = "0";
            string dbITEM;
            string dbVALUE0;
            string dbVALUE1;
            string dbOBJTYPE = "0";
            string dbDATA_DATE;
            string dbP2KStatus = "0";
            string dbRcvStamp = "GETDATE()";
            string dbSEASON = GetCurrentSeason();

            string t1 = GetDate(CSec);
            dbITEM = Convert.ToString(Item);

            // удаление данных из БД c времени  CurrSec
            DBCommand = new SqlCommand("DELETE FROM DATA" +
                  " WHERE  (PARNUMBER=302) AND ITEM = " + dbITEM + " AND (DATA_DATE = '" + t1 + "')", DBConn);
            try
            {
                DBCommand.ExecuteNonQuery();
                //--------------------------------------------------------------------------
                //пишем в базу значение измеренной мощности 
                dbPARNUMBER = "302";
                // запись   
                dbVALUE0 = dbVALUE1 = Convert.ToString(Power).Replace(',', '.');

                dbDATA_DATE = t1;

                DBCommand = new SqlCommand("INSERT INTO DATA (PARNUMBER,OBJECT,ITEM,VALUE0,VALUE1,OBJTYPE,DATA_DATE,P2KStatus,RcvStamp,SEASON)" +
                       " VALUES (" + dbPARNUMBER + "," + dbOBJECT + "," + dbITEM + "," + dbVALUE0 + "," + dbVALUE1 + "," + dbOBJTYPE + ",'" + dbDATA_DATE + "'," + dbP2KStatus + "," + dbRcvStamp + "," + dbSEASON + ")", DBConn);
                DBCommand.ExecuteNonQuery();
            }
            catch { ;}
        }
        //--------------------------------------------------------------------------
        private void ReadPGTPfromDB() // читаем графики измеренных значений из БД
        {
            SqlDataReader DBReader;
            SqlCommand DBCommand;

            string datetimeSTART = GetDate(0);
            DBCommand = new SqlCommand("SELECT * FROM DATA WHERE DATA_DATE BETWEEN '" + datetimeSTART +
                    "' AND DATEADD(HOUR,24,'" + datetimeSTART + "') AND (PARNUMBER = 302)" +
                    "ORDER BY DATA_DATE", DBConn);
            try
            {
                DBReader = DBCommand.ExecuteReader();
                while (DBReader.Read())
                {
                    string dt = Convert.ToString(DBReader.GetDateTime(6));
                    char[] sep = { '-', ' ', ':', '.' };
                    string[] tempLine = dt.Split(sep);
                    int d = Convert.ToInt32(tempLine[0]);
                    int m = Convert.ToInt32(tempLine[1]);
                    int y = Convert.ToInt32(tempLine[2]);
                    int hh = Convert.ToInt32(tempLine[3]);
                    int mm = Convert.ToInt32(tempLine[4]);
                    int ss = Convert.ToInt32(tempLine[5]);
                    int index = ss + 60 * mm + 3600 * hh;
                    double value = DBReader.GetDouble(3);

                    switch (DBReader.GetInt32(2))
                    {
                        case 0:
                            {
                                if (!PGES.Keys.Contains<int>(index))
                                    PGES.Add(index, value);
                                break;
                            }

                    }
                } // while (DBReader.Read()) ....
                DBReader.Close();
                DBCommand.Dispose();
            }
            catch { ;}
        }
    }
    //---------------------------------------------------------------------
    public class Vyrabotka
    {
        double PlanVyr; //плановая выработка общая
        double PlanVyrToday; //плановая выработка за сегодня
        double PlanVyrYesterday; //плановая выработка за вчера

        double FactVyr; //фактическая выработка общая
        double FactVyrToday; //фактическая выработка за сегодня
        double FactVyrYesterday; //фактическая выработка за вчера

        double DeltaVyr; //разница между выработанной и планом

        public Vyrabotka()
        {
            PlanVyr = 0;
            PlanVyrToday = 0;
            PlanVyrYesterday = 0;

            FactVyr = 0;
            FactVyrToday = 0;
            FactVyrYesterday = 0;

            DeltaVyr = 0;
        }

        public void ToNULL() // обнуление выработки
        {
            PlanVyr = PlanVyrToday = PlanVyrYesterday =0;
            FactVyr = FactVyrToday = FactVyrYesterday =0;
        }

        public void AddPlanVyr(double addition) //добавить величину к плановой выработке
        {
            PlanVyr += addition;
        }

        public void AddFactVyr(double addition) //добавить величину к фактической выработке
        {
            FactVyr += addition;
        }

        public double GetPlanVyr()
        {
            return PlanVyr;
        }

        public double GetFactVyr()
        {
            return FactVyr;
        }

        public double GetDeltaVyr()
        {
            return FactVyr - PlanVyr;
        }
    }
}
