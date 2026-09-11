/*
 *******************************************************************************
 * 해당 소스는 현장 유지 보수 외에 다른 목적의 사용을 금합니다.
 * - 주식회사 태루 -
 *******************************************************************************
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeocGrabber
{
    class SystemParam
    {

        // Comm
        string master_IP1 = "127.0.0.1";
        int master_Port1 = 20000;
        string master_IP2 = "127.0.0.1";
        int master_Port2 = 20000;
        int javas = 1;

        // Log
        bool callstack = false;

        string imagePath = "/IMAGE/";
        string logPath = "./LOG/";
        string imageExt = "png";
        string logExt = "csv";
        int maintenanceDays = 30;

        string savedrive = "d";

        // Devices
        string lightPortTop1 = "COM1";
        string lightPortTop2 = "COM2";
        string lightPortTop3 = "COM3";
        string lightPortTop4 = "COM4";
        string lightPortBot1 = "COM5";
        string lightPortBot2 = "COM6";
        string lightPortBot3 = "COM7";
        string lightPortBot4 = "COM8";

        string camport_Front = "COM9";
        string camport_Rear = "COM10";
        string camport_Inside = "COM11";
        string camport_Outside = "COM12";

        bool useEuresys = true;
        MILBOARD_TYPE boardType = MILBOARD_TYPE.EN_BT_RADIENTEVCL;

        // Sensor I/O (Euresys 15pin D-Sub : #3 = IIN11+, #12 = IIN11-)
        // 폴링 주기/램프 유지 시간은 현장별 조정이 필요 없어 SensorIOManager 안에
        // 상수(POLL_INTERVAL_MS/LAMP_HOLD_MS)로 고정했다. 여기 두는 값은 실제 배선/운용에
        // 따라 달라지는 것만 남긴다.
        bool useSensorIO = true;
        string sensorInputLine = "IIN11";
        bool sensorLogEnable = true;
        string sensorDelayTool = "DEL1";

        // 카메라(보드)마다 센서-시야 간 거리가 다를 수 있어 지연은 카메라별로 둔다.
        // (CamExposure1~4, LightTop1~4와 동일한 관례)
        int sensorTriggerDelay1 = 0;
        int sensorTriggerDelay2 = 0;
        int sensorTriggerDelay3 = 0;
        int sensorTriggerDelay4 = 0;

        // 카메라별 라인주기(us). Matrox DCF의 LinePeriod와 같은 단위라 현장에서 값을 그대로 옮길 수 있다.
        // 렌즈/센서 조합이 카메라마다 다를 수 있어(예: 늘어짐/눌림 보정) 공용 상수 대신 카메라별 값으로 둔다.
        // Euresys가 쓰는 Hz(AcquisitionLineRate)는 fn_GetCamLineRate()가 환산한다.
        const double DEFAULT_LINE_PERIOD_US = 90.5;
        double camLinePeriod1 = DEFAULT_LINE_PERIOD_US;
        double camLinePeriod2 = DEFAULT_LINE_PERIOD_US;
        double camLinePeriod3 = DEFAULT_LINE_PERIOD_US;
        double camLinePeriod4 = DEFAULT_LINE_PERIOD_US;

        int grabTimeout = 45000;
        int grabHeight = 32768;

        bool localSave = false;
        
        int serialDelay = 100;
        // Device Count
        int camCount = 2;
        //int lightCount = 2;

        // GiGA Board
        int mynodeID = 1;
        int linkch = 0;
        int mailboxNo = 0;

        // Manual Recipe Set
        double dSwitchRecipeTimeOut = 60;

        string strCurrRecipeName = "-";
        public string CurrRecipeName { get { return strCurrRecipeName; } set { strCurrRecipeName = value; } }

        DateTime dtCurrRecipeEditTime = DateTime.MinValue;
        public DateTime CurrRecipeEditTime { get { return dtCurrRecipeEditTime; } set { dtCurrRecipeEditTime = value; } }

        string strCurrRecipePath = "-";
        public string CurrRecipePath { get { return strCurrRecipePath; } set { strCurrRecipePath = value; } }

        public string Master_IP1 { get { return master_IP1; } set { master_IP1 = value; } }
        public int Master_Port1 { get { return master_Port1; } set { master_Port1 = value; } }
        public string Master_IP2 { get { return master_IP2; } set { master_IP2 = value; } }
        public int Master_Port2 { get { return master_Port2; } set { master_Port2 = value; } }

        public int JavasCount { get { return javas; } set { javas = value; } }

        public bool CallStack { get { return callstack; } set { callstack = value; } }

        public string ImagePath { get { return imagePath; } set { imagePath = value; } }
        public string LogPath { get { return logPath; } set { logPath = value; } }
        public string ImageExt { get { return imageExt; } set { imageExt = value; } }
        public string LogExt { get { return logExt; } set { logExt = value; } }
        public int MaintenanceDays { get { return maintenanceDays; } set { maintenanceDays = value; } }

        // 카메라 대수. 1 ~ Define.CAM_COUNT(4) 범위로 제한한다.
        // (레시피/조명/포트 설정이 4채널까지만 존재하므로 범위를 벗어난 XML 값은 잘라낸다)
        public int CamCount
        {
            get { return camCount; }
            set { camCount = Math.Min(Math.Max(1, value), Define.CAM_COUNT); }
        }

        /// <summary>카메라 인덱스별 시리얼 포트. (0:Front 1:Rear 2:InSide 3:OutSide, Matrox 경로 전용)</summary>
        public string fn_GetCamPort(int idx)
        {
            switch (idx)
            {
                case 0: return camport_Front;
                case 1: return camport_Rear;
                case 2: return camport_Inside;
                case 3: return camport_Outside;
            }
            return "";
        }

        /// <summary>상부(DAWOO) 조명 컨트롤러 인덱스별 시리얼 포트.</summary>
        public string fn_GetLightPortTop(int idx)
        {
            switch (idx)
            {
                case 0: return lightPortTop1;
                case 1: return lightPortTop2;
                case 2: return lightPortTop3;
                case 3: return lightPortTop4;
            }
            return "";
        }
        //public int LightCount{ get { return lightCount; } set { lightCount = value; } }

        public int GrabTimeout { get { return grabTimeout; } set { grabTimeout = value; } }

        public string LightPortTop1 { get { return lightPortTop1; } set { lightPortTop1 = value; } }
        public string LightPortTop2 { get { return lightPortTop2; } set { lightPortTop2 = value; } }
        public string LightPortTop3 { get { return lightPortTop3; } set { lightPortTop3 = value; } }
        public string LightPortTop4 { get { return lightPortTop4; } set { lightPortTop4 = value; } }
        public string LightPortBot1 { get { return lightPortBot1; } set { lightPortBot1 = value; } }
        public string LightPortBot2 { get { return lightPortBot2; } set { lightPortBot2 = value; } }
        public string LightPortBot3 { get { return lightPortBot3; } set { lightPortBot3 = value; } }
        public string LightPortBot4 { get { return lightPortBot4; } set { lightPortBot4 = value; } }

        public string CamPort_Front { get { return camport_Front; } set { camport_Front = value; } }
        public string CamPort_Rear { get { return camport_Rear; } set { camport_Rear = value; } }
        public string CamPort_Inside { get { return camport_Inside; } set { camport_Inside = value; } }
        public string CamPort_Outside { get { return camport_Outside; } set { camport_Outside = value; } }

        // 그래버 벤더 선택. true : Euresys(Coaxlink), false : Matrox
        public bool UseEuresys { get { return useEuresys; } set { useEuresys = value; } }

        // Matrox 세부 보드 타입 (UseEuresys == false 일 때만 참조됨).
        public MILBOARD_TYPE BoardType { get { return boardType; } set { boardType = value; } }

        // 센서 입력 모니터링 사용 여부. (Euresys 전용, Matrox는 추후 지원)
        public bool UseSensorIO { get { return useSensorIO; } set { useSensorIO = value; } }

        // 센서가 연결된 Euresys Interface 라인 이름.
        // 현장 배선 : 15pin D-Sub #3 = IIN11+, #12 = IIN11- → "IIN11"
        // 이 값은 스캔 시작 트리거(LIN1) 소스로도 함께 사용된다.
        public string SensorInputLine { get { return sensorInputLine; } set { sensorInputLine = value; } }

        // 센서 신호(상승 에지) 검출 시 로그 기록 여부.
        public bool SensorLogEnable { get { return sensorLogEnable; } set { sensorLogEnable = value; } }

        // 센서 ON 후 스캔 시작까지의 지연(ms). 카메라 인덱스별(0:Front 1:Rear 2:InSide 3:OutSide).
        // 0 = 지연 없음(센서 즉시 촬상). 보드의 IOToolbox DelayTool로 처리하므로 소프트웨어 지터가 없다.
        // XML에는 ms로 저장하고(1000 = 1초), fn_GetSensorTriggerDelay()가 실제 하드웨어
        // 계산에 필요한 us로 환산해 돌려준다.
        public int SensorTriggerDelay1 { get { return sensorTriggerDelay1; } set { sensorTriggerDelay1 = value; } }
        public int SensorTriggerDelay2 { get { return sensorTriggerDelay2; } set { sensorTriggerDelay2 = value; } }
        public int SensorTriggerDelay3 { get { return sensorTriggerDelay3; } set { sensorTriggerDelay3 = value; } }
        public int SensorTriggerDelay4 { get { return sensorTriggerDelay4; } set { sensorTriggerDelay4 = value; } }

        /// <summary>카메라 인덱스(0-base)별 센서 트리거 지연(us). XML 저장값(ms)을 us로 환산해 반환한다.</summary>
        public int fn_GetSensorTriggerDelay(int idx)
        {
            switch (idx)
            {
                case 0: return sensorTriggerDelay1 * 1000;
                case 1: return sensorTriggerDelay2 * 1000;
                case 2: return sensorTriggerDelay3 * 1000;
                case 3: return sensorTriggerDelay4 * 1000;
            }
            return 0;
        }

        // 사용할 IOToolbox 지연 블록 이름 (DEL1 ~ DEL4). 카메라마다 자기 보드의 블록을 쓰므로
        // 보드 간 충돌 없이 공통 이름을 써도 된다 — 필요해지면 카메라별로도 나눌 수 있다.
        public string SensorDelayTool { get { return sensorDelayTool; } set { sensorDelayTool = value; } }

        // 카메라별 라인주기(us). Setup 화면 CAM SETTING 탭에서 Gain/Exposure와 함께 편집한다.
        // 0 이하가 들어오면 Hz 환산이 깨지므로 최소 1us로 막는다.
        public double CamLinePeriod1 { get { return camLinePeriod1; } set { camLinePeriod1 = Math.Max(1.0, value); } }
        public double CamLinePeriod2 { get { return camLinePeriod2; } set { camLinePeriod2 = Math.Max(1.0, value); } }
        public double CamLinePeriod3 { get { return camLinePeriod3; } set { camLinePeriod3 = Math.Max(1.0, value); } }
        public double CamLinePeriod4 { get { return camLinePeriod4; } set { camLinePeriod4 = Math.Max(1.0, value); } }

        /// <summary>카메라 인덱스(0-base)별 라인주기(us)</summary>
        public double fn_GetCamLinePeriod(int idx)
        {
            switch (idx)
            {
                case 0: return camLinePeriod1;
                case 1: return camLinePeriod2;
                case 2: return camLinePeriod3;
                case 3: return camLinePeriod4;
            }
            return DEFAULT_LINE_PERIOD_US;
        }

        /// <summary>카메라 인덱스(0-base)별 라인레이트(Hz) = 1e6 / 라인주기(us). Euresys AcquisitionLineRate용.</summary>
        public double fn_GetCamLineRate(int idx)
        {
            return 1e6 / fn_GetCamLinePeriod(idx);
        }

        /// <summary>카메라 인덱스(0-base)별 노출시간 상한(us). 노출은 라인주기를 넘을 수 없다.</summary>
        // 예전엔 8us 여유를 뺐지만 이 카메라는 노출 = 라인주기로도 정상 촬상됨(실측).
        // 여유가 필요한 카메라가 나오면 여기서만 빼면 UI 슬라이더 상한과 그래버 캡이 같이 바뀐다.
        public double fn_GetCamExposureMax(int idx)
        {
            return fn_GetCamLinePeriod(idx);
        }

        public int GrabHeight { get { return grabHeight; } set { grabHeight = value; } }

        public int SerialDelay { get { return serialDelay; } set { serialDelay = value; } }

        public int MyNodeID { get { return mynodeID; } set { mynodeID = value; } }
        public int LinkCh { get { return linkch; } set { linkch = value; } }
        public int MailboxNo { get { return mailboxNo; } set { mailboxNo = value; } }
        public string SaveDrive { get { return savedrive; } set { savedrive = value; } }

        public double SwitchRecipeTimeOut { get { return dSwitchRecipeTimeOut; } set { dSwitchRecipeTimeOut = value; } }

        public bool LocalSave { get { return localSave; } set { localSave = value; } }

    }
}
