using System;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.XPath;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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
        public Slave MySlave = new Slave();
        private Thread ServThread;   // экземпляр потока
        private double[] GTP110;
        private double[] GTP220;
        private double[] GTP500;
        private double[] GTP110_temp;
        private double[] GTP220_temp;
        private double[] GTP500_temp;
        private double[] GTP110_linear;
        private double[] GTP220_linear;
        private double[] GTP500_linear;
        private double[] GTP110_final;
        private double[] GTP220_final;
        private double[] GTP500_final;
        private int[] TimeReperPoints110;
        private int[] TimeReperPoints220;
        private int[] TimeReperPoints500;
        private int ReperPointsNumber110;
        private int ReperPointsNumber220;
        private int ReperPointsNumber500;
        private int pbrY; //Год в ПБР
        private int pbrM; //Месяц в ПБР
        private int pbrD; //День в ПБР
        private int pbrHour; //Час в ПБР
        private int pbrMin; //Минута в ПБР
        private int pbrTZ; //Часовой пояс в ПБР
        private int CurrSec = 0;// текущая секунда
        private int OvationSaw = 0;// Пила из Овации
        private const string PBRFile = "D:\\AutoOper\\AutoOper.txt";
        private string XsltFile;
        private string XmlFile;
        private int TimeStep;   //шаг по времени
        private int PowerStep;  //шаг по мощности
        // ------------------------------------------------------------------------
        public Form1()
        {
            InitializeComponent();
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
        private void button2_Click(object sender, EventArgs e)
        {
            if (!MySlave.connected && MySlave.Listener == null) //connect
            {
                try
                {
                    MySlave.Slave_ID = Convert.ToByte(textBox2.Text);
                    MySlave.IP = textBox1.Text;
                    MySlave.PORT = Convert.ToInt32(textBox3.Text);
                }
                catch
                {
                    MySlave.Slave_ID = 1;
                    MessageBox.Show("Incorrect Slave ID value!");
                }
                //Clear in/out buffers:
                MySlave.DiscardOutBuffer();
                MySlave.DiscardInBuffer();
                try
                {
                    ServThread = new Thread(new ThreadStart(ServStart));
                    ServThread.Priority = ThreadPriority.Normal;             // установка приоритета потока
                    ServThread.Start();                                      // запустили поток. Стартовая функция – 
                    button2.Text = "Разъединить";
                }
                catch
                {
                    MessageBox.Show("Incorrect IP Address!");
                    button2.Text = "Соединить";
                }
            }
            else  //disconnect
            {
                ServThread.Abort();            
                MySlave.disconnect();
                button2.Text = "Разъединить";            
            }
        }
        //-------------------------------------------------------------------
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
        private void timer1_Tick(object sender, EventArgs e) // работа по таймеру
        {
           // заведем счётчик времени в 40001-м регистре
           if(Convert.ToInt16(dataGridView1.Rows[0].Cells[1].Value ) <60) 
           {
               dataGridView1.Rows[0].Cells[1].Value = Convert.ToString(Convert.ToInt16(dataGridView1.Rows[0].Cells[1].Value) + 1);
           }
           else
           {
               dataGridView1.Rows[0].Cells[1].Value = Convert.ToString(0);
           }
            //переписываем значения таблицы в аналоговые регистры Modbus
           try
           {
                    byte[] bBytes = new byte[4];
                    // пила в Овацию
                    bBytes = BitConverter.GetBytes((float)Convert.ToDouble(dataGridView1.Rows[0].Cells[1].Value));
                    MySlave.ModbusRegisters[1].HiByte = bBytes[3];
                    MySlave.ModbusRegisters[1].LoByte = bBytes[2];
                    MySlave.ModbusRegisters[0].HiByte = bBytes[1];
                    MySlave.ModbusRegisters[0].LoByte = bBytes[0];

                    // номер текущей секунды из Овации
                    bBytes = BitConverter.GetBytes((float)Convert.ToDouble(dataGridView1.Rows[2].Cells[1].Value));
                    MySlave.ModbusRegisters[3].HiByte = bBytes[3];
                    MySlave.ModbusRegisters[3].LoByte = bBytes[2];
                    MySlave.ModbusRegisters[2].HiByte = bBytes[1];
                    MySlave.ModbusRegisters[2].LoByte = bBytes[0];

                    // Задание ГТП-110 в Овацию
                    bBytes = BitConverter.GetBytes((float)Convert.ToDouble(dataGridView1.Rows[4].Cells[1].Value));
                    MySlave.ModbusRegisters[5].HiByte = bBytes[3];
                    MySlave.ModbusRegisters[5].LoByte = bBytes[2];
                    MySlave.ModbusRegisters[4].HiByte = bBytes[1];
                    MySlave.ModbusRegisters[4].LoByte = bBytes[0];

                    // Задание ГТП-220 в Овацию
                    bBytes = BitConverter.GetBytes((float)Convert.ToDouble(dataGridView1.Rows[6].Cells[1].Value));
                    MySlave.ModbusRegisters[7].HiByte = bBytes[3];
                    MySlave.ModbusRegisters[7].LoByte = bBytes[2];
                    MySlave.ModbusRegisters[6].HiByte = bBytes[1];
                    MySlave.ModbusRegisters[6].LoByte = bBytes[0];

                    // Задание ГТП-500 в Овацию
                    bBytes = BitConverter.GetBytes((float)Convert.ToDouble(dataGridView1.Rows[8].Cells[1].Value));
                    MySlave.ModbusRegisters[9].HiByte = bBytes[3];
                    MySlave.ModbusRegisters[9].LoByte = bBytes[2];
                    MySlave.ModbusRegisters[8].HiByte = bBytes[1];
                    MySlave.ModbusRegisters[8].LoByte = bBytes[0];
           }
           catch(Exception ex)
           {
               toolStripStatusLabel1.Text = "Ошибка: неверный формат данных. " + ex.ToString();
           }

            //переписываем значения аналоговых регистров Modbus в таблицу
            try
           {
                byte[] data = new byte[4];
                // пила в Овацию
                data[3] = MySlave.ModbusRegisters[1].HiByte;
                data[2] = MySlave.ModbusRegisters[1].LoByte;
                data[1] = MySlave.ModbusRegisters[0].HiByte;
                data[0] = MySlave.ModbusRegisters[0].LoByte;
                OvationSaw = Convert.ToInt32(BitConverter.ToSingle(data, 0));
                dataGridView1.Rows[0].Cells[1].Value = Convert.ToString(OvationSaw);

                // номер текущей секунды из Овации
                data[3] = MySlave.ModbusRegisters[3].HiByte;
                data[2] = MySlave.ModbusRegisters[3].LoByte;
                data[1] = MySlave.ModbusRegisters[2].HiByte;
                data[0] = MySlave.ModbusRegisters[2].LoByte;
                CurrSec = Convert.ToInt32(BitConverter.ToSingle(data, 0));
                dataGridView1.Rows[2].Cells[1].Value = Convert.ToString(CurrSec);

                // Задание ГТП-110 в Овацию
                data[3] = MySlave.ModbusRegisters[5].HiByte;
                data[2] = MySlave.ModbusRegisters[5].LoByte;
                data[1] = MySlave.ModbusRegisters[4].HiByte;
                data[0] = MySlave.ModbusRegisters[4].LoByte;
                dataGridView1.Rows[4].Cells[1].Value = Convert.ToString(GTP110_final[CurrSec]);

                // Задание ГТП-220 в Овацию
                data[3] = MySlave.ModbusRegisters[7].HiByte;
                data[2] = MySlave.ModbusRegisters[7].LoByte;
                data[1] = MySlave.ModbusRegisters[6].HiByte;
                data[0] = MySlave.ModbusRegisters[6].LoByte;
                dataGridView1.Rows[6].Cells[1].Value = Convert.ToString(GTP220_final[CurrSec]);
                
                // Задание ГТП-500 в Овацию
                data[3] = MySlave.ModbusRegisters[9].HiByte;
                data[2] = MySlave.ModbusRegisters[9].LoByte;
                data[1] = MySlave.ModbusRegisters[8].HiByte;
                data[0] = MySlave.ModbusRegisters[8].LoByte;
                dataGridView1.Rows[8].Cells[1].Value = Convert.ToString(GTP500_final[CurrSec]);
           }
            catch (Exception ex)
           {
                toolStripStatusLabel1.Text = "Ошибка: неверный формат данных. " + ex.ToString();
           }  
            
           //выводим Овационное время      
            textBox5.Text = Convert.ToString(CurrSec / 3600) + ":" + Convert.ToString((CurrSec % 3600) / 60) + ":" + Convert.ToString((CurrSec % 3600) % 60);
        }

        //--------------------------------------------------------------------------------
        private void Form1_Load(object sender, EventArgs e)
        {           
            // таблица регистров 40000 - ...
            dataGridView1.Rows.Add(20);
            for (int k = 0; k < 20; k++)
            {
                dataGridView1.Rows[k].Cells[0].Value = Convert.ToString(40000 + k+1);
                dataGridView1.Rows[k].Cells[2].Value = "Float";
            }
            dataGridView1.Rows[0].Cells[3].Value = "Счётчик секунд";
            dataGridView1.Rows[2].Cells[3].Value = "Номер текущей секунды в сутках";
            dataGridView1.Rows[4].Cells[3].Value = "ГТП-110";
            dataGridView1.Rows[6].Cells[3].Value = "ГТП-220";
            dataGridView1.Rows[8].Cells[3].Value = "ГТП-500";

            for (int i = 100; i >= 1; i--)
                domainUpDown1.Items.Add(i);

            TimeStep = Convert.ToInt32(comboBox2.Text);
            PowerStep = Convert.ToInt32(domainUpDown1.Text);

            GTP110 = new double[86400];
            GTP220 = new double[86400];
            GTP500 = new double[86400];
            GTP110_temp = new double[86400];
            GTP220_temp = new double[86400];
            GTP500_temp = new double[86400];
            GTP110_linear = new double[86400];
            GTP220_linear = new double[86400];
            GTP500_linear = new double[86400];
            TimeReperPoints110 = new int[288];
            TimeReperPoints220 = new int[288];
            TimeReperPoints500 = new int[288];
        }
        //----------------------------------------------------------------------------------
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
        //-----------------------------------------------------------------------------------
        private void button4_Click(object sender, EventArgs e)
        {// считать ПБР
            ReadPBR();
        }
        //---------------------------------------------------------------------
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            label4.Text = XmlFile;
        }
        //---------------------------------------------------------------------
        private void ReadPBR()
        {
            try
            {
                //load the Xml doc
                XPathDocument myXPathDoc = new XPathDocument(XmlFile);
                XslTransform myXslTrans = new XslTransform();
                //load the Xsl 
                myXslTrans.Load(XsltFile);

                //create the output stream
                XmlTextWriter myWriter = new XmlTextWriter(PBRFile, null);

                //do the actual transform of Xml
                myXslTrans.Transform(myXPathDoc, null, myWriter);

                myWriter.Close();
            }
            catch (Exception ex)
            {
                
                toolStripStatusLabel1.Text = ex.Message;
            }
            ParsePBR();
            DrawPBR();
        }
        //----------------------------------------------------------------------------
        private void ParsePBR()
        {
            try
            {
                // обнуление массивов данных
                GTP110 = new double[86400];
                GTP220 = new double[86400];
                GTP500 = new double[86400];
                GTP110_temp = new double[86400];
                GTP220_temp = new double[86400];
                GTP500_temp = new double[86400];
                GTP110_linear = new double[86400];
                GTP220_linear = new double[86400];
                GTP500_linear = new double[86400];
                TimeReperPoints110 = new int[288];
                TimeReperPoints220 = new int[288];
                TimeReperPoints500 = new int[288];
                ReperPointsNumber110 = 0;
                ReperPointsNumber220 = 0;
                ReperPointsNumber500 = 0;
                int flag = 1;       // флаг чтения данных
                int k110 = 0;
                int k220 = 0;
                int k500 = 0;
                string[] lines = System.IO.File.ReadAllLines(PBRFile);
                foreach (string line in lines)
                {
                    char sep1 = ';';
                    char[] sep2 = { '_' };
                    string[] tempLine = line.Split(sep1);
                    string[] strs = tempLine[1].Split(sep2);
                    pbrY = Convert.ToInt32(strs[0]);
                    pbrM = Convert.ToInt32(strs[1]);
                    pbrD = Convert.ToInt32(strs[2]);
                    pbrHour = Convert.ToInt32(strs[3]) - 2;
                    if (pbrHour < 0)
                        pbrHour = 24 - pbrHour * (-1);
                    pbrMin = Convert.ToInt32(strs[4]);
                    pbrTZ = Convert.ToInt32(strs[5]);
                    if (flag == 1)
                    {
                        switch (tempLine[0])
                        {
                            case "Воткинская ГЭС-110":
                                {
                                    if ((k110 > 0) && (pbrHour == 0) && (pbrMin == 0))
                                    {
                                        GTP110[86399] = Convert.ToDouble(tempLine[2]);
                                        GTP110_temp[86399] = Convert.ToDouble(tempLine[2]);
                                        GTP110_linear[86399] = Convert.ToDouble(tempLine[2]);
                                        TimeReperPoints110[ReperPointsNumber110++] = 86399;//!!!!!!!!!!
                                    }
                                    else
                                    {
                                        GTP110[3600 * pbrHour + 60 * pbrMin] = Convert.ToDouble(tempLine[2]);
                                        GTP110_temp[3600 * pbrHour + 60 * pbrMin] = Convert.ToDouble(tempLine[2]);
                                        GTP110_linear[3600 * pbrHour + 60 * pbrMin] = Convert.ToDouble(tempLine[2]);
                                        TimeReperPoints110[ReperPointsNumber110++] = 3600 * pbrHour + 60 * pbrMin;
                                    }
                                    k110++;
                                    break;
                                }
                            case "Воткинская ГЭС-220":
                                {
                                    if ((k220 > 0) && (pbrHour == 0) && (pbrMin == 0))
                                    {
                                        GTP220[86399] = Convert.ToDouble(tempLine[2]);
                                        GTP220_temp[86399] = Convert.ToDouble(tempLine[2]);
                                        GTP220_linear[86399] = Convert.ToDouble(tempLine[2]);
                                        TimeReperPoints220[ReperPointsNumber220++] = 86399;//!!!!!!!!!!
                                    }
                                    else
                                    {
                                        GTP220[3600 * pbrHour + 60 * pbrMin] = Convert.ToDouble(tempLine[2]);
                                        GTP220_temp[3600 * pbrHour + 60 * pbrMin] = Convert.ToDouble(tempLine[2]);
                                        GTP220_linear[3600 * pbrHour + 60 * pbrMin] = Convert.ToDouble(tempLine[2]);
                                        TimeReperPoints220[ReperPointsNumber220++] = 3600 * pbrHour + 60 * pbrMin;
                                    }
                                    k220++;
                                    break;
                                }
                            case "Воткинская ГЭС-500":
                                {
                                    if ((k500 > 0) && (pbrHour == 0) && (pbrMin == 0))
                                    {
                                        GTP500[86399] = Convert.ToDouble(tempLine[2]);
                                        GTP500_temp[86399] = Convert.ToDouble(tempLine[2]);
                                        GTP500_linear[86399] = Convert.ToDouble(tempLine[2]);
                                        TimeReperPoints500[ReperPointsNumber500++] = 86399;//!!!!!!!!!!
                                    }
                                    else
                                    {
                                        GTP500[3600 * pbrHour + 60 * pbrMin] = Convert.ToDouble(tempLine[2]);
                                        GTP500_temp[3600 * pbrHour + 60 * pbrMin] = Convert.ToDouble(tempLine[2]);
                                        GTP500_linear[3600 * pbrHour + 60 * pbrMin] = Convert.ToDouble(tempLine[2]);
                                        TimeReperPoints500[ReperPointsNumber500++] = 3600 * pbrHour + 60 * pbrMin;
                                    }
                                    k500++;
                                    break;
                                }
                            case "Воткинская ГЭС":
                                {
                                    flag = 0;
                                    break;
                                }
                        }
                    }
                }
            }
            catch (IOException e)
            {
                toolStripStatusLabel1.Text = e.Message;
            }
            // Заполнение массивов GTP110  GTP220 GTP500
            for (long i = 1; i < 86400; i++)
            {
                if (GTP110[i] == 0.0)
                    GTP110[i] = GTP110[i - 1];
                if (GTP220[i] == 0.0)
                    GTP220[i] = GTP220[i - 1];
                if (GTP500[i] == 0.0)
                    GTP500[i] = GTP500[i - 1];
            }
        }
        //----------------------------------------------------------------------------
        private void DrawLinear()       //строит линеаризованный график ГТП
        {
            // для ГТП-110
            for (int i = 0; i < ReperPointsNumber110 - 1; i++)  // в данном цикле обрабатывается каждый скачок
            {
                // ищем ближайшую реперную точку
                int StartPoint;
                int EndPoint;

                StartPoint = TimeReperPoints110[i];
                EndPoint = TimeReperPoints110[i + 1];
                if (GTP110_linear[StartPoint] == GTP110_linear[EndPoint])
                    for (int j = StartPoint; j < EndPoint; j++)
                        GTP110_linear[j] = GTP110_linear[StartPoint];
                else
                {
                    double k = (GTP110_linear[EndPoint] - GTP110_linear[StartPoint]) / (EndPoint - StartPoint);
                    double b = GTP110_linear[StartPoint] - k * StartPoint;
                    for (int j = StartPoint; j < EndPoint; j++)
                        GTP110_linear[j] = k * j + b;
                }
            }
            // Для ГТП-220
            for (int i = 0; i < ReperPointsNumber220 - 1; i++)  // в данном цикле обрабатывается каждый скачок
            {
                // ищем ближайшую реперную точку
                int StartPoint;
                int EndPoint;

                StartPoint = TimeReperPoints220[i];
                EndPoint = TimeReperPoints220[i + 1];
                if (GTP220_linear[StartPoint] == GTP220_linear[EndPoint])
                    for (int j = StartPoint; j < EndPoint; j++)
                        GTP220_linear[j] = GTP220_linear[StartPoint];
                else
                {
                    double k = (GTP220_linear[EndPoint] - GTP220_linear[StartPoint]) / (EndPoint - StartPoint);
                    double b = GTP220_linear[StartPoint] - k * StartPoint;
                    for (int j = StartPoint; j < EndPoint; j++)
                        GTP220_linear[j] = k * j + b;
                }
            }
            // Для ГТП-500
            for (int i = 0; i < ReperPointsNumber500 - 1; i++)  // в данном цикле обрабатывается каждый скачок
            {
                // ищем ближайшую реперную точку
                int StartPoint;
                int EndPoint;

                StartPoint = TimeReperPoints500[i];
                EndPoint = TimeReperPoints500[i + 1];
                if (GTP500_linear[StartPoint] == GTP500_linear[EndPoint])
                    for (int j = StartPoint; j < EndPoint; j++)
                        GTP500_linear[j] = GTP500_linear[StartPoint];
                else
                {
                    double k = (GTP500_linear[EndPoint] - GTP500_linear[StartPoint]) / (EndPoint - StartPoint);
                    double b = GTP500_linear[StartPoint] - k * StartPoint;
                    for (int j = StartPoint; j < EndPoint; j++)
                        GTP500_linear[j] = k * j + b;
                }
            }
        
        }
        //----------------------------------------------------------------------------
        private void DrawPBR()
        {
            DrawGTP(zg110, GTP110, 110, Color.Blue, 0);
            DrawGTP(zg220, GTP220, 220, Color.Blue, 0);
            DrawGTP(zg500, GTP500, 500, Color.Blue, 0);
        }
        //----------------------------------------------------------------------------
        private void timer2_Tick(object sender, EventArgs e)
        {
            // разбор xml-файла по xslt-шаблону
            if (checkBox1.Checked)
            {
                ReadPBR();
            }
        }
        //-----------------------------------------------------------------------------
        private void DrawGTP(ZedGraphControl zgc, double[] GTP, int Power, Color col, int DrawFill)
        {
            GraphPane myPane = null;
            if (Power == 110)
                myPane = zg110.GraphPane;
            if (Power == 220)
                myPane = zg220.GraphPane;
            if (Power == 500)
                myPane = zg500.GraphPane;

            // Set the titles and axis labels
            myPane.Title.Text = "График ГТП-" + Convert.ToString(Power);
            myPane.XAxis.Title.Text = "Время, ч";
            myPane.YAxis.Title.Text = "Мощность, МВт";
            myPane.XAxis.Type = AxisType.Linear;
            PointPairList MyList = new PointPairList();
            double maxValue = 0;
            double minValue = 0;

            for (int i = 1; i < 86400; i++)
            {
                double ttt = Convert.ToDouble(i)/ 3600.0;
                MyList.Add(ttt, GTP[i]);
                if (GTP[i] > maxValue)
                    maxValue = GTP[i];
                if (GTP[i] < minValue)
                    minValue = GTP[i];
            }

            // Generate a blue curve with circle symbols, and "My Curve 2" in the legend
            LineItem myCurve = myPane.AddCurve("GTP-" + Convert.ToString(Power), MyList, col, SymbolType.None);
            // Fill the area under the curve with a white-red gradient at 45 degrees
            if (DrawFill == 1)
                myCurve.Line.Fill = new Fill(Color.White, Color.YellowGreen, 45F);
            // myCurve.Line.IsSmooth = true;
            // Make the symbols opaque by filling them with white
            myCurve.Symbol.Fill = new Fill(Color.White);
            

            // Устанавливаем интересующий нас интервал по оси X
            myPane.XAxis.Scale.Min = 0;
            myPane.XAxis.Scale.Max = 24;
            // Устанавливаем интересующий нас интервал по оси Y
            myPane.YAxis.Scale.Min = minValue;
            myPane.YAxis.Scale.Max = maxValue;
            if (minValue == maxValue)
                myPane.YAxis.Scale.Max = minValue + 100;

            // Включаем отображение сетки напротив крупных рисок по оси X
            myPane.XAxis.MajorGrid.IsVisible = true;

            // Задаем вид пунктирной линии для крупных рисок по оси X:
            // Длина штрихов равна 10 пикселям, ... 
            myPane.XAxis.MajorGrid.DashOn = 10;

            // затем 5 пикселей - пропуск
            myPane.XAxis.MajorGrid.DashOff = 5;

            
            // Включаем отображение сетки напротив крупных рисок по оси Y
            myPane.YAxis.MajorGrid.IsVisible = true;

            // Аналогично задаем вид пунктирной линии для крупных рисок по оси Y
            myPane.YAxis.MajorGrid.DashOn = 10;
            myPane.YAxis.MajorGrid.DashOff = 5;


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
            zg110.Location = new Point(11, 34);
            zg220.Location = new Point(11, 34);
            zg500.Location = new Point(11, 34);
            // Leave a small margin around the outside of the control
            tabControl1.Size = new Size(this.ClientSize.Width - 200, this.ClientSize.Height - 200);
            zg110.Size = new Size(this.tabControl1.Width - 20, this.tabControl1.Height - 100);
            zg110.AxisChange();
            zg110.Invalidate();
            zg220.Size = new Size(this.tabControl1.Width - 20, this.tabControl1.Height - 100);
            zg220.AxisChange();
            zg220.Invalidate();
            zg500.Size = new Size(this.tabControl1.Width - 20, this.tabControl1.Height - 100);
            zg500.AxisChange();
            zg500.Invalidate();
        }
        //--------------------------------------------------------------------------
        private void Form1_Resize(object sender, EventArgs e)
        {
            SetSizeZG();
        }
        //--------------------------------------------------------------------------
        private void button5_Click(object sender, EventArgs e)  // обновить график ГТП-110
        {
            double maxValue = 0;
            double minValue = 0;
            for (int i = 1; i < 86400; i++)
            {
                if (GTP110[i] > maxValue)
                    maxValue = GTP110[i];
                if (GTP110[i] < minValue)
                    minValue = GTP110[i];
            }
                // Устанавливаем интересующий нас интервал по оси X
            zg110.GraphPane.XAxis.Scale.Min = 0;
            zg110.GraphPane.XAxis.Scale.Max = 24;
            // Устанавливаем интересующий нас интервал по оси Y
            zg110.GraphPane.YAxis.Scale.Min = minValue;
            zg110.GraphPane.YAxis.Scale.Max = maxValue;
            if (minValue == maxValue)
                zg110.GraphPane.YAxis.Scale.Max = minValue + 100;
            zg110.AxisChange();
            zg110.Invalidate();
        }
        //--------------------------------------------------------------------------
        private void button6_Click(object sender, EventArgs e) // обновить график ГТП-220
        {
            double maxValue = 0;
            double minValue = 0;
            for (int i = 1; i < 86400; i++)
            {
                if (GTP220[i] > maxValue)
                    maxValue = GTP220[i];
                if (GTP220[i] < minValue)
                    minValue = GTP220[i];
            }
            // Устанавливаем интересующий нас интервал по оси X
            zg220.GraphPane.XAxis.Scale.Min = 0;
            zg220.GraphPane.XAxis.Scale.Max = 24;
            // Устанавливаем интересующий нас интервал по оси Y
            zg220.GraphPane.YAxis.Scale.Min = minValue;
            zg220.GraphPane.YAxis.Scale.Max = maxValue;
            if (minValue == maxValue)
                zg220.GraphPane.YAxis.Scale.Max = minValue + 100;
            zg220.AxisChange();
            zg220.Invalidate();
        }
        //--------------------------------------------------------------------------
        private void button7_Click(object sender, EventArgs e) // обновить график ГТП-500
        {
            double maxValue = 0;
            double minValue = 0;
            for (int i = 1; i < 86400; i++)
            {
                if (GTP500[i] > maxValue)
                    maxValue = GTP500[i];
                if (GTP500[i] < minValue)
                    minValue = GTP500[i];
            }
            // Устанавливаем интересующий нас интервал по оси X
            zg500.GraphPane.XAxis.Scale.Min = 0;
            zg500.GraphPane.XAxis.Scale.Max = 24;
            // Устанавливаем интересующий нас интервал по оси Y
            zg500.GraphPane.YAxis.Scale.Min = minValue;
            zg500.GraphPane.YAxis.Scale.Max = maxValue;
            if (minValue == maxValue)
                zg500.GraphPane.YAxis.Scale.Max = minValue + 100;
            zg500.AxisChange();
            zg500.Invalidate();
        }
        //--------------------------------------------------------------------------
        private string zg110_PointValueEvent(ZedGraphControl sender, GraphPane pane, CurveItem curve, int iPt)
        {
            string result;
            PointPair point = curve[iPt];
            result = string.Format("Время: {0:F3}\nМощность: {1:F3}", point.X, point.Y);
            return result;
        }
        //--------------------------------------------------------------------------
        private string zg220_PointValueEvent(ZedGraphControl sender, GraphPane pane, CurveItem curve, int iPt)
        {
            string result;
            PointPair point = curve[iPt];
            result = string.Format("Время: {0:F3}\nМощность: {1:F3}", point.X, point.Y);
            return result;
        }
        //--------------------------------------------------------------------------
        private string zg500_PointValueEvent(ZedGraphControl sender, GraphPane pane, CurveItem curve, int iPt)
        {
            string result;
            PointPair point = curve[iPt];
            result = string.Format("Время: {0:F3}\nМощность: {1:F3}", point.X, point.Y);
            return result;
        }
        //--------------------------------------------------------------------------
        private void button8_Click(object sender, EventArgs e) // моделировать график расчёта 
        {
            ParsePBR();
            switch (comboBox1.SelectedIndex)
            {
                case 0: 
                {
                    // моделируем с шагом по времени
                    comboBox2.Enabled = true;
                    domainUpDown1.Enabled = false;

                    // ГТП-110
                    for (int i = 0; i < ReperPointsNumber110 - 1; i++)  // в данном цикле обрабатывается каждый скачок
                    {
                        // ищем ближайшую реперную точку
                        int StartPoint;
                        int EndPoint;
                        double IdealSquare;         // плановая выработка
                        double RealSquare;          // реальная выработка
                        double SparePower;          // неотработанная мощность
                        double InternalPowerStep;   // внутренняя ступенька по мощности в МВт
                        double InternalStepNumber;  // количество внутренних ступенек по мощности
                        int tempTimeStep;           // локальная переменная с величиной шага по времени
                        int CurrTime;               // текущая секунда
                        int BreakPoint = 0;             // точка излома

                        StartPoint = TimeReperPoints110[i];
                        EndPoint = TimeReperPoints110[i+1];
                        if (GTP110_temp[StartPoint] == GTP110_temp[EndPoint])
                            for(int k = StartPoint ; k<EndPoint ; k++)
                                GTP110_temp[k] = GTP110_temp[StartPoint];
                        else
                        {
                            IdealSquare = 0.5 * Math.Abs(GTP110_temp[EndPoint] - GTP110_temp[StartPoint]) * (Convert.ToDouble(EndPoint - StartPoint) / 3600.0);
                            if ((GTP110_temp[StartPoint] == 0) && (GTP110_temp[EndPoint] > 35))
                            {
                                // если есть переход через границу 35 снизу вверх
                                for (int h = StartPoint; h < EndPoint; h++)
                                {
                                    if ((GTP110_temp[h + 1] > 35) && (GTP110_temp[h] < 35))
                                        BreakPoint = h;
                                }

                                for (CurrTime = StartPoint; CurrTime < EndPoint; CurrTime++)
                                {
                                    if (CurrTime < BreakPoint)
                                    {
                                        tempTimeStep = 1; //период набора мощности для участка 0-35 МВт
                                        InternalStepNumber = (EndPoint - StartPoint) / tempTimeStep;
                                        InternalPowerStep = (GTP110_temp[EndPoint] - GTP110_temp[StartPoint]) / InternalStepNumber;

                                        if ((CurrTime - StartPoint) % tempTimeStep == 0)
                                            GTP110_temp[CurrTime] = GTP110_temp[CurrTime - 1] + InternalPowerStep;
                                        else
                                            GTP110_temp[CurrTime] = GTP110_temp[CurrTime - 1];
                                    }
                                    else
                                    {
                                        tempTimeStep = TimeStep; //период набора мощности для участка 0-35 МВт
                                        InternalStepNumber = (EndPoint - StartPoint) / tempTimeStep;
                                        InternalPowerStep = (GTP110_temp[EndPoint] - GTP110_temp[StartPoint]) / InternalStepNumber;

                                        if ((CurrTime - StartPoint) % tempTimeStep == 0)
                                            GTP110_temp[CurrTime] = GTP110_temp[CurrTime - 1] + InternalPowerStep;
                                        else
                                            GTP110_temp[CurrTime] = GTP110_temp[CurrTime - 1];                                        
                                    }
                                }
                            }
                            else
                            {
                                if ((GTP110_temp[StartPoint] != 0) && (GTP110_temp[EndPoint] == 0))
                                {
                                    // если есть переход через границу 35 сверху вниз
                                    for (int h = StartPoint; h < EndPoint; h++)
                                    {
                                        if ((GTP110_temp[h + 1] < 35) && (GTP110_temp[h] > 35))
                                            BreakPoint = h;
                                    }

                                    for (CurrTime = StartPoint; CurrTime < EndPoint; CurrTime++)
                                    {
                                        if (CurrTime > BreakPoint)
                                        {
                                            tempTimeStep = 1; //период набора мощности для участка 0-35 МВт
                                            InternalStepNumber = (EndPoint - StartPoint) / tempTimeStep;
                                            InternalPowerStep = (GTP110_temp[EndPoint] - GTP110_temp[StartPoint]) / InternalStepNumber;

                                            if ((CurrTime - StartPoint) % tempTimeStep == 0)
                                                GTP110_temp[CurrTime] = GTP110_temp[CurrTime - 1] + InternalPowerStep;
                                            else
                                                GTP110_temp[CurrTime] = GTP110_temp[CurrTime - 1];
                                        }
                                        else
                                        {
                                            tempTimeStep = TimeStep; //период набора мощности для участка 0-35 МВт
                                            InternalStepNumber = (EndPoint - StartPoint) / tempTimeStep;
                                            InternalPowerStep = (GTP110_temp[EndPoint] - GTP110_temp[StartPoint]) / InternalStepNumber;

                                            if ((CurrTime - StartPoint) % tempTimeStep == 0)
                                                GTP110_temp[CurrTime] = GTP110_temp[CurrTime - 1] + InternalPowerStep;
                                            else
                                                GTP110_temp[CurrTime] = GTP110_temp[CurrTime - 1];
                                        }
                                    }
                                }
                                else   // нет перехода через границу 35 мвт
                                {
                                    for (CurrTime = StartPoint; CurrTime < EndPoint; CurrTime++)
                                    {
                                        tempTimeStep = TimeStep; //период набора мощности для участка 0-35 МВт
                                        InternalStepNumber = (EndPoint - StartPoint) / tempTimeStep;
                                        InternalPowerStep = (GTP110_temp[EndPoint] - GTP110_temp[StartPoint]) / InternalStepNumber;

                                        if ((CurrTime - StartPoint) % tempTimeStep == 0)
                                            GTP110_temp[CurrTime] = GTP110_temp[CurrTime - 1] + InternalPowerStep;
                                        else
                                            GTP110_temp[CurrTime] = GTP110_temp[CurrTime - 1];
                                    }
                                }



                            }
                        }
                    }

                    // ГТП-220


                    // ГТП-500




                    break;
                }
                case 1:
                {
                    // моделируем с шагом по мощности
                    domainUpDown1.Enabled = true;
                    comboBox2.Enabled = false;
                    break;
                }
                case 2:
                {
                    // моделируем линейный набор мощности
                    domainUpDown1.Enabled = false;
                    comboBox2.Enabled = false;
                    // для ГТП-110
                    for (int i = 0; i < ReperPointsNumber110 - 1; i++)  // в данном цикле обрабатывается каждый скачок
                    {
                        // ищем ближайшую реперную точку
                        int StartPoint;
                        int EndPoint;

                        StartPoint = TimeReperPoints110[i];
                        EndPoint = TimeReperPoints110[i + 1];
                        if (GTP110_temp[StartPoint] == GTP110_temp[EndPoint])
                            for (int j = StartPoint; j < EndPoint; j++)
                                GTP110_temp[j] = GTP110_temp[StartPoint];
                        else
                        { 
                        double k = (GTP110_temp[EndPoint] - GTP110_temp[StartPoint])/(EndPoint-StartPoint);
                        double b = GTP110_temp[StartPoint] - k*StartPoint;
                        for (int j = StartPoint; j < EndPoint; j++) 
                            GTP110_temp[j] = k*j + b;
                        }
                    }
                    // Для ГТП-220
                    for (int i = 0; i < ReperPointsNumber220 - 1; i++)  // в данном цикле обрабатывается каждый скачок
                    {
                        // ищем ближайшую реперную точку
                        int StartPoint;
                        int EndPoint;

                        StartPoint = TimeReperPoints220[i];
                        EndPoint = TimeReperPoints220[i + 1];
                        if (GTP220_temp[StartPoint] == GTP220_temp[EndPoint])
                            for (int j = StartPoint; j < EndPoint; j++)
                                GTP220_temp[j] = GTP220_temp[StartPoint];
                        else
                        {
                            double k = (GTP220_temp[EndPoint] - GTP220_temp[StartPoint]) / (EndPoint - StartPoint);
                            double b = GTP220_temp[StartPoint] - k * StartPoint;
                            for (int j = StartPoint; j < EndPoint; j++)
                                GTP220_temp[j] = k * j + b;
                        }
                    }
                    // Для ГТП-500
                    for (int i = 0; i < ReperPointsNumber500 - 1; i++)  // в данном цикле обрабатывается каждый скачок
                    {
                        // ищем ближайшую реперную точку
                        int StartPoint;
                        int EndPoint;

                        StartPoint = TimeReperPoints500[i];
                        EndPoint = TimeReperPoints500[i + 1];
                        if (GTP500_temp[StartPoint] == GTP500_temp[EndPoint])
                            for (int j = StartPoint; j < EndPoint; j++)
                                GTP500_temp[j] = GTP500_temp[StartPoint];
                        else
                        {
                            double k = (GTP500_temp[EndPoint] - GTP500_temp[StartPoint]) / (EndPoint - StartPoint);
                            double b = GTP500_temp[StartPoint] - k * StartPoint;
                            for (int j = StartPoint; j < EndPoint; j++)
                                GTP500_temp[j] = k * j + b;
                        }
                    }
                    break;
                }
                case 3:
                {
                    // моделируем экспоненциальный фильтр задания мощности
                    domainUpDown1.Enabled = false;
                    comboBox2.Enabled = false;
                    // для ГТП-110
                    for (int i = 0; i < ReperPointsNumber110 - 1; i++)  // в данном цикле обрабатывается каждый скачок
                    {
                        // ищем ближайшую реперную точку
                        int StartPoint;
                        int EndPoint;

                        StartPoint = TimeReperPoints110[i];
                        EndPoint = TimeReperPoints110[i + 1];
                        if (GTP110_temp[StartPoint] == GTP110_temp[EndPoint])
                            for (int j = StartPoint; j < EndPoint; j++)
                                GTP110_temp[j] = GTP110_temp[StartPoint];
                        else
                        {
                            double alpha = 0.008;
                            for (int j = StartPoint + 1; j < EndPoint; j++)
                            {
                                if (j < StartPoint + 60)
                                    GTP110_temp[j] = GTP110_temp[j - 1];
                                else
                                    GTP110_temp[j] = alpha * GTP110_temp[EndPoint] + (1 - alpha) * GTP110_temp[j - 1];
                            }
                        }
                    }
                    // Для ГТП-220
                    for (int i = 0; i < ReperPointsNumber220 - 1; i++)  // в данном цикле обрабатывается каждый скачок
                    {
                        // ищем ближайшую реперную точку
                        int StartPoint;
                        int EndPoint;

                        StartPoint = TimeReperPoints220[i];
                        EndPoint = TimeReperPoints220[i + 1];
                        if (GTP220_temp[StartPoint] == GTP220_temp[EndPoint])
                            for (int j = StartPoint; j < EndPoint; j++)
                                GTP220_temp[j] = GTP220_temp[StartPoint];
                        else
                        {
                            double alpha = 0.008;
                            for (int j = StartPoint + 1; j < EndPoint; j++)
                            {
                                if (j < StartPoint + 60)
                                    GTP220_temp[j] = GTP220_temp[j - 1];
                                else
                                    GTP220_temp[j] = alpha * GTP220_temp[EndPoint] + (1 - alpha) * GTP220_temp[j - 1];
                            }
                        }
                    }
                    // Для ГТП-500
                    for (int i = 0; i < ReperPointsNumber500 - 1; i++)  // в данном цикле обрабатывается каждый скачок
                    {
                        // ищем ближайшую реперную точку
                        int StartPoint;
                        int EndPoint;

                        StartPoint = TimeReperPoints500[i];
                        EndPoint = TimeReperPoints500[i + 1];
                        if (GTP500_temp[StartPoint] == GTP500_temp[EndPoint])
                            for (int j = StartPoint; j < EndPoint; j++)
                                GTP500_temp[j] = GTP500_temp[StartPoint];
                        else
                        {
                            double alpha = 0.008;
                            for (int j = StartPoint + 1; j < EndPoint; j++)
                            {
                                if (j < StartPoint + 60)
                                    GTP500_temp[j] = GTP500_temp[j - 1];
                                else
                                    GTP500_temp[j] = alpha * GTP500_temp[EndPoint] + (1 - alpha) * GTP500_temp[j - 1];
                            }
                        }
                    }
                    break;
                } 

                    
            }
            // рисуем интерполированный график загрузки станции
            DrawGTP(zg110, GTP110_temp, 110, Color.Red, 1);
            DrawGTP(zg220, GTP220_temp, 220, Color.Red, 1);
            DrawGTP(zg500, GTP500_temp, 500, Color.Red, 1);
            
        }
        //--------------------------------------------------------------------------
        private void domainUpDown1_Click(object sender, EventArgs e)
        {
            PowerStep = Convert.ToInt32(domainUpDown1.Text);
        }
        //--------------------------------------------------------------------------
        private void button9_Click(object sender, EventArgs e) // очистить график ГТП-110
        {
            zg110.GraphPane.CurveList.Clear();
            zg110.AxisChange();
            zg110.Invalidate();
        }
        //--------------------------------------------------------------------------
        private void button10_Click(object sender, EventArgs e) // очистить график ГТП-220
        {
            zg220.GraphPane.CurveList.Clear();
            zg220.AxisChange();
            zg220.Invalidate();
        }
        //--------------------------------------------------------------------------
        private void button11_Click(object sender, EventArgs e) // очистить график ГТП-500
        {
            zg500.GraphPane.CurveList.Clear();
            zg500.AxisChange();
            zg500.Invalidate();
        }
        //--------------------------------------------------------------------------
        private void comboBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            switch (comboBox1.SelectedIndex)
            {
                case 0:
                    {
                        // моделируем с шагом по времени
                        comboBox2.Enabled = true;
                        domainUpDown1.Enabled = false;
                        break;
                    }
                case 1:
                    {
                        // моделируем с шагом по мощности
                        comboBox2.Enabled = false;
                        domainUpDown1.Enabled = true;
                        break;
                    }
                case 2:
                    {
                        // моделируем с шагом по мощности
                        comboBox2.Enabled = false;
                        domainUpDown1.Enabled = false;
                        break;
                    }
                case 3:
                    {
                        // моделируем с шагом по мощности
                        comboBox2.Enabled = false;
                        domainUpDown1.Enabled = false;
                        break;
                    }
            }
        }
        //--------------------------------------------------------------------------
        private void comboBox2_SelectedValueChanged(object sender, EventArgs e)
        {
            TimeStep = Convert.ToInt32(comboBox2.Text);
        }
        //--------------------------------------------------------------------------

    }
}
