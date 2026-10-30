/*
 *******************************************************************************
 * 해당 소스는 현장 유지 보수 외에 다른 목적의 사용을 금합니다.
 * - 주식회사 태루 -
 *******************************************************************************
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Matrox.MatroxImagingLibrary;
using OpenCvSharp;

namespace KeocGrabber
{
    public enum MILBOARD_TYPE
    {
        EN_BT_DEFAULT = 0,
        EN_BT_HOST,
        EN_BT_SOLIOS,
        EN_BT_RADIENT,
        EN_BT_RADIENTCLHS,
        EN_BT_RADIENTEVCL,
        EN_BT_RADIENTCXP,
        EN_BT_RADIENTPRO,
        EN_BT_RAPIXOCXP
    }
    class MatroxGrabber
    {
        Func<Mat, int, bool> delGrab = null;
        Func<string, bool> delLog = null;
        public Func<Mat, int, bool> OnGrab { set { delGrab = value; } }
        public Func<string, bool> OnLog { set { delLog = value; } }

        int m_nGrabBufferListSize = 0;

        uint m_nFrameCount = 0;

        Mat m_matGrab;
        byte[] m_databuff;

        int m_nBoardIndex = 0;
        int m_nCameraIndex = 0;

        // ── 촬상 진단([GrabDiag]) ─────────────────────────────────────────────
        // 그랩 콜백에서는 값만 기록하고 로그는 fn_GrabStop에서 카메라당 한 줄로 남긴다.
        // (G.WriteLog는 파일+UI까지 가므로 콜백 안에서 쓰면 프레임 처리가 늦어질 수 있음)
        int m_nDiagSeq = 0;
        bool m_bDiagProduction = false;
        readonly Stopwatch m_swDiag = new Stopwatch();
        readonly object m_lockDiag = new object();
        readonly List<int> m_listDiagIndex = new List<int>();
        readonly List<double> m_listDiagMs = new List<double>();
        int m_nDiagFirstSerialPending = -1;
        volatile bool m_bStopping = false;
        int m_nDiagFramesWhileStopping = 0;
        int m_nDiagDiscarded = 0;

        /// <summary>도착했지만 이미지에 붙이지 않고 버린 프레임 수(이미 완료된 이미지에 온 프레임). GrabberManager가 올린다.</summary>
        public void fn_DiagAddDiscarded() { lock (m_lockDiag) m_nDiagDiscarded++; }

        // 프레임 시작 훅(M_GRAB_FRAME_START): 보드가 프레임(청크) 스캔을 시작한 순간.
        // M_TRIGGER_FOR_FIRST_GRAB에서 첫 프레임 시작 = 센서 트리거로 촬상이 시작된 순간.
        // 보드가 지원하지 않으면 MIL 에러 콜백에 기록만 되고(팝업 없음) 촬상에는 영향 없다.
        MIL_DIG_HOOK_FUNCTION_PTR m_hookFrameStart = null;
        bool m_bFrameStartHooked = false;
        int m_nDiagFrameStartCount = 0;
        public bool IsDiagProduction { get { return m_bDiagProduction; } }

        MIL_ID m_MilApplication = MIL.M_NULL;         // Application identifier.
        MIL_ID m_MilSystem = MIL.M_NULL;              // System identifier.
        MIL_ID m_MilDigitizer = MIL.M_NULL;           // Digitizer identifier.
        MIL_ID m_MilImage = MIL.M_NULL;               // Image buffer identifier.

        MIL_ID[] m_MilBuffer = null;

        MIL_ID m_MilImageSizeX;
        MIL_ID m_MilImageSizeY;
        MIL_ID m_MilImageType;
        MIL_ID m_MilImageBand;

        GCHandle hUserData;

        public MIL_ID ApplicationID { get { return m_MilApplication; } }
        public MIL_ID SystemID { get { return m_MilSystem; } }
        public int Width { get { return (int)m_MilImageSizeX; } }
        public int Height { get { return (int)m_MilImageSizeY; } }
        public int Channel { get { return (int)m_MilImageBand; } }

        public int BoardIndex { get { return m_nBoardIndex; } }

        public int BufferCount { get { return m_MilBuffer.Length; } }

        bool m_bIsInit = false;
        bool m_bIsCanSystemFree = false;
        bool m_bIsGrabbing = false;

        public bool IsGrabbing { get { return m_bIsGrabbing; } }

        public bool IsInit { get { return m_bIsInit; } }
        public bool IsCanSystemFree { set { m_bIsCanSystemFree = value; } }
        MIL_DIG_HOOK_FUNCTION_PTR UserHookFunctionDelegate = null;

        public MatroxGrabber()
        {
            // Allocate defaults.
        }

        public void fn_Init(MILBOARD_TYPE boardtype, int boardindex, int cameraindex, string dcfpath, MIL_ID systemid)
        {
            MIL_ID rtn = MIL.M_NULL;
            if (hUserData.IsAllocated) hUserData.Free();
            hUserData = GCHandle.Alloc(this);
            try
            {
                m_nBoardIndex = boardindex;
                m_nCameraIndex = cameraindex;
                //MIL.MappAlloc(MIL.M_DEFAULT, ref m_MilApplication);
                if (UserHookFunctionDelegate == null)
                    UserHookFunctionDelegate = new MIL_DIG_HOOK_FUNCTION_PTR(OnGrabbedImage);

                string board = MIL.M_SYSTEM_DEFAULT;
                switch (boardtype)
                {
                    case MILBOARD_TYPE.EN_BT_DEFAULT: board = MIL.M_SYSTEM_DEFAULT; break;
                    case MILBOARD_TYPE.EN_BT_HOST: board = MIL.M_SYSTEM_HOST; break;
                    case MILBOARD_TYPE.EN_BT_SOLIOS: board = MIL.M_SYSTEM_SOLIOS; break;
                    case MILBOARD_TYPE.EN_BT_RADIENT: board = MIL.M_SYSTEM_RADIENT; break;
                    case MILBOARD_TYPE.EN_BT_RADIENTCLHS: board = MIL.M_SYSTEM_RADIENTCLHS; break;
                    case MILBOARD_TYPE.EN_BT_RADIENTEVCL: board = MIL.M_SYSTEM_RADIENTEVCL; break;
                    case MILBOARD_TYPE.EN_BT_RADIENTCXP: board = MIL.M_SYSTEM_RADIENTCXP; break;
                    case MILBOARD_TYPE.EN_BT_RADIENTPRO: board = MIL.M_SYSTEM_RADIENTPRO; break;
                    case MILBOARD_TYPE.EN_BT_RAPIXOCXP: board = MIL.M_SYSTEM_RADIENTCXP; break;
                }
                //#if DEBUG
                //board = MIL.M_SYSTEM_DEFAULT;
                //#endif

                //if (systemid == MIL.M_NULL)
                MIL.MsysAlloc(board, MIL.M_DEV0 + m_nBoardIndex, MIL.M_DEFAULT, ref m_MilSystem);
                //MIL.MsysAlloc(MIL.M_DEFAULT, board, MIL.M_DEV0 + m_nBoardIndex, MIL.M_DEFAULT, ref m_MilSystem);
                //else
                //    m_MilSystem = systemid;

                rtn = MIL.MdigAlloc(m_MilSystem, MIL.M_DEV0 + m_nCameraIndex, dcfpath, MIL.M_DEFAULT, ref m_MilDigitizer);

                if (rtn == MIL.M_NULL)
                {
                    G.WriteLog("Digitizer Alloc Fail.", true);
                    return;
                }

                MIL.MdigInquire(m_MilDigitizer, MIL.M_SOURCE_SIZE_X, ref m_MilImageSizeX); // size 받음.
                MIL.MdigInquire(m_MilDigitizer, MIL.M_SOURCE_SIZE_Y, ref m_MilImageSizeY);
                MIL.MdigInquire(m_MilDigitizer, MIL.M_TYPE, ref m_MilImageType);
                MIL.MdigInquire(m_MilDigitizer, MIL.M_SIZE_BAND, ref m_MilImageBand);

                CreateBuffer();

                //MbufClear(m_MilImage, 0);   // 버퍼 초기화

                MatType matType = new MatType();
                matType = MatType.CV_8UC1;
                switch (m_MilImageType)
                {
                    case 3: matType = MatType.CV_8UC3; break;
                }
                // Digitizer 설정
                MIL.MdigControl(m_MilDigitizer, MIL.M_GRAB_TIMEOUT, MIL.M_INFINITE);
                MIL.MdigControl(m_MilDigitizer, MIL.M_GRAB_MODE, MIL.M_ASYNCHRONOUS);

                if (G.SYSTEM.GrabDiagEnable)
                {
                    try
                    {
                        m_hookFrameStart = new MIL_DIG_HOOK_FUNCTION_PTR(OnFrameStart);
                        MIL.MdigHookFunction(m_MilDigitizer, MIL.M_GRAB_FRAME_START, m_hookFrameStart, GCHandle.ToIntPtr(hUserData));
                        m_bFrameStartHooked = true;
                    }
                    catch (Exception ex) { G.WriteLog($"[GrabDiag] CAM{m_nBoardIndex + 1} 프레임 시작 훅 등록 실패(촬상과 무관): {ex.Message}"); }
                }

                delLog?.Invoke($"MIL Init.({BoardIndex})");

                m_matGrab = Mat.Zeros(new Size(m_MilImageSizeX, m_MilImageSizeY), matType);

                m_databuff = new byte[m_MilImageSizeX * m_MilImageSizeY * m_MilImageType];

                delLog?.Invoke($"Buffer Init.({BoardIndex}) [{m_MilImageSizeX} x {m_MilImageSizeY} x {m_MilImageType}]");
                m_bIsInit = true;
            }
            catch (Exception ex)
            {
                G.WriteLog(ex.Message, true);
            }
        }

        public void fn_Final()
        {
            if(m_bIsGrabbing)
            {
                fn_GrabStop();
                Thread.Sleep(200);
            }

            m_databuff = null;
            if (m_matGrab != null && !m_matGrab.Empty()) m_matGrab.Release();

            delLog?.Invoke($"Buffer Final.({BoardIndex})");

            //for (m_nGrabBufferListSize = 0; m_nGrabBufferListSize < BUFFERING_SIZE_MAX; m_nGrabBufferListSize++)
            for (m_nGrabBufferListSize = 0; m_nGrabBufferListSize < m_MilBuffer?.Length; m_nGrabBufferListSize++)
            {
                if (m_MilBuffer[m_nGrabBufferListSize] != MIL.M_NULL)
                {
                    MIL.MbufFree(m_MilBuffer[m_nGrabBufferListSize]);
                    m_MilBuffer[m_nGrabBufferListSize] = MIL.M_NULL;
                }
            }

            if (m_MilImage != MIL.M_NULL)
            {
                MIL.MbufFree(m_MilImage); // Display Buffer 해제
                m_MilImage = MIL.M_NULL;
            }
            if (m_bFrameStartHooked && m_MilDigitizer != MIL.M_NULL)
            {
                try { MIL.MdigHookFunction(m_MilDigitizer, MIL.M_GRAB_FRAME_START + MIL.M_UNHOOK, m_hookFrameStart, GCHandle.ToIntPtr(hUserData)); } catch { }
                m_bFrameStartHooked = false;
            }
            if (m_MilDigitizer != MIL.M_NULL)
            {
                MIL.MdigFree(m_MilDigitizer); // Digitizer 해제
                m_MilDigitizer = MIL.M_NULL;
            }
            if (m_bIsCanSystemFree && m_MilSystem != MIL.M_NULL)
            {
                MIL.MsysFree(m_MilSystem); // System 해제
                m_MilSystem = MIL.M_NULL;
            }
            UserHookFunctionDelegate = null;
            //if (m_MilApplication != MIL.M_NULL) MIL.MappFree(m_MilApplication); // Application 해제
            if (hUserData.IsAllocated) hUserData.Free();
            delLog?.Invoke($"MIL Final.({BoardIndex})");
            m_bIsInit = false;
        }

        public void CreateBuffer()
        {
            for (m_nGrabBufferListSize = 0; m_nGrabBufferListSize < m_MilBuffer?.Length; m_nGrabBufferListSize++)
            {
                if (m_MilBuffer[m_nGrabBufferListSize] != MIL.M_NULL)
                {
                    MIL.MbufFree(m_MilBuffer[m_nGrabBufferListSize]);
                    m_MilBuffer[m_nGrabBufferListSize] = MIL.M_NULL;
                }
            }

            int GrabBufferCount = (int)Math.Ceiling(G.SYSTEM.GrabHeight / (double)((int)m_MilImageSizeY));
            m_MilBuffer = new MIL_ID[GrabBufferCount];

            for (m_nGrabBufferListSize = 0; m_nGrabBufferListSize < m_MilBuffer.Length; m_nGrabBufferListSize++)
            {
                if (m_MilImageType == 3)
                {
                    MIL.MbufAllocColor(m_MilSystem, m_MilImageBand, m_MilImageSizeX, m_MilImageSizeY, m_MilImageType, MIL.M_IMAGE + MIL.M_GRAB, ref m_MilBuffer[m_nGrabBufferListSize]);
                }
                else
                {
                    MIL.MbufAlloc2d(m_MilSystem, m_MilImageSizeX, m_MilImageSizeY, m_MilImageType, MIL.M_IMAGE + MIL.M_GRAB, ref m_MilBuffer[m_nGrabBufferListSize]);
                }
                MIL.MbufClear(m_MilBuffer[m_nGrabBufferListSize], 0);
            }
        }

        public void fn_GrabStart(bool bSetup = false)
        {
            m_nFrameCount = 0;
            lock (m_lockDiag)
            {
                m_nDiagSeq++;
                m_bDiagProduction = !bSetup;
                m_listDiagIndex.Clear();
                m_listDiagMs.Clear();
                m_nDiagFirstSerialPending = -1;
                m_nDiagFramesWhileStopping = 0;
                m_nDiagDiscarded = 0;
                m_nDiagFrameStartCount = 0;
                m_swDiag.Restart();
            }
            if (bSetup)
            {
                MIL.MdigControl(m_MilDigitizer, MIL.M_GRAB_TRIGGER_STATE, MIL.M_DISABLE); // sw trigger?
                MIL.MdigProcess(m_MilDigitizer, m_MilBuffer, m_MilBuffer.Length, MIL.M_START, MIL.M_ASYNCHRONOUS, UserHookFunctionDelegate, GCHandle.ToIntPtr(hUserData));
            }
            else
            {
                for (m_nGrabBufferListSize = 0; m_nGrabBufferListSize < m_MilBuffer.Length; m_nGrabBufferListSize++)
                {
                    MIL.MbufClear(m_MilBuffer[m_nGrabBufferListSize], 0);
                }

                MIL.MdigControl(m_MilDigitizer, MIL.M_GRAB_TRIGGER_STATE, MIL.M_ENABLE);
                MIL.MdigProcess(m_MilDigitizer, m_MilBuffer, m_MilBuffer.Length, MIL.M_SEQUENCE, MIL.M_ASYNCHRONOUS + MIL.M_TRIGGER_FOR_FIRST_GRAB, UserHookFunctionDelegate, GCHandle.ToIntPtr(hUserData));
            }

            m_bIsGrabbing = true;
            delLog?.Invoke($"Grab Start.({BoardIndex})");
        }

        public void fn_GrabStop()
        {
            // M_STOP은 이미 채워진 버퍼의 콜백이 끝날 때까지 기다린다 → 그 사이 도착한 프레임을 지각 프레임으로 센다.
            m_bStopping = true;
            MIL.MdigProcess(m_MilDigitizer, m_MilBuffer, m_MilBuffer.Length, MIL.M_STOP, MIL.M_DEFAULT, UserHookFunctionDelegate, GCHandle.ToIntPtr(hUserData));
            m_bStopping = false;
            m_bIsGrabbing = false;
            delLog?.Invoke($"Grab Stop.({BoardIndex})");
            fn_WriteDiagSummary();
        }

        private void fn_WriteDiagSummary()
        {
            try
            {
                if (!m_bDiagProduction || !G.SYSTEM.GrabDiagEnable) return;   // Setup 라이브뷰·진단 OFF는 요약하지 않음
                m_bDiagProduction = false;        // 같은 촬상에 대해 Stop이 두 번 불려도 한 번만

                int nExpected = m_MilBuffer != null ? m_MilBuffer.Length : 0;
                int[] idx; double[] ms; int stopping, discarded, serial, frameStart;
                double totalMs;
                lock (m_lockDiag)
                {
                    idx = m_listDiagIndex.ToArray();
                    ms = m_listDiagMs.ToArray();
                    stopping = m_nDiagFramesWhileStopping;
                    discarded = m_nDiagDiscarded;
                    serial = m_nDiagFirstSerialPending;
                    frameStart = m_nDiagFrameStartCount;
                    totalMs = m_swDiag.Elapsed.TotalMilliseconds;
                }

                string strFirst = ms.Length > 0 ? $"{ms[0]:F0}ms" : "-";
                string strGap = "-";
                if (ms.Length > 1)
                {
                    double min = double.MaxValue, max = 0, sum = 0;
                    for (int i = 1; i < ms.Length; i++)
                    {
                        double g = ms[i] - ms[i - 1];
                        sum += g; if (g < min) min = g; if (g > max) max = g;
                    }
                    strGap = $"{sum / (ms.Length - 1):F0}/{min:F0}/{max:F0}ms";
                }

                bool bOrderOk = true;
                for (int i = 0; i < idx.Length; i++) if (idx[i] != i) { bOrderOk = false; break; }
                string strOrder = bOrderOk ? "OK" : string.Join(",", idx);

                // 정상 촬상은 메인 로그에 남기지 않는다(세부는 GrabDiag CSV). 이상이 있을 때만 경고 한 줄.
                bool bFrameStartMismatch = m_bFrameStartHooked && frameStart > 0 && frameStart != idx.Length;
                bool bWarn = idx.Length != nExpected || !bOrderOk || stopping > 0 || discarded > 0 || bFrameStartMismatch;
                if (!bWarn) return;
                G.WriteLog($"[GrabDiag][경고] CAM{BoardIndex + 1} #{m_nDiagSeq} 준비→첫프레임:{strFirst} 프레임:{idx.Length}/{nExpected} " +
                           $"간격(평균/최소/최대):{strGap} 순서:{strOrder} 정지중도착:{stopping} 완료후버림:{discarded} " +
                           $"첫프레임시시리얼대기:{(serial < 0 ? "-" : serial.ToString())} 프레임시작훅:{(m_bFrameStartHooked ? frameStart.ToString() : "-")} 총:{totalMs / 1000.0:F1}s", true);
            }
            catch (Exception ex) { G.WriteLog($"[GrabDiag] CAM{BoardIndex + 1} 요약 실패: {ex.Message}", true); }
        }

        static private MIL_INT OnFrameStart(MIL_INT HookType, MIL_ID HookId, IntPtr HookDataPtr)
        {
            try
            {
                MatroxGrabber pMain = GCHandle.FromIntPtr(HookDataPtr).Target as MatroxGrabber;
                // Setup 라이브뷰(연속 촬상)는 기록하지 않는다
                if (pMain == null || !GrabDiagLog.On || !pMain.m_bDiagProduction) return 0;
                int n = Interlocked.Increment(ref pMain.m_nDiagFrameStartCount);
                // n=1 이 Production에서 센서 트리거로 스캔이 시작된 순간
                GrabDiagLog.Write(n == 1 ? "FRAME_START_1ST" : "FRAME_START", pMain.m_nBoardIndex,
                    $"#{pMain.m_nDiagSeq} n={n} 준비후 {pMain.m_swDiag.Elapsed.TotalMilliseconds:F1}ms");
            }
            catch { }
            return 0;
        }

        static private MIL_INT OnGrabbedImage(MIL_INT HookType, MIL_ID HookId, IntPtr HookDataPtr)
        {
            MatroxGrabber pMain = GCHandle.FromIntPtr(HookDataPtr).Target as MatroxGrabber;

            MIL_ID MilGrabBufID = MIL.M_NULL;
            MIL_INT MilIndex = MIL.M_NULL;

            MIL.MdigGetHookInfo(HookId, MIL.M_MODIFIED_BUFFER + MIL.M_BUFFER_ID, ref MilGrabBufID);
            MIL.MdigGetHookInfo(HookId, MIL.M_MODIFIED_BUFFER + MIL.M_BUFFER_INDEX, ref MilIndex);

            //if ()

            // 버퍼가 할당되어 있다면.
            //if (pMain.m_MilBuffer[MilIndex] != MIL.M_NULL)
            if (MilGrabBufID != MIL.M_NULL)
            {
                lock (pMain.m_lockDiag)
                {
                    if (pMain.m_bStopping) pMain.m_nDiagFramesWhileStopping++;
                    if (pMain.m_listDiagIndex.Count < 256)
                    {
                        pMain.m_listDiagIndex.Add((int)MilIndex);
                        pMain.m_listDiagMs.Add(pMain.m_swDiag.Elapsed.TotalMilliseconds);
                    }
                    if (pMain.m_nDiagFirstSerialPending < 0)
                    {
                        int nCam = pMain.m_nBoardIndex;
                        pMain.m_nDiagFirstSerialPending =
                            (G.CAMERA != null && nCam < G.CAMERA.Length && G.CAMERA[nCam] != null) ? G.CAMERA[nCam].PendingCount : 0;
                    }
                }

                // 버퍼(프레임 1장) 수신 순간
                if (GrabDiagLog.On && pMain.m_bDiagProduction)
                    GrabDiagLog.Write(pMain.m_bStopping ? "BUF_LATE" : "BUF", pMain.m_nBoardIndex,
                        $"#{pMain.m_nDiagSeq} n={pMain.m_nFrameCount + 1} MilIndex={MilIndex} 준비후 {pMain.m_swDiag.Elapsed.TotalMilliseconds:F1}ms");

                if (pMain.m_databuff != null) Array.Clear(pMain.m_databuff, 0, pMain.m_databuff.Length);

                if (pMain.m_MilImageType == 3)
                    MIL.MbufGetColor2d(MilGrabBufID, MIL.M_PACKED + MIL.M_BGR24, MIL.M_ALL_BANDS, 0, 0,
                        pMain.m_MilImageSizeX, pMain.m_MilImageSizeY, pMain.m_databuff);
                else
                    MIL.MbufGet2d(MilGrabBufID, 0, 0, pMain.m_MilImageSizeX, pMain.m_MilImageSizeY,
                        pMain.m_databuff);

                pMain.m_matGrab.SetArray<byte>(pMain.m_databuff);

                //Cv2.ImShow("Test", pMain.m_matGrab);
                //G.WriteLog($"OnGrabbedImage [idx : {pMain.m_nCameraIndex}] framecnt : {pMain.m_nFrameCount} milidx : {MilIndex} milbufid : {MilGrabBufID}");
                pMain.m_nFrameCount++;

                // 콜백 실행.
                pMain.delGrab?.Invoke(pMain.m_matGrab.Clone(), (int)MilIndex);

                Thread.Sleep(1);
            }
            return 0;
        }

        public bool fn_GetIOValue()
        {
            MIL_INT IOState = 0;
            if (m_MilDigitizer != 0)
            {
                MIL.MdigInquire(m_MilDigitizer, MIL.M_IO_STATUS + MIL.M_AUX_IO6, ref IOState); //aux 6, 7
            }
            else
            {
                G.WriteLog("GetIOValue Error : MilDigitizer is Zero.");
            }
            bool bRet = IOState == 0 ? false : true;
            return bRet;
        }

        public bool fn_GetSpecificIO(long ioAttribute)
        {
            double dValue = 0;
            if (m_MilDigitizer != MIL.M_NULL)
            {
                MIL.MdigInquire(m_MilDigitizer, MIL.M_IO_STATUS + ioAttribute, ref dValue);
            }
            return dValue != 0;
        }

        public void fn_SetTriggerDelay(int delay)
        {
            if (m_MilDigitizer == MIL.M_NULL) return;

            if (delay > 0)
            {
                double dDelay = delay / 1000.0;
                MIL.MdigControl(m_MilDigitizer, MIL.M_GRAB_TRIGGER_DELAY, dDelay);
            }
            else {
                MIL.MdigControl(m_MilDigitizer, MIL.M_GRAB_TRIGGER_DELAY, 0.0);
            }
        }

        public void fn_SetExposureTime(float valueUs)
        {
            if (m_MilDigitizer == MIL.M_NULL) return;
            try { MIL.MdigControl(m_MilDigitizer, MIL.M_EXPOSURE_TIME, (double)valueUs); }
            catch (Exception ex) { delLog?.Invoke($"SetExposureTime Fail: {ex.Message}"); }
        }

        public float fn_GetExposureTime()
        {
            if (m_MilDigitizer == MIL.M_NULL) return 0;
            double val = 0;
            try { MIL.MdigInquire(m_MilDigitizer, MIL.M_EXPOSURE_TIME, ref val); }
            catch { }
            return (float)val;
        }

        public void fn_SetGain(float value)
        {
            if (m_MilDigitizer == MIL.M_NULL) return;
            try { MIL.MdigControl(m_MilDigitizer, MIL.M_GAIN, (double)value); }
            catch (Exception ex) { delLog?.Invoke($"SetGain Fail: {ex.Message}"); }
        }

        public float fn_GetGain()
        {
            if (m_MilDigitizer == MIL.M_NULL) return 0;
            double val = 0;
            try { MIL.MdigInquire(m_MilDigitizer, MIL.M_GAIN, ref val); }
            catch { }
            return (float)val;
        }

        public int fn_GetScanLength()
        {
            if (m_MilDigitizer == MIL.M_NULL) return 0;
            MIL_ID val = 0;
            try { MIL.MdigInquire(m_MilDigitizer, MIL.M_SIZE_Y, ref val); }
            catch { }
            return (int)val;
        }
    }
}
