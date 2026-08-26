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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Euresys.EGrabber;
using Matrox.MatroxImagingLibrary;
using OpenCvSharp;

namespace KeocGrabber
{
    internal class GrabberManager
    {
        // Matrox 전용
        MIL_APP_HOOK_FUNCTION_PTR UserHookFunctionDelegate = null;
        List<MatroxGrabber> m_listMatrox = new List<MatroxGrabber>();
        MIL_ID m_MilApplication = MIL.M_NULL;
        GCHandle hUserData;

        // Euresys 전용
        EGenTL m_gentl = null;
        EGrabberDiscovery m_discovery = null;
        List<EuresysGrabber> m_listEuresys = new List<EuresysGrabber>();

        bool m_bIsEuresys = false;
        int m_nBoardCount = 0;

        bool m_bGrabSetup = false;
        bool m_bIsInit = false;
        public bool IsInited { get { return m_bIsInit; } }

        public int ImageWidth
        {
            get
            {
                if (m_bIsEuresys) return m_listEuresys.Count > 0 ? m_listEuresys[0].Width : 0;
                return m_listMatrox.Count > 0 ? m_listMatrox[0].Width : 0;
            }
        }
        public int ImageHeight
        {
            get
            {
                if (m_bIsEuresys) return m_listEuresys.Count > 0 ? m_listEuresys[0].Height : 0;
                return m_listMatrox.Count > 0 ? m_listMatrox[0].Height : 0;
            }
        }
        public int ImageChannel
        {
            get
            {
                if (m_bIsEuresys) return m_listEuresys.Count > 0 ? m_listEuresys[0].Channel : 0;
                return m_listMatrox.Count > 0 ? m_listMatrox[0].Channel : 0;
            }
        }

        public bool IsGrabbing
        {
            get
            {
                if (m_bIsEuresys)
                {
                    if (m_listEuresys.Count == 0) return false;
                    bool bRet = true;
                    for (int i = 0; i < m_listEuresys.Count; i++) bRet &= m_listEuresys[i].IsGrabbing;
                    return !bRet;
                }
                else
                {
                    if (m_listMatrox.Count == 0) return false;
                    bool bRet = true;
                    for (int i = 0; i < m_listMatrox.Count; i++) bRet &= m_listMatrox[i].IsGrabbing;
                    return !bRet;
                }
            }
        }

        // 주의: 이 필드는 G의 정적 초기화(=ImageGrabber.xml 로드 전) 시점에 만들어진다.
        //       그때 G.SYSTEM.CamCount는 아직 기본값이므로 CamCount로 잡으면 4캠 구성에서
        //       인덱스를 벗어난다. 지원 최대 대수(Define.CAM_COUNT)로 고정 확보한다.
        int[] m_nBufferCounter = new int[Define.CAM_COUNT];

        // ─── Euresys 초기화 ─────────────────────────────────────────────────────

        public void fn_Init(int boardcount)
        {
            m_bIsEuresys = true;
            m_nBoardCount = boardcount;
            try
            {
                m_listEuresys.Clear();
                m_gentl = new EGenTL();

                m_discovery = new EGrabberDiscovery(m_gentl);
                m_discovery.Discover(false);

                int found = m_discovery.GrabberCount;
                G.WriteLog($"Euresys Discovery: {found} slot(s) found. (요청: {boardcount}개)");

                for (int i = 0; i < found && m_listEuresys.Count < boardcount; i++)
                {
                    EGrabberInfo info = m_discovery.GetGrabber(i);
                    EuresysGrabber grabber = new EuresysGrabber();
                    grabber.OnLog = fn_LogWrite;
                    // 카메라 인덱스 = 성공 시 들어갈 리스트 위치. Cam1~4/CamExposure1~4와 동일한 인덱싱.
                    grabber.fn_Init(info, m_listEuresys.Count);
                    if (grabber.IsInit)
                        m_listEuresys.Add(grabber);
                }

                m_nBoardCount = m_listEuresys.Count;

                fn_AssignEuresysCallbacks();

                m_bIsInit = m_listEuresys.Count > 0;
                G.WriteLog($"Euresys Grabber Init {(m_bIsInit ? "Succ" : "Fail")}. (연결됨: {m_listEuresys.Count}/{boardcount})");
            }
            catch (Exception ex)
            {
                G.WriteLog($"Euresys Grabber Init Fail: {ex.Message}", true);
            }
        }

