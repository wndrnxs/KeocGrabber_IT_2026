using System;
using System.Collections.Generic;
using System.Threading;
using Euresys.EGrabber;
using OpenCvSharp;

namespace KeocGrabber
{
    class EuresysGrabber
    {
        Func<Mat, int, bool> delGrab = null;
        Func<string, bool> delLog = null;
        public Func<Mat, int, bool> OnGrab { set { delGrab = value; } }
        public Func<string, bool> OnLog { set { delLog = value; } }

        EGrabber _egrabber;

        int m_nInterfaceIndex;
        int m_nDeviceIndex;
        int m_nCameraIndex;   // GrabberManager 리스트상의 위치(0:Front 1:Rear 2:InSide 3:OutSide) — 카메라별 설정(트리거 지연 등) 조회에 사용

        int m_nWidth;
        int m_nHeight;
        int m_nChannel = 1;
        int m_nCurrentBufferHeight;
        MatType m_matType = MatType.CV_8UC1;
        double m_dBitShift = 1.0;

        bool m_bIsInit = false;
        bool m_bIsGrabbing = false;
        volatile bool m_bParamChanging = false;
        bool m_bSetupMode = false;    // true=라이브뷰(throttle 표시), false=production(전 청크 누적)

        Thread m_grabThread;
        bool m_bThreadRunning = false;
        int m_nFrameIndex = 0;

        const ulong BUFFER_COUNT      = 16;
        const ulong GRAB_BUFFER_COUNT = 8;       // production 누적용 버퍼 풀(청크 순환). 8×1024×16384×2≈256MB/cam
        const ulong POP_TIMEOUT_MS    = 1000;    // 짧게: m_bThreadRunning=false 후 스레드가 ~1s 내 종료(stop 응답성 ↑, 중복 pop 방지)
        const ulong LIVE_BUFFER_HEIGHT = 256;
        const ulong GRAB_CHUNK_HEIGHT  = 1024;   // production: 1024라인씩 받아 ImageManager가 GrabHeight까지 누적
        // 현장조건: 200mm/s 물체 스캔용 라인레이트. 기본 11049.7Hz(=라인주기 90.5us).
        // 라인주기 = X픽셀분해능 / 속도 라야 정사각 비율. 늘어짐 보정 필요시 = 현재값 × (정사각물체 결과 H/W).
        // 카메라(렌즈·센서)마다 값이 다를 수 있어 SystemParam.CamLineRate1~4(Hz)로 카메라별로 둔다.
        // fn_SetExternalTrigger()가 매 grab 시작마다 자기 카메라 인덱스로 최신값을 읽으므로,
        // Setup 화면에서 값을 바꾸면 재시작 없이 다음 grab부터 바로 반영된다.

        // ── 센서 입력 라인 (15pin D-Sub #3 = IIN11+, #12 = IIN11-) ──────────────
        // 이 라인은 fn_SetExternalTrigger()에서 LIN1(스캔 시작 트리거)으로 매핑된다.
        // 여기서는 같은 물리 라인의 현재 레벨을 읽어 UI 램프/카운터로 보여준다(배선·센서 점검용).
        string m_strSensorLine = "IIN11";
        string m_strDelayTool = "DEL1";     // 센서 ON → 스캔 시작 지연에 쓸 IOToolbox 블록
        volatile bool m_bSensorReadable = true;   // LineStatus 미지원이면 false로 내려 폴링 중단
        bool m_bLastSensorLevel = false;
        int m_nSensorFailCount = 0;
        readonly object m_lockSensor = new object();
        const int SENSOR_FAIL_LIMIT = 10;         // 연속 실패 한계(초과 시 모니터링 중단)

        public string SensorLine { get { return m_strSensorLine; } }

        public int Width { get { return m_nWidth; } }
        public int Height { get { return m_nHeight; } }
        public int Channel { get { return m_nChannel; } }
        public int BoardIndex { get { return m_nInterfaceIndex; } }
        public bool IsInit { get { return m_bIsInit; } }
        public bool IsGrabbing { get { return m_bIsGrabbing; } }

