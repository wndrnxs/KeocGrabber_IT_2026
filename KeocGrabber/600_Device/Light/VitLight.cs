/*
 *******************************************************************************
 * 해당 소스는 현장 유지 보수 외에 다른 목적의 사용을 금합니다.
 * - 주식회사 태루 -
 *******************************************************************************
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KeocGrabber
{
    internal class VitLight
    {
        SerialComm serialComm = new SerialComm();

        public bool IsConnected { get { return serialComm.IsConnected; } }

        int m_nRecieveTimeout = 3000;

        bool m_bIsRecv = false;

        const int MAX_CHANNEL = 16;
        bool[] m_bOnoff = new bool[MAX_CHANNEL];
        int[] m_nLightValue = new int[MAX_CHANNEL];

        List<byte> m_bufRecvBuff = new List<byte>();

        public VitLight()
        {
            //comm.Open
        }

        /// <summary>
        /// Vit 조명 컨트롤러 포트 열기.
        /// 포트 정보는 시스템 파라메터에서 받아옴.
        /// </summary>
        public void fn_InitPort()
        {
            bool bRet = false;

            bRet = serialComm.Open(G.SYSTEM.LightPortBot1, 9600, 8, Parity.None, StopBits.One);

            serialComm.ReciveDataevent = RecvData1;

            m_bIsRecv = false;

            m_bufRecvBuff = new List<byte>();

            G.WriteLog($"Light Controller Open {(bRet ? "Succ." : "Fail.")}. Bottom : {serialComm.ConnPort}");

            //RecvData1(Encoding.ASCII.GetBytes("R1255\r\n"));
            //RecvData1(new byte[] { 0x4f,0x4e,0x46,0xf0,0xf0,0x0d});
            //RecvData1(new byte[] { 0x0a});
        }

        /// <summary>
        /// Vit 조명 컨트롤러 포트 해제.
        /// </summary>
        public void fn_FinalPort()
        {
            bool bRet = false;
            bRet = serialComm.Close();
            G.WriteLog($"Light Controller Open {(bRet ? "Succ." : "Fail.")}. [{serialComm.ConnPort}]");
        }

        /// <summary>
        /// Vit Light 값 변경
        /// </summary>
        /// <param name="idx">컨트롤러 인덱스 [0~3]</param>
        /// <param name="value">컨트롤러 전체 조명 값 [0~255]</param>
        public void SetLightValue(int value)
        {
            // D0123\r\0
            // D : 조명값 설정 명령
            // 1~16 : 채널
            // 123 : 조명 값.
            for (int i = 1; i <= 8; i++)
            //int i = 1;
            {
                string strcmd = $"D{i:X}{value:D3}";
                serialComm.WriteValue(Encoding.ASCII.GetBytes(strcmd));
            }
            G.WriteLog($"Vit Set : {value}");
        }
        /// <summary>
        /// Vit Light 값 변경
        /// </summary>
        /// <param name="ch">컨트롤러 채널 [1~16]</param>
        /// <param name="value">컨트롤러 전체 조명 값 [0~255]</param>
        public void SetLightValue(int ch, int value)
        {
            // D0123\r\0
            // D : 조명값 설정 명령
            // 1~16 : 채널
            // 123 : 조명 값.
            
            string strcmd = $"D{ch:X}{value:D3}";
            serialComm.WriteValue(Encoding.ASCII.GetBytes(strcmd));

            G.WriteLog($"Vit Set : {ch}, {value}");
        }

        public int? GetLightValue(int ch)
        {
            int? value = null;
            if (!serialComm.IsConnected)
                return null;
            m_bIsRecv = false;
            // R0DAT\r\0 : 데이터 값 요청.
            // R0ONF\r\0 : ON/OFF 요청.
            // R : 조명값 설정 명령
            // 0 : 채널
            // DAT : 조명 값.
            string strcmd = $"R{ch:X}DAT";
            serialComm.WriteValue(Encoding.ASCII.GetBytes(strcmd));

            if (fn_CheckTimeOut() == -1)
            {
                G.WriteLog($"Vit Time-Out");
            }
            else
            {
                value = m_nLightValue[ch];
                G.WriteLog($"Vit {ch} Get : {value}");
            }
            return value;
        }

        public bool GetLightOnOff()
        {
            if (!serialComm.IsConnected)
                return false;
            bool bRet = true;
            m_bIsRecv = false;

            string strcmd = $"ROONF";
            serialComm.WriteValue(Encoding.ASCII.GetBytes(strcmd));
            if (fn_CheckTimeOut() == -1)
            {
                G.WriteLog($"Vit Time-Out");
                bRet = false;
            }

            return bRet;
        }

        /// <summary>
        /// Vit Light On
        /// ※ ONF CMD 사용 하지 말것. 이상동작.-> ONN 사용
        /// </summary>
        /// <param name="idx">컨트롤러 인덱스 [0~3]</param>
        public void LightOn(int ch = -1)
        {
            //string strcmd = $"ONF{0xff}{0xff}";
            byte[] bytecmd = new byte[5];
            bytecmd[0] = (byte)'O';
            bytecmd[1] = (byte)'N';
            bytecmd[2] = (byte)'N';
            bytecmd[3] = 0x00;
            
            if (ch == -1) bytecmd[4] = 0xff;
            else bytecmd[4] = (byte)(0x01 << ch);

            serialComm?.WriteValue(bytecmd);

            if (ch == -1)   G.WriteLog($"Vit All ch Light ON");
            else            G.WriteLog($"Vit {ch + 1} ch Light ON");
        }

        /// <summary>
        /// Vit Light Off
        /// ※ ONF CMD 사용 하지 말것. 이상동작.-> OFF 사용
        /// </summary>
        /// <param name="ch">조명 채널(-1 : All Channel)</param>
        public void LightOff(int ch = -1)
        {
            //string strcmd = $"ONF{0x00}{0x00}";
            byte[] bytecmd = new byte[5];
            bytecmd[0] = (byte)'O';
            bytecmd[1] = (byte)'F';
            bytecmd[2] = (byte)'F';
            bytecmd[3] = 0x00;
                
            if (ch == -1) bytecmd[4] = 0xff;
            else bytecmd[4] = (byte)(0x01 << ch);

            serialComm?.WriteValue(bytecmd);

            if (ch == -1)   G.WriteLog($"Vit All ch Light OFF");
            else            G.WriteLog($"Vit {ch + 1} ch Light OFF");
        }

        /// <summary>
        /// Get 함수 사용 시 데이터 수신 확인 함수.
        /// </summary>
        /// <param name="idx">컨트롤러 인덱스</param>
        /// <returns>성공시 0, 타임아웃 발생시 -1</returns>
        private int fn_CheckTimeOut()
        {
            int nState = 0;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (!m_bIsRecv)
            {
                Thread.Sleep(50);
                if (sw.ElapsedMilliseconds > m_nRecieveTimeout)
                {
                    nState = -1; // time-out
                    break;
                }
            }

            return nState;
        }

        /// <summary>
        /// 데이터 프로토콜 분석기
        /// </summary>
        /// <param name="buff">확인할 버퍼 데이터</param>
        /// <returns></returns>
        private int CheckData(byte[] buff)
        {
            int nRet = -1;
            string strCh = "";
            string strValue = "";

            switch (buff[0])
            {
                case (byte)'R': // Return Light Value
                    strCh += (char)buff[1];
                    strValue += (char)buff[2];
                    strValue += (char)buff[3];
                    strValue += (char)buff[4];

                    try
                    {
                        int ch = Convert.ToInt32(strCh);
                        int val = Convert.ToInt32(strValue);
                        m_nLightValue[ch] = val;
                        nRet = 1;
                    }
                    catch (Exception ex)
                    {
                        G.WriteLog($"Vit Check Data Error : {ex.Message}");
                    }
                    break;
                case (byte)'O': // On/Off
                    //ushort value = (ushort)(buff[3] + (buff[4] << 8));
                    ushort value = (ushort)((buff[3]<<8) + buff[4]);

                    int testvalue = 0;
                    for (int i = 0; i < 16; i++)
                    {
                        testvalue = ((value >> i) & 0x01);
                        m_bOnoff[i] = testvalue == 1 ? true : false;
                    }
                    nRet = 1;
                    //buff[3] 
                    break;
                case (byte)'E': // Error
                    nRet = 1;
                    G.WriteLog($"Vit CMD Error");
                    break;
            }
            return nRet;
        }

        /// <summary>
        /// 컨트롤러 1번 콜백.
        /// </summary>
        /// <param name="buff">수신 데이터</param>
        private void RecvData1(byte[] buff)
        {
            m_bufRecvBuff.AddRange(buff);

            int idx = SerialComm.IndexOfByteArray(m_bufRecvBuff, (byte)'\n');
            List<byte> data;
            if (idx > 0)
            {
                data = SerialComm.SubBytes(m_bufRecvBuff, 0, idx);
                m_bufRecvBuff = SerialComm.RemoveBytes(m_bufRecvBuff, 0, idx + 1);
            }
            else
                return;

            // Check Protocall
            if (CheckData(data.ToArray()) > 0)
            {
                m_bIsRecv = true;
            }
        }

        public bool IsLightOn(int idx)
        {
            if (idx > -1 && idx < MAX_CHANNEL)
            {
                return m_bOnoff[idx];
            }
            else
                return false;
        }

    }
}