        private void fn_AssignEuresysCallbacks()
        {
            if (m_listEuresys.Count > 0) m_listEuresys[0].OnGrab = OnGrabedCam1;
            if (m_listEuresys.Count > 1) m_listEuresys[1].OnGrab = OnGrabedCam2;
            if (m_listEuresys.Count > 2) m_listEuresys[2].OnGrab = OnGrabedCam3;
            if (m_listEuresys.Count > 3) m_listEuresys[3].OnGrab = OnGrabedCam4;
        }

        // ─── Matrox 초기화 ──────────────────────────────────────────────────────

        public void fn_Init(int boardcount, IList<string> dcflist)
        {
            m_bIsEuresys = false;
            MILBOARD_TYPE type = G.SYSTEM.BoardType;
            try
            {
                fn_InitMILApplication();
                fn_MILErrorMsgCallback();

                m_listMatrox.Clear();
                m_nBoardCount = boardcount;

                for (int i = 0; i < boardcount; i++)
                {
                    string dcf = (i < dcflist.Count) ? dcflist[i] : null;
                    m_listMatrox.Add(new MatroxGrabber());
                    if (!string.IsNullOrEmpty(dcf))
                    {
                        m_listMatrox[i].fn_Init(type, i, 0, dcf, m_MilSystem);
                        m_listMatrox[i].OnLog = fn_LogWrite;
                    }
                }

                if (m_listMatrox.Count > 0) m_listMatrox[0].OnGrab = OnGrabedCam1;
                if (m_listMatrox.Count > 1) m_listMatrox[1].OnGrab = OnGrabedCam2;
                if (m_listMatrox.Count > 2) m_listMatrox[2].OnGrab = OnGrabedCam3;
                if (m_listMatrox.Count > 3) m_listMatrox[3].OnGrab = OnGrabedCam4;
                m_listMatrox[m_listMatrox.Count - 1].IsCanSystemFree = true;

                m_bIsInit = m_listMatrox.Count > 0 && m_listMatrox.All(g => g.IsInit);
                G.WriteLog($"Matrox Grabber Init {(m_bIsInit ? "Succ" : "Fail")}.");
            }
            catch (Exception ex)
            {
                G.WriteLog($"Matrox Grabber Init Fail: {ex.Message}", true);
            }
        }

        // MilSystem은 Matrox 전용 — MatroxGrabber 내부에서 직접 할당하므로 여기선 M_NULL 유지
        MIL_ID m_MilSystem = MIL.M_NULL;

        private bool fn_LogWrite(string strMsg) { G.WriteLog(strMsg); return false; }

        // ─── Final ──────────────────────────────────────────────────────────────

        public void fn_Final()
        {
            try
            {
                if (m_bIsEuresys)
                {
                    foreach (var g in m_listEuresys) g.fn_Final();
                    m_listEuresys.Clear();
                    try { m_discovery?.Dispose(); } catch { }
                    m_discovery = null;
                    try { m_gentl?.Dispose(); } catch { }
                    m_gentl = null;
                }
                else
                {
                    foreach (var g in m_listMatrox) g.fn_Final();
                    m_listMatrox.Clear();
                    fn_FinalMILApplication();
                    UserHookFunctionDelegate = null;
                }
                G.WriteLog($"Grabber Final Succ.");
            }
            catch (Exception ex)
            {
                G.WriteLog($"Grabber Final Fail: {ex.Message}", true);
            }
            m_bIsInit = false;
        }

        // ─── MIL Application (Matrox 전용) ──────────────────────────────────────

        private void fn_InitMILApplication()
        {
            hUserData = GCHandle.Alloc(this);
            MIL.MappAlloc(MIL.M_DEFAULT, ref m_MilApplication);
        }