        public void fn_Init(EGrabberInfo info, int camIndex)
        {
            try
            {
                m_nInterfaceIndex = info.InterfaceIndex;
                m_nDeviceIndex    = info.DeviceIndex;
                m_nCameraIndex    = camIndex;

                if (!string.IsNullOrWhiteSpace(G.SYSTEM.SensorInputLine))
                    m_strSensorLine = G.SYSTEM.SensorInputLine.Trim();
                if (!string.IsNullOrWhiteSpace(G.SYSTEM.SensorDelayTool))
                    m_strDelayTool = G.SYSTEM.SensorDelayTool.Trim();
                m_bSensorReadable  = true;
                m_nSensorFailCount = 0;

                _egrabber = new EGrabber(info, DEVICE_ACCESS_FLAGS.DEVICE_ACCESS_CONTROL);

                m_nWidth  = (int)_egrabber.Width;
                m_nHeight = G.SYSTEM.GrabHeight;
                m_nCurrentBufferHeight = (int)LIVE_BUFFER_HEIGHT;

                string pixelFormat = _egrabber.PixelFormat;
                m_matType   = fn_GetMatType(pixelFormat);
                m_dBitShift = fn_GetBitShift(pixelFormat);

                fn_SetBufferAndScan(LIVE_BUFFER_HEIGHT);
                _egrabber.ReallocBuffers(BUFFER_COUNT, 0);

                fn_EnableDualSensor();

                delLog?.Invoke($"Euresys Init. IF:{m_nInterfaceIndex} DEV:{m_nDeviceIndex} [{m_nWidth} x {m_nHeight}] [{info.DeviceModelName}] [{pixelFormat}]");
                m_bIsInit = true;
            }
            catch (Exception ex)
            {
                G.WriteLog($"Euresys Init Fail (IF:{info.InterfaceIndex} DEV:{info.DeviceIndex}): {ex.Message}", true);
            }
        }

        public void fn_Final()
        {
            if (m_bIsGrabbing) fn_GrabStop();

            // 센서 폴링 스레드가 읽는 중에 Dispose되지 않도록 같은 락으로 보호한다.
            lock (m_lockSensor)
            {
                m_bSensorReadable = false;

                try { _egrabber?.Dispose(); } catch { }
                _egrabber = null;
            }

            m_bIsInit = false;
            delLog?.Invoke($"Euresys Final. IF:{m_nInterfaceIndex}");
        }

        // BufferHeight와 ScanLength를 항상 동일하게 설정한다.
        // ScanLength(스트림이 한 스캔으로 모을 라인 수) < BufferHeight면 버퍼가
        // 스캔길이만큼만 차고 나머지는 검정 → 512+암흑 줄무늬가 반복된다.
        private void fn_SetBufferAndScan(ulong height)
        {
            _egrabber.Stream.Set<ulong>("BufferHeight", height);
            try { _egrabber.Stream.Set<ulong>("ScanLength", height); }
            catch (Exception ex) { G.WriteLog($"Euresys ScanLength set fail: {ex.Message}", true); }
        }

        public void fn_GrabStart(bool bSetup = false)
        {
            if (!m_bIsInit) return;

            // 중복 grab 스레드 방지: 이전 스레드(Live/직전 grab)가 아직 살아있으면 확실히 종료시킨 뒤 시작한다.
            // 두 스레드가 같은 EGrabber에서 동시에 pop하면 "EGrabber is busy in another thread" 폭발.
            if (m_bThreadRunning || (m_grabThread != null && m_grabThread.IsAlive))
            {
                m_bThreadRunning = false;
                // controlRemoteDevice=false로 시작했으므로 Stop()이 카메라를 안 멈춘다 → 수동 AcquisitionStop.
                try { _egrabber?.Remote.Execute("AcquisitionStop"); } catch { }
                try { _egrabber?.Stop(); } catch { }
                // 이전 스레드는 pop(최대 POP_TIMEOUT_MS) 안에서 블록될 수 있으므로 그보다 넉넉히 기다린다.
                // (Join이 pop 타임아웃보다 짧으면 이전 스레드가 안 죽은 채 새 스레드가 시작돼 중복 pop 발생)
                try { m_grabThread?.Join((int)POP_TIMEOUT_MS + 2000); } catch { }

                // 그래도 살아있으면 새 스레드를 띄우지 않는다(둘이 동시에 pop하는 상황을 원천 차단).
                if (m_grabThread != null && m_grabThread.IsAlive)
                {
                    G.WriteLog($"Euresys 이전 GrabThread 종료 실패 → grab 시작 취소 (IF:{m_nInterfaceIndex})", true);
                    return;
                }
            }

            try
            {
                m_nFrameIndex = 0;

                if (bSetup)
                {
                    m_bSetupMode = true;
                    try
                    {
                        fn_SetBufferAndScan(LIVE_BUFFER_HEIGHT);
                        _egrabber.ReallocBuffers(BUFFER_COUNT, 0);
                        m_nCurrentBufferHeight = (int)LIVE_BUFFER_HEIGHT;
                    }
                    catch (Exception ex)
                    {
                        m_nCurrentBufferHeight = m_nHeight;
                        G.WriteLog($"Euresys Live BufferHeight set fail: {ex.Message}", true);
                    }

                    fn_SetFreeRun();
                    _egrabber.Start(ulong.MaxValue, false);
                    fn_ExecuteRemote("AcquisitionStart");
                }
                else
                {
                    m_bSetupMode = false;
                    // 트리거/카메라 설정을 먼저 한 뒤, 1024라인 청크 버퍼를 할당.
                    // 한 이미지(GrabHeight)는 1024줄씩 여러 청크로 들어와 ImageManager.AttachImage가 누적한다.
                    fn_SetExternalTrigger();
                    try
                    {
                        fn_SetBufferAndScan(GRAB_CHUNK_HEIGHT);
                        _egrabber.ReallocBuffers(GRAB_BUFFER_COUNT, 0);
                        m_nCurrentBufferHeight = (int)GRAB_CHUNK_HEIGHT;
                    }
                    catch (Exception ex) { G.WriteLog($"Euresys BufferHeight set fail: {ex.Message}", true); }

                    // 카메라가 이전(라이브뷰/직전 grab)에서 acquiring 상태로 남아 있으면
                    // 중복 AcquisitionStart가 GENAPI_ERR로 실패하므로, 시작 전 idle 보장(idle이면 무해).
                    try { _egrabber.Remote.Execute("AcquisitionStop"); } catch { }

                    // 연속 수신(ulong.MaxValue): 청크가 GrabHeight까지 누적되면
                    // ComplateImage→fn_GrabStop이 호출돼 자동으로 멈춘다.
                    _egrabber.Start(ulong.MaxValue, false);
                    // Start(.., controlRemoteDevice:false) → 카메라 arming 수동.
                    // arming 후 카메라는 보드 CIC의 라인트리거(LIN1 시퀀스)만 대기 → 신호 없으면 촬상 안 함.
                    fn_ExecuteRemote("AcquisitionStart");
                }

                m_bIsGrabbing = true;
                m_bThreadRunning = true;
                m_grabThread = new Thread(GrabThreadProc) { IsBackground = true };
                m_grabThread.Start();

                delLog?.Invoke($"Euresys Grab Start. IF:{m_nInterfaceIndex} Setup:{bSetup}");
            }
            catch (Exception ex)
            {
                G.WriteLog($"Euresys GrabStart Fail: {ex.Message}", true);
            }
        }

