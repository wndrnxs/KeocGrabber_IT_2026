/*
 *******************************************************************************
 * 해당 소스는 현장 유지 보수 외에 다른 목적의 사용을 금합니다.
 * - 주식회사 태루 -
 *******************************************************************************
 */
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace KeocGrabber
{
    /// <summary>
    /// 촬상 타임라인 진단 로그. 그랩 콜백처럼 시간에 민감한 곳에서는 Write로 메모리 큐에 넣기만 하고,
    /// 파일 기록은 백그라운드 스레드가 별도 CSV(LOG/yyyy/MM/dd/GrabDiag_yyyyMMdd.csv)로 한다.
    /// 호출 측이 디스크 I/O, UI, 락 경합을 기다리지 않게 하기 위함. 큐가 가득 차면 기록을 버리고(촬상 우선) 개수만 남긴다.
    /// </summary>
    static class GrabDiagLog
    {
        struct Item
        {
            public long Ticks;
            public string Evt;
            public int Cam;
            public string Msg;
        }

        const int MAX_QUEUE = 200000;
        const int FLUSH_INTERVAL_MS = 200;

        static readonly ConcurrentQueue<Item> s_que = new ConcurrentQueue<Item>();
        static int s_nQueued = 0;
        static int s_nDropped = 0;
        static Thread s_thread = null;
        static volatile bool s_bRun = false;
        static DateTime s_baseTime;
        static long s_baseTicks;
        static long s_lastCheckTicks = 0;

        /// <summary>기록 중이면 true. 호출 측은 문자열을 만들기 전에 이것으로 거른다.</summary>
        public static bool On { get { return s_bRun; } }

        public static void Start()
        {
            if (s_bRun || !G.SYSTEM.GrabDiagEnable) return;
            s_baseTime = DateTime.Now;
            s_baseTicks = Stopwatch.GetTimestamp();
            s_bRun = true;
            s_thread = new Thread(ThreadProc) { IsBackground = true, Priority = ThreadPriority.BelowNormal, Name = "GrabDiagLog" };
            s_thread.Start();
            Write("LOG_START", -1, $"Stopwatch {Stopwatch.Frequency}Hz");
        }

        public static void Stop()
        {
            if (!s_bRun) return;
            Write("LOG_STOP", -1, "");
            s_bRun = false;
            try { s_thread?.Join(2000); } catch { }
            Flush();
        }

        public static void Write(string evt, int cam, string msg)
        {
            if (!s_bRun) return;
            if (Interlocked.Increment(ref s_nQueued) > MAX_QUEUE)
            {
                Interlocked.Decrement(ref s_nQueued);
                Interlocked.Increment(ref s_nDropped);
                return;
            }
            s_que.Enqueue(new Item { Ticks = Stopwatch.GetTimestamp(), Evt = evt, Cam = cam, Msg = msg });
        }

        static void ThreadProc()
        {
            while (s_bRun)
            {
                Thread.Sleep(FLUSH_INTERVAL_MS);
                Flush();
            }
        }

        static void Flush()
        {
            if (s_que.IsEmpty) return;
            try
            {
                StreamWriter sw = null;
                string strOpenPath = null;
                try
                {
                    int nDropped = Interlocked.Exchange(ref s_nDropped, 0);
                    Item it;
                    while (s_que.TryDequeue(out it))
                    {
                        Interlocked.Decrement(ref s_nQueued);
                        DateTime t = s_baseTime.AddTicks((long)((it.Ticks - s_baseTicks) * (TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency)));

                        // 같은 날짜 폴더 구조(Logger와 동일)로, 날짜가 바뀌면 파일도 바뀐다.
                        string strPath = Path.Combine(G.SYSTEM.LogPath, $"{t:yyyy}", $"{t:MM}", $"{t:dd}", $"GrabDiag_{t:yyyyMMdd}.csv");
                        if (strPath != strOpenPath)
                        {
                            sw?.Dispose();
                            Directory.CreateDirectory(Path.GetDirectoryName(strPath));
                            bool bNew = !File.Exists(strPath);
                            sw = new StreamWriter(strPath, true, new UTF8Encoding(true));
                            if (bNew) sw.WriteLine("Time,FromCHECK(ms),Event,Cam,Detail");
                            strOpenPath = strPath;
                        }

                        // 마지막 START(CHECK) 수신 기준 경과 ms — 한 촬상 안의 순서/간격을 바로 읽기 위함
                        if (it.Evt == "CHECK_RECV") s_lastCheckTicks = it.Ticks;
                        string strRel = s_lastCheckTicks != 0
                            ? ((it.Ticks - s_lastCheckTicks) * 1000.0 / Stopwatch.Frequency).ToString("F3")
                            : "";

                        string strMsg = (it.Msg ?? "").Replace(",", ";").Replace("\r", "\\r").Replace("\n", "\\n");
                        sw.WriteLine($"{t:yyyy-MM-dd HH:mm:ss.ffffff},{strRel},{it.Evt},{(it.Cam < 0 ? "" : $"CAM{it.Cam + 1}")},{strMsg}");
                    }
                    if (nDropped > 0 && sw != null)
                        sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.ffffff},,LOG_DROPPED,,{nDropped} (큐 가득 참)");
                }
                finally { sw?.Dispose(); }
            }
            catch { /* 진단 로그 실패가 촬상에 영향을 주면 안 된다 */ }
        }
    }
}
