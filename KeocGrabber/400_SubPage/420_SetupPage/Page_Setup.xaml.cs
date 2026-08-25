/*
 *******************************************************************************
 * 해당 소스는 현장 유지 보수 외에 다른 목적의 사용을 금합니다.
 * - 주식회사 태루 -
 *******************************************************************************
 */
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FalconWpf;
using Microsoft.Win32;
using OpenCvSharp;

namespace KeocGrabber
{
    public class Page_SetupDataContext : MVVMBase.IPropertyChanged
    {
        Func<float, int, bool> delUpdateExposure = null;
        Func<float, int, bool> delUpdateGain = null;
        Func<int, int, bool> delUpdateLightTop = null;
        Func<int, int, bool> delUpdateLightBot = null;
        Action<int> delUpdateCamIndex = null;
        Action<int> delUpdateLightTopIndex = null;
        Action<int> delUpdateLightBotIndex = null;

        public Func<float, int, bool> DelUpdateExposure { set { delUpdateExposure = value; } }
        public Func<float, int, bool> DelUpdateGain { set { delUpdateGain = value; } }
        public Func<int, int, bool> DelUpdateLightTop { set { delUpdateLightTop = value; } }
        public Func<int, int, bool> DelUpdateLightBot { set { delUpdateLightBot = value; } }
        public Action<int> DelUpdateCamIndex { set { delUpdateCamIndex = value; } }
        public Action<int> DelUpdateLightTopIndex { set { delUpdateLightTopIndex = value; } }
        public Action<int> DelUpdateLightBotIndex { set { delUpdateLightBotIndex = value; } }

        bool bLightOn = false;
        public bool IsLightEnabled { get { return !bLightOn; } set { bLightOn = value; OnPropertyChanged(); } }
        public bool IsLightDisabled { get { return bLightOn; } }

        string strRecipePath = "";
        string strRecipeWriteTime = "";
        public string RecipePath { get { return strRecipePath; } set { strRecipePath = value; IsRecipeLoaded = strRecipePath != "" ? true : false; OnPropertyChanged(); } }
        public string RecipeWriteTime { get { return strRecipeWriteTime; } set { strRecipeWriteTime = value; OnPropertyChanged(); } }

        float fCamExposure1 = 0;
        float fCamExposure2 = 0;
        float fCamExposure3 = 0;
        float fCamExposure4 = 0;
        
        float fCamGain1 = 0;
        float fCamGain2 = 0;
        float fCamGain3 = 0;
        float fCamGain4 = 0;

        int nLightTopValue = 0;
        int nLightTop1 = 0;
        int nLightTop2 = 0;
        int nLightTop3 = 0;
        int nLightTop4 = 0;

        int nLightBotValue = 0;
        int nLightBot1 = 0;
        int nLightBot2 = 0;
        int nLightBot3 = 0;
        int nLightBot4 = 0;
        int nLightBot5 = 0;
        int nLightBot6 = 0;

        int nCamIndex = 0;

        int nMyNodeNo;
        int nTargetNodeNo;
        int nTargetNodeNo2;
        int nMailBoxNo;

        int nLightTopCtrlNo = 0;
        int nLightBotChNo = 0;

        double dTestCtrlWidth = 120;

        bool bIsGrabbing = false;

        string strTopLightString = "On";
        string strBotLightString = "On";
        bool bIsRecipeLoaded = false;


        DataTable dtCropROI = new DataTable();

        //DataTable dtCropROI2 = new DataTable();

        public DataTable DTCropROI { get { return dtCropROI; } set { dtCropROI = value; OnPropertyChanged(nameof(DTCropROI)); } }
        //public DataTable DTCropROI2 { get { return dtCropROI2; } set { dtCropROI2 = value; OnPropertyChanged(nameof(DTCropROI2)); } }

        int nSelectedROI = -1;
        public int SelectedROI { get { return nSelectedROI; } set { if (nSelectedROI != value) { nSelectedROI = value; OnPropertyChanged(); } } }

        public int LightTopCtrlNo { get { return nLightTopCtrlNo; } set { nLightTopCtrlNo = value; delUpdateLightTopIndex?.Invoke(value); OnPropertyChanged(nameof(LightTopCtrlNo)); } }
        public int LightBotChNo { get { return nLightBotChNo; } set { nLightBotChNo = value; delUpdateLightBotIndex?.Invoke(value); OnPropertyChanged(nameof(LightBotChNo)); } }

        public float CamExposure1 { get { return fCamExposure1; } set { fCamExposure1 = value; delUpdateExposure?.Invoke(value, 0); OnPropertyChanged(nameof(CamExposure1)); } }
        public float CamExposure2 { get { return fCamExposure2; } set { fCamExposure2 = value; delUpdateExposure?.Invoke(value, 1); OnPropertyChanged(nameof(CamExposure2)); } }
        public float CamExposure3 { get { return fCamExposure3; } set { fCamExposure3 = value; delUpdateExposure?.Invoke(value, 2); OnPropertyChanged(nameof(CamExposure3)); } }
        public float CamExposure4 { get { return fCamExposure4; } set { fCamExposure4 = value; delUpdateExposure?.Invoke(value, 3); OnPropertyChanged(nameof(CamExposure4)); } }

