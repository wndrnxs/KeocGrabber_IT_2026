/*
 *******************************************************************************
 * 해당 소스는 현장 유지 보수 외에 다른 목적의 사용을 금합니다.
 * - 주식회사 태루 -
 *******************************************************************************
 */
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KeocGrabber
{
    public class SerialComm
    {
        SerialPort m_sPort;
        Action<byte[]> del_ReciveData = null;
        public Action<byte[]> ReciveDataevent { set { del_ReciveData = value; } }
        byte[] m_CRLF = new byte[2];
        List<int>       m_BaudrateList  = new List<int>     ();
        List<Parity>    m_ParityList    = new List<Parity>  ();
        List<int>       m_DatabitList   = new List<int>     ();
        List<StopBits>  m_StopbitList   = new List<StopBits>();

        public bool IsConnected { get { return m_sPort?.IsOpen == true ? true : false; } }

        string m_strLastReciveMsg = "";
        public string LastReciveMsg { get { return m_strLastReciveMsg; } set { m_strLastReciveMsg = value; } }

        string strConnPort = "";
        public string ConnPort { get { return strConnPort; } }

        private bool bWork = false;
        private Queue<byte[]> m_queCMD = new Queue<byte[]> ();
        private Mutex m_mutex = new Mutex();

        private Thread m_thread = null;

        public SerialComm()
        {
            m_CRLF[0] = 0x0d;
            m_CRLF[1] = 0x0a;
            
            InitList();
        }
        
        private void InitList()
        {
            m_BaudrateList.Clear();
            m_BaudrateList.Add(600);
            m_BaudrateList.Add(1200);
            m_BaudrateList.Add(2400);
            m_BaudrateList.Add(4800);
            m_BaudrateList.Add(9600);
            m_BaudrateList.Add(14400);
            m_BaudrateList.Add(19200);
            m_BaudrateList.Add(28800);
            m_BaudrateList.Add(33600);
            m_BaudrateList.Add(38400);
            m_BaudrateList.Add(56000);
            m_BaudrateList.Add(57600);
            m_BaudrateList.Add(115200);
            m_BaudrateList.Add(128000);
            m_BaudrateList.Add(256000);

            m_ParityList.Clear();
            m_ParityList.Add(Parity.None);
            m_ParityList.Add(Parity.Even);
            m_ParityList.Add(Parity.Odd);
            m_ParityList.Add(Parity.Mark);
            m_ParityList.Add(Parity.Space);

            m_DatabitList.Clear();
            m_DatabitList.Add(7);
            m_DatabitList.Add(8);

            m_StopbitList.Clear();
            m_StopbitList.Add(StopBits.None);
            m_StopbitList.Add(StopBits.One);
            m_StopbitList.Add(StopBits.OnePointFive);
            m_StopbitList.Add(StopBits.Two);
        }

        static public string[] CommPortList()
        {
            return SerialPort.GetPortNames();
        }

        public bool CheckCommport(string strPortName)
        {
            bool bRet = false;

            if (CommPortList().Where(c => c == strPortName).ToArray().Length > 0)
                bRet = true;

            return bRet;
        }

        public bool Open(string port, string baudrate, string databit, string parity, string stopbit)
        {
            bool bRet = true;

            bRet &= CheckCommport(port);
            var retbaudrate = m_BaudrateList.Where(c => c.ToString() == baudrate).ToArray();
            var retdatabit = m_DatabitList.Where(c => c.ToString() == databit).ToArray();
            var retparity = m_ParityList.Where(c => c.ToString() == parity).ToArray();
            var retstop = m_StopbitList.Where(c => c.ToString() == stopbit).ToArray();

            bRet &= retbaudrate.Length > 0;
            bRet &= retdatabit.Length > 0;
            bRet &= retparity.Length > 0;
            bRet &= retstop.Length > 0;

            bRet &= Open(port, retbaudrate[0], retdatabit[0], retparity[0], retstop[0]);

            return bRet;
        }

        public bool OpenAuto(byte[] sendcommand, byte[] checkcommand, int baudrate)
        {
            bool bRet = false;
            //bRet = TryOpen(ctrltype);

            if (!bRet)
            {
                DateTime StartTime;
                string[] comports = CommPortList();
                byte[] bytelightoff = { 0x4C, 0x15, 0x00, 0x00, 0x00, 0x15, 0x0D, 0x0A };

                string strRtn = "";
                byte[] byteRtn;

                int nChkLen = 0;
                for (int i = 0; i < comports.Length; i++)
                {
                    try
                    {
                        if (m_sPort != null)
                            break;

                        m_sPort = new SerialPort();

                        m_sPort.DataReceived += new SerialDataReceivedEventHandler(Serial_DataReceived);
                        m_sPort.PortName = comports[i];
                        m_sPort.BaudRate = baudrate;
                        m_sPort.DataBits = 8;
                        m_sPort.Parity = Parity.None;
                        m_sPort.StopBits = StopBits.One;
                        m_sPort.ReadTimeout = 3;
                        m_sPort.Open();

                        m_sPort.Write(sendcommand, 0, sendcommand.Length);
                        nChkLen = checkcommand.Length;

                        StartTime = DateTime.Now;
                        while (true)
                        {
                            if (m_sPort.BytesToRead != 0)
                            {
                                strRtn = m_sPort.ReadLine();
                                byteRtn = Encoding.ASCII.GetBytes(strRtn);

                                if ((byteRtn.Length + 1) == nChkLen)
                                {
                                    strConnPort = comports[i];
                                    bRet = true;
                                    break;
                                }
                                else
                                {
                                    PortClose();
                                    break;
                                }
                            }
                            if ((DateTime.Now - StartTime).TotalSeconds >= 3)
                            {
                                // Time-Out
                                PortClose();
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        PortClose();
                    }
                }
            }
            return bRet;
        }

        public bool OpenIndex(string port, int baudrate, int databit, int parity, int stopbit)
        {
            bool bRet = false;

            bRet = Open(port, m_BaudrateList[baudrate], m_DatabitList[databit], m_ParityList[parity], m_StopbitList[stopbit]);

            return bRet;
        }

        public bool Open(string port, int baudrate, int databit, Parity parity, StopBits stopbit)
        {
            bool bRet = false;

            try
            {
                if (CheckCommport(port))
                {
                    if (m_sPort == null)
                    {
                        m_sPort = new SerialPort();

                        m_sPort.DataReceived += new SerialDataReceivedEventHandler(Serial_DataReceived);
                        m_sPort.PortName = port;
                        m_sPort.BaudRate = baudrate;
                        m_sPort.DataBits = databit;
                        m_sPort.Parity = parity;
                        m_sPort.StopBits = stopbit;
                        

                        m_sPort.Open();
                        strConnPort = port;

                        m_thread = new Thread(new ThreadStart(THREAD_SENDDATA));
                        bWork = true;
                        m_thread.Start();

                        bRet = true;
                    }
                }
                else
                {
                    // Can't Find CommPort
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
                m_sPort.Close();
                m_sPort = null;
            }

            return bRet;
        }

        public bool Close()
        {
            bool bRet = false;

            if (m_sPort != null)
            {
                if (m_sPort.IsOpen)
                {
                    PortClose();
                    bRet = true;
                }
            }
            bWork = false;
            m_thread?.Abort();
            return bRet;
        }

        private void PortClose()
        {
            
            m_sPort.DataReceived -= Serial_DataReceived;
            m_sPort.Close();
            m_sPort = null;
        }

        private void Serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (strConnPort != "" && m_sPort.BytesToRead != 0)
            {
                try
                {
                    byte[] buffer = new byte[m_sPort.BytesToRead];
                    m_sPort.Read(buffer, 0, m_sPort.BytesToRead);
                    //Console.WriteLine($"{(EN_ACK)buffer[0]}");
                    m_strLastReciveMsg = Encoding.ASCII.GetString(buffer);
                    //m_strLastReciveMsg = m_sPort.ReadLine();
                    del_ReciveData?.Invoke(buffer);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public bool WriteValue(byte[] bufMsg, bool bCR = true, bool bLF = true)
        {
            bool bRet = false;
            byte[] buff = null;

            if (bCR && bLF) buff = new byte[bufMsg.Length + m_CRLF.Length];
            else if (bCR)   buff = new byte[bufMsg.Length + 1];
            else if (bLF)   buff = new byte[bufMsg.Length + 1];
            else            buff = new byte[bufMsg.Length];
            
            Array.Copy(bufMsg, 0, buff, 0, bufMsg.Length);

            if (bCR && bLF) Array.Copy(m_CRLF, 0, buff, bufMsg.Length, m_CRLF.Length);
            else if (bCR) buff[bufMsg.Length] = m_CRLF[0];
            else if (bLF) buff[bufMsg.Length] = m_CRLF[1];

            if (m_sPort != null && m_sPort.IsOpen)
            {
                m_mutex.WaitOne();
                m_queCMD.Enqueue(buff);
                m_mutex.ReleaseMutex();
                //m_sPort.Write(buff, 0, buff.Length);
                bRet = true;
            }

            return bRet;
        }

        public bool WriteValue(string strMsg, bool bCR = true, bool bLF = true)
        {
            bool bRet = false;
            byte[] bufMsg = Encoding.ASCII.GetBytes(strMsg);

            bRet = WriteValue(bufMsg, bCR, bLF);

            return bRet;
        }

        private void THREAD_SENDDATA()
        {
            while (bWork)
            {
                if (m_queCMD.Count > 0)
                {
                    if (m_mutex.WaitOne())
                    {
                        byte[] buff = m_queCMD.Dequeue();

                        if (m_sPort.IsOpen)
                        {
                            m_sPort.Write(buff, 0, buff.Length);
                        }
                        m_mutex.ReleaseMutex();
                        Thread.Sleep(G.SYSTEM.SerialDelay);
                    }
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
        }


        public static int IndexOfByteArray(List<byte> bytes, byte cmp)
        {
            int idx = -1;
            for (int i = 0; i < bytes.Count; i++)
            {
                if (bytes[i] == cmp)
                {
                    idx = i;
                    break;
                }
            }
            return idx;
        }

        public static List<byte> SubBytes(List<byte> bytes, int start, int count)
        {
            List<byte> rtn = new List<byte>();
            for (int i = 0; i < count; i++)
            {
                rtn.Add(bytes[start + i]);
            }
            return rtn;
        }

        public static List<byte> RemoveBytes(List<byte> bytes, int start, int count)
        {
            List<byte> rtn = new List<byte>();

            for (int i = 0; i < count; i++)
            {
                bytes.RemoveAt(start);
            }

            return rtn;
        }
    }
}
