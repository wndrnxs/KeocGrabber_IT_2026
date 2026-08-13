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
using System.Windows.Shapes;

namespace KeocGrabber
{
    public enum EN_AUTHORITY
    { 
        EN_OPERATOR = 0,
        EN_MAINTENANCE,
        EN_ENGINEER
    }

    public class Win_AutorityDataContext : MVVMBase.IPropertyChanged
    {
        string strLevelPassword = "";
        double dThickOp = 1;
        double dThickMa = 1;
        double dThickEn = 1;
        FontWeight fwWeightOp = FontWeights.Normal;
        FontWeight fwWeightMa = FontWeights.Normal;
        FontWeight fwWeightEn = FontWeights.Normal;

        public double ThickOP { get { return dThickOp; } set { dThickOp = value; OnPropertyChanged(); } }
        public double ThickMA { get { return dThickMa; } set { dThickMa = value; OnPropertyChanged(); } }
        public double ThickEN {  get { return dThickEn; } set { dThickEn = value; OnPropertyChanged(); } }

        public FontWeight WeightOP { get { return fwWeightOp; } set { fwWeightOp = value; OnPropertyChanged(); } }
        public FontWeight WeightMA { get { return fwWeightMa; } set { fwWeightMa = value; OnPropertyChanged(); } }
        public FontWeight WeightEN { get { return fwWeightEn; } set { fwWeightEn = value; OnPropertyChanged(); } }

        public string LevelPassword { get { return strLevelPassword; } set { strLevelPassword = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// Win_Authority.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Win_Authority : Window
    {
        EN_AUTHORITY level;

        public EN_AUTHORITY Level { get { return level; } }
        public Win_Authority()
        {
            InitializeComponent();
        }

        private void bnInit()
        {
            datacontext.ThickOP = 1;
            datacontext.ThickMA = 1;
            datacontext.ThickEN = 1;

            datacontext.WeightOP = FontWeights.Normal;
            datacontext.WeightMA = FontWeights.Normal;
            datacontext.WeightEN = FontWeights.Normal;
        }

        public void SelLevel(EN_AUTHORITY lv)
        {
            bnInit();
            level = lv;
            switch (lv)
            {
                case EN_AUTHORITY.EN_OPERATOR   : datacontext.ThickOP = 3; datacontext.WeightOP = FontWeights.Bold; break;
                case EN_AUTHORITY.EN_MAINTENANCE: datacontext.ThickMA = 3; datacontext.WeightMA = FontWeights.Bold; break;
                case EN_AUTHORITY.EN_ENGINEER   : datacontext.ThickEN = 3; datacontext.WeightEN = FontWeights.Bold; break;
            }
        }

        private void bn_Op_Click(object sender, RoutedEventArgs e)
        {
            SelLevel(EN_AUTHORITY.EN_OPERATOR);
        }

        private void bn_Ma_Click(object sender, RoutedEventArgs e)
        {
            SelLevel(EN_AUTHORITY.EN_MAINTENANCE);
        }

        private void bn_En_Click(object sender, RoutedEventArgs e)
        {
            SelLevel(EN_AUTHORITY.EN_ENGINEER);
        }

        private void fn_Check()
        {
            bool bOK = false;
            datacontext.LevelPassword = pb_password.Password;
            switch (level)
            {
                case EN_AUTHORITY.EN_OPERATOR   : bOK = datacontext.LevelPassword == Define.PASSWORD_OP; break;
                case EN_AUTHORITY.EN_MAINTENANCE: bOK = datacontext.LevelPassword == Define.PASSWORD_MA; break;
                case EN_AUTHORITY.EN_ENGINEER   : bOK = datacontext.LevelPassword == Define.PASSWORD_EN; break;
            }
            if (!bOK)
            {
                level = G.USERLEVEL; // 되돌리기.
                MessageBox.Show(this, $"{level.ToString().Replace("EN_", "")} Login Fail.","User Login", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show(this, $"{level.ToString().Replace("EN_", "")} Login Success.", "User Login", MessageBoxButton.OK, MessageBoxImage.Information);
            }

        }

        private void bn_Ok_Click(object sender, RoutedEventArgs e)
        {
            fn_Check();
            this.DialogResult = true;
        }

        private void bn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void pb_password_KeyDown(object sender, KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.Enter  : bn_Ok_Click       (null, null); break;
                case Key.Escape : bn_Cancel_Click   (null, null); break;
            }
        }
    }
}
