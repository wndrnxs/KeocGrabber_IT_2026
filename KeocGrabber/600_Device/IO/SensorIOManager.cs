/*
 *******************************************************************************
 * 해당 소스는 현장 유지 보수 외에 다른 목적의 사용을 금합니다.
 * - 주식회사 태루 -
 *******************************************************************************
 */
using System;
using System.Threading;

namespace KeocGrabber
{
    /// <summary>
    /// 프레임그래버 센서 입력 모니터링.
    ///
    /// 배선 : Euresys 15pin D-Sub #3 = IIN11+, #12 = IIN11-
    /// 동작 : 이 라인은 EuresysGrabber.fn_SetExternalTrigger()에서 LIN1으로 매핑되어
    ///        "펄스 1개 = SequenceLength(GrabHeight) 라인 스캔"의 시작 트리거가 된다.
    ///        본 클래스는 같은 라인의 레벨을 별도 스레드로 폴링해 상승 에지를 세고,
    ///        UI(Main 화면 Sensor I/O 램프)가 신호 유입 여부를 눈으로 볼 수 있게 한다.
    ///
    /// 주의 : grab 스레드가 버퍼를 pop 하는 동안에는 보드 접근이 잠깐 막힐 수 있어
    ///        (EGrabber is busy in another thread) 폴링이 몇 회 건너뛸 수 있다.
    ///        이때는 마지막 값을 유지하며, 짧은 펄스는 놓칠 수 있다.
    /// </summary>
    class SensorIOManager
    {
        class SensorChannel
        {
            public bool Valid;                              // 마지막 폴링 성공 여부
            public bool Level;                              // 현재 레벨
            public long Count;                              // 상승 에지 누적
            public DateTime RiseTime = DateTime.MinValue;   // 마지막 상승 에지 시각
            public DateTime HoldUntil = DateTime.MinValue;  // 램프 유지 종료 시각
            public double PulseMs;                          // 마지막 펄스 폭(ms)
        }

        Thread m_thread = null;
        volatile bool m_bRunning = false;

        int m_nCamCount = 0;
        int[] m_nChannelOfCam = new int[0];     // 카메라 인덱스 → 채널(물리 보드) 인덱스
        SensorChannel[] m_channels = new SensorChannel[0];

        int m_nPollInterval = 10;
        int m_nLampHold = 1000;

        readonly object m_lock = new object();

        public bool IsRunning { get { return m_bRunning; } }

        public void fn_Init(int camCount)
        {
            fn_Final();

            if (!G.SYSTEM.UseSensorIO)
            {
                G.WriteLog("Sensor I/O : 사용 안 함 (UseSensorIO=false)");
                return;
            }

            if (!G.GRABBER.IsSensorIoSupported)
            {
                // Matrox는 아직 미지원 — GrabberManager.fn_TryGetSensorInput()의 TODO 참고.
                G.WriteLog("Sensor I/O : 현재 그래버(Matrox)는 미지원 → 모니터링 비활성");
                return;
            }

            m_nCamCount     = Math.Max(0, camCount);
            m_nPollInterval = Math.Max(1, G.SYSTEM.SensorPollInterval);
            m_nLampHold     = Math.Max(0, G.SYSTEM.SensorLampHold);

            fn_BuildChannelMap();

            if (m_nCamCount == 0)
            {
                G.WriteLog("Sensor I/O : 대상 그래버 없음 → 모니터링 비활성");
                return;
            }

            m_bRunning = true;
            m_thread = new Thread(PollThreadProc) { IsBackground = true };
            m_thread.Start();

            G.WriteLog($"Sensor I/O Start. Line:{G.GRABBER.fn_GetSensorLine(0)} (15pin D-Sub #3/#12), Poll:{m_nPollInterval}ms");
        }

        public void fn_Final()
        {
            if (!m_bRunning && m_thread == null) return;

            m_bRunning = false;
            try { m_thread?.Join(1000); } catch { }
            m_thread = null;

            G.WriteLog("Sensor I/O Stop.");
        }

        /// <summary>
        /// 한 보드(Interface)에 여러 카메라가 붙으면 I/O 커넥터는 하나이므로,
        /// 같은 Interface를 쓰는 카메라들은 하나의 채널을 공유하도록 묶는다.
        /// (보드 접근 횟수도 카메라 수가 아니라 보드 수만큼으로 줄어든다)
        /// </summary>
        private void fn_BuildChannelMap()
        {
            int[] nMap = new int[m_nCamCount];
            SensorChannel[] channels = new SensorChannel[m_nCamCount];

            for (int i = 0; i < m_nCamCount; i++)
            {
                nMap[i] = i;
                channels[i] = new SensorChannel();

                int nIf = G.GRABBER.fn_GetSensorInterfaceIndex(i);
                if (nIf < 0) continue;

                for (int j = 0; j < i; j++)
                {
                    if (G.GRABBER.fn_GetSensorInterfaceIndex(j) == nIf)
                    {
                        nMap[i] = nMap[j];
                        break;
                    }
                }
            }

            // UI 스레드가 조회하는 중에 길이가 어긋나지 않도록 한 번에 교체한다.
            lock (m_lock)
            {
                m_nChannelOfCam = nMap;
                m_channels = channels;
            }
        }

