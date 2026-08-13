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
    class VieworksCamera
    {
        Func<string, bool> delWriteLog = null;
        public Func<string, bool> WriteLog { set { delWriteLog = value; } }
        SerialComm m_comm = new SerialComm();
        public bool this[int index] { get { return m_comm.IsConnected; } }

        public string Comport { get { return m_comm.ConnPort; } }

        public bool IsConnected { get { return m_comm != null ? m_comm.IsConnected : false; } }

        bool m_bUpdated = false;
        int m_nRecieveTimeout = 5000;

        int  ?   m_nImageOffset          = 0;
        int  ?   m_nImageWidth           = 0;
        float?   m_fLinePeriod           = 0.0f;
        float?   m_fExposureTime         = 0.0f;
        int  ?   m_nTestImage            = 0;
        int  ?   m_nDataBit              = 0;
        int  ?   m_nCameraLinkMode       = 0;
        int  ?   m_nCameraLinkClockSpeed = 0;
        bool ?   m_bHorizontalFlip       = false;
        float?   m_fDigitalGain          = 0.0f;
        int  ?   m_nDigitalOffset        = 0;
        int  ?   m_nTriggerMode          = 0;
        int  ?   m_nExposureSource       = 0;
        int  ?   m_nTriggerSource        = 0;
        bool ?   m_bTriggerPolarity      = false;
        float?   m_fTriggerConverter     = 0.0f;
        int  ?   m_nImageMode            = 0;


        public VieworksCamera()
        {
            // <명령어><파라미터1><파라미터2><cr>
            // OK <cr> <lf>
        }

        public void fn_Init(string comport)
        {
            m_comm.Open(comport, 115200, 8, Parity.None, StopBits.One);
            m_comm.ReciveDataevent = RecvData;
        }

        public void fn_Final()
        {
            m_comm.Close();
            m_comm.ReciveDataevent = null;
        }

        string m_strMsg;
        private void RecvData(byte[] buff)
        {
            string strMsg = Encoding.ASCII.GetString(buff);

            m_strMsg += strMsg;

            string strtemp;

            int idx = m_strMsg.IndexOf('>');
            if (idx < 0)
            {
                return;
            }
            else
            {
                strtemp = m_strMsg.Substring(0, idx);
                m_strMsg = m_strMsg.Remove(0, idx + 1);
            }
            

            var strCmd = strtemp.Split((char)0x0a);

            if (strCmd.Length == 1)
            {
                // error
                delWriteLog?.Invoke($"{Comport} Recv Error. {strCmd[0]:X}");
            }
            else if(strCmd.Length >= 3)
            {
                try
                {
                    string strcmd = strCmd[0].Replace("\r", "");
                    string strvalue = strCmd[1].Replace("\r", "");
                    switch (strcmd)
                    {
                        case "gio" : m_nImageOffset          = Convert.ToInt32  (strvalue); break;
                        case "giw" : m_nImageWidth           = Convert.ToInt32  (strvalue); break;
                        case "glr" : m_fLinePeriod           = Convert.ToSingle (strvalue); break;
                        case "get" : m_fExposureTime         = Convert.ToSingle (strvalue); break;
                        case "gti" : m_nTestImage            = Convert.ToInt32  (strvalue); break;
                        case "gdb" : m_nDataBit              = Convert.ToInt32  (strvalue); break;
                        case "gcl" : m_nCameraLinkMode       = Convert.ToInt32  (strvalue); break;
                        case "gccs": m_nCameraLinkClockSpeed = Convert.ToInt32  (strvalue); break;
                        case "ghf" : m_bHorizontalFlip       = Convert.ToBoolean(Convert.ToInt32(strvalue)); break;
                        case "gdg" : m_fDigitalGain          = Convert.ToSingle (strvalue); break;
                        case "gdo" : m_nDigitalOffset        = Convert.ToInt32  (strvalue); break;
                        case "gtm" : m_nTriggerMode          = Convert.ToInt32  (strvalue); break;
                        case "ges" : m_nExposureSource       = Convert.ToInt32  (strvalue); break;
                        case "gts" : m_nTriggerSource        = Convert.ToInt32  (strvalue); break;
                        case "gtp" : m_bTriggerPolarity      = Convert.ToBoolean(Convert.ToInt32(strvalue)); break;
                        case "gtc" : m_fTriggerConverter     = Convert.ToSingle (strvalue); break;
                        case "gim" : m_nImageMode            = Convert.ToInt32  (strvalue); break;
                    }
                }
                catch (Exception ex)
                {
                    delWriteLog?.Invoke($"{Comport} Convert Error. {strCmd[0]} : {strCmd[1]} : {ex.Message}");
                }

                m_bUpdated = true;
            }
        }

        private void fn_WriteValue(string msg)
        {
            m_comm.WriteValue(msg, true, false);
            delWriteLog?.Invoke(msg);
        }

        public void fn_SetImageOffset(int value)
        {
            fn_WriteValue($"sio {value}");
        }

        public int? fn_GetImageOffset()
        {
            m_bUpdated = false;
            fn_WriteValue($"gio");
            if (fn_CheckTimeOut() == -1)
            {
                delWriteLog?.Invoke("Time-Out");
                m_nImageOffset = null;
            }
            return m_nImageOffset;
        }

        public void fn_SetImageWidth(int value)
        {
            fn_WriteValue($"siw {value}");
        }

        public int? fn_GetImageWidth()
        {
            m_bUpdated = false;
            fn_WriteValue($"giw");
            if (fn_CheckTimeOut() == -1)
            {
                delWriteLog?.Invoke("Time-Out");
                m_nImageWidth = null;
            }
            return m_nImageWidth;
        }

        public void fn_SetLinePeriod(float value)
        {
            fn_WriteValue($"slr {value}");
        }

        public float? fn_GetLinePeriod()
        {
            m_bUpdated = false;
            fn_WriteValue($"glr");
            if (fn_CheckTimeOut() == -1)
            {
                delWriteLog?.Invoke("Time-Out");
                m_fLinePeriod = null;
            }
            return m_fLinePeriod;
        }

        public void fn_SetExposureTime(float value)
        {
            // lineperiod 보다 작아야함.
            if (value >= 2.00f && value < 10000.00f)
            {
                fn_WriteValue($"set {value}");
            }
        }

        public float? fn_GetExposureTime()
        {
            m_bUpdated = false;
            fn_WriteValue($"get");
            if (fn_CheckTimeOut() == -1)
            {
                delWriteLog?.Invoke("Time-Out");
                m_fExposureTime = null;
            }
            return m_fExposureTime;
        }

        /// <summary>
        /// Test Image Setting
        /// </summary>
        /// <param name="value">0:Off, 1/2:Fixed pattern image, 3:Moving pattern image</param>
        public void fn_SetTestImage(int value)
        {
            if (value >= 0 && value <= 3)
            {
                fn_WriteValue($"sti {value}");
            }
        }

        public int? fn_GetTestImage()
        {
            m_bUpdated = false;
            fn_WriteValue($"gti");
            if (fn_CheckTimeOut() == -1)
            {
                delWriteLog?.Invoke("Time-Out");
                m_nTestImage = null;
            }
            return m_nTestImage;
        }

        /// <summary>
        /// DataBit Setting
        /// </summary>
        /// <param name="value">8:8 bit output, 10:10 bit output, 12:12 bit output</param>
        public void fn_SetDataBit(int value)
        {
            if (value == 8 || value == 10 || value == 12)
            {
                fn_WriteValue($"sdb {value}");
            }
        }

        public int? fn_GetDataBit()
        {
            m_bUpdated = false;
            fn_WriteValue($"gdb");
            if (fn_CheckTimeOut() == -1)
            {
                delWriteLog?.Invoke("Time-Out");
                m_nDataBit = null;
            }
            return m_nDataBit;
        }

        /// <summary>
        /// CameraLink Mode Setting
        /// </summary>
        /// <param name="value">0:2Tap Base, 1:4Tap Base, 2:8Tap Full, 3:10Tap</param>
        public void fn_SetCameraLinkMode(int value)
        {
            if (value >= 0 && value <= 3)
            {
                fn_WriteValue($"scl {value}");
            }
        }

        public int? fn_GetCameraLinkMode()
        {
            m_bUpdated = false;
            fn_WriteValue($"gcl");
            if (fn_CheckTimeOut() == -1)
            {
                delWriteLog?.Invoke("Time-Out");
                m_nCameraLinkMode = null;
            }
            return m_nCameraLinkMode;
        }

        /// <summary>
        /// CameraLink Clock Speed Setting
        /// </summary>
        /// <param name="idx"></param>
        /// <param name="value">0:50Mhz, 1:60Mhz, 2:70Mhz, 3:80Mhz</param>
        public void fn_SetCameraLinkClockSpeed(int value)
        {
            if (value >= 0 && value <= 3)
            {
                fn_WriteValue($"sccs {value}");
            }
        }

        public int? fn_GetCameraLinkClockSpeed()
        {
            m_bUpdated = false;
            fn_WriteValue($"gccs");
            if (fn_CheckTimeOut() == -1)
            {
                delWriteLog?.Invoke("Time-Out");
                m_nCameraLinkClockSpeed = null;
            }
            return m_nCameraLinkClockSpeed;
        }

        public void fn_SetHorizontalFlip(bool value)
        {
            fn_WriteValue($"shf {(value ? 1 : 0)}");
        }

        public bool? fn_GetHorizontalFlip()
        {
            m_bUpdated = false;
            fn_WriteValue($"ghf");
            if (fn_CheckTimeOut() == -1)
            {
                delWriteLog?.Invoke("Time-Out");
                m_bHorizontalFlip = null;
            }
            return m_bHorizontalFlip;
        }

        /// <summary>
        /// Digital Gain Setting
        /// </summary>
        /// <param name="value">0.0~32.0</param>
        public void fn_SetDigitalGain(float value)
        {
            if (value >= 0.0 && value <= 32.0)
            {
                fn_WriteValue($"sdg {value}");
            }
        }

        public float? fn_GetDigitalGain()
        {
            m_bUpdated = false;
            fn_WriteValue($"gdg");
            if (fn_CheckTimeOut() == -1)
            {
                delWriteLog?.Invoke("Time-Out");
                m_fDigitalGain = null;
            }
            return m_fDigitalGain;
        }

        /// <summary>
        /// Digital Offset Setting
        /// </summary>
        /// <param name="value">0~2048</param>
        public void fn_SetDigitalOffset(int value)
        {
            if (value >= 0 && value <= 2048)
            {
                fn_WriteValue($"sdo {value}");
            }
        }

        public int? fn_GetDigitalOffset()
        {
            m_bUpdated = false;
            fn_WriteValue($"gdo");
            if (fn_CheckTimeOut() == -1)
            {
                delWriteLog?.Invoke("Time-Out");
                m_nDigitalOffset = null;
            }
            return m_nDigitalOffset;
        }

        /// <summary>
        /// Trigger Mode Setting
        /// </summary>
        /// <param name="value">0:Free-Run, 1:External Sync, 2:External Sync Converter mode</param>
        public void fn_SetTriggerMode(int value)
        {
            if (value >= 0 && value <= 2)
            {
                fn_WriteValue($"stm {value}");
            }
        }

        public int? fn_GetTriggerMode()
        {
            m_bUpdated = false;
            fn_WriteValue($"gtm");
            if (fn_CheckTimeOut() == -1)
            {
                delWriteLog?.Invoke("Time-Out");
                m_nTriggerMode = null;
            }
            return m_nTriggerMode;
        }

        /// <summary>
        /// ExposureTime Source Setting
        /// </summary>
        /// <param name="value">0:Program Exposure(by Cam), 1:Pulse Width (by ext trig sig), 2:Edge(by ext trg sig)</param>
        public void fn_SetExposureSource(int value)
        {
            if (value >= 0 && value <= 2)
            {
                fn_WriteValue($"ses {value}");
            }
        }

        public int? fn_GetExposureSource()
        {
            m_bUpdated = false;
            fn_WriteValue($"ges");
            if (fn_CheckTimeOut() == -1)
            {
                delWriteLog?.Invoke("Time-Out");
                m_nExposureSource = null;
            }
            return m_nExposureSource;
        }

        /// <summary>
        /// Trigger Source Setting
        /// </summary>
        /// <param name="value">1:CC1 port input(cam link), 2:External input(ctrl Receptacle)</param>
        public void fn_SetTriggerSource(int value)
        {
            if (value >= 1 && value <= 2)
            {
                fn_WriteValue($"sts {value}");
            }
        }

        public int? fn_GetTriggerSource()
        {
            m_bUpdated = false;
            fn_WriteValue($"gts");
            if (fn_CheckTimeOut() == -1)
            {
                delWriteLog?.Invoke("Time-Out");
                m_nTriggerSource = null;
            }
            return m_nTriggerSource;
        }

        public void fn_SetTriggerPolarity(bool value)
        {
            fn_WriteValue($"stp {(value ? 1 : 0)}");
        }

        public bool? fn_GetTriggerPolarity()
        {
            m_bUpdated = false;
            fn_WriteValue($"gtp");
            if (fn_CheckTimeOut() == -1)
            {
                delWriteLog?.Invoke("Time-Out");
                m_bTriggerPolarity = null;
            }
            return m_bTriggerPolarity;
        }

        /// <summary>
        /// Trigger Converter Setting
        /// </summary>
        /// <param name="value">Trigger converter ratio (0.10~100.00)</param>
        public void fn_SetTriggerConverter(float value)
        {
            if (value >= 0.10f && value <= 100.00f)
            {
                fn_WriteValue($"stc {value}");
            }
        }

        public float? fn_GetTriggerConverter()
        {
            m_bUpdated = false;
            fn_WriteValue($"gtc");
            if (fn_CheckTimeOut() == -1)
            {
                delWriteLog?.Invoke("Time-Out");
                m_fTriggerConverter = null;
            }
            return m_fTriggerConverter;
        }

        /// <summary>
        /// Image Mode Setting
        /// </summary>
        /// <param name="value">0:Single line, 1:Dual line, 2:Horizontal binning, 3: Vertical Binning, 4: 2x2 binning</param>
        public void fn_SetImageMode(int value)
        {
            if (value >= 0 && value <= 4)
            {
                fn_WriteValue($"sim {value}");
            }
        }

        public int? fn_GetImageMode()
        {
            m_bUpdated = false;
            fn_WriteValue($"gim");
            if (fn_CheckTimeOut() == -1)
            {
                delWriteLog?.Invoke("Time-Out");
                m_nImageMode = null;
            }
            return m_nImageMode;
        }

        private int fn_CheckTimeOut()
        {
            int nState = 0;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (!m_bUpdated)
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
    }
}
