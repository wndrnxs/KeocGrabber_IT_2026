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

namespace FalconWpf
{
    /// <summary>
    /// UserProgress.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class UserProgress : ProgressBar
    {
        public static readonly DependencyProperty PrgLabelProperty =
            DependencyProperty.Register("PrgLabel",
            typeof(string),
            typeof(UserProgress),
            new PropertyMetadata(""));

        public string PrgLabel
        {
            get { return (string)GetValue(PrgLabelProperty); }
            set { SetValue(PrgLabelProperty, value); }
        }

        public static readonly DependencyProperty PrgLabelWidthProperty =
            DependencyProperty.Register("PrgLabelWidth",
            typeof(string),
            typeof(UserProgress),
            new PropertyMetadata("Auto"));

        public string PrgLabelWidth
        {
            get { return (string)GetValue(PrgLabelWidthProperty); }
            set { SetValue(PrgLabelWidthProperty, value); }
        }

        public static readonly DependencyProperty PrgLabelHorizontalAlignmentProperty =
            DependencyProperty.Register("PrgLabelHorizontalAlignment",
            typeof(HorizontalAlignment),
            typeof(UserProgress),
            new PropertyMetadata(HorizontalAlignment.Center));

        public HorizontalAlignment PrgLabelHorizontalAlignment
        {
            get { return (HorizontalAlignment)GetValue(PrgLabelHorizontalAlignmentProperty); }
            set { SetValue(PrgLabelHorizontalAlignmentProperty, value); }
        }



        public UserProgress()
        {
            InitializeComponent();
        }
    }
}