        private void fn_FinalMILApplication()
        {
            MIL.MappFree(m_MilApplication);
            m_MilApplication = MIL.M_NULL;
            if (hUserData.IsAllocated) hUserData.Free();
        }

        private void fn_MILErrorMsgCallback()
        {
            UserHookFunctionDelegate = new MIL_APP_HOOK_FUNCTION_PTR(ErrorMessageCallBack);
            MIL.MappControl(m_MilApplication, MIL.M_ERROR, MIL.M_PRINT_DISABLE);
            MIL.MappHookFunction(m_MilApplication, MIL.M_ERROR_CURRENT, UserHookFunctionDelegate, GCHandle.ToIntPtr(hUserData));
        }

        static private MIL_INT ErrorMessageCallBack(MIL_INT HookType, MIL_ID HookId, IntPtr HookDataPtr)
        {
            GrabberManager pMain = GCHandle.FromIntPtr(HookDataPtr).Target as GrabberManager;
            StringBuilder MilError = new StringBuilder();
            StringBuilder MilErrorFCT = new StringBuilder();
            StringBuilder MilError_sub1 = new StringBuilder();
            StringBuilder MilError_sub2 = new StringBuilder();
            StringBuilder MilError_sub3 = new StringBuilder();
            MIL_INT nsuberrmsg = 0;

            MIL.MappGetHookInfo(MIL.M_DEFAULT, HookId, MIL.M_MESSAGE + MIL.M_CURRENT_FCT, MilErrorFCT);
            MIL.MappGetHookInfo(MIL.M_DEFAULT, HookId, MIL.M_MESSAGE + MIL.M_CURRENT, MilError);
            MIL.MappGetHookInfo(MIL.M_DEFAULT, HookId, MIL.M_CURRENT_SUB_NB, ref nsuberrmsg);
            MIL.MappGetHookInfo(MIL.M_DEFAULT, HookId, MIL.M_CURRENT_SUB_1 + MIL.M_MESSAGE, MilError_sub1);
            MIL.MappGetHookInfo(MIL.M_DEFAULT, HookId, MIL.M_CURRENT_SUB_2 + MIL.M_MESSAGE, MilError_sub2);
            MIL.MappGetHookInfo(MIL.M_DEFAULT, HookId, MIL.M_CURRENT_SUB_3 + MIL.M_MESSAGE, MilError_sub3);

            switch (nsuberrmsg)
            {
                case 0: G.WriteLog($"[MIL_ERROR] {MilErrorFCT} {MilError}", true); break;
                case 1: G.WriteLog($"[MIL_ERROR] {MilErrorFCT} {MilError} {MilError_sub1}", true); break;
                case 2: G.WriteLog($"[MIL_ERROR] {MilErrorFCT} {MilError} {MilError_sub1} {MilError_sub2}", true); break;
                case 3: G.WriteLog($"[MIL_ERROR] {MilErrorFCT} {MilError} {MilError_sub1} {MilError_sub2} {MilError_sub3}", true); break;
            }
            return 0;
        }

        // ─── Grab Callbacks ──────────────────────────────────────────────────────

        bool OnGrabedCam1(Mat matImg, int milindex) { fn_OnGrab(0, matImg, milindex); return false; }
        bool OnGrabedCam2(Mat matImg, int milindex) { fn_OnGrab(1, matImg, milindex); return false; }
        bool OnGrabedCam3(Mat matImg, int milindex) { fn_OnGrab(2, matImg, milindex); return false; }
        bool OnGrabedCam4(Mat matImg, int milindex)
        {
            if (!G.bChk_Light_On) { G.bChk_Light_On = true; G.LIGHT.fn_Vit_ON(); }
            fn_OnGrab(3, matImg, milindex);
            return false;
        }

        void fn_OnGrab(int idx, Mat matImg, int milindex)
        {
            if (m_bGrabSetup)
            {
                G.MAIN.Dispatcher.BeginInvoke(DispatcherPriority.Render, new System.Action(delegate ()
                {
                    if (G.MAIN.SetupCamIndex == idx)
                        G.MAIN.SetupView.SetImage(OpenCvSharp.WpfExtensions.WriteableBitmapConverter.ToWriteableBitmap(matImg));
                }));
            }
            else
            {
                if (idx >= 0 && idx < m_nBufferCounter.Length) m_nBufferCounter[idx]++;
                if (G.IMAGEMANAGER?.IsImageCompalte[idx] == false)
                    G.IMAGEMANAGER?.AttachImage(idx, matImg, milindex);
            }
        }