        public float CamGain1 { get { return fCamGain1; } set { fCamGain1 = value; delUpdateGain?.Invoke(value, 0);  OnPropertyChanged(nameof(CamGain1)); } }
        public float CamGain2 { get { return fCamGain2; } set { fCamGain2 = value; delUpdateGain?.Invoke(value, 1);  OnPropertyChanged(nameof(CamGain2)); } }
        public float CamGain3 { get { return fCamGain3; } set { fCamGain3 = value; delUpdateGain?.Invoke(value, 2);  OnPropertyChanged(nameof(CamGain3)); } }
        public float CamGain4 { get { return fCamGain4; } set { fCamGain4 = value; delUpdateGain?.Invoke(value, 3);  OnPropertyChanged(nameof(CamGain4)); } }

        // 라인레이트(Hz)는 레시피가 아니라 카메라(렌즈) 물리 설정값이라 SystemParam에 저장한다.
        // (SensorTriggerDelay1~4와 동일한 이유) — 값 변경은 다음 grab 시작부터 바로 반영된다.
        public double CamLineRate1 { get { return G.SYSTEM.CamLineRate1; } set { G.SYSTEM.CamLineRate1 = value; OnPropertyChanged(nameof(CamLineRate1)); } }
        public double CamLineRate2 { get { return G.SYSTEM.CamLineRate2; } set { G.SYSTEM.CamLineRate2 = value; OnPropertyChanged(nameof(CamLineRate2)); } }
        public double CamLineRate3 { get { return G.SYSTEM.CamLineRate3; } set { G.SYSTEM.CamLineRate3 = value; OnPropertyChanged(nameof(CamLineRate3)); } }
        public double CamLineRate4 { get { return G.SYSTEM.CamLineRate4; } set { G.SYSTEM.CamLineRate4 = value; OnPropertyChanged(nameof(CamLineRate4)); } }

        public int LightTopValue { get { return nLightTopValue; } set { nLightTopValue = value; delUpdateLightTop?.Invoke(nLightTopCtrlNo, value);  OnPropertyChanged(nameof(LightTopValue)); } }
        public int LightTop1 { get { return nLightTop1; } set { nLightTop1 = value; OnPropertyChanged(nameof(LightTop1)); } }
        public int LightTop2 { get { return nLightTop2; } set { nLightTop2 = value; OnPropertyChanged(nameof(LightTop2)); } }
        public int LightTop3 { get { return nLightTop3; } set { nLightTop3 = value; OnPropertyChanged(nameof(LightTop3)); } }
        public int LightTop4 { get { return nLightTop4; } set { nLightTop4 = value; OnPropertyChanged(nameof(LightTop4)); } }

        public int LightBotValue { get { return nLightBotValue; } set { nLightBotValue = value; delUpdateLightBot?.Invoke(nLightBotChNo, value);  OnPropertyChanged(nameof(LightBotValue)); } }
        public int LightBot1 { get { return nLightBot1; } set { nLightBot1 = value; OnPropertyChanged(nameof(LightBot1)); } }
        public int LightBot2 { get { return nLightBot2; } set { nLightBot2 = value; OnPropertyChanged(nameof(LightBot2)); } }
        public int LightBot3 { get { return nLightBot3; } set { nLightBot3 = value; OnPropertyChanged(nameof(LightBot3)); } }
        public int LightBot4 { get { return nLightBot4; } set { nLightBot4 = value; OnPropertyChanged(nameof(LightBot4)); } }
        public int LightBot5 { get { return nLightBot5; } set { nLightBot5 = value; OnPropertyChanged(nameof(LightBot5)); } }
        public int LightBot6 { get { return nLightBot6; } set { nLightBot6 = value; OnPropertyChanged(nameof(LightBot6)); } }


        public int CamIndex { get { return nCamIndex; } set { nCamIndex = value; delUpdateCamIndex?.Invoke(value); OnPropertyChanged(nameof(CamIndex)); } }
        public int MyNodeNo { get { return nMyNodeNo; } set { nMyNodeNo = value; OnPropertyChanged(nameof(MyNodeNo)); } }
        public int TargetNodeNo { get { return nTargetNodeNo; } set { nTargetNodeNo = value; OnPropertyChanged(nameof(TargetNodeNo)); } }
        public int TargetNodeNo2 { get { return nTargetNodeNo2; } set { nTargetNodeNo2 = value; OnPropertyChanged(nameof(TargetNodeNo2)); } }
        public int MailBoxNo { get { return nMailBoxNo; } set { nMailBoxNo = value; OnPropertyChanged(nameof(MailBoxNo)); } }

        bool bIsAdmin = false;
        public bool IsAdmin { get { return bIsAdmin; } set { bIsAdmin = value; TestCtrlWidth = value ? 120 : 0; OnPropertyChanged(); } }