        private void PollThreadProc()
        {
            while (m_bRunning)
            {
                try
                {
                    for (int i = 0; i < m_nCamCount && m_bRunning; i++)
                    {
                        // 채널 대표 카메라만 실제로 읽는다(같은 보드 중복 접근 방지).
                        if (m_nChannelOfCam[i] != i) continue;

                        bool bLevel;
                        bool bOk = G.GRABBER.fn_TryGetSensorInput(i, out bLevel);
                        if (!bOk) continue;

                        fn_UpdateChannel(i, bLevel);
                    }
                }
                catch (Exception ex)
                {
                    if (m_bRunning) G.WriteLog($"Sensor I/O Poll Error : {ex.Message}", true);
                }

                Thread.Sleep(m_nPollInterval);
            }
        }

        private void fn_UpdateChannel(int ch, bool bLevel)
        {
            bool bRisingEdge = false;
            long nCount = 0;
            DateTime dtNow = DateTime.Now;

            lock (m_lock)
            {
                SensorChannel st = m_channels[ch];

                // 첫 성공 읽기는 기준값만 잡는다(시작 시 이미 High여도 에지로 세지 않음).
                if (!st.Valid)
                {
                    st.Level = bLevel;
                    st.Valid = true;
                    return;
                }

                bool bPrev = st.Level;

                if (!bPrev && bLevel)
                {
                    // 상승 에지 = 센서 검출 = 스캔 시작 트리거
                    st.Count++;
                    st.RiseTime = dtNow;
                    st.HoldUntil = dtNow.AddMilliseconds(m_nLampHold);
                    bRisingEdge = true;
                    nCount = st.Count;
                }
                else if (bPrev && !bLevel && st.RiseTime != DateTime.MinValue)
                {
                    st.PulseMs = (dtNow - st.RiseTime).TotalMilliseconds;
                }

                st.Level = bLevel;
                st.Valid = true;
            }

            if (bRisingEdge && G.SYSTEM.SensorLogEnable)
                G.WriteLog($"[SENSOR] {G.GRABBER.fn_GetSensorLine(ch)} 신호 검출 (BOARD{ch + 1})");
                //G.WriteLog($"[SENSOR] {G.GRABBER.fn_GetSensorLine(ch)} 신호 검출 (BOARD{ch + 1}, #{nCount})");
        }

        // ─── UI 조회용 (모두 카메라 인덱스 기준) ────────────────────────────────

        private SensorChannel fn_GetChannel(int camIdx)
        {
            if (camIdx < 0 || camIdx >= m_nChannelOfCam.Length) return null;
            return m_channels[m_nChannelOfCam[camIdx]];
        }

        /// <summary>
        /// UI 램프용 상태. 짧은 펄스도 눈에 보이도록 상승 에지 후 SensorLampHold(ms) 동안 켜 둔다.
        /// </summary>
        public bool fn_IsSignalOn(int camIdx)
        {
            lock (m_lock)
            {
                SensorChannel st = fn_GetChannel(camIdx);
                if (st == null) return false;
                return st.Level || DateTime.Now < st.HoldUntil;
            }
        }

        /// <summary>신호(상승 에지) 누적 횟수</summary>
        public long fn_GetCount(int camIdx)
        {
            lock (m_lock)
            {
                SensorChannel st = fn_GetChannel(camIdx);
                return st != null ? st.Count : 0;
            }
        }

        /// <summary>마우스 오버용 상세 문자열 (마지막 검출 시각 / 펄스 폭)</summary>
        public string fn_GetDetailText(int camIdx)
        {
            lock (m_lock)
            {
                SensorChannel st = fn_GetChannel(camIdx);
                if (st == null) return "그래버 없음";
                if (!st.Valid) return "라인 상태를 읽지 못함";
                if (st.Count == 0) return "신호 대기 중";

                string strRet = $"마지막 검출 {st.RiseTime:HH:mm:ss.fff} / 누적 {st.Count}회";
                if (st.PulseMs > 0) strRet += $"\n펄스 폭 {st.PulseMs:F0}ms";
                return strRet;
            }
        }

        public void fn_ResetCount()
        {
            lock (m_lock)
            {
                foreach (var st in m_channels)
                {
                    if (st == null) continue;
                    st.Count = 0;
                    st.RiseTime = DateTime.MinValue;
                    st.HoldUntil = DateTime.MinValue;
                    st.PulseMs = 0;
                }
            }
            G.WriteLog("Sensor I/O Count Reset.");
        }
    }
}
