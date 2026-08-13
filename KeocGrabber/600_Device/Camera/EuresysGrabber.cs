using System;
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
        // 현장조건: 200mm/s 물체 스캔용 라인주기(=11050Hz).
        // 라인주기 = X픽셀분해능 / 속도 라야 정사각 비율. 늘어짐 보정 필요시 = 현주기 × (정사각물체 결과 H/W).
        const double TARGET_LINE_PERIOD_US = 90.5;

        public int Width { get { return m_nWidth; } }
        public int Height { get { return m_nHeight; } }
        public int Channel { get { return m_nChannel; } }
        public int BoardIndex { get { return m_nInterfaceIndex; } }
        public bool IsInit { get { return m_bIsInit; } }
        public bool IsGrabbing { get { return m_bIsGrabbing; } }

        public void fn_Init(EGrabberInfo info)
        {
            try
            {
                m_nInterfaceIndex = info.InterfaceIndex;
                m_nDeviceIndex    = info.DeviceIndex;

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

            try { _egrabber?.Dispose(); } catch { }
            _egrabber = null;

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
            try
            {
                _egrabber.Interface.Set<string>("LineInputToolSelector", "LIN1");
                _egrabber.Interface.Set<string>("LineInputToolSource", "IIN11");
                _egrabber.Interface.Set<string>("LineInputToolActivation", "RisingEdge");
            }
            catch (Exception ex) { G.WriteLog($"Euresys Interface LineInput set fail: {ex.Message}", true); }

            // ── 2) Camera(Remote): 보드 CoaXPress 라인트리거로 라인 스캔 ──
            //   이 카메라는 순수 라인스캔(FrameStart 없음, LineStart 트리거만 존재)이므로
            //   라인마다 보드가 보내는 CXP 트리거를 받아 1라인씩 스캔한다.
            //   라인주기는 TARGET_LINE_PERIOD_US(현장조건+늘어짐 보정). 카메라가 이 속도를 내려면 노출시간 < 라인주기 여야 한다.
            //   노출이 라인주기보다 길면 카메라 라인레이트가 제한돼 보드가 더 빨라 오버런→일부 줄에서 멈춘다.
            double dLineRate = 9600.0;
            double targetRate = 1e6 / TARGET_LINE_PERIOD_US;   // 목표 라인레이트(Hz)
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
                double maxExp = TARGET_LINE_PERIOD_US - 8.0;
                try
                {
                    double curExp = _egrabber.Remote.Get<double>("ExposureTime");
                    if (curExp > maxExp)
                    {
                        _egrabber.Remote.Set<double>("ExposureTime", maxExp);
                        G.WriteLog($"Euresys 노출시간 {curExp:F1}->{maxExp:F1}us 제한 (라인주기 {TARGET_LINE_PERIOD_US}us 달성)");
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
            //     내부 클럭(TARGET_LINE_PERIOD_US)으로 자동 생성 (cycle 1개 = line 1개)
            //   • StartOfSequenceTriggerSource=LIN1 : 센서 펄스가 시퀀스(=1 이미지) 시작
            //   • EndOfSequenceTriggerSource=SequenceLength, SequenceLength=N : N라인 후 종료
            try
            {
                _egrabber.Device.Set<string>("CameraControlMethod", "RC");

                try { _egrabber.Device.Set<string>("CycleTriggerSource", "Immediate"); }
                catch (Exception ex) { G.WriteLog($"Euresys CycleTriggerSource set fail: {ex.Message}", true); }

                // 보드 라인트리거 주기: 카메라가 목표를 낼 수 있으면 정확히 TARGET_LINE_PERIOD_US 유지,
                // 못 내면 카메라 실제속도×1.03(오버런 방지, 전체 라인 우선).
                double dCyclePeriodUs = (dLineRate >= targetRate * 0.99)
                                      ? TARGET_LINE_PERIOD_US
                                      : (1e6 / dLineRate) * 1.03;
                try { _egrabber.Device.Set<double>("CycleMinimumPeriod", dCyclePeriodUs); }
                catch (Exception ex) { G.WriteLog($"Euresys CycleMinimumPeriod set fail: {ex.Message}", true); }

                try { _egrabber.Device.Set<string>("StartOfSequenceTriggerSource", "LIN1"); }
                catch (Exception ex) { G.WriteLog($"Euresys StartOfSequenceTriggerSource set fail: {ex.Message}", true); }

                try { _egrabber.Device.Set<string>("EndOfSequenceTriggerSource", "SequenceLength"); }
                catch (Exception ex) { G.WriteLog($"Euresys EndOfSequenceTriggerSource set fail: {ex.Message}", true); }

                try { _egrabber.Device.Set<long>("SequenceLength", (long)m_nHeight); }
                catch (Exception ex) { G.WriteLog($"Euresys SequenceLength set fail: {ex.Message}", true); }
            }
            catch (Exception ex) { G.WriteLog($"Euresys Device sequence set fail: {ex.Message}", true); }
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