        public bool IsRecipeLoaded { get { return bIsRecipeLoaded; } set { bIsRecipeLoaded = value; OnPropertyChanged(); } }

        public double TestCtrlWidth { get { return dTestCtrlWidth; } set { dTestCtrlWidth = value; OnPropertyChanged(); } }

        public bool IsGrabable { get { return !bIsGrabbing; } }
        public bool IsGrabbing { get { return bIsGrabbing; } set { bIsGrabbing = value; OnPropertyChanged(); } }

        public string TopLightString { get { return strTopLightString; } set { strTopLightString = value; OnPropertyChanged(); } }
        public string BotLightString { get { return strBotLightString; } set { strBotLightString = value; OnPropertyChanged(); } }

        public double ManualRecipeTimeOut { get { return G.SYSTEM.SwitchRecipeTimeOut; } set { G.SYSTEM.SwitchRecipeTimeOut = value; OnPropertyChanged(); G.MAIN.datacontext.UpdateValue(); } }

        public ObservableCollection<string> CameraItems { get; set; }
        public ObservableCollection<string> TopLightItems { get; set; }
        // 콤보 항목 순서 = 카메라 인덱스 순서(0:FRONT 1:REAR 2:IN SIDE 3:OUT SIDE).
        // 선택 인덱스가 그대로 카메라 인덱스로 쓰이므로 중간을 건너뛰면 안 된다.
        static readonly string[] CAM_NAMES = { "FRONT CAM (CAM1)", "REAR CAM (CAM2)", "IN SIDE CAM (CAM3)", "OUT SIDE CAM (CAM4)" };
        static readonly string[] TOP_LIGHT_NAMES = { "FRONT", "REAR", "IN SIDE", "OUT SIDE" };

        public void UpdateCamList(int count)
        {
            CameraItems.Clear();
            for (int i = 0; i < count && i < CAM_NAMES.Length; i++)
                CameraItems.Add(CAM_NAMES[i]);
        }

        // 상부 조명 컨트롤러는 카메라 대수만큼 존재한다.
        public void UpdateLightList(int count)
        {
            TopLightItems.Clear();
            for (int i = 0; i < count && i < TOP_LIGHT_NAMES.Length; i++)
                TopLightItems.Add(TOP_LIGHT_NAMES[i]);
        }

        // ── 카메라 인덱스 기반 UI 반영 (카메라 대수 가변 대응) ────────────────────
        public void SetCamExposure(int idx, float value)
        {
            switch (idx)
            {
                case 0: CamExposure1 = value; break;
                case 1: CamExposure2 = value; break;
                case 2: CamExposure3 = value; break;
                case 3: CamExposure4 = value; break;
            }
        }

        public void SetCamGain(int idx, float value)
        {
            switch (idx)
            {
                case 0: CamGain1 = value; break;
                case 1: CamGain2 = value; break;
                case 2: CamGain3 = value; break;
                case 3: CamGain4 = value; break;
            }
        }

        public void SetLightTop(int idx, int value)
        {
            switch (idx)
            {
                case 0: LightTop1 = value; break;
                case 1: LightTop2 = value; break;
                case 2: LightTop3 = value; break;
                case 3: LightTop4 = value; break;
            }
        }

        public void SetLightBot(int idx, int value)
        {
            switch (idx)
            {
                case 0: LightBot1 = value; break;
                case 1: LightBot2 = value; break;
                case 2: LightBot3 = value; break;
                case 3: LightBot4 = value; break;
                case 4: LightBot5 = value; break;
                case 5: LightBot6 = value; break;
            }
        }
    }

