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
    /// 호출 측은 디스크 I/O, UI, 파일 잠금을 기다리지 않는다. 큐가 가득 차면 기록을 버리고(촬상 우선) 개수만 남긴다.
    /// 기존 메인 로그에 이미 남는 사건(메시지 수신, Grab Start/Stop/Complate, 안전 타이머 등)은 여기에 다시 쓰지 않는다.
    /// </summary>
    static class GrabDiagLog
    {
        struct Item
        {
            public long Ticks;
            public long StartTicks;
            public string Evt;
            public int Cam;
            public string Msg;
        }

        const int MAX_QUEUE = 50000;
        const int FLUSH_INTERVAL_MS = 200;

        static readonly ConcurrentQueue<Item> s_que = new ConcurrentQueue<Item>();
        static int s_nQueued = 0;
        static int s_nDropped = 0;
        static Thread s_thread = null;
        static volatile bool s_bRun = false;
        static DateTime s_baseTime;
        static long s_baseTicks;
        static long s_startTicks = 0;
        static readonly object s_lockFlush = new object();   // 종료 시 Join이 시간 초과돼도 Flush가 두 곳에서 동시에 돌지 않도록

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
        }

        public static void Stop()
        {
            if (!s_bRun) return;
            s_bRun = false;
            try { s_thread?.Join(2000); } catch { }
            Flush();
        }

        /// <summary>촬상 시작(START 처리) 시각을 기준점으로 잡는다. 이후 줄의 FromStart(ms)가 이 기준. 줄은 쓰지 않는다.</summary>
        public static void MarkStart()
        {
            if (!s_bRun) return;
            Interlocked.Exchange(ref s_startTicks, Stopwatch.GetTimestamp());
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
            s_que.Enqueue(new Item
            {
                Ticks = Stopwatch.GetTimestamp(),
                StartTicks = Interlocked.Read(ref s_startTicks),
                Evt = evt,
                Cam = cam,
                Msg = msg
            });
        }

        static void ThreadProc()
        {
            while (s_bRun)
            {
                Thread.Sleep(FLUSH_INTERVAL_MS);
                Flush();
            }
        }

        // Logger와 같은 날짜 폴더 구조. 날짜가 바뀌면 파일도 바뀐다.
        static StreamWriter fn_GetWriter(DateTime t, StreamWriter sw, ref string strOpenPath)
        {
            string strPath = Path.Combine(G.SYSTEM.LogPath, $"{t:yyyy}", $"{t:MM}", $"{t:dd}", $"GrabDiag_{t:yyyyMMdd}.csv");
            if (sw != null && strPath == strOpenPath) return sw;
            sw?.Dispose();
            Directory.CreateDirectory(Path.GetDirectoryName(strPath));
            bool bNew = !File.Exists(strPath);
            var fs = new FileStream(strPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            var swNew = new StreamWriter(fs, new UTF8Encoding(true));
            if (bNew) swNew.WriteLine("Time,FromStart(ms),Event,Cam,Detail");
            strOpenPath = strPath;
            return swNew;
        }

        static void Flush()
        {
            lock (s_lockFlush) fn_FlushCore();
        }

        static void fn_FlushCore()
        {
            if (s_que.IsEmpty && Volatile.Read(ref s_nDropped) == 0) return;
            StreamWriter sw = null;
            string strOpenPath = null;
            try
            {
                Item it;
                // 쓰기에 성공한 뒤에 큐에서 뺀다 → 파일을 열 수 없을 때(예: 엑셀이 파일을 잠금) 다음 주기에 다시 시도
                while (s_que.TryPeek(out it))
                {
                    DateTime t = s_baseTime.AddTicks((long)((it.Ticks - s_baseTicks) * (TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency)));

                    // Logger와 같은 날짜 폴더 구조. 날짜가 바뀌면 파일도 바뀐다.
                    sw = fn_GetWriter(t, sw, ref strOpenPath);

                    string strRel = it.StartTicks != 0 && it.Ticks >= it.StartTicks
                        ? ((it.Ticks - it.StartTicks) * 1000.0 / Stopwatch.Frequency).ToString("F3")
                        : "";
                    string strMsg = (it.Msg ?? "").Replace(",", ";").Replace("\r", " ").Replace("\n", " ");
                    sw.WriteLine($"{t:yyyy-MM-dd HH:mm:ss.ffffff},{strRel},{it.Evt},{(it.Cam < 0 ? "" : "CAM" + (it.Cam + 1))},{strMsg}");

                    s_que.TryDequeue(out it);
                    Interlocked.Decrement(ref s_nQueued);
                }
                // 버린 개수는 파일 쓰기가 성공한 뒤에 가져온다(잠금으로 실패한 주기에 개수가 사라지지 않도록)
                if (Volatile.Read(ref s_nDropped) > 0)
                {
                    DateTime now = DateTime.Now;
                    sw = fn_GetWriter(now, sw, ref strOpenPath);
                    int nDropped = Interlocked.Exchange(ref s_nDropped, 0);
                    sw.WriteLine($"{now:yyyy-MM-dd HH:mm:ss.ffffff},,LOG_DROPPED,,{nDropped}건 기록 못 함 (큐 가득 참)");
                }
            }
            catch { /* 진단 로그 실패가 촬상에 영향을 주면 안 된다. 남은 항목은 다음 주기에 다시 시도 */ }
            finally
            {
                try { sw?.Dispose(); } catch { }
            }
        }
    }
}
