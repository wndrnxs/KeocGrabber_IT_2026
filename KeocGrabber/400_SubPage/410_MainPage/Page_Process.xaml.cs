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
    public class Page_ProcessDataContext : MVVMBase.IPropertyChanged
    {
        SolidColorBrush brsStateReady   = Brushes.Transparent;
        SolidColorBrush brsStateGrab    = Brushes.Transparent;
        SolidColorBrush brsStateEnd     = Brushes.Transparent;
        SolidColorBrush brsStateAlarm   = Brushes.Transparent;

        public SolidColorBrush StateReady { get { return brsStateReady; } set { brsStateReady = value; OnPropertyChanged(); } }
        public SolidColorBrush StateGrab  { get { return brsStateGrab ; } set { brsStateGrab  = value; OnPropertyChanged(); } }
        public SolidColorBrush StateEnd   { get { return brsStateEnd  ; } set { brsStateEnd   = value; OnPropertyChanged(); } }
        public SolidColorBrush StateAlarm { get { return brsStateAlarm; } set { brsStateAlarm = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// Page_Process.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Page_Process : Page
    {
        DispatcherTimer timer = new DispatcherTimer();

        const string CLROK = "OKBrush";
        const string CLRNG = "NGBrush";

        public Page_Process()
        {
            InitializeComponent();

            timer.Interval = TimeSpan.FromMilliseconds(500);

            timer.Tick += timer_ProcUpdate;
            timer.Start();
        }

        private void timer_ProcUpdate(object sender, EventArgs e)
        {
            switch (G.READYSTATE)
            {
                case EN_READYSTATE.None:
                    datacontext.StateReady  = Brushes.Transparent;
                    datacontext.StateAlarm  = Brushes.Transparent;
                    break;
                case EN_READYSTATE.Ready:
                    datacontext.StateReady = (SolidColorBrush)G.MAIN.resource[CLROK];
                    datacontext.StateAlarm = Brushes.Transparent;
                    break;
                case EN_READYSTATE.Alarm:
                    datacontext.StateReady = Brushes.Transparent;
                    datacontext.StateAlarm = (SolidColorBrush)G.MAIN.resource[CLRNG];
                    break;
            }

            switch (G.GRABSTATE)
            {
                case EN_GRABSTATE.None:
                    datacontext.StateGrab = Brushes.Transparent;
                    datacontext.StateEnd =  Brushes.Transparent;
                    break;
                case EN_GRABSTATE.Grab:
                    datacontext.StateGrab = (SolidColorBrush)G.MAIN.resource[CLROK];
                    datacontext.StateEnd = Brushes.Transparent;
                    break;
                case EN_GRABSTATE.End:
                    datacontext.StateGrab = Brushes.Transparent;
                    datacontext.StateEnd = (SolidColorBrush)G.MAIN.resource[CLROK];
                    break;
            }
        }
    }
}