    /// <summary>
    /// Page_Setup.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Page_Setup : Page
    {
        RecipeParam mc_loadedRecipe = new RecipeParam();

        public Page_Setup()
        {
            InitializeComponent();
            datacontext.CameraItems = new ObservableCollection<string>();
            datacontext.TopLightItems = new ObservableCollection<string>();

            datacontext.DelUpdateExposure = cb_UpdateExposure;
            datacontext.DelUpdateGain = cb_UpdateGain;
            datacontext.DelUpdateLightTop = cb_UpdateLightTop;
            datacontext.DelUpdateLightBot = cb_UpdateLightBot;
            datacontext.DelUpdateCamIndex = cb_UpdateCamIndex;
            datacontext.IsGrabbing = G.GRABBER.fn_IsGrabbing(datacontext.CamIndex);

            datacontext.DelUpdateLightTopIndex = cb_UpdateLightTopIndex;
            datacontext.DelUpdateLightBotIndex = cb_UpdateLightBotIndex;
            fnInitTable();

        }

        private void UpdateLayout(int count)
        {
            // 카메라 대수만큼만 설정 항목을 열어 둔다. (없는 카메라 조작 방지)
            this.CAM1_Option.IsEnabled = count >= 1;
            this.CAM2_Option.IsEnabled = count >= 2;
            this.CAM3_Option.IsEnabled = count >= 3;
            this.CAM4_Option.IsEnabled = count >= 4;

            // 하부(VIT) 조명은 3캠 이상 구성에서만 존재.
            this.SetBotLight.IsEnabled = G.LIGHT.UseBottomLight;

            if (G.SYSTEM.JavasCount == 2)
            {
                this.Tab_ROI.Visibility = Visibility.Visible;
            }

            else if(G.SYSTEM.JavasCount == 1)
            {
                this.Tab_ROI.Visibility = Visibility.Hidden;
            }
         
            datacontext.CamIndex = 0;
            datacontext.LightTopCtrlNo = 0;
        }

        private void bn_ImageTest_Click(object sender, RoutedEventArgs e)
        {
            //Mat matTemp = Mat.Zeros(new OpenCvSharp.Size(100, 100), MatType.CV_8UC1);
            //matTemp.SetTo(Scalar.Gray);
            //
            //G.IMAGEMANAGER.AttachImage(0, matTemp);
            //G.IMAGEMANAGER.AttachImage(0, matTemp);
            //G.IMAGEMANAGER.AttachImage(0, matTemp);
            //OpenCvSharp.Cv2.ImWrite("Test.jpg", OpenCvSharp.WpfExtensions.WriteableBitmapConverter.ToMat(imgview.fn_GetImageStream()), new int[] { (int)ImwriteFlags.JpegQuality, 100 });

        }

        private void bn_Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "RecipeFile|*.xml";
            dlg.FileName = @"Recipe";
            if (dlg.ShowDialog() == true)
            {
                LoadRecipe(dlg.FileName);
                //XmlManager.LoadXml(dlg.FileName, mc_loadedRecipe);
                //fn_LoadRecipeFromObj();

                switch (datacontext.LightTopCtrlNo)
                {
                    case 0: datacontext.LightTopValue = datacontext.LightTop1; break;
                    case 1: datacontext.LightTopValue = datacontext.LightTop2; break;
                    case 2: datacontext.LightTopValue = datacontext.LightTop3; break;
                    case 3: datacontext.LightTopValue = datacontext.LightTop4; break;
                }

                switch (datacontext.LightBotChNo)
                {
                    case 0: datacontext.LightBotValue = datacontext.LightBot1; break;
                    case 1: datacontext.LightBotValue = datacontext.LightBot2; break;
                    case 2: datacontext.LightBotValue = datacontext.LightBot3; break;
                    case 3: datacontext.LightBotValue = datacontext.LightBot4; break;
                    case 4: datacontext.LightBotValue = datacontext.LightBot5; break;
                    case 5: datacontext.LightBotValue = datacontext.LightBot6; break;
                }

                //datacontext.RecipePath = dlg.FileName;
                //datacontext.RecipeWriteTime = mc_loadedRecipe.RecipeWriteTime;
            }
        }

        public void LoadRecipe(string recipename)
        {
            if (XmlManager.LoadXml(recipename, mc_loadedRecipe))
            {
                fn_LoadRecipeFromObj();
                datacontext.DTCropROI = mc_loadedRecipe.CropROI.Copy();
                datacontext.RecipePath = recipename;
                datacontext.RecipeWriteTime = mc_loadedRecipe.RecipeWriteTime;

                for(int i =3; i >= 0; i--)
                {
                    imgview.SelectedObject = i;
                    fnUpdateROIRect(i);
                }
            }
        }

