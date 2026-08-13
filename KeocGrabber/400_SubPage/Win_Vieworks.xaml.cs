/*
 *******************************************************************************
 * 해당 소스는 현장 유지 보수 외에 다른 목적의 사용을 금합니다.
 * - 주식회사 태루 -
 *******************************************************************************
 */
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace KeocGrabber
{
    class Win_VieworksDataContext : MVVMBase.IPropertyChanged
    {
        Action<int> delChangeIndex = null;
        public Action<int> DelChangeIndex { set { delChangeIndex = value; } }
        ObservableCollection<string> cbComport = new ObservableCollection<string>();

        public ObservableCollection<string> Comport { get { return cbComport; } set { cbComport = value; OnPropertyChanged("Comport"); } }

        int nSelectIndex = 0;
        public int SelectedIndex { get { return nSelectIndex; } set { nSelectIndex = value; delChangeIndex?.Invoke(nSelectIndex); OnPropertyChanged(); } }

        int     m_nImageOffset          = 0;
        int     m_nImageWidth           = 0;
        float   m_fLinePeriod           = 0.0f;
        float   m_fExposureTime         = 0.0f;
        int     m_nTestImage            = 0;
        int     m_nDataBit              = 0;
        int     m_nCameraLinkMode       = 0;
        int     m_nCameraLinkClockSpeed = 0;
        bool    m_bHorizontalFlip       = false;
        float   m_fDigitalGain          = 0.0f;
        int     m_nDigitalOffset        = 0;
        int     m_nTriggerMode          = 0;
        int     m_nExposureSource       = 0;
        int     m_nTriggerSource        = 0;
        bool    m_bTriggerPolarity      = false;
        float   m_fTriggerConverter     = 0.0f;
        int     m_nImageMode            = 0;

        public int     ImageOffset          { get { return m_nImageOffset         ; } set { m_nImageOffset          = value; OnPropertyChanged(nameof(ImageOffset         )); } }
        public int     ImageWidth           { get { return m_nImageWidth          ; } set { m_nImageWidth           = value; OnPropertyChanged(nameof(ImageWidth          )); } }
        public float   LinePeriod           { get { return m_fLinePeriod          ; } set { m_fLinePeriod           = value; OnPropertyChanged(nameof(LinePeriod          )); } }
        public float   ExposureTime         { get { return m_fExposureTime        ; } set { m_fExposureTime         = value; OnPropertyChanged(nameof(ExposureTime        )); } }
        public int     TestImage            { get { return m_nTestImage           ; } set { m_nTestImage            = value; OnPropertyChanged(nameof(TestImage           )); } }
        public int     DataBit              { get { return m_nDataBit             ; } set { m_nDataBit              = (value - 8) / 2; OnPropertyChanged(nameof(DataBit             )); } }
        public int     CameraLinkMode       { get { return m_nCameraLinkMode      ; } set { m_nCameraLinkMode       = value; OnPropertyChanged(nameof(CameraLinkMode      )); } }
        public int     CameraLinkClockSpeed { get { return m_nCameraLinkClockSpeed; } set { m_nCameraLinkClockSpeed = value; OnPropertyChanged(nameof(CameraLinkClockSpeed)); } }
        public bool    HorizontalFlip       { get { return m_bHorizontalFlip      ; } set { m_bHorizontalFlip       = value; OnPropertyChanged(nameof(HorizontalFlip      )); } }
        public float   DigitalGain          { get { return m_fDigitalGain         ; } set { m_fDigitalGain          = value; OnPropertyChanged(nameof(DigitalGain         )); } }
        public int     DigitalOffset        { get { return m_nDigitalOffset       ; } set { m_nDigitalOffset        = value; OnPropertyChanged(nameof(DigitalOffset       )); } }
        public int     TriggerMode          { get { return m_nTriggerMode         ; } set { m_nTriggerMode          = value; OnPropertyChanged(nameof(TriggerMode         )); } }
        public int     ExposureSource       { get { return m_nExposureSource      ; } set { m_nExposureSource       = value; OnPropertyChanged(nameof(ExposureSource      )); } }
        public int     TriggerSource        { get { return m_nTriggerSource       ; } set { m_nTriggerSource        = value - 1; OnPropertyChanged(nameof(TriggerSource       )); } }
        public bool    TriggerPolarity      { get { return m_bTriggerPolarity     ; } set { m_bTriggerPolarity      = value; OnPropertyChanged(nameof(TriggerPolarity     )); } }
        public float   TriggerConverter     { get { return m_fTriggerConverter    ; } set { m_fTriggerConverter     = value; OnPropertyChanged(nameof(TriggerConverter    )); } }
        public int     ImageMode            { get { return m_nImageMode           ; } set { m_nImageMode            = value; OnPropertyChanged(nameof(ImageMode           )); } }

        bool bIsConnected = false;
        public bool IsConnected { get { return bIsConnected; } set { bIsConnected = value; OnPropertyChanged(nameof(IsConnected)); } }
    }

    /// <summary>
    /// Win_Vieworks.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Win_Vieworks : Window
    {
        VieworksCamera cam = new VieworksCamera();

        Task task1 = null;
        Task task2 = null;
        public Win_Vieworks()
        {
            InitializeComponent();
            datacontext.DelChangeIndex = del_OnUpdateIndex;
        }

        private void del_OnUpdateIndex(int idx)
        {
            lbMessage.Items.Clear();
            //if (task1 != null) { task1.IsCanceled = false; }
            //if (task2 != null) { task2.IsCanceled = false; }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            fn_GetCamComport();

            //fn_GetComport();
        }

        private void fn_GetComport()
        {
            datacontext.Comport.Clear();
            var list = SerialPort.GetPortNames();
            foreach (var port in list)
            {
                datacontext.Comport.Add(port);
            }
        }

        private void fn_GetCamComport()
        {
            datacontext.Comport.Clear();
            for (int i = 0; i < G.CAMERA.Length; i++)
            {
                datacontext.Comport.Add(G.CAMERA[i].Comport);
                G.CAMERA[i].WriteLog = WriteLog;
            }
        }

        private void fn_GetParameter(VieworksCamera cam)
        {
            int? nValue = null;
            float? fValue = null;
            bool? bValue = null;

             //if (G.CAMERA.Length > datacontext.SelectedIndex && datacontext.SelectedIndex >= 0 && G.CAMERA[datacontext.SelectedIndex] != null)
             if (cam.IsConnected)
             {
                nValue = cam.fn_GetImageOffset()            ; if (nValue != null) datacontext.ImageOffset           = (int  )nValue;
                nValue = cam.fn_GetImageWidth()             ; if (nValue != null) datacontext.ImageWidth            = (int  )nValue;
                fValue = cam.fn_GetLinePeriod()             ; if (fValue != null) datacontext.LinePeriod            = (float)fValue;
                fValue = cam.fn_GetExposureTime()           ; if (fValue != null) datacontext.ExposureTime          = (float)fValue;
                nValue = cam.fn_GetTestImage()              ; if (nValue != null) datacontext.TestImage             = (int  )nValue;
                nValue = cam.fn_GetDataBit()                ; if (nValue != null) datacontext.DataBit               = (int)(((int  )nValue - 8) / 2.0);
                nValue = cam.fn_GetCameraLinkMode()         ; if (nValue != null) datacontext.CameraLinkMode        = (int  )nValue;
                nValue = cam.fn_GetCameraLinkClockSpeed()   ; if (nValue != null) datacontext.CameraLinkClockSpeed  = (int  )nValue;
                bValue = cam.fn_GetHorizontalFlip()         ; if (bValue != null) datacontext.HorizontalFlip        = (bool )bValue;
                fValue = cam.fn_GetDigitalGain()            ; if (fValue != null) datacontext.DigitalGain           = (float)fValue;
                nValue = cam.fn_GetDigitalOffset()          ; if (nValue != null) datacontext.DigitalOffset         = (int  )nValue;
                nValue = cam.fn_GetTriggerMode()            ; if (nValue != null) datacontext.TriggerMode           = (int  )nValue;
                nValue = cam.fn_GetExposureSource()         ; if (nValue != null) datacontext.ExposureSource        = (int  )nValue - 1;
                nValue = cam.fn_GetTriggerSource()          ; if (nValue != null) datacontext.TriggerSource         = (int  )nValue;
                bValue = cam.fn_GetTriggerPolarity()        ; if (bValue != null) datacontext.TriggerPolarity       = (bool )bValue;
                fValue = cam.fn_GetTriggerConverter()       ; if (fValue != null) datacontext.TriggerConverter      = (float)fValue;
                nValue = cam.fn_GetImageMode()              ; if (nValue != null) datacontext.ImageMode             = (int  )nValue;
             }
        }

        private void fn_SetParameter(VieworksCamera cam)
        {
            //if (G.CAMERA.Length > datacontext.SelectedIndex && datacontext.SelectedIndex >= 0 && G.CAMERA[datacontext.SelectedIndex] != null)
            if (cam.IsConnected)
             {
                cam.fn_SetImageOffset           (datacontext.ImageOffset          );
                cam.fn_SetImageWidth            (datacontext.ImageWidth           );
                cam.fn_SetLinePeriod            (datacontext.LinePeriod           );
                cam.fn_SetExposureTime          (datacontext.ExposureTime         );
                cam.fn_SetTestImage             (datacontext.TestImage            );
                cam.fn_SetDataBit               (datacontext.DataBit * 2 + 8      );
                cam.fn_SetCameraLinkMode        (datacontext.CameraLinkMode       );
                cam.fn_SetCameraLinkClockSpeed  (datacontext.CameraLinkClockSpeed );
                cam.fn_SetHorizontalFlip        (datacontext.HorizontalFlip       );
                cam.fn_SetDigitalGain           (datacontext.DigitalGain          );
                cam.fn_SetDigitalOffset         (datacontext.DigitalOffset        );
                cam.fn_SetTriggerMode           (datacontext.TriggerMode          );
                cam.fn_SetExposureSource        (datacontext.ExposureSource + 1   );
                cam.fn_SetTriggerSource         (datacontext.TriggerSource        );
                cam.fn_SetTriggerPolarity       (datacontext.TriggerPolarity      );
                cam.fn_SetTriggerConverter      (datacontext.TriggerConverter     );
                cam.fn_SetImageMode             (datacontext.ImageMode            );
             }
        }

        private void bn_Apply_Click(object sender, RoutedEventArgs e)
        {
            task1 = Task.Run(() => { fn_SetParameter(G.CAMERA[datacontext.SelectedIndex]); });
        }

        private void bn_Update_Click(object sender, RoutedEventArgs e)
        {
            datacontext.IsConnected = G.CAMERA[datacontext.SelectedIndex].IsConnected;
            task2 = Task.Run(() => { fn_GetParameter(G.CAMERA[datacontext.SelectedIndex]); });
        }

        private bool WriteLog(string strmsg)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(delegate () {

                lbMessage.Items.Add(strmsg);
                if (lbMessage.Items.Count > 200)
                {
                    lbMessage.Items.RemoveAt(0);
                }
                lbMessage.ScrollIntoView(lbMessage.Items[lbMessage.Items.Count - 1]);
            }));
            return false;
        }
    }
}
