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

        //Cam Delay
        int grabDelayCam1 = 0;
        int grabDelayCam2 = 0;
        int grabDelayCam3 = 0;
        int grabDelayCam4 = 0;

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

        public int CamCount { get { return camCount; } set { camCount = value; } }
        //public int LightCount{ get { return lightCount; } set { lightCount = value; } }

        public int GrabDelayCam1 { get { return grabDelayCam1; } set { grabDelayCam1 = value; } }
        public int GrabDelayCam2 { get { return grabDelayCam2; } set { grabDelayCam2 = value; } }
        public int GrabDelayCam3 { get { return grabDelayCam3; } set { grabDelayCam3 = value; } }
        public int GrabDelayCam4 { get { return grabDelayCam4; } set { grabDelayCam4 = value; } }

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
