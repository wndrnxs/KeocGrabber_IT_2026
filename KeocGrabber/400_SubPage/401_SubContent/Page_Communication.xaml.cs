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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace KeocGrabber
{
    public class Page_CommnucationDataContext : MVVMBase.IPropertyChanged
    {
        SolidColorBrush brushConnMaster = Brushes.Transparent;
        SolidColorBrush brushConnMaster2 = Brushes.Transparent;
        SolidColorBrush brushConnGiGA   = Brushes.Transparent;
        SolidColorBrush brushConnCam    = Brushes.Transparent;
        SolidColorBrush brushConnGrabber = Brushes.Transparent;
        SolidColorBrush brushConnLight  = Brushes.Transparent;

        public SolidColorBrush ConnMaster { get { return brushConnMaster; } set { brushConnMaster = value; OnPropertyChanged(); } }
        public SolidColorBrush ConnMaster2 { get { return brushConnMaster2; } set { brushConnMaster2 = value; OnPropertyChanged(); } }
        public SolidColorBrush ConnGiGA   { get { return brushConnGiGA  ; } set { brushConnGiGA   = value; OnPropertyChanged(); } }
        public SolidColorBrush ConnCam    { get { return brushConnCam   ; } set { brushConnCam    = value; OnPropertyChanged(); } }
        public SolidColorBrush ConnGrabber { get { return brushConnGrabber; } set { brushConnGrabber = value; OnPropertyChanged(); } }
        public SolidColorBrush ConnLight  { get { return brushConnLight ; } set { brushConnLight  = value; OnPropertyChanged(); } }
    }
    /// <summary>
    /// Page_Communication.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Page_Communication : Page
    {
        DispatcherTimer timer = new DispatcherTimer();
        public Page_Communication()
        {
            InitializeComponent();

            timer.Interval = TimeSpan.FromMilliseconds(1000);
            timer.Tick += timer_ConnUpdate;
            timer.Start();

            UpdateLayout();
        }
        
        private void Page_Communication_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateLayout(G.SYSTEM.JavasCount);
        }

        private void UpdateLayout(int javas)
        {
            if (javas == 1)
            {
                this.TcpMaster2.Visibility = Visibility.Collapsed;
                Grid.SetColumnSpan(TcpMaster1, 2);
            }
            else
            {
                this.TcpMaster2.Visibility = Visibility.Visible;
                Grid.SetColumnSpan(TcpMaster1, 1);
            }
        }
        private void timer_ConnUpdate(object sender, EventArgs e)
        {
            try
            {
//#if DEBUG
//                SolidColorBrush ok = (SolidColorBrush)G.MAIN.resource["OKBrush"];
//                datacontext.ConnMaster = ok;
//                datacontext.ConnMaster2 = ok;
//                datacontext.ConnGiGA = ok;
//                datacontext.ConnGrabber = ok;
//                datacontext.ConnCam = ok;
//                datacontext.ConnLight = ok;
//#else
                string strClr = "";
                strClr = G.COMM.IsConnected ? "OKBrush" : "NGBrush";
                datacontext.ConnMaster = (SolidColorBrush)G.MAIN.resource[strClr];
                if (G.COMM2 != null)
                {
                    strClr = G.COMM2.IsConnected ? "OKBrush" : "NGBrush";
                    datacontext.ConnMaster2 = (SolidColorBrush)G.MAIN.resource[strClr];
                }
                strClr = G.GIGABOARD.IsConnected ? "OKBrush" : "NGBrush";
                datacontext.ConnGiGA = (SolidColorBrush)G.MAIN.resource[strClr];

                strClr = G.GRABBER.IsInited ? "OKBrush" : "NGBrush";
                datacontext.ConnGrabber = (SolidColorBrush)G.MAIN.resource[strClr];

                bool bRet = true;
                for (int i = 0; i < G.SYSTEM.CamCount; i++)
                {
                    bRet &= G.GRABBER.fn_IsCameraConnected(i);
                }

                strClr = bRet ? "OKBrush" : "NGBrush";
                datacontext.ConnCam = (SolidColorBrush)G.MAIN.resource[strClr];

                int MaxlightCount = G.SYSTEM.CamCount == 2 ? 2 : 5;
                bRet = true;
                for (int i = 0; i < MaxlightCount; i++)
                {
                    bRet &= G.LIGHT[i];
                }

                strClr = bRet ? "OKBrush" : "NGBrush";
                datacontext.ConnLight = (SolidColorBrush)G.MAIN.resource[strClr];
//#endif
            }
            catch {}
        }
    }
}
