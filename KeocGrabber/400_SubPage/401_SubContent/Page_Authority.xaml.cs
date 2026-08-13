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

namespace KeocGrabber
{
    public class Page_AutorityDataContext : MVVMBase.IPropertyChanged
    {
        EN_AUTHORITY level;

        public EN_AUTHORITY Auth { get { return level; } 
            set 
            { 
                level = value;
                AuthLevel = level.ToString().Replace("EN_", "");
                switch(level)
                {
                    case EN_AUTHORITY.EN_OPERATOR   : UserBrush = (SolidColorBrush)G.MAIN.resource["UserOperator"   ]; break;
                    case EN_AUTHORITY.EN_MAINTENANCE: UserBrush = (SolidColorBrush)G.MAIN.resource["UserMaintenace" ]; break;
                    case EN_AUTHORITY.EN_ENGINEER   : UserBrush = (SolidColorBrush)G.MAIN.resource["UserEngineer"   ]; break;
                }
                OnPropertyChanged(); 
            } 
        }

        SolidColorBrush brsUserBrush = Brushes.Transparent;
        public SolidColorBrush UserBrush { get { return brsUserBrush; } set { brsUserBrush = value; OnPropertyChanged(); } }

        string strAuthLevel = "";
        public string AuthLevel { get { return strAuthLevel; } set { strAuthLevel = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// Page_Authority.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Page_Authority : Page
    {
        public Page_Authority()
        {
            InitializeComponent();
        }

        private void bn_Authority_Click(object sender, RoutedEventArgs e)
        {
            fn_ChangeAuthority();
        }

        public void fn_ChangeAuthority(bool bShowMessage = true)
        {
            Win_Authority win = new Win_Authority();
            win.Owner = G.MAIN;
            win.SelLevel(datacontext.Auth);
            if (win.ShowDialog() == true)
            {
                G.USERLEVEL = datacontext.Auth = win.Level;
                G.MAIN.fn_SetupUpdateAutority();
                if (bShowMessage) G.WriteLog($"Authority Changed. [{datacontext.AuthLevel}]");
            }
            else
            {
                if (bShowMessage) G.WriteLog($"Authority Change Fail. [{datacontext.AuthLevel}]", true);
            }
        }
    }
}
