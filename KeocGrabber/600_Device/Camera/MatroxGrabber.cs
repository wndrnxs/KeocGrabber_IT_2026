/*
 *******************************************************************************
 * 해당 소스는 현장 유지 보수 외에 다른 목적의 사용을 금합니다.
 * - 주식회사 태루 -
 *******************************************************************************
 */
using System;
using System.Collections.Generic;
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
            MIL.MdigProcess(m_MilDigitizer, m_MilBuffer, m_MilBuffer.Length, MIL.M_STOP, MIL.M_DEFAULT, UserHookFunctionDelegate, GCHandle.ToIntPtr(hUserData));
            m_bIsGrabbing = false;
            delLog?.Invoke($"Grab Stop.({BoardIndex})");
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