        public void fn_GrabStop()
        {
            m_bThreadRunning = false;
            fn_ExecuteRemote("AcquisitionStop");
            try { _egrabber?.Stop(); } catch { }
            m_bIsGrabbing = false;
            delLog?.Invoke($"Euresys Grab Stop. IF:{m_nInterfaceIndex}");
        }

        private void GrabThreadProc()
        {
            DateTime dtLastDisplay = DateTime.MinValue;

            while (m_bThreadRunning)
            {
                try
                {
                    using (ScopedBuffer buffer = new ScopedBuffer(_egrabber, POP_TIMEOUT_MS))
                    {
                        int w = m_nWidth;
                        int h = m_nCurrentBufferHeight;
                        IntPtr ptr = buffer.Pixels;

                        if (w <= 0 || h <= 0 || ptr == IntPtr.Zero) continue;

                        // Live(setup): 화면 표시 부하 방지로 33ms throttle(청크 스킵 OK).
                        // Production: 모든 청크를 누적해야 완전한 이미지가 되므로 절대 스킵 금지.
                        if (m_bSetupMode)
                        {
                            if ((DateTime.Now - dtLastDisplay).TotalMilliseconds < 33) continue;
                            dtLastDisplay = DateTime.Now;
                        }

                        Mat mat;
                        if (m_matType == MatType.CV_16UC1)
                        {
                            using (Mat raw = new Mat(h, w, m_matType, ptr))
                            {
                                mat = new Mat();
                                raw.ConvertTo(mat, MatType.CV_8UC1, m_dBitShift);
                            }
                        }
                        else
                        {
                            mat = new Mat(h, w, m_matType, ptr).Clone();
                        }

                        delGrab?.Invoke(mat, m_nFrameIndex);
                        m_nFrameIndex++;

                        // Production은 AttachImage가 동기로 복사하므로 청크 Mat을 즉시 해제(누수 방지).
                        // Live는 BeginInvoke로 비동기 표시되므로 여기서 해제하면 안 됨(GC에 맡김).
                        if (!m_bSetupMode) mat.Dispose();
                    }
                }
                catch (Exception ex) when (ex.Message.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    //if (!m_bParamChanging)
                        //G.WriteLog($"Euresys PopBuffer Timeout (IF:{m_nInterfaceIndex})", true);
                }
                catch (Exception ex)
                {
                    if (m_bThreadRunning)
                        G.WriteLog($"Euresys GrabThread Error (IF:{m_nInterfaceIndex}): {ex.Message}", true);
                }
            }
        }

        private void fn_EnableDualSensor()
        {
            try
            {
                string[] bands = _egrabber.Remote.EnumEntries("BandSelector", true);
                foreach (var band in bands)
                {
                    _egrabber.Remote.Set<string>("BandSelector", band);
                    if (!_egrabber.Remote.Get<bool>("BandEnable"))
                        _egrabber.Remote.Set<bool>("BandEnable", true);
                }
            }
            catch { }
        }

