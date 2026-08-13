/*
 *******************************************************************************
 * 해당 소스는 현장 유지 보수 외에 다른 목적의 사용을 금합니다.
 * - 주식회사 태루 -
 *******************************************************************************
 */
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace FalconWpf
{
    /// <summary>
    /// UserControlTEset.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class UserParam : UserControl
    {
        public event TextChangedEventHandler UPValueChanged = null;

        #region Content
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UPTitleProperty
           = DependencyProperty.Register(
                 "UPTitle",
                 typeof(string),
                 typeof(UserControl),
                 new PropertyMetadata("Title :")
             );

        public string UPTitle
        {
            get { return (string)GetValue(UPTitleProperty); }
            set { SetValue(UPTitleProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UPValueProperty
           = DependencyProperty.Register(
                 "UPValue",
                 typeof(string),
                 typeof(UserControl),
                 new PropertyMetadata("0")
             );

        public string UPValue
        {
            get { return (string)GetValue(UPValueProperty); }
            set { SetValue(UPValueProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UPUnitProperty
           = DependencyProperty.Register(
                 "UPUnit",
                 typeof(string),
                 typeof(UserControl),
                 new PropertyMetadata("mm")
             );
        public string UPUnit
        {
            get { return (string)GetValue(UPUnitProperty); }
            set { SetValue(UPUnitProperty, value); }
        }
        //---------------------------------------------------------------------------
        #endregion

        #region Layout
        //---------------------------------------------------------------------------
        // Width
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UPTitleWidthProperty
           = DependencyProperty.Register(

                 "UPTitleWidth",
                 typeof(string),
                 typeof(UserControl),
                 new PropertyMetadata("1*")
             );
        public string UPTitleWidth
        {
            get { return (string)GetValue(UPTitleWidthProperty); }
            set { SetValue(UPTitleWidthProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UPValueWidthProperty
           = DependencyProperty.Register(
                 "UPValueWidth",
                 typeof(string),
                 typeof(UserControl),
                 new PropertyMetadata("1*")
             );

        public string UPValueWidth
        {
            get { return (string)GetValue(UPValueWidthProperty); }
            set { SetValue(UPValueWidthProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UPUnitWidthProperty
           = DependencyProperty.Register(
                 "UPUnitWidth",
                 typeof(string),
                 typeof(UserControl),
                 new PropertyMetadata("1*")
             );

        public string UPUnitWidth
        {
            get { return (string)GetValue(UPUnitWidthProperty); }
            set { SetValue(UPUnitWidthProperty, value); }
        }
        //---------------------------------------------------------------------------
        // Content Alignment
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UPTitleHorizontalAlignProperty
            = DependencyProperty.Register(
                "UPTitleHorizontalAlign",
                typeof(HorizontalAlignment),
                typeof(UserControl),
                new PropertyMetadata(HorizontalAlignment.Stretch)
                );

        public HorizontalAlignment UPTitleHorizontalAlign
        {
            get { return (HorizontalAlignment)GetValue(UPTitleHorizontalAlignProperty); }
            set { SetValue(UPTitleHorizontalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UPTitleVerticalAlignProperty
            = DependencyProperty.Register(
                "UPTitleVerticalAlign",
                typeof(VerticalAlignment),
                typeof(UserControl),
                new PropertyMetadata(VerticalAlignment.Center)
                );

        public VerticalAlignment UPTitleVerticalAlign
        {
            get { return (VerticalAlignment)GetValue(UPTitleVerticalAlignProperty); }
            set { SetValue(UPTitleVerticalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UPTitleContentHorizontalAlignProperty
            = DependencyProperty.Register(
                "UPTitleContentHorizontalAlign",
                typeof(HorizontalAlignment),
                typeof(UserControl),
                new PropertyMetadata(HorizontalAlignment.Center)
                );

        public HorizontalAlignment UPTitleContentHorizontalAlign
        {
            get { return (HorizontalAlignment)GetValue(UPTitleContentHorizontalAlignProperty); }
            set { SetValue(UPTitleContentHorizontalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UPTitleContentVerticalAlignProperty
            = DependencyProperty.Register(
                "UPTitleContentVerticalAlign",
                typeof(VerticalAlignment),
                typeof(UserControl),
                new PropertyMetadata(VerticalAlignment.Center)
                );

        public VerticalAlignment UPTitleContentVerticalAlign
        {
            get { return (VerticalAlignment)GetValue(UPTitleContentVerticalAlignProperty); }
            set { SetValue(UPTitleContentVerticalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UPValueHorizontalAlignProperty
            = DependencyProperty.Register(
                "UPValueHorizontalAlign",
                typeof(HorizontalAlignment),
                typeof(UserControl),
                new PropertyMetadata(HorizontalAlignment.Stretch)
                );

        public HorizontalAlignment UPValueHorizontalAlign
        {
            get { return (HorizontalAlignment)GetValue(UPValueHorizontalAlignProperty); }
            set { SetValue(UPValueHorizontalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UPValueVerticalAlignProperty
            = DependencyProperty.Register(
                "UPValueVerticalAlign",
                typeof(VerticalAlignment),
                typeof(UserControl),
                new PropertyMetadata(VerticalAlignment.Center)
                );

        public VerticalAlignment UPValueVerticalAlign
        {
            get { return (VerticalAlignment)GetValue(UPValueVerticalAlignProperty); }
            set { SetValue(UPValueVerticalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UPValueContentHorizontalAlignProperty
            = DependencyProperty.Register(
                "UPValueContentHorizontalAlign",
                typeof(HorizontalAlignment),
                typeof(UserControl),
                new PropertyMetadata(HorizontalAlignment.Center)
                );

        public HorizontalAlignment UPValueContentHorizontalAlign
        {
            get { return (HorizontalAlignment)GetValue(UPValueContentHorizontalAlignProperty); }
            set { SetValue(UPValueContentHorizontalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UPValueContentVerticalAlignProperty
            = DependencyProperty.Register(
                "UPValueContentVerticalAlign",
                typeof(VerticalAlignment),
                typeof(UserControl),
                new PropertyMetadata(VerticalAlignment.Center)
                );

        public VerticalAlignment UPValueContentVerticalAlign
        {
            get { return (VerticalAlignment)GetValue(UPValueContentVerticalAlignProperty); }
            set { SetValue(UPValueContentVerticalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UPUnitHorizontalAlignProperty
            = DependencyProperty.Register(
                "UPUnitHorizontalAlign",
                typeof(HorizontalAlignment),
                typeof(UserControl),
                new PropertyMetadata(HorizontalAlignment.Stretch)
                );

        public HorizontalAlignment UPUnitHorizontalAlign
        {
            get { return (HorizontalAlignment)GetValue(UPUnitHorizontalAlignProperty); }
            set { SetValue(UPUnitHorizontalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UPUnitVerticalAlignProperty
            = DependencyProperty.Register(
                "UPUnitVerticalAlign",
                typeof(VerticalAlignment),
                typeof(UserControl),
                new PropertyMetadata(VerticalAlignment.Center)
                );

        public VerticalAlignment UPUnitVerticalAlign
        {
            get { return (VerticalAlignment)GetValue(UPUnitVerticalAlignProperty); }
            set { SetValue(UPUnitVerticalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UPUnitContentHorizontalAlignProperty
            = DependencyProperty.Register(
                "UPUnitContentHorizontalAlign",
                typeof(HorizontalAlignment),
                typeof(UserControl),
                new PropertyMetadata(HorizontalAlignment.Left)
                );

        public HorizontalAlignment UPUnitContentHorizontalAlign
        {
            get { return (HorizontalAlignment)GetValue(UPUnitContentHorizontalAlignProperty); }
            set { SetValue(UPUnitContentHorizontalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UPUnitContentVerticalAlignProperty
            = DependencyProperty.Register(
                "UPUnitContentVerticalAlign",
                typeof(VerticalAlignment),
                typeof(UserControl),
                new PropertyMetadata(VerticalAlignment.Center)
                );

        public VerticalAlignment UPUnitContentVerticalAlign
        {
            get { return (VerticalAlignment)GetValue(UPUnitContentVerticalAlignProperty); }
            set { SetValue(UPUnitContentVerticalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        // Border Thickness
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UPTitleBorderThicknessProperty
            = DependencyProperty.Register(
                "UPTitleBorderThickness",
                typeof(Thickness),
                typeof(UserControl),
                new PropertyMetadata(new Thickness(0))
                );

        public Thickness UPTitleBorderThickness
        {
            get { return (Thickness)GetValue(UPTitleBorderThicknessProperty); }
            set { SetValue(UPTitleBorderThicknessProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UPValueBorderThicknessProperty
            = DependencyProperty.Register(
                "UPValueBorderThickness",
                typeof(Thickness),
                typeof(UserControl),
                new PropertyMetadata(new Thickness(1))
                );

        public Thickness UPValueBorderThickness
        {
            get { return (Thickness)GetValue(UPValueBorderThicknessProperty); }
            set { SetValue(UPValueBorderThicknessProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UPUnitBorderThicknessProperty
            = DependencyProperty.Register(
                "UPUnitBorderThickness",
                typeof(Thickness),
                typeof(UserControl),
                new PropertyMetadata(new Thickness(0))
                );

        public Thickness UPUnitBorderThickness
        {
            get { return (Thickness)GetValue(UPUnitBorderThicknessProperty); }
            set { SetValue(UPUnitBorderThicknessProperty, value); }
        }
        //---------------------------------------------------------------------------
        #endregion

        #region Color
        //---------------------------------------------------------------------------
        // Border Brush
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UPTitleBorderBrushProperty
            = DependencyProperty.Register(
                "UPTitleBorderBrush",
                typeof(Brush),
                typeof(UserControl),
                new PropertyMetadata(Brushes.Transparent)
                );

        public Brush UPTitleBorderBrush
        {
            get { return (Brush)GetValue(UPTitleBorderBrushProperty); }
            set { SetValue(UPTitleBorderBrushProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UPValueBorderBrushProperty
            = DependencyProperty.Register(
                "UPValueBorderBrush",
                typeof(Brush),
                typeof(UserControl),
                new PropertyMetadata(Brushes.Gray)
                );

        public Brush UPValueBorderBrush
        {
            get { return (Brush)GetValue(UPValueBorderBrushProperty); }
            set { SetValue(UPValueBorderBrushProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UPUnitBorderBrushProperty
            = DependencyProperty.Register(
                "UPUnitBorderBrush",
                typeof(Brush),
                typeof(UserControl),
                new PropertyMetadata(Brushes.Transparent)
                );

        public Brush UPUnitBorderBrush
        {
            get { return (Brush)GetValue(UPUnitBorderBrushProperty); }
            set { SetValue(UPUnitBorderBrushProperty, value); }
        }
        //---------------------------------------------------------------------------
        // BackGround
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UPTitleBackgroundProperty
            = DependencyProperty.Register(
                "UPTitleBackground",
                typeof(Brush),
                typeof(UserControl),
                new PropertyMetadata(Brushes.Transparent)
                );

        public Brush UPTitleBackground
        {
            get { return (Brush)GetValue(UPTitleBackgroundProperty); }
            set { SetValue(UPTitleBackgroundProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UPValueBackgroundProperty
            = DependencyProperty.Register(
                "UPValueBackground",
                typeof(Brush),
                typeof(UserControl),
                new PropertyMetadata(Brushes.White)
                );

        public Brush UPValueBackground
        {
            get { return (Brush)GetValue(UPValueBackgroundProperty); }
            set { SetValue(UPValueBackgroundProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UPUnitBackgroundProperty
            = DependencyProperty.Register(
                "UPUnitBackground",
                typeof(Brush),
                typeof(UserControl),
                new PropertyMetadata(Brushes.Transparent)
                );

        public Brush UPUnitBackground
        {
            get { return (Brush)GetValue(UPUnitBackgroundProperty); }
            set { SetValue(UPUnitBackgroundProperty, value); }
        }
        //---------------------------------------------------------------------------
        // Foreground
        //---------------------------------------------------------------------------
//         public static readonly DependencyProperty UPTitleForegroundProperty
//            = DependencyProperty.Register(
//                "UPTitleForeground",
//                typeof(Brush),
//                typeof(UserControl),
//                new PropertyMetadata(Brushes.Black)
//                );
// 
//         public Brush UPTitleForeground
//         {
//             get { return (Brush)GetValue(UPTitleForegroundProperty); }
//             set { SetValue(UPTitleForegroundProperty, value); }
//         }
//         //---------------------------------------------------------------------------
//         public static readonly DependencyProperty UPValueForegroundProperty
//            = DependencyProperty.Register(
//                "UPValueForeground",
//                typeof(Brush),
//                typeof(UserControl),
//                new PropertyMetadata(Brushes.Black)
//                );
// 
//         public Brush UPValueForeground
//         {
//             get { return (Brush)GetValue(UPValueForegroundProperty); }
//             set { SetValue(UPValueForegroundProperty, value); }
//         }
//         //---------------------------------------------------------------------------
//         public static readonly DependencyProperty UPUnitForegroundProperty
//            = DependencyProperty.Register(
//                "UPUnitForeground",
//                typeof(Brush),
//                typeof(UserControl),
//                new PropertyMetadata(Brushes.Black)
//                );
// 
//         public Brush UPUnitForeground
//         {
//             get { return (Brush)GetValue(UPUnitForegroundProperty); }
//             set { SetValue(UPUnitForegroundProperty, value); }
//         }
//         //---------------------------------------------------------------------------

        #endregion

        #region Option
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UPIsReadOnlyProperty
            = DependencyProperty.Register(
                "UPIsReadOnly",
                typeof(bool),
                typeof(UserControl),
                new PropertyMetadata(false)
                );

        public bool UPIsReadOnly
        {
            get { return (bool)GetValue(UPIsReadOnlyProperty); }
            set { SetValue(UPIsReadOnlyProperty, value); }
        }

        public static readonly DependencyProperty UPUseKeypadProperty
            = DependencyProperty.Register(
                "UPUseKeypad",
                typeof(bool),
                typeof(UserControl),
                new PropertyMetadata(false)
                );

        public bool UPUseKeypad
        {
            get { return (bool)GetValue(UPUseKeypadProperty); }
            set { SetValue(UPUseKeypadProperty, value); }
        }
        //---------------------------------------------------------------------------
        #endregion
        public UserParam()
        {
            InitializeComponent();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(UPValueChanged != null)
            {
                UPValueChanged(sender, e);
            }
        }

        public void SourceUpdate()
        {
            BindingExpression be = tb_Value.GetBindingExpression(TextBox.TextProperty);
            be?.UpdateSource();
        }

        private void TextBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (UPUseKeypad)
            {
                //LPI.KeyPad keypad = new LPI.KeyPad();
                //keypad.Owner = Application.Current.MainWindow;
                //keypad.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                //keypad.keypadResource.KPPrevValue = UPValue;
                //if (keypad.ShowDialog() == true)
                //{
                //    UPValue = keypad.keypadResource.KPValue;
                //}
            }
        }
    }
}