        // ─── Grab 제어 ───────────────────────────────────────────────────────────

        public bool fn_IsGrabbing(int idx)
        {
            if (m_bIsEuresys)
                return idx >= 0 && idx < m_listEuresys.Count && m_listEuresys[idx].IsGrabbing;
            return idx >= 0 && idx < m_listMatrox.Count && m_listMatrox[idx].IsGrabbing;
        }

        // Euresys: 그래버 초기화 성공 여부, Matrox: VieworksCamera 시리얼 연결 여부
        public bool fn_IsCameraConnected(int idx)
        {
            if (m_bIsEuresys)
                return idx >= 0 && idx < m_listEuresys.Count && m_listEuresys[idx].IsInit;
            return idx >= 0 && idx < G.CAMERA.Length && G.CAMERA[idx] != null && G.CAMERA[idx].IsConnected;
        }

        // 실제로 초기화된 그래버 수. m_nBoardCount가 리스트보다 크면 인덱스를 벗어나므로
        // 항상 이 값으로 순회한다. (요청 대수 > 발견 대수인 구성 대비)
        private int GrabberCount { get { return m_bIsEuresys ? m_listEuresys.Count : m_listMatrox.Count; } }

        public void fn_GrabStart()
        {
            m_bGrabSetup = false;
            for (int i = 0; i < GrabberCount; i++)
            {
                if (i < m_nBufferCounter.Length) m_nBufferCounter[i] = 0;
                if (m_bIsEuresys) m_listEuresys[i].fn_GrabStart();
                else m_listMatrox[i].fn_GrabStart();
            }
        }

        public void fn_GrabStop()
        {
            for (int i = 0; i < GrabberCount; i++)
            {
                if (m_bIsEuresys) m_listEuresys[i].fn_GrabStop();
                else m_listMatrox[i].fn_GrabStop();
            }
        }

        public void fn_GrabStart(int idx, bool bSetup = false)
        {
            int maxCount = m_bIsEuresys ? m_listEuresys.Count : m_listMatrox.Count;
            if (idx >= 0 && idx < maxCount)
            {
                m_bGrabSetup = bSetup;
                m_nBufferCounter[idx] = 0;
                if (m_bIsEuresys) m_listEuresys[idx].fn_GrabStart(bSetup);
                else m_listMatrox[idx].fn_GrabStart(bSetup);
            }
        }

        public void fn_GrabStop(int idx)
        {
            int maxCount = m_bIsEuresys ? m_listEuresys.Count : m_listMatrox.Count;
            if (idx >= 0 && idx < maxCount)
            {
                if (m_bIsEuresys) m_listEuresys[idx].fn_GrabStop();
                else m_listMatrox[idx].fn_GrabStop();
                m_bGrabSetup = false;
            }
        }

        // ─── 카메라 파라미터 접근 (외부에서 호출) ───────────────────────────────

        public void fn_SetExposureTime(int idx, float valueUs)
        {
            if (m_bIsEuresys)
            {
                if (idx >= 0 && idx < m_listEuresys.Count) m_listEuresys[idx].fn_SetExposureTime(valueUs);
            }
            else
            {
                if (idx >= 0 && idx < m_listMatrox.Count) m_listMatrox[idx].fn_SetExposureTime(valueUs);
            }
        }

        public float fn_GetExposureTime(int idx)
        {
            if (m_bIsEuresys)
                return idx >= 0 && idx < m_listEuresys.Count ? m_listEuresys[idx].fn_GetExposureTime() : 0;
            return idx >= 0 && idx < m_listMatrox.Count ? m_listMatrox[idx].fn_GetExposureTime() : 0;
        }

