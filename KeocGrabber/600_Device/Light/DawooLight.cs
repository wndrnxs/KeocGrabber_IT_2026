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
    class DawooLight
    {
        List<SerialComm> m_listComm = new List<SerialComm>();

        public bool this[int index] { get { return (index >= 0 && m_listComm.Count > index) ? m_listComm[index].IsConnected : false; } }

        public int CtrlCount { get { return m_listComm.Count; } }

        int m_nRecieveTimeout = 3000;

        bool[] m_bIsRecv = new bool[4];

        const int MAX_CHANNEL = 3;
        bool[,] m_bOnoff = new bool[4, MAX_CHANNEL];
        int[,] m_nLightValue = new int[4, MAX_CHANNEL];

        List<byte>[] m_bufRecvBuff = new List<byte>[4];

        byte m_bReqMode = 0x00;

        public DawooLight()
        {

        }

        /// <summary>
        /// DAWOO 조명 컨트롤러 포트 열기.
        /// 포트 정보는 시스템 파라메터에서 받아옴.
        /// </summary>
        public void fn_InitPort()
        {
            bool[] bRet = new bool[G.SYSTEM.CamCount];
            for (int i = 0; i < G.SYSTEM.CamCount; i++)
            {
                m_listComm.Add(new SerialComm());

                switch (i) {
                    case 0:
                        bRet[0] = m_listComm[0].Open(G.SYSTEM.LightPortTop1, 9600, 8, Parity.None, StopBits.One);
                        m_listComm[0].ReciveDataevent = RecvData1;
                        break;
                    case 1:
                        bRet[1] = m_listComm[1].Open(G.SYSTEM.LightPortTop2, 9600, 8, Parity.None, StopBits.One);
                        m_listComm[1].ReciveDataevent = RecvData2;
                        break;
                    case 2:
                        bRet[2] = m_listComm[2].Open(G.SYSTEM.LightPortTop3, 9600, 8, Parity.None, StopBits.One);
                        m_listComm[2].ReciveDataevent = RecvData3;
                        break;
                    case 3:
                        bRet[3] = m_listComm[3].Open(G.SYSTEM.LightPortTop4, 9600, 8, Parity.None, StopBits.One);
                        m_listComm[3].ReciveDataevent = RecvData4;
                        break;
                }

                m_bIsRecv[i] = false;

                m_bufRecvBuff[i] = new List<byte>();

                G.WriteLog($"Light Controller Open {(bRet[i] ? "Succ." : "Fail.")}. Bot {i + 1} : {m_listComm[i].ConnPort}");
            }

            //m_listComm.Add(new SerialComm());
            //m_listComm.Add(new SerialComm());
            //m_listComm.Add(new SerialComm());
            //m_listComm.Add(new SerialComm());


            //bRet[0] = m_listComm[0].Open(G.SYSTEM.LightPortTop1, 9600, 8, Parity.None, StopBits.One);
            //bRet[1] = m_listComm[1].Open(G.SYSTEM.LightPortTop2, 9600, 8, Parity.None, StopBits.One);
            //bRet[2] = m_listComm[2].Open(G.SYSTEM.LightPortTop3, 9600, 8, Parity.None, StopBits.One);
            //bRet[3] = m_listComm[3].Open(G.SYSTEM.LightPortTop4, 9600, 8, Parity.None, StopBits.One);

            //m_listComm[0].ReciveDataevent = RecvData1;
            //m_listComm[1].ReciveDataevent = RecvData2;
            //m_listComm[2].ReciveDataevent = RecvData3;
            //m_listComm[3].ReciveDataevent = RecvData4;

            //m_bIsRecv[0] = m_bIsRecv[1] = m_bIsRecv[2] = m_bIsRecv[3] = false;

            //m_bufRecvBuff[0] = new List<byte>();
            //m_bufRecvBuff[1] = new List<byte>();
            //m_bufRecvBuff[2] = new List<byte>();
            //m_bufRecvBuff[3] = new List<byte>();

            //for (int i = 0; i < m_listComm.Count; i++)
            //{
            //    G.WriteLog($"Light Controller Open {(bRet[i] ? "Succ." : "Fail.")}. Bot {i + 1} : {m_listComm[i].ConnPort}");
            //}
        }

        /// <summary>
        /// DAWOO 조명 컨트롤러 포트 해제.
        /// </summary>
        public void fn_FinalPort()
        {
            bool bRet = false;
            for (int i = 0; i < m_listComm.Count; i++)
            {
                bRet = m_listComm[i].Close();
                G.WriteLog($"Light Controller Open {(bRet ? "Succ." : "Fail.")}. [{m_listComm[i].ConnPort}]");
            }
            m_listComm.Clear();
        }

        /// <summary>
        /// DAWOO Light 값 변경
        /// </summary>
        /// <param name="idx">컨트롤러 인덱스 [0~3]</param>
        /// <param name="value">컨트롤러 전체 채널 조명 값 [0~255]</param>
        public void SetLightValue(int idx, int value)
        {
            if (m_listComm.Count > idx && idx >= 0)
            {
                byte[] strcmd = new byte[6];
                strcmd[0] = 0xff;
                strcmd[1] = 0x01;        // Light Value Cmd
                strcmd[2] = 0x07;        // channel
                strcmd[3] = (byte)(value >> 8);        // Param
                strcmd[4] = (byte)(value & 0xff); // Light Value
                strcmd[5] = (byte)(strcmd[0] ^ strcmd[1] ^ strcmd[2] ^ strcmd[3] ^ strcmd[4]);
                m_listComm[idx].WriteValue(strcmd, false, false);
                G.WriteLog($"Dawoo {idx + 1} Set : {value}");
            }
        }

        public int? GetLightValue(int idx, int ch)
        {
            int? value = null;
            if (m_listComm.Count > idx && idx >= 0)
            {
                if (!m_listComm[idx].IsConnected)
                    return null;
                byte[] strcmd = new byte[3];
                strcmd[0] = 0xff;
                strcmd[1] = 0x11;        // Read Cmd
                strcmd[2] = 0xee;        // channel
                m_bIsRecv[idx] = false;
                m_bReqMode = strcmd[1];

                m_listComm[idx].WriteValue(strcmd);
                if (fn_CheckTimeOut(idx) == -1)
                {
                    G.WriteLog($"Dawoo {idx + 1} Time-Out");
                }
                else
                {
                    value = m_nLightValue[idx, ch];
                    G.WriteLog($"Dawoo {idx + 1} / {ch} Get : {value}");
                }

            }
            return value;
        }

        public bool GetLightOnOff(int idx)
        {
            bool bRet = true;
            if (m_listComm.Count > idx && idx >= 0)
            {
                if (!m_listComm[idx].IsConnected)
                    return false;

                byte[] strcmd = new byte[3];
                strcmd[0] = 0xff;
                strcmd[1] = 0x12;        // Read OnOff Cmd
                strcmd[2] = 0xed;        // channel
                m_bIsRecv[idx] = false;
                m_bReqMode = strcmd[1];

                m_listComm[idx].WriteValue(strcmd);
                if (fn_CheckTimeOut(idx) == -1)
                {
                    G.WriteLog($"Dawoo {idx + 1} Time-Out");
                    bRet = false;
                }
            }
            return bRet;
        }


        /// <summary>
        /// DAWOO Light On
        /// </summary>
        /// <param name="idx">컨트롤러 인덱스 [0~3]</param>
        public void LightOn(int idx)
        {
            if (m_listComm.Count > idx && idx >= 0)
            {
                byte[] strcmd = new byte[5];
                strcmd[0] = 0xff;
                strcmd[1] = 0x02; // On/Off Cmd
                strcmd[2] = 0x07; // All Channel
                strcmd[3] = 0x01; // Light On
                strcmd[4] = (byte)(strcmd[0] ^ strcmd[1] ^ strcmd[2] ^ strcmd[3]);
                m_listComm[idx].WriteValue(strcmd, false, false);
                G.WriteLog($"Dawoo {idx + 1} Light ON");
            }
        }

        /// <summary>
        /// DAWOO Light Off
        /// </summary>
        /// <param name="idx">컨트롤러 인덱스 [0~3]</param>
        public void LightOff(int idx)
        {
            if (m_listComm.Count > idx && idx >= 0)
            {
                byte[] strcmd = new byte[5];
                strcmd[0] = 0xff;
                strcmd[1] = 0x02; // On/Off Cmd
                strcmd[2] = 0x07; // All Channel
                strcmd[3] = 0x00; // Light Off
                strcmd[4] = (byte)(strcmd[0] ^ strcmd[1] ^ strcmd[2] ^ strcmd[3]);
                m_listComm[idx].WriteValue(strcmd, false, false);
                G.WriteLog($"Dawoo {idx + 1} Light OFF");
            }
        }

        /// <summary>
        /// Get 함수 사용 시 데이터 수신 확인 함수.
        /// </summary>
        /// <param name="idx">컨트롤러 인덱스</param>
        /// <returns>성공시 0, 타임아웃 발생시 -1</returns>
        private int fn_CheckTimeOut(int idx)
        {
            int nState = 0;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (!m_bIsRecv[idx])
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
        /// <param name="index">컨트롤러 인덱스</param>
        /// <param name="buff">확인할 버퍼 데이터</param>
        /// <returns></returns>
        private int CheckData(int index, byte[] buff)
        {
            int nRet = -1;

            switch (m_bReqMode)
            {
                case 0x11: // Dimming값 읽기
                    //m_nLightValue[index, 0] = (buff[1] << 8) | buff[2];
                    //m_nLightValue[index, 1] = (buff[3] << 8) | buff[4];
                    //m_nLightValue[index, 2] = (buff[5] << 8) | buff[6];
                    m_nLightValue[index, 0] = buff[2];
                    m_nLightValue[index, 1] = buff[4];
                    m_nLightValue[index, 2] = buff[6];
                    m_bReqMode = 0x00;
                    nRet = 1;
                    break;
                case 0x12: // 광원 ON/OFF 상태 읽기
                    
                    m_bOnoff[index, 0] = (buff[2] & 0x01) == 0x01;
                    m_bOnoff[index, 1] = (buff[2] & 0x02) == 0x02;
                    m_bOnoff[index, 2] = (buff[2] & 0x04) == 0x04;

                    m_bReqMode = 0x00;
                    nRet = 1;
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
            // Append Data
            const int INDEX = 0;
            Dataparsing(INDEX, buff);
        }

        /// <summary>
        /// 컨트롤러 2번 콜백.
        /// </summary>
        /// <param name="buff">수신 데이터</param>
        private void RecvData2(byte[] buff)
        {
            // Append Data
            const int INDEX = 1;
            Dataparsing(INDEX, buff);
        }

        /// <summary>
        /// 컨트롤러 3번 콜백.
        /// </summary>
        /// <param name="buff">수신 데이터</param>
        private void RecvData3(byte[] buff)
        {
            // Append Data
            const int INDEX = 2;
            Dataparsing(INDEX, buff);
        }

        /// <summary>
        /// 컨트롤러 4번 콜백.
        /// </summary>
        /// <param name="buff">수신 데이터</param>
        private void RecvData4(byte[] buff)
        {
            // Append Data
            const int INDEX = 3;
            Dataparsing(INDEX, buff);
        }

        private void Dataparsing(int index, byte[] buff)
        {

            //0x11 9byte.
            //0x12 4byte.
            // 1. 버퍼 누적.
            m_bufRecvBuff[index].AddRange(buff);

            List<byte> data;

            // 2. 누적된 버퍼에서 요청한 모드에 대한 응담 체크. (0x11, 0x12)
            int idx = SerialComm.IndexOfByteArray(m_bufRecvBuff[index], m_bReqMode);
            if (idx > -1)
            {
                int nCheckCount = 0;
                switch (m_bReqMode)
                {
                    case 0x11: nCheckCount = 9; break;
                    case 0x12: nCheckCount = 4; break;
                }

                // 2-2. 응답이 있다면 각 모드에 대한 길이 체크. idx-1에 0xff여부 및 길이 체크.
                if (nCheckCount > 0 && m_bufRecvBuff[index][idx - 1] == 0xff && m_bufRecvBuff[index].Count > (idx - 1 + nCheckCount))
                {
                    // 조건이 만족하면 데이터 파싱.
                    data = SerialComm.SubBytes(m_bufRecvBuff[index], idx - 1, nCheckCount);
                    m_bufRecvBuff[index] = SerialComm.RemoveBytes(m_bufRecvBuff[index], idx - 1, nCheckCount);

                    // Check Protocall
                    if (CheckData(index, data.ToArray()) > 0)
                    {
                        m_bIsRecv[index] = true;
                    }
                }
            }
            else
                return;
            // 2-1. 응답이 없다면 리턴.
        }

        public bool IsLightOn(int idx)
        {
            bool bRet = true;
            if (idx > -1 && idx < CtrlCount)
            {
                for (int i = 0; i < MAX_CHANNEL; i++)
                {
                    bRet &= m_bOnoff[idx, i];
                }
            }
            else
            {
                bRet = false;
            }
            return bRet;
        }
    }
}
