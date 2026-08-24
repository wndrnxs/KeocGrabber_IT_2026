/*
 *******************************************************************************
 * 해당 소스는 현장 유지 보수 외에 다른 목적의 사용을 금합니다.
 * - 주식회사 태루 -
 *******************************************************************************
 */
using FalconWpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace KeocGrabber
{
    public enum EN_GRABSTATE
    {
        None = 0,
        Grab,
        End
    }

    public enum EN_READYSTATE
    {
        None = 0,
        Ready,
        Alarm
    }

    static class G
    {
        static public MainWindow MAIN = null;
        //static public 
        static public SystemParam SYSTEM = new SystemParam();

        static public GrabberManager GRABBER = new GrabberManager();
        static public SensorIOManager SENSORIO = new SensorIOManager();
        static public LightManager LIGHT = new LightManager();
        static public VieworksCamera[] CAMERA = new VieworksCamera[G.SYSTEM.CamCount];

        static public ProtocallManager COMM = new ProtocallManager();
        static public ProtocallManager COMM2 = new ProtocallManager();

        static public ImageManager IMAGEMANAGER = new ImageManager();
        static public GiGABoard GIGABOARD = new GiGABoard();
        static public MessageManager MSGPROC = new MessageManager();

        static public Logger LOGGER = new Logger();
        static public DiskManager DISKMANAGER = new DiskManager();

        static public RecipeParam CURRRECIPE = new RecipeParam();

        static public bool bChk_Light_On = false;
        // Member
        static public EN_AUTHORITY USERLEVEL = EN_AUTHORITY.EN_OPERATOR;
        static public EN_GRABSTATE GRABSTATE = EN_GRABSTATE.None;
        static public EN_READYSTATE READYSTATE = EN_READYSTATE.None;
        // ~Member

        static public bool IsManualRecipe = false;
        static public bool IsNowRecipeLoading = false;

        private static readonly object _obj = new object();

        static public System.Timers.Timer m_SafetyTimer = new System.Timers.Timer();

        #region Program Version
        static public DateTime fn_GetBuildDate()
        {
            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

            int day = version.Build;
            System.DateTime dtBuild = (new System.DateTime(2000, 1, 1)).AddDays(day);

            int intSeconds = version.Revision;
            intSeconds = intSeconds * 2;
            dtBuild = dtBuild.AddSeconds(intSeconds);

            System.Globalization.DaylightTime daylingTime = System.TimeZone.CurrentTimeZone.GetDaylightChanges(dtBuild.Year);
            if (System.TimeZone.IsDaylightSavingTime(dtBuild, daylingTime))
                dtBuild = dtBuild.Add(daylingTime.Delta);

            return dtBuild;
        }

        static public string fn_GetVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        static public string fn_GetVer()
        {
            return $"{G.fn_GetBuildDate():Ver.yyyyMMdd_HH}H";
        }
        #endregion

        #region Log
        /// <summary>
        /// Log 작성.
        /// </summary>
        /// <param name="strMsg">로그 메시지</param>
        static public void WriteLog(string strMsg, bool bError = false)
        {
            lock (_obj)
            {
                var st = new StackTrace();
                var sf = st.GetFrame(1);
                var sm = sf.GetMethod();
                strMsg = strMsg.Insert(0, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff},{G.MSGPROC.CellID},");

                if (G.SYSTEM.CallStack)
                {
                    strMsg = strMsg.Insert(strMsg.Length - 1, $",{sm.DeclaringType.Name},{sm.Name}");
                }
                MAIN?.fn_WriteLog(strMsg, bError);
                LOGGER?.fn_PushMsg(strMsg, sm.DeclaringType.Name, sm.Name, bError);
            }
        }
        #endregion

        #region ReadPathFromINI

        [DllImport("kernel32.dll")]
        private static extern uint GetPrivateProfileString(string section, string key, string defaultval,
            StringBuilder returnString, uint size, string filePath);
        public static string GetSavePath()
        {
            string filepath = "D:\\MAVT\\INI\\MAVT.ini";
            // 경로가 없다면.
            if (File.Exists(filepath) == false)
            {
                G.WriteLog($"Can't find INI Path. {filepath}", true);
                return "";
            }

            StringBuilder strrtn = new StringBuilder(4096);
            uint nRtn = GetPrivateProfileString("General", "Image Save Path", "", strrtn, 4096, filepath);
            return strrtn.ToString().Split(';')[0];
        }

        #endregion


        /// <summary>
        /// Device Init
        /// </summary>
        static public void Init()
        {
            // Load INI File.
            //DISKMANAGER.DstImgPath = G.SYSTEM.ImagePath;
            //DISKMANAGER.DstImgPath = GetSavePath(); //-> 저장하기전에 항상 받기.
            DISKMANAGER.DstLogPath = G.SYSTEM.LogPath;
            DISKMANAGER.DstImgExt = G.SYSTEM.ImageExt;
            DISKMANAGER.DstLogExt = G.SYSTEM.LogExt;
            DISKMANAGER.MaintenaceDay = G.SYSTEM.MaintenanceDays;

            CAMERA = new VieworksCamera[G.SYSTEM.CamCount];

            /** 디스크 관리 비활성화. Taeroo-kgseon 2025.04.01 11:05:40 */
            //DISKMANAGER.fn_AddDriveLetter(new string[] { "D", "E" });
            DISKMANAGER.fn_StartThread();
            LOGGER.fn_Init(G.SYSTEM.LogPath);
            G.WriteLog($"Init Start.");

            MSGPROC.fn_Init();

            DISKMANAGER.ImageSavePath = G.SYSTEM.ImagePath;

            //MessageAnalyzer("ANGLEVIEW.ReceiveReady:A123123412341234:0:2:2:3/");

            //G.SYSTEM.Camera1DCF = "PatternGenerator_1024_100fps_Mono.sdcf";
            bool bEuresys = G.SYSTEM.UseEuresys;
            if (bEuresys)
            {
                GRABBER.fn_Init(G.SYSTEM.CamCount);
            }
            else
            {
                var emptyDcf = new List<string>(new string[G.SYSTEM.CamCount]);
                GRABBER.fn_Init(G.SYSTEM.CamCount, emptyDcf);
            }

            // 센서 입력(IIN11 = 15pin D-Sub #3/#12) 모니터링 시작.
            SENSORIO.fn_Init(G.SYSTEM.CamCount);

            IMAGEMANAGER.fn_Init(G.SYSTEM.CamCount, GRABBER.ImageWidth, G.SYSTEM.GrabHeight, 1);
            //IMAGEMANAGER.fn_Init(G.SYSTEM.CamCount, 16384, G.SYSTEM.GrabHeight, 1);

            // GiGABoard 초기화.
            GIGABOARD.MyNodeID = G.SYSTEM.MyNodeID;
            GIGABOARD.LinkCh = G.SYSTEM.LinkCh;
            GIGABOARD.MailBoxNo = G.SYSTEM.MailboxNo;
            GIGABOARD.fn_Init(G.SYSTEM.CamCount, GRABBER.ImageWidth, G.SYSTEM.GrabHeight, GRABBER.ImageChannel);
            //GIGABOARD.fn_Init(G.SYSTEM.CamCount, 16384, G.SYSTEM.GrabHeight, 1);
            // ~GiGABoard 초기화.

            LIGHT.fn_Init();

            for (int i = 0; i < G.SYSTEM.CamCount; i++)
            {
                CAMERA[i] = new VieworksCamera();
            }

            // Euresys: GenICam Remote으로 파라미터 제어 — 시리얼 카메라 통신 불필요
            if (!bEuresys)
            {
                // 카메라 대수만큼 시리얼 포트 연결 (0:Front 1:Rear 2:InSide 3:OutSide)
                for (int i = 0; i < G.SYSTEM.CamCount; i++)
                {
                    CAMERA[i].fn_Init(G.SYSTEM.fn_GetCamPort(i));
                }
            }

            COMM.fn_Init(G.SYSTEM.Master_IP1, G.SYSTEM.Master_Port1, ProtocallManager.ServerType.MASTER1);
            if (G.SYSTEM.JavasCount == 2)
            {
                COMM2.fn_Init(G.SYSTEM.Master_IP2, G.SYSTEM.Master_Port2, ProtocallManager.ServerType.MASTER2);
            }

            m_SafetyTimer.Interval += G.SYSTEM.GrabTimeout;
            m_SafetyTimer.Elapsed += OnSafetyTimeOut;
            m_SafetyTimer.AutoReset = false;

            G.WriteLog($"Init End.");
        }

        static public void Final()
        {
            G.WriteLog($"Final Start.");
            DISKMANAGER.fn_StopThread();

            MSGPROC.fn_Final();

            LIGHT.fn_Final();

            COMM.fn_Final();
            COMM2.fn_Final();
            
            // 센서 I/O 폴링은 그래버 해제 전에 멈춘다(해제된 EGrabber 접근 방지).
            SENSORIO.fn_Final();

            GIGABOARD.fn_Final();
            IMAGEMANAGER.fn_Final();
            //#if !DEBUG
            GRABBER.fn_Final();
            //#endif

            if (!G.SYSTEM.UseEuresys)
            {
                for (int i = 0; i < G.SYSTEM.CamCount; i++)
                {
                    CAMERA[i].fn_Final();
                }
            }

            G.WriteLog($"Final End.");
            G.WriteLog($"Program End.");
            LOGGER.fn_Final();
        }

        static public void SetGrabHeight(int height)
        {
            G.SYSTEM.GrabHeight = height;
            GIGABOARD.fn_CreateBuffer(G.SYSTEM.CamCount, G.GRABBER.ImageWidth, G.SYSTEM.GrabHeight, G.GRABBER.ImageChannel);
            IMAGEMANAGER.fn_CreateBuffer(G.SYSTEM.CamCount, G.GRABBER.ImageWidth, G.SYSTEM.GrabHeight, G.GRABBER.ImageChannel);
        }

        static public void GrabStart()
        {
            if (GRABSTATE != EN_GRABSTATE.Grab)
            {
                SetCurrRecipe();

                for (int i = 0; i < G.SYSTEM.CamCount; i++) {
                    IMAGEMANAGER.InitAttachCount(i);
                }

                LIGHT.fn_LightOn_DAWOO();

                // 하부(VIT) 조명 중 상시 점등 채널. 4캠 기준 인덱스 6,7과 동일한 상대 위치.
                if (LIGHT.UseBottomLight)
                {
                    LIGHT.fn_LightOn(G.SYSTEM.CamCount + 2);
                    LIGHT.fn_LightOn(G.SYSTEM.CamCount + 3);
                }

                GRABBER.fn_GrabStart();
                GRABSTATE = EN_GRABSTATE.Grab;
            }
        }

        static public void GrabStart(ProtocallManager.ServerType type)
        {
            if (GRABSTATE != EN_GRABSTATE.Grab)
            {
                if (type == ProtocallManager.ServerType.MASTER1)
                {
                    SetCurrRecipe();

                    for (int i = 0; i < G.SYSTEM.CamCount; i++)
                    {
                        IMAGEMANAGER.InitAttachCount(i);
                    }

                    LIGHT.fn_LightOn_DAWOO();

                    // 하부(VIT) 조명 중 상시 점등 채널. 4캠 기준 인덱스 6,7과 동일한 상대 위치.
                    if (LIGHT.UseBottomLight)
                    {
                        LIGHT.fn_LightOn(G.SYSTEM.CamCount + 2);
                        LIGHT.fn_LightOn(G.SYSTEM.CamCount + 3);
                    }
                    GRABBER.fn_GrabStart();

                    if (G.SYSTEM.GrabTimeout > 0)
                    {
                        m_SafetyTimer.Interval = G.SYSTEM.GrabTimeout;
                        m_SafetyTimer.Stop();
                        m_SafetyTimer.Start();
                    }

                    GRABSTATE = EN_GRABSTATE.Grab;
                }

            }
        }

        static public void SetCurrRecipe()
        {
            int nCamCount = G.SYSTEM.CamCount;

            if (G.SYSTEM.UseEuresys)
            {
                // Euresys: GenICam Remote 레이어로 파라미터 직접 제어
                for (int i = 0; i < nCamCount; i++) GRABBER.fn_SetGain(i, CURRRECIPE.fn_GetCamGain(i));
                for (int i = 0; i < nCamCount; i++) GRABBER.fn_SetExposureTime(i, CURRRECIPE.fn_GetCamExposure(i));
            }
            else
            {
                // Matrox: VieworksCamera 시리얼 통신으로 파라미터 제어
                for (int i = 0; i < nCamCount && i < CAMERA.Length; i++)
                {
                    if (CAMERA[i] == null) continue;
                    CAMERA[i].fn_SetDigitalGain(CURRRECIPE.fn_GetCamGain(i));
                    CAMERA[i].fn_SetExposureTime(CURRRECIPE.fn_GetCamExposure(i));
                }
            }

            // 상부 조명 = 카메라 대수만큼, 그 뒤 인덱스가 하부(VIT) 채널.
            // 4캠 기준 : 0~3 상부(LightTop1~4), 4~7 하부(LightBot1~4) — 기존 매핑과 동일.
            for (int i = 0; i < nCamCount; i++)
            {
                LIGHT.fn_SetLightValue(i, CURRRECIPE.fn_GetLightTop(i));
            }
            if (LIGHT.UseBottomLight)
            {
                for (int i = 0; i < LightManager.BOTTOM_LIGHT_CH_COUNT; i++)
                {
                    LIGHT.fn_SetLightValue(nCamCount + i, CURRRECIPE.fn_GetLightBot(i));
                }
            }
        }

        static public void GrabStop()
        {
            m_SafetyTimer.Stop();

            if (GRABSTATE == EN_GRABSTATE.Grab)
            {
                LIGHT.fn_LightOffAll();
                GRABBER.fn_GrabStop();
                GRABSTATE = EN_GRABSTATE.End;
            }
        }

        static public bool SyncRecipe(string RecvName)
        {
            bool bRet = false;

            lock (_obj)
            {
                if (!G.IsNowRecipeLoading)
                {
                    G.IsNowRecipeLoading = true;
                    if (!G.IsManualRecipe)
                    {
                        // Load Recipe.
                        // if Recipe 파일이 있다면
                        //   if Recipe Edit Time이 다르면?
                        //     Load Recipe
                        //   else
                        //     Pass.
                        // Recipe 파일이 없다면
                        //  Light값 0 셋팅
                        string strRecipeFile = $"C:/KEOC/Recipe/{RecvName}.xml";
                        FileInfo fileInfo = new FileInfo(strRecipeFile);
                        if (fileInfo.Exists)
                        {
                            try
                            {
                                if (G.SYSTEM.CurrRecipeEditTime != fileInfo.LastWriteTime)
                                {
                                    // load Recipe
                                    XmlManager.LoadXml(strRecipeFile, G.CURRRECIPE);
                                    //G.MAIN.datacontext.CurrRecipe = G.CURRRECIPE.RecipeName;
                                    G.MAIN.datacontext.CurrRecipe = fileInfo.Name;
                                    G.SYSTEM.CurrRecipePath = strRecipeFile;
                                    G.SYSTEM.CurrRecipeEditTime = fileInfo.LastWriteTime;

                                    G.WriteLog($"{G.MAIN.datacontext.CurrRecipe} Recipe Set Succ.[{G.SYSTEM.CurrRecipePath}]");
                                }
                                else
                                {
                                    // pass
                                    //G.WriteLog($"{G.MAIN.datacontext.CurrRecipe} Recipe Set Succ.[{G.SYSTEM.CurrRecipePath}]");
                                }
                                bRet = true;
                            }
                            catch (Exception e)
                            {
                                G.WriteLog($"{G.MAIN.datacontext.CurrRecipe} Recipe Set Succ.[{G.SYSTEM.CurrRecipePath}]");
                            }

                        }
                        else
                        {
                            // Light zero (구성에 없는 채널은 어차피 적용되지 않으므로 전부 0으로)
                            G.CURRRECIPE.LightTop1 = 0;
                            G.CURRRECIPE.LightTop2 = 0;
                            G.CURRRECIPE.LightTop3 = 0;
                            G.CURRRECIPE.LightTop4 = 0;
                            G.CURRRECIPE.LightBot1 = 0;
                            G.CURRRECIPE.LightBot2 = 0;
                            G.CURRRECIPE.LightBot3 = 0;
                            G.CURRRECIPE.LightBot4 = 0;
                            string strMsg = $"Unkown File Name {strRecipeFile}";
                            //G.MAIN.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
                            //{
                            //    MessageBox.Show(strMsg);
                            //}));
                            G.WriteLog(strMsg, false);
                        }
                    }
                    else
                    {
                        G.WriteLog("Recipe Manual Mode!!!", false);
                    }
                    G.IsNowRecipeLoading = false;
                }
                else
                {
                    G.WriteLog("SYNC Duplication Recieve!", true);
                }
            }
            return bRet;
        }

        static private void OnSafetyTimeOut(object sender, System.Timers.ElapsedEventArgs e)
        {
            G.WriteLog("[Warning] Safety Time out!", true);
            GrabStop();
        }
    }

    static public class Define
    {
        public const int CAM_COUNT = 4;
        public const int LIGHT_COUNT = 5;
        public const int LIGHT_CTRL_CH_COUNT = LIGHT_COUNT + 6;

        public const int MESSAGEPOP_TIMEOUT = 3000;

        public const string PASSWORD_OP = "";
        public const string PASSWORD_MA = "";
        public const string PASSWORD_EN = "keoc";

        
    }
}