        private void fn_SetFreeRun()
        {
            try { _egrabber.Remote.Set<string>("AcquisitionMode", "Continuous"); } catch { }

            // 모든 트리거 OFF → 카메라 내부 클럭으로 자유 연속 촬상
            // (직전 production grab이 FrameStart 트리거를 ON 해뒀을 수 있으므로 반드시 해제)
            try
            {
                string frameStart = fn_FindRemoteEnum("TriggerSelector", "(Frame|Exposure) *Start");
                if (frameStart != null)
                {
                    _egrabber.Remote.Set<string>("TriggerSelector", frameStart);
                    try { _egrabber.Remote.Set<string>("TriggerMode", "Off"); } catch { }
                }
                string lineStart = fn_FindRemoteEnum("TriggerSelector", "Line *Start");
                if (lineStart != null)
                {
                    _egrabber.Remote.Set<string>("TriggerSelector", lineStart);
                    _egrabber.Remote.Set<string>("TriggerMode", "Off");
                }
            }
            catch (Exception ex) { G.WriteLog($"Euresys FreeRun set fail: {ex.Message}", true); }

            // 보드 CIC 제어 해제 (NC) → 직전 grab의 RC/LIN1 사이클 게이팅 제거
            try { _egrabber.Device.Set<string>("CameraControlMethod", "NC"); } catch { }

            try
            {
                double rate = _egrabber.Remote.Get<double>("AcquisitionLineRate");
                if (rate <= 0) _egrabber.Remote.Set<double>("AcquisitionLineRate", 5000.0);
            }
            catch { }
        }

        private void fn_SetExternalTrigger()
        {
            // ── 1) Interface: IIN11(Pin3+/Pin12-) 물리핀 → LIN1 논리라인 매핑 ──
            //   라인 이름은 SystemParam.SensorInputLine(기본 "IIN11")과 동일하게 쓴다.
            //   → UI의 센서 I/O 램프가 실제 트리거 소스와 항상 같은 라인을 본다.
            try
            {
                _egrabber.Interface.Set<string>("LineInputToolSelector", "LIN1");
                _egrabber.Interface.Set<string>("LineInputToolSource", m_strSensorLine);
                _egrabber.Interface.Set<string>("LineInputToolActivation", "RisingEdge");
            }
            catch (Exception ex) { G.WriteLog($"Euresys Interface LineInput set fail: {ex.Message}", true); }

            // ── 1-1) 센서 ON → 스캔 시작 지연 (IOToolbox DelayTool) ──
            //   지연을 쓰면 시퀀스 시작 트리거를 LIN1이 아니라 지연 블록 출력에서 받는다.
            //   실패하면 LIN1을 그대로 써서 기존 동작(지연 없음)을 유지한다.
            string strSeqTriggerSource = fn_SetupTriggerDelay(G.SYSTEM.fn_GetSensorTriggerDelay(m_nCameraIndex)) ?? "LIN1";

            // ── 2) Camera(Remote): 보드 CoaXPress 라인트리거로 라인 스캔 ──
            //   이 카메라는 순수 라인스캔(FrameStart 없음, LineStart 트리거만 존재)이므로
            //   라인마다 보드가 보내는 CXP 트리거를 받아 1라인씩 스캔한다.
            //   라인주기는 카메라별 목표 라인레이트(현장조건+늘어짐 보정)에서 나온다. 카메라가 이 속도를
            //   내려면 노출시간 < 라인주기 여야 한다. 노출이 라인주기보다 길면 카메라 라인레이트가
            //   제한돼 보드가 더 빨라 오버런→일부 줄에서 멈춘다.
            double dLineRate = 9600.0;
            double targetRate = G.SYSTEM.fn_GetCamLineRate(m_nCameraIndex);   // 목표 라인레이트(Hz), 카메라별
            double targetLinePeriodUs = 1e6 / targetRate;
            try
            {
                try { _egrabber.Remote.Set<string>("AcquisitionMode", "Continuous"); } catch { }

                string lineStart = fn_FindRemoteEnum("TriggerSelector", "Line *Start");
                if (lineStart != null) _egrabber.Remote.Set<string>("TriggerSelector", lineStart);

                _egrabber.Remote.Set<string>("TriggerMode", "On");

                string cxpin = fn_FindRemoteEnum("TriggerSource", "CXP *In")
                            ?? fn_FindRemoteEnum("TriggerSource", "Link *Trigger")
                            ?? fn_FindRemoteEnum("TriggerSource", "CXP *Trigger");
                if (cxpin != null) _egrabber.Remote.Set<string>("TriggerSource", cxpin);
                else G.WriteLog("Euresys Camera: CXP 라인트리거 소스를 찾지 못함 (TriggerSource 후보 없음)", true);

                try { _egrabber.Remote.Set<string>("TriggerActivation", "RisingEdge"); } catch { }
                try { _egrabber.Remote.Set<string>("ExposureMode", "Timed"); } catch { }

                // 노출시간을 라인주기에 맞게 캡(여유 8us) → 카메라가 목표 라인레이트를 낼 수 있게 함
                double maxExp = targetLinePeriodUs - 8.0;
                try
                {
                    double curExp = _egrabber.Remote.Get<double>("ExposureTime");
                    if (curExp > maxExp)
                    {
                        _egrabber.Remote.Set<double>("ExposureTime", maxExp);
                        G.WriteLog($"Euresys[CAM{m_nCameraIndex + 1}] 노출시간 {curExp:F1}->{maxExp:F1}us 제한 (라인주기 {targetLinePeriodUs:F1}us 달성)");
                    }
                }
                catch { }

                // 목표 라인레이트 설정 후 실제 허용값 readback
                try { _egrabber.Remote.Set<double>("AcquisitionLineRate", targetRate); } catch { }
                try { dLineRate = _egrabber.Remote.Get<double>("AcquisitionLineRate"); } catch { }
                if (dLineRate <= 0) dLineRate = 9600.0;
            }
            catch (Exception ex) { G.WriteLog($"Euresys Camera trigger set fail: {ex.Message}", true); }

            // ── 3) Device(보드 CIC): 센서(LIN1) 1펄스 → N개 라인 시퀀스 ──
            //   • CycleTriggerSource=Immediate + CycleMinimumPeriod : 시퀀스 내 라인을
            //     내부 클럭(카메라별 목표 라인주기)으로 자동 생성 (cycle 1개 = line 1개)
            //   • StartOfSequenceTriggerSource=LIN1 : 센서 펄스가 시퀀스(=1 이미지) 시작
            //   • EndOfSequenceTriggerSource=SequenceLength, SequenceLength=N : N라인 후 종료
            try
            {
                _egrabber.Device.Set<string>("CameraControlMethod", "RC");

                try { _egrabber.Device.Set<string>("CycleTriggerSource", "Immediate"); }
                catch (Exception ex) { G.WriteLog($"Euresys CycleTriggerSource set fail: {ex.Message}", true); }

                // 보드 라인트리거 주기: 카메라가 목표를 낼 수 있으면 목표 라인주기 그대로 유지,
                // 못 내면 카메라 실제속도×1.03(오버런 방지, 전체 라인 우선).
                double dCyclePeriodUs = (dLineRate >= targetRate * 0.99)
                                      ? targetLinePeriodUs
                                      : (1e6 / dLineRate) * 1.03;
                try { _egrabber.Device.Set<double>("CycleMinimumPeriod", dCyclePeriodUs); }
                catch (Exception ex) { G.WriteLog($"Euresys CycleMinimumPeriod set fail: {ex.Message}", true); }

                G.WriteLog($"Euresys[CAM{m_nCameraIndex + 1}] 라인레이트 목표:{targetRate:F1}Hz 카메라:{dLineRate:F1}Hz 적용주기:{dCyclePeriodUs:F2}us");

                try { _egrabber.Device.Set<string>("StartOfSequenceTriggerSource", strSeqTriggerSource); }
                catch (Exception ex) { G.WriteLog($"Euresys StartOfSequenceTriggerSource set fail: {ex.Message}", true); }

                try { _egrabber.Device.Set<string>("EndOfSequenceTriggerSource", "SequenceLength"); }
                catch (Exception ex) { G.WriteLog($"Euresys EndOfSequenceTriggerSource set fail: {ex.Message}", true); }

                try { _egrabber.Device.Set<long>("SequenceLength", (long)m_nHeight); }
                catch (Exception ex) { G.WriteLog($"Euresys SequenceLength set fail: {ex.Message}", true); }
            }
            catch (Exception ex) { G.WriteLog($"Euresys Device sequence set fail: {ex.Message}", true); }
        }