        private void bn_Save_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "RecipeFile|*.xml";
            dlg.FileName = @"Recipe.xml";
            if (dlg.ShowDialog() == true)
            {
                fn_SaveRecipeToObj();
                mc_loadedRecipe.RecipeName = dlg.SafeFileName;
                mc_loadedRecipe.RecipeWriteTime = DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss");
                mc_loadedRecipe.CropROI = datacontext.DTCropROI.Copy();
                XmlManager.SaveXml(dlg.FileName, mc_loadedRecipe);
                datacontext.RecipePath = dlg.FileName;
                datacontext.RecipeWriteTime = mc_loadedRecipe.RecipeWriteTime;
            }
        }

        /// <summary>
        /// 로드된 레피시 클래스에서 UI로 값 업데이트.
        /// </summary>
        private void fn_LoadRecipeFromObj()
        {
            //datacontext
            List<PropertyInfo> listObj = new List<PropertyInfo>();
            listObj.AddRange(mc_loadedRecipe.GetType().GetProperties());

            List<PropertyInfo> listDatacontext = new List<PropertyInfo>();
            listDatacontext.AddRange(datacontext.GetType().GetProperties());
            for (int i = 0; i < listObj.Count; i++)
            {
                for (int j = 0; j < listDatacontext.Count; j++)
                {
                    if (listObj[i].Name == listDatacontext[j].Name)
                    {
                        listDatacontext[j].SetValue(datacontext, listObj[i].GetValue(mc_loadedRecipe));
                        break;
                    }

                }
            }
        }

        /// <summary>
        /// 저장 전 UI에서 레시피 클래스로 값 업데이트.
        /// </summary>
        private void fn_SaveRecipeToObj()
        {
            List<PropertyInfo> listObj = new List<PropertyInfo>();
            listObj.AddRange(mc_loadedRecipe.GetType().GetProperties());

            List<PropertyInfo> listDatacontext = new List<PropertyInfo>();
            listDatacontext.AddRange(datacontext.GetType().GetProperties());
            for (int j = 0; j < listDatacontext.Count; j++)
            {
                for (int i = 0; i < listObj.Count; i++)
                {
                    if (listObj[i].Name == listDatacontext[j].Name)
                    {
                        var tempValue = Convert.ChangeType(listDatacontext[j].GetValue(datacontext), listObj[i].PropertyType);
                        listObj[i].SetValue(mc_loadedRecipe, tempValue);
                        break;
                    }
                }
            }
        }

        private void bn_CamConfig_Click(object sender, RoutedEventArgs e)
        {
            Win_Vieworks cam = new Win_Vieworks();
            cam.Owner = Application.Current.MainWindow;
            cam.Show();
        }

        private void bn_Stop_Click(object sender, RoutedEventArgs e)
        {
            G.GRABBER.fn_GrabStop(datacontext.CamIndex);
            datacontext.IsGrabbing = G.GRABBER.fn_IsGrabbing(datacontext.CamIndex);
        }

        /// <summary>
        /// Setup 라이브뷰를 멈춘다. Main으로 전환 시 호출 → Manual Grab과 같은 grabber를
        /// 동시에 pop하는("EGrabber is busy in another thread") 충돌을 원천 차단.
        /// </summary>
        public void fn_StopLive()
        {
            if (G.GRABBER.fn_IsGrabbing(datacontext.CamIndex))
            {
                G.GRABBER.fn_GrabStop(datacontext.CamIndex);
                datacontext.IsGrabbing = G.GRABBER.fn_IsGrabbing(datacontext.CamIndex);
            }
        }

        private void bn_Grab_Click(object sender, RoutedEventArgs e)
        {
            G.GRABBER.fn_GrabStart(datacontext.CamIndex, true);
            datacontext.IsGrabbing = G.GRABBER.fn_IsGrabbing(datacontext.CamIndex);
        }

        private bool cb_UpdateExposure(float fExposure, int idx)
        {
            if (idx < G.SYSTEM.CamCount)
            {
                if (G.SYSTEM.UseEuresys)
                    G.GRABBER.fn_SetExposureTime(idx, fExposure);
                else
                    G.CAMERA[idx].fn_SetExposureTime(fExposure);
            }
            return false;
        }

        private bool cb_UpdateGain(float fGain, int idx)
        {
            if (idx < G.SYSTEM.CamCount)
            {
                if (G.SYSTEM.UseEuresys)
                    G.GRABBER.fn_SetGain(idx, fGain);
                else
                    G.CAMERA[idx].fn_SetDigitalGain(fGain);
            }
            return false;
        }
        private bool cb_UpdateLightTop(int index, int nLight)
        {
            datacontext.SetLightTop(index, nLight);
            G.LIGHT.fn_SetLightValue(index, nLight);
            return false;
        }
        private bool cb_UpdateLightBot(int index, int nLight)
        {
            datacontext.SetLightBot(index, nLight);
            G.LIGHT.fn_SetLightValue(G.LIGHT.TopLightCount + index, nLight);
            return false;
        }

        private void cb_UpdateCamIndex(int index)
        {
            //G.GRABBER.fn_GrabStop();
            imgview.ClearCanvas();
            datacontext.IsGrabbing = G.GRABBER.fn_IsGrabbing(datacontext.CamIndex);
        }

        private void cb_UpdateLightTopIndex(int index)
        {
            switch (index)
            {
                case 0: datacontext.LightTopValue = datacontext.LightTop1; break;
                case 1: datacontext.LightTopValue = datacontext.LightTop2; break;
                case 2: datacontext.LightTopValue = datacontext.LightTop3; break;
                case 3: datacontext.LightTopValue = datacontext.LightTop4; break;
            }
            G.LIGHT.fn_GetOnOff(index);

            //Thread.Sleep(100);
            //if (G.LIGHT.fn_IsLightOn(index))
            //    datacontext.TopLightString = "On";
            //else
            //    datacontext.TopLightString = "Off";
        }
        private void cb_UpdateLightBotIndex(int index)
        {
            switch (index)
            {
                case 0: datacontext.LightBotValue = datacontext.LightBot1; break;
                case 1: datacontext.LightBotValue = datacontext.LightBot2; break;
                case 2: datacontext.LightBotValue = datacontext.LightBot3; break;
                case 3: datacontext.LightBotValue = datacontext.LightBot4; break;
                case 4: datacontext.LightBotValue = datacontext.LightBot5; break;
                case 5: datacontext.LightBotValue = datacontext.LightBot6; break;
            }

            G.LIGHT.fn_GetOnOff(index + G.LIGHT.TopLightCount);

            //Thread.Sleep(100);
            //if (G.LIGHT.fn_IsLightOn(index + G.LIGHT.TopLightCount))
            //    datacontext.BotLightString = "On";
            //else
            //    datacontext.BotLightString = "Off";
        }

        private void bn_LightOn_Click(object sender, RoutedEventArgs e)
        {
            datacontext.IsLightEnabled = true;
            G.LIGHT.fn_LightOnAll();
        }

        private void bn_LightOff_Click(object sender, RoutedEventArgs e)
        {
            datacontext.IsLightEnabled = false;
            G.LIGHT.fn_LightOffAll();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
//             Task.Run(() => { fn_GetCamValue(); });
//             Task.Run(() => { fn_GetLightValue(); });

            datacontext.MyNodeNo = G.SYSTEM.MyNodeID;
            datacontext.TargetNodeNo = 2;
            datacontext.MailBoxNo = G.SYSTEM.MailboxNo;
            fn_UpdateAutority();

            datacontext.UpdateCamList(G.SYSTEM.CamCount);
            datacontext.UpdateLightList(G.SYSTEM.CamCount);
            UpdateLayout(G.SYSTEM.CamCount);
        }

        public void fn_UpdateAutority()
        {
            datacontext.IsAdmin = G.USERLEVEL == EN_AUTHORITY.EN_ENGINEER;
        }

        private void fn_GetCamValue()
        {
            float? fValue = null;

            // 카메라 대수만큼만 조회. (배열 길이와 연결 상태를 각각 확인)
            for (int i = 0; i < G.SYSTEM.CamCount && i < G.CAMERA.Length; i++)
            {
                if (G.CAMERA[i] == null || !G.CAMERA[i].IsConnected) continue;

                fValue = G.CAMERA[i].fn_GetExposureTime();
                if (fValue != null) datacontext.SetCamExposure(i, (float)fValue);

                fValue = G.CAMERA[i].fn_GetDigitalGain();
                if (fValue != null) datacontext.SetCamGain(i, (float)fValue);
            }
        }

        private void fn_GetLightValue()
        {
            int? nValue = null;
            if (G.LIGHT != null)
            {
                int nCamCount = G.SYSTEM.CamCount;

                // 상부 = 카메라 대수만큼, 이어지는 인덱스가 하부(VIT) 채널.
                // 4캠 기준 0~3 상부 / 4~7 하부 — 기존 매핑과 동일.
                for (int i = 0; i < nCamCount; i++)
                {
                    nValue = G.LIGHT.fn_GetLightValue(i);
                    if (nValue != null) datacontext.SetLightTop(i, (int)nValue);
                }

                if (G.LIGHT.UseBottomLight)
                {
                    for (int i = 0; i < LightManager.BOTTOM_LIGHT_CH_COUNT; i++)
                    {
                        nValue = G.LIGHT.fn_GetLightValue(nCamCount + i);
                        if (nValue != null) datacontext.SetLightBot(i, (int)nValue);
                    }
                }
            }
        }

        private void bn_LinkTest_Click(object sender, RoutedEventArgs e)
        {
            G.GIGABOARD.fn_LinkNodesInitialize(datacontext.MyNodeNo, datacontext.TargetNodeNo, datacontext.MailBoxNo);
        }

        private void bn_GigaSend_Click(object sender, RoutedEventArgs e)
        {
            WriteableBitmap wb = imgview.fn_GetImageStream();
            if (wb != null)
            {
                Mat mattemp = OpenCvSharp.WpfExtensions.WriteableBitmapConverter.ToMat(wb);
                byte[] byteArray = new byte[mattemp.Total()];
                GCHandle handle = GCHandle.Alloc(byteArray, GCHandleType.Pinned);
                IntPtr dataPtr = mattemp.Data;
                Marshal.Copy(dataPtr, byteArray, 0, byteArray.Length);
                handle.Free();
                //var byteArray = mattemp.ToBytes(".bmp");
                G.GIGABOARD.fn_SendData(1, 2, 0, byteArray, new int[] { datacontext.TargetNodeNo, datacontext.TargetNodeNo2 });
                
            }
        }

        private void bn_TcpIPSend_Click(object sender, RoutedEventArgs e)
        {
            //G.SendReady(0, 16384, 10000, 1, datacontext.TargetNodeNo);
            //G.COMM.SendReady(0, 16384, 32000, 1, datacontext.TargetNodeNo);
            G.COMM.SendReady(0, 16384, 32000, 32000, new int[] { datacontext.TargetNodeNo });
        }

        private void bn_SetRecipe_Click(object sender, RoutedEventArgs e)
        {
            CopyRecipe(mc_loadedRecipe, ref G.CURRRECIPE);
            G.MAIN.datacontext.CurrRecipe = G.CURRRECIPE.RecipeName;
            G.SYSTEM.CurrRecipePath = datacontext.RecipePath;
            FileInfo fileinfo = new FileInfo(G.SYSTEM.CurrRecipePath);
            G.SYSTEM.CurrRecipeEditTime = fileinfo.LastWriteTime;

            G.WriteLog($"{G.MAIN.datacontext.CurrRecipe} Recipe Set Succ.[{G.SYSTEM.CurrRecipePath}]");
            MessageBox.Show($"{G.MAIN.datacontext.CurrRecipe} Recipe Set Succ.", "Recipe Set", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CopyRecipe(RecipeParam src, ref RecipeParam dst)
        {
            List<PropertyInfo> listSrc = new List<PropertyInfo>();
            listSrc.AddRange(src.GetType().GetProperties());

            List<PropertyInfo> listDst = new List<PropertyInfo>();
            listDst.AddRange(dst.GetType().GetProperties());
            for (int i = 0; i < listSrc.Count; i++)
            {
                for (int j = 0; j < listDst.Count; j++)
                {
                    if (listSrc[i].Name == listDst[j].Name)
                    {
                        var tempValue = Convert.ChangeType(listSrc[j].GetValue(src), listSrc[i].PropertyType);
                        listDst[i].SetValue(dst, tempValue);
                        break;
                    }
                }
            }
        }

        private void bn_PushImage_Click(object sender, RoutedEventArgs e)
        {
            //WriteableBitmap wb = imgview.fn_GetImageStream();
            //if (wb != null)
            {
                //Mat mattemp = OpenCvSharp.WpfExtensions.WriteableBitmapConverter.ToMat(wb);
                Mat mattemp = OpenCvSharp.Mat.Zeros(new OpenCvSharp.Size(16384, 32000), MatType.CV_8UC1);
                Cv2.Rectangle(mattemp, new OpenCvSharp.Rect(0, 0, 16384, 32000), new Scalar(255), -1);

                G.IMAGEMANAGER.SetTestImage(mattemp, 0);
                G.IMAGEMANAGER.SetTestImage(mattemp, 1);
                G.IMAGEMANAGER.SetTestImage(mattemp, 2);
                G.IMAGEMANAGER.SetTestImage(mattemp, 3);

                //G.DISKMANAGER.PushSaveImage(0, mattemp, "Test");
            }
        }

        private void bn_LightOnTop_Click(object sender, RoutedEventArgs e)
        {
            int index = datacontext.LightTopCtrlNo;
            UserButton ub = sender as UserButton;
            if (ub != null)
            {
                if (G.LIGHT.fn_IsLightOn(index))
                {
                    G.LIGHT.fn_LightOff(index);
                    datacontext.TopLightString = "On";
                }
                else
                {
                    G.LIGHT.fn_LightOn(index);
                    datacontext.TopLightString = "Off";
                }
                Task.Run(() => { G.LIGHT.fn_GetOnOff(index); });
            }
        }

        private void bn_LightOnBot_Click(object sender, RoutedEventArgs e)
        {
            int index = G.LIGHT.TopLightCount + datacontext.LightBotChNo;
            UserButton ub = sender as UserButton;
            if (ub != null)
            {
                if (G.LIGHT.fn_IsLightOn(index))
                {
                    G.LIGHT.fn_LightOff(index);
                    datacontext.BotLightString = "On";
                }
                else
                {
                    G.LIGHT.fn_LightOn(index);
                    datacontext.BotLightString = "Off";
                }
                Task.Run(() => { G.LIGHT.fn_GetOnOff(index); });
            }
        }

        private void bn_LightOnTopAll_Click(object sender, RoutedEventArgs e)
        {
            UserButton ub = sender as UserButton;
            if (ub != null)
            {
                string strName = ub.Content as string;
                if (strName != null)
                {
                    if (strName == "Light Off")
                    {
                        G.LIGHT.fn_LightOff(0);
                        G.LIGHT.fn_LightOff(1);
                        G.LIGHT.fn_LightOff(2);
                        G.LIGHT.fn_LightOff(3);
                        ub.Content = "Light On";
                    }
                    else
                    {
                        G.LIGHT.fn_LightOn(0);
                        G.LIGHT.fn_LightOn(1);
                        G.LIGHT.fn_LightOn(2);
                        G.LIGHT.fn_LightOn(3);
                        ub.Content = "Light Off";
                    }
                }
            }
        }

        private void bn_LightOnBotAll_Click(object sender, RoutedEventArgs e)
        {
            UserButton ub = sender as UserButton;
            if (ub != null)
            {
                string strName = ub.Content as string;
                if (strName != null)
                {
                    if (strName == "Light Off")
                    {
                        G.LIGHT.fn_LightOff(4);
                        G.LIGHT.fn_LightOff(5);
                        G.LIGHT.fn_LightOff(6);
                        G.LIGHT.fn_LightOff(7);
                        G.LIGHT.fn_LightOff(8);
                        G.LIGHT.fn_LightOff(9);
                        ub.Content = "Light On";
                    }
                    else
                    {
                        G.LIGHT.fn_LightOn(4);
                        G.LIGHT.fn_LightOn(5);
                        G.LIGHT.fn_LightOn(6);
                        G.LIGHT.fn_LightOn(7);
                        G.LIGHT.fn_LightOn(8);
                        G.LIGHT.fn_LightOn(9);
                        ub.Content = "Light Off";
                    }
                }
            }
        }

        private void bn_TestLIght(object sender, RoutedEventArgs e)
        {
            G.LIGHT.fn_IsLightOn(0);
        }

        private void bn_CurrentRecipe_Click(object sender, RoutedEventArgs e)
        {
            CopyRecipe(G.CURRRECIPE, ref mc_loadedRecipe);
            mc_loadedRecipe = G.CURRRECIPE;

            fn_LoadRecipeFromObj();

            switch (datacontext.LightTopCtrlNo)
            {
                case 0: datacontext.LightTopValue = datacontext.LightTop1; break;
                case 1: datacontext.LightTopValue = datacontext.LightTop2; break;
                case 2: datacontext.LightTopValue = datacontext.LightTop3; break;
                case 3: datacontext.LightTopValue = datacontext.LightTop4; break;
            }

            switch (datacontext.LightBotChNo)
            {
                case 0: datacontext.LightBotValue = datacontext.LightBot1; break;
                case 1: datacontext.LightBotValue = datacontext.LightBot2; break;
                case 2: datacontext.LightBotValue = datacontext.LightBot3; break;
                case 3: datacontext.LightBotValue = datacontext.LightBot4; break;
                case 4: datacontext.LightBotValue = datacontext.LightBot5; break;
                case 5: datacontext.LightBotValue = datacontext.LightBot6; break;
            }

            datacontext.RecipePath = $"C:/KEOC/Recipe/{G.CURRRECIPE.RecipeName}";
            datacontext.RecipeWriteTime = mc_loadedRecipe.RecipeWriteTime;
        }

        private void DataGrid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            imgview.SelectedObject = datacontext.SelectedROI;
            //datacontext.SelectedROI
        }

        private void bn_ROIAlign_Click(object sender, RoutedEventArgs e)
        {
            Button ub = sender as Button;

            if (ub != null)
            {
                int nMode = 0;
                int.TryParse(ub.Tag.ToString(), out nMode);
                //! 1 : left, 2 : center, 3 : right, 4 : top, 5 : middle, 6 : bottom, 7 : vertical, 8 : horizontal.  Taeroo-kgseon - 2024/08/26  18:48
                imgview.AlignObject(nMode);
            }
        }
        public void fnInitTable()
        {
            datacontext.DTCropROI.Columns.Add("CellID");
            datacontext.DTCropROI.Columns.Add("X");
            datacontext.DTCropROI.Columns.Add("Y");
            datacontext.DTCropROI.Columns.Add("Width");
            datacontext.DTCropROI.Columns.Add("Height");

            datacontext.DTCropROI.Rows.Add("FRONT(CELL1)", 0, 0, 0, 0);
            datacontext.DTCropROI.Rows.Add("FRONT(CELL2)", 0, 0, 0, 0);
            datacontext.DTCropROI.Rows.Add("REAR(CELL1)", 0, 0, 0, 0);
            datacontext.DTCropROI.Rows.Add("REAR(CELL2)", 0, 0, 0, 0);

            datacontext.DTCropROI.ColumnChanged += DTCropROI_ColumnChanged;

            imgview.delUpdateRect = cb_UpdateROIRect;
            //imgview.SetROIRect();
        }

        private bool cb_UpdateROIRect(int idx, double x, double y, double w, double h)
        {
            datacontext.DTCropROI.Rows[idx]["X"] = (int)x;
            datacontext.DTCropROI.Rows[idx]["Y"] = (int)y;
            datacontext.DTCropROI.Rows[idx]["Width"] = (int)w;
            datacontext.DTCropROI.Rows[idx]["Height"] = (int)h;

            return false;
        }

        private void DTCropROI_ColumnChanged(object sender, DataColumnChangeEventArgs e)
        {
            //int[] value = new int[datacontext.DTCropROI.Columns.Count];
            //for (int i = 1; i < value.Length; i++)
            //{
            //    try
            //    {
            //        value[i] = Convert.ToInt32(e.Row.ItemArray[i]);
            //    }
            //    catch { }
            //}
            //imgview.SetROIRect(value[1], value[2], value[3], value[4]);
            fnUpdateROIRect(datacontext.SelectedROI);
        }
        private void fnUpdateROIRect(int index)
        {
            if (index < 0) return;
            int[] value = new int[datacontext.DTCropROI.Columns.Count];
            for (int i = 1; i < value.Length; i++)
            {
                try
                {
                    //value[i] = Convert.ToInt32(e.Row.ItemArray[i]);
                    value[i] = Convert.ToInt32(datacontext.DTCropROI.Rows[index][i]);
                }
                catch { }
            }
            imgview.SetROIRect(value[1], value[2], value[3], value[4]);
        }
    }
}
