/*
 *******************************************************************************
 * 해당 소스는 현장 유지 보수 외에 다른 목적의 사용을 금합니다.
 * - 주식회사 태루 -
 *******************************************************************************
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KeocGrabber
{
    class Logger
    {
        Queue<Tuple<string, string, string, bool>> m_queLog = new Queue<Tuple<string, string, string, bool>>();

        Mutex m_mutexQue = new Mutex();
        Thread m_threadLogger = null;

        string m_strLogPath = "./LOG/";


        public void fn_Init(string strLogPath = "./LOG/")
        {
            m_strLogPath = strLogPath;
            m_threadLogger = new Thread(new ThreadStart(THREAD_LOGGER));
            m_threadLogger.Start();
        }

        public void fn_Final()
        {
            if (m_threadLogger != null && m_threadLogger.IsAlive)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                while(m_queLog.Count != 0)
                {
                    Thread.Sleep(100);
                    if (sw.ElapsedMilliseconds >= 3000) break;
                }
                m_threadLogger.Abort();
            }
        }

        public void fn_PushMsg(string strMsg, string declaringtype, string method, bool bError = false)
        {
            m_mutexQue.WaitOne();
            m_queLog.Enqueue(new Tuple<string,string,string,bool>(strMsg, declaringtype, method, bError));
            m_mutexQue.ReleaseMutex();
        }

        private Tuple<string, string, string, bool> fn_PopMsg()
        {
            Tuple<string, string, string, bool> strMsg = null;
            if (m_queLog.Count > 0)
            {
                if (m_mutexQue.WaitOne(1000))
                {
                    strMsg = m_queLog.Dequeue();
                    m_mutexQue.ReleaseMutex();
                }
            }

            return strMsg;
        }

        private void fn_WriteFile(string strMsg, string declaringtype, string method, bool bError)
        {
            DateTime now = DateTime.Now;
            //string strPath = $"{m_strLogPath}/{now:yyyy-MM-dd}/";
            string strPath = $"{m_strLogPath}/{now:yyyy}/{now:MM}/{now:dd}/"; // 240714 현장 요청 내용 반영. - 최중권 선임
            string strName = "";

            if (bError)
            {
                strName = $"Error.{G.SYSTEM.LogExt}";
                if (!G.SYSTEM.CallStack) // Call Stack이 켜져있을때 2중 추가 방지.
                {
                    strMsg = strMsg.Insert(strMsg.Length, $",{declaringtype},{method}"); // error는 stacktrace를 적음.
                }
            }
            else
                strName = $"{fn_GetFileName(declaringtype)}.{G.SYSTEM.LogExt}";


            Directory.CreateDirectory(strPath);
            try
            {
//                 using (var sw = new StreamWriter($"{strPath}{strName}", true, Encoding.UTF8))
//                 {
//                     sw.WriteLine(strMsg);
//                     sw.Flush();
//                 }

                using (var fs = File.Open($"{strPath}{strName}", FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                {
                    strMsg = strMsg.Insert(strMsg.Length, "\r\n");
                    var bytes = Encoding.Default.GetBytes(strMsg);
                    fs.Write(bytes, 0, bytes.Length);
                    fs.Flush();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        // G, ImageManager
        private string fn_GetFileName(string declaringtype)
        {
            string strFileName = "";

            switch(declaringtype)
            {
                case "GiGABoard"        : strFileName = "GiGABoard"; break;
                case "TCPIPClient"      : strFileName = "Communication"; break;
                case "GrabberManager"   :
                case "LightManager"     :
                case "VieworksCamera"   :
                case "DiskManager"      :
                case "G"                :
                default                 : strFileName = "Trace"; break;
            }


            return strFileName;
        }


        private void THREAD_LOGGER()
        {
            Tuple<string, string, string, bool> strMsg = null;
            while(true)
            {
                strMsg = fn_PopMsg();

                if (strMsg != null)     fn_WriteFile(strMsg.Item1, strMsg.Item2, strMsg.Item3, strMsg.Item4);
                else                    Thread.Sleep(100);
            }
        }
    }
}