        // --- 센서 트리거 지연 (Interface > IOToolbox > DelayTool) ---

        /// <summary>
        /// 센서 신호(LIN1)를 지연 블록에 통과시켜, 지연된 출력을 시퀀스 시작 트리거로 쓴다.
        ///
        ///   LIN1 ──▶ DelayTool(DEL1) ──▶ StartOfSequenceTriggerSource
        ///
        /// DelayToolDelayValue는 시간이 아니라 DelayToolClockSource의 "틱 수"이므로,
        /// 요청 지연을 담을 수 있는 가장 분해능 높은 클럭을 골라 틱으로 환산한다.
        /// 설정 후 readback으로 실제 반영 여부를 확인하고, 실패하면 null을 돌려
        /// 호출측이 기존 경로(LIN1 직결 = 지연 없음)를 그대로 쓰게 한다.
        /// </summary>
        /// <param name="nDelayUs">지연 시간(us). 0 이하면 지연 사용 안 함.</param>
        /// <returns>시퀀스 시작 트리거로 쓸 소스 이름. 지연 미사용/실패 시 null.</returns>
        private string fn_SetupTriggerDelay(int nDelayUs)
        {
            if (nDelayUs <= 0)
            {
                // 이전 설정이 남아 있으면 끊어 둔다(지연 없이 동작해야 하므로).
                try
                {
                    _egrabber.Interface.Set<string>("DelayToolSelector", m_strDelayTool);
                    _egrabber.Interface.Set<string>("DelayToolSource1", "NONE");
                }
                catch { }
                return null;
            }

            try
            {
                _egrabber.Interface.Set<string>("DelayToolSelector", m_strDelayTool);
                _egrabber.Interface.Set<string>("DelayToolSource1", "LIN1");

                // 지연 출력을 시퀀스 시작 트리거로 받을 수 있는지 먼저 확인.
                string strSource = fn_FindDelayTriggerSource();
                if (strSource == null)
                {
                    G.WriteLog($"Euresys[CAM{m_nCameraIndex + 1}] 트리거 지연: StartOfSequenceTriggerSource에 {m_strDelayTool} 출력이 없음 → 지연 미적용 " +
                               $"(후보: {string.Join(",", fn_DeviceEnumEntries("StartOfSequenceTriggerSource"))})", true);
                    return null;
                }

                // 클럭 후보를 주기(us) 오름차순 = 분해능 높은 순으로 시도.
                var clocks = fn_GetClockCandidates();
                if (clocks.Count == 0)
                {
                    G.WriteLog($"Euresys[CAM{m_nCameraIndex + 1}] 트리거 지연: 사용 가능한 DelayToolClockSource를 해석하지 못함 → 지연 미적용 " +
                               $"(후보: {string.Join(",", fn_InterfaceEnumEntries("DelayToolClockSource"))})", true);
                    return null;
                }

                foreach (var clk in clocks)
                {
                    long nTicks = (long)Math.Round(nDelayUs / clk.Value);
                    if (nTicks < 1) continue;   // 이 클럭으로는 표현 불가(너무 느린 클럭)

                    try
                    {
                        _egrabber.Interface.Set<string>("DelayToolClockSource", clk.Key);
                        _egrabber.Interface.Set<long>("DelayToolDelayValue", nTicks);

                        // 레지스터 폭을 넘으면 값이 잘리므로 반드시 readback으로 확인.
                        long nReadback = _egrabber.Interface.Get<long>("DelayToolDelayValue");
                        if (nReadback != nTicks) continue;

                        double dActualUs = nReadback * clk.Value;
                        G.WriteLog($"Euresys[CAM{m_nCameraIndex + 1}] 트리거 지연 설정: {dActualUs:F1}us (요청 {nDelayUs}us, " +
                                   $"{m_strDelayTool} clk:{clk.Key} {nReadback}tick, 분해능 {clk.Value:F2}us) → {strSource}");
                        return strSource;
                    }
                    catch { /* 다음 클럭으로 */ }
                }

                G.WriteLog($"Euresys[CAM{m_nCameraIndex + 1}] 트리거 지연: {nDelayUs}us를 표현할 수 있는 클럭이 없음 → 지연 미적용", true);
            }
            catch (Exception ex)
            {
                G.WriteLog($"Euresys[CAM{m_nCameraIndex + 1}] 트리거 지연 설정 실패 → 지연 미적용: {ex.Message}", true);
            }
            return null;
        }