        public void fn_SetGain(int idx, float value)
        {
            if (m_bIsEuresys)
            {
                if (idx >= 0 && idx < m_listEuresys.Count) m_listEuresys[idx].fn_SetGain(value);
            }
            else
            {
                if (idx >= 0 && idx < m_listMatrox.Count) m_listMatrox[idx].fn_SetGain(value);
            }
        }

        public float fn_GetGain(int idx)
        {
            if (m_bIsEuresys)
                return idx >= 0 && idx < m_listEuresys.Count ? m_listEuresys[idx].fn_GetGain() : 0;
            return idx >= 0 && idx < m_listMatrox.Count ? m_listMatrox[idx].fn_GetGain() : 0;
        }

        public int fn_GetScanLength(int idx = 0)
        {
            if (m_bIsEuresys)
                return idx >= 0 && idx < m_listEuresys.Count ? m_listEuresys[idx].fn_GetScanLength() : 0;
            return idx >= 0 && idx < m_listMatrox.Count ? m_listMatrox[idx].fn_GetScanLength() : 0;
        }

        // ─── Matrox IO (Matrox 전용, Euresys는 미지원) ──────────────────────────

        public bool fn_GetSpecificIO(int boardIdx, long ioAttribute)
        {
            if (!m_bIsEuresys && boardIdx >= 0 && boardIdx < m_listMatrox.Count)
                return m_listMatrox[boardIdx].fn_GetSpecificIO(ioAttribute);
            return false;
        }

        // ─── 센서 입력 I/O ───────────────────────────────────────────────────────
        // Euresys : Interface 모듈의 IIN11(15pin D-Sub #3=+, #12=-) 레벨을 직접 읽는다.
        //           이 라인이 곧 스캔 시작 트리거(LIN1) 소스이므로, 램프가 켜졌는데
        //           스캔이 안 되면 보드 이후(트리거 설정/카메라) 문제로 좁힐 수 있다.
        // Matrox  : 현재 미지원. 검증 장비 확보 후 아래 TODO 위치에 AUX IO를 연결한다.

        // 센서 I/O 모니터링 지원 여부 (현재 Euresys 전용)
        public bool IsSensorIoSupported { get { return m_bIsEuresys; } }

        /// <summary>
        /// 센서 입력 라인의 현재 레벨을 읽는다.
        /// </summary>
        /// <param name="idx">카메라(그래버) 인덱스</param>
        /// <param name="bLevel">읽은 레벨. 실패 시 false 또는 마지막 성공값.</param>
        /// <returns>읽기 성공 여부</returns>
        public bool fn_TryGetSensorInput(int idx, out bool bLevel)
        {
            bLevel = false;

            if (m_bIsEuresys)
            {
                if (idx >= 0 && idx < m_listEuresys.Count)
                    return m_listEuresys[idx].fn_TryGetSensorInput(out bLevel);
                return false;
            }

            // TODO(Matrox) : 테스트 환경 확보 후 아래처럼 AUX IO를 센서 입력으로 매핑한다.
            //                실제 사용 핀은 배선 확인 후 결정(M_AUX_IO0 ~ M_AUX_IO7).
            //   bLevel = fn_GetSpecificIO(idx, MIL.M_AUX_IO6);
            //   return true;
            return false;
        }

        /// <summary>센서 라인 이름 (UI 표기용)</summary>
        public string fn_GetSensorLine(int idx)
        {
            if (m_bIsEuresys && idx >= 0 && idx < m_listEuresys.Count)
                return m_listEuresys[idx].SensorLine;
            return G.SYSTEM.SensorInputLine;
        }

        /// <summary>
        /// 센서 라인이 물려 있는 물리 보드(Interface) 인덱스.
        /// 한 보드에 여러 카메라(Device)가 붙으면 I/O 커넥터는 하나이므로,
        /// 이 값이 같은 카메라들은 동일한 센서 신호를 공유한다.
        /// </summary>
        public int fn_GetSensorInterfaceIndex(int idx)
        {
            if (m_bIsEuresys)
                return idx >= 0 && idx < m_listEuresys.Count ? m_listEuresys[idx].BoardIndex : -1;
            return idx >= 0 && idx < m_listMatrox.Count ? idx : -1;
        }
    }
}