        /// <summary>
        /// StartOfSequenceTriggerSource 열거값 중 지연 블록의 첫 번째 출력을 찾는다.
        /// (블록당 출력이 2개이므로 "DEL1"/"DEL11" 순으로 우선 매칭)
        /// </summary>
        private string fn_FindDelayTriggerSource()
        {
            string[] entries = fn_DeviceEnumEntries("StartOfSequenceTriggerSource");

            foreach (var e in entries) if (e == m_strDelayTool) return e;
            foreach (var e in entries) if (e == m_strDelayTool + "1") return e;
            foreach (var e in entries) if (e.StartsWith(m_strDelayTool, StringComparison.OrdinalIgnoreCase)) return e;
            return null;
        }

        /// <summary>
        /// DelayToolClockSource 열거값을 (이름, 1틱당 us)로 해석해 분해능 높은 순으로 정렬한다.
        /// 이름 형식은 보드/드라이버 버전에 따라 "MHz100" / "100MHz" 등으로 다를 수 있어 둘 다 인식한다.
        /// </summary>
        // Coaxlink IOToolbox 클럭 이름은 두 형식이 관측된다.
        //   1) 주기 직접 표기: "TIME8NS", "TIME200NS", "TIME1US" (실제 이 보드가 사용하는 형식)
        //   2) 주파수 표기: "MHz100" 등 (드라이버/보드 버전에 따라 있을 수 있어 폴백으로 유지)
        // 두 형식 모두 시도해 1틱당 us(주기)로 통일해 반환한다.
        private static readonly System.Text.RegularExpressions.Regex RE_CLOCK_PERIOD =
            new System.Text.RegularExpressions.Regex(
                @"^TIME(?<num>\d+(?:\.\d+)?)(?<unit>NS|US|MS|S)$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        private static readonly System.Text.RegularExpressions.Regex RE_CLOCK_FREQ =
            new System.Text.RegularExpressions.Regex(
                @"(?:(?<unit>MHz|kHz|Hz)\s*(?<num>\d+(?:\.\d+)?))|(?:(?<num2>\d+(?:\.\d+)?)\s*(?<unit2>MHz|kHz|Hz))",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        private List<KeyValuePair<string, double>> fn_GetClockCandidates()
        {
            var list = new List<KeyValuePair<string, double>>();

            foreach (var name in fn_InterfaceEnumEntries("DelayToolClockSource"))
            {
                double dPeriodUs;

                var mPeriod = RE_CLOCK_PERIOD.Match(name);
                if (mPeriod.Success)
                {
                    double dNum;
                    if (!double.TryParse(mPeriod.Groups["num"].Value, out dNum) || dNum <= 0) continue;

                    string strUnit = mPeriod.Groups["unit"].Value;
                    dPeriodUs = strUnit.Equals("NS", StringComparison.OrdinalIgnoreCase) ? dNum / 1000.0
                              : strUnit.Equals("US", StringComparison.OrdinalIgnoreCase) ? dNum
                              : strUnit.Equals("MS", StringComparison.OrdinalIgnoreCase) ? dNum * 1000.0
                              : dNum * 1e6;   // S
                }
                else
                {
                    var mFreq = RE_CLOCK_FREQ.Match(name);
                    if (!mFreq.Success) continue;   // NONE, LINx, QDCx 등 시간 클럭이 아닌 항목은 제외

                    string strNum  = mFreq.Groups["num"].Success  ? mFreq.Groups["num"].Value  : mFreq.Groups["num2"].Value;
                    string strUnit = mFreq.Groups["unit"].Success ? mFreq.Groups["unit"].Value : mFreq.Groups["unit2"].Value;

                    double dNum;
                    if (!double.TryParse(strNum, out dNum) || dNum <= 0) continue;

                    double dHz = strUnit.Equals("MHz", StringComparison.OrdinalIgnoreCase) ? dNum * 1e6
                               : strUnit.Equals("kHz", StringComparison.OrdinalIgnoreCase) ? dNum * 1e3
                               : dNum;
                    dPeriodUs = 1e6 / dHz;
                }

                list.Add(new KeyValuePair<string, double>(name, dPeriodUs));   // 1틱당 us
            }

            list.Sort((a, b) => a.Value.CompareTo(b.Value));   // 분해능 높은(주기 짧은) 순
            return list;
        }

        private string[] fn_InterfaceEnumEntries(string feature)
        {
            try { return _egrabber.Interface.EnumEntries(feature, true); }
            catch { return new string[0]; }
        }

        private string[] fn_DeviceEnumEntries(string feature)
        {
            try { return _egrabber.Device.EnumEntries(feature, true); }
            catch { return new string[0]; }
        }

        // --- 센서 입력 I/O (Interface 레이어) ---

        /// <summary>
        /// 센서 입력 라인(기본 IIN11 = 15pin D-Sub #3/#12)의 현재 레벨을 읽는다.
        /// Interface 모듈의 LineSelector로 라인을 고른 뒤 LineStatus를 읽는 방식이라
        /// grab 중에도(트리거를 가로채지 않고) 신호 유무만 확인할 수 있다.
        /// </summary>
        /// <param name="bLevel">읽은 레벨. 실패 시 마지막으로 성공한 값.</param>
        /// <returns>읽기 성공 여부</returns>
        public bool fn_TryGetSensorInput(out bool bLevel)
        {
            lock (m_lockSensor)
            {
                bLevel = m_bLastSensorLevel;
                if (!m_bIsInit || !m_bSensorReadable || _egrabber == null) return false;

                try
                {
                    _egrabber.Interface.Set<string>("LineSelector", m_strSensorLine);

                    bool bRead;
                    try { bRead = _egrabber.Interface.Get<bool>("LineStatus"); }
                    catch { bRead = fn_ParseBool(_egrabber.Interface.Get<string>("LineStatus")); }

                    m_bLastSensorLevel = bRead;
                    m_nSensorFailCount = 0;
                    bLevel = bRead;
                    return true;
                }
                catch (Exception ex)
                {
                    // grab 스레드가 pop 중이면 보드 접근이 잠깐 막힌다("EGrabber is busy in another thread").
                    // 일시적인 상황이므로 마지막 값을 유지하고 다음 폴링에서 재시도한다.
                    if (ex.Message.IndexOf("busy", StringComparison.OrdinalIgnoreCase) >= 0) return false;

                    // 그 외 오류(미지원 feature 등)가 연속으로 나면 로그 폭주를 막기 위해 모니터링을 중단한다.
                    if (++m_nSensorFailCount >= SENSOR_FAIL_LIMIT)
                    {
                        m_bSensorReadable = false;
                        G.WriteLog($"Euresys 센서 I/O 읽기 실패 → 모니터링 중단 (IF:{m_nInterfaceIndex} Line:{m_strSensorLine}): {ex.Message}", true);
                    }
                    return false;
                }
            }
        }

        private static bool fn_ParseBool(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            string v = value.Trim();
            return v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase)
                            || v.Equals("on", StringComparison.OrdinalIgnoreCase)
                            || v.Equals("high", StringComparison.OrdinalIgnoreCase)
                            || v.Equals("active", StringComparison.OrdinalIgnoreCase);
        }

        private void fn_ExecuteRemote(string command)
        {
            try { _egrabber.Remote.Execute(command); }
            catch (Exception ex) { G.WriteLog($"Euresys Execute [{command}] fail: {ex.Message}", true); }
        }

        // Remote enum 항목 중 정규식과 일치하는 첫 값을 반환 (카메라별 명칭 차이 자동 흡수)
        // 예: TriggerSelector → "FrameStart"/"ExposureStart", TriggerSource → "CXPin"/"LinkTrigger0"
        private string fn_FindRemoteEnum(string feature, string pattern)
        {
            try
            {
                string[] entries = _egrabber.Remote.EnumEntries(feature, true);
                var re = new System.Text.RegularExpressions.Regex(
                    pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                foreach (var e in entries)
                    if (re.IsMatch(e)) return e;
            }
            catch { }
            return null;
        }

        // --- 카메라 파라미터 (GenICam Remote 레이어) ---

        public void fn_SetExposureTime(float valueUs)
        {
            if (!m_bIsInit) return;
            try
            {
                _egrabber.Remote.Set<double>("ExposureTime", (double)valueUs);
                fn_RestartAcquisitionIfNeeded();
            }
            catch (Exception ex) { G.WriteLog($"Euresys SetExposureTime Fail: {ex.Message}", true); }
        }

        public float fn_GetExposureTime()
        {
            if (!m_bIsInit) return 0;
            try { return (float)_egrabber.Remote.Get<double>("ExposureTime"); }
            catch { return 0; }
        }

        public void fn_SetGain(float value)
        {
            if (!m_bIsInit) return;
            try
            {
                _egrabber.Remote.Set<double>("Gain", (double)value);
                fn_RestartAcquisitionIfNeeded();
            }
            catch (Exception ex) { G.WriteLog($"Euresys SetGain Fail: {ex.Message}", true); }
        }

        private void fn_RestartAcquisitionIfNeeded()
        {
            if (!m_bIsGrabbing) return;
            m_bParamChanging = true;
            try
            {
                _egrabber.Remote.Execute("AcquisitionStop");
                _egrabber.Remote.Execute("AcquisitionStart");
            }
            catch { }
            finally { m_bParamChanging = false; }
        }

        public float fn_GetGain()
        {
            if (!m_bIsInit) return 0;
            try { return (float)_egrabber.Remote.Get<double>("Gain"); }
            catch { return 0; }
        }

        // --- 스트림 파라미터 (Stream 레이어) ---

        public int fn_GetScanLength()
        {
            if (!m_bIsInit) return 0;
            try { return (int)_egrabber.Stream.Get<ulong>("ScanLength"); }
            catch { return 0; }
        }

        public void fn_SetScanLength(int value)
        {
            if (!m_bIsInit) return;
            try { _egrabber.Stream.Set<ulong>("ScanLength", (ulong)value); }
            catch (Exception ex) { G.WriteLog($"Euresys SetScanLength Fail: {ex.Message}", true); }
        }

        public int fn_GetBufferHeight()
        {
            if (!m_bIsInit) return 0;
            try { return (int)_egrabber.Stream.Get<ulong>("BufferHeight"); }
            catch { return 0; }
        }

        public void fn_SetBufferHeight(int value)
        {
            if (!m_bIsInit) return;
            // ScanLength도 함께 맞춰 줄무늬(ScanLength≠BufferHeight) 방지
            try { fn_SetBufferAndScan((ulong)value); }
            catch (Exception ex) { G.WriteLog($"Euresys SetBufferHeight Fail: {ex.Message}", true); }
        }

        private static MatType fn_GetMatType(string pixelFormat)
        {
            if (pixelFormat == null) return MatType.CV_8UC1;
            string pf = pixelFormat.ToLower();
            if (pf.Contains("rgb8") || pf.Contains("bgr8")) return MatType.CV_8UC3;
            if (pf.Contains("mono10") || pf.Contains("mono12") || pf.Contains("mono16")) return MatType.CV_16UC1;
            return MatType.CV_8UC1;
        }

        private static double fn_GetBitShift(string pixelFormat)
        {
            if (pixelFormat == null) return 1.0;
            string pf = pixelFormat.ToLower();
            if (pf.Contains("mono10")) return 1.0 / 4.0;
            if (pf.Contains("mono12")) return 1.0 / 16.0;
            if (pf.Contains("mono14")) return 1.0 / 64.0;
            if (pf.Contains("mono16")) return 1.0 / 256.0;
            return 1.0;
        }
    }
}
