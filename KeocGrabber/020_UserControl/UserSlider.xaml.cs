/*
 *******************************************************************************
 * 해당 소스는 현장 유지 보수 외에 다른 목적의 사용을 금합니다.
 * - 주식회사 태루 -
 *******************************************************************************
 */
using KeocGrabber;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace FalconWpf
{
    /// <summary>
    /// UserSlider.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class UserSlider : UserControl
    {
        #region Content
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty USTitleProperty
            = DependencyProperty.Register(
                  "USTitle",
                  typeof(string),
                  typeof(UserControl),
                  new PropertyMetadata("param")
              );

        public string USTitle
        {
            get { return (string)GetValue(USTitleProperty); }
            set { SetValue(USTitleProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty USValueProperty
            = DependencyProperty.Register(
                  "USValue",
                  typeof(double),
                  typeof(UserControl),
                  new PropertyMetadata(0.0)
              );

        public double USValue
        {
            get 
            {
                return (double)GetValue(USValueProperty); 
            }
            set
            {
                SetValue(USValueProperty, value);
            }
        }
        //---------------------------------------------------------------------------
        #endregion

        #region Layout
        //---------------------------------------------------------------------------
        // Width
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty USTitleWidthProperty
            = DependencyProperty.Register(
                  "USTitleWidth",
                  typeof(string),
                  typeof(UserControl),
                  new PropertyMetadata("1*")
              );

        public string USTitleWidth
        {
            get { return (string)GetValue(USTitleWidthProperty); }
            set { SetValue(USTitleWidthProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty USSliderWidthProperty
            = DependencyProperty.Register(
                  "USSliderWidth",
                  typeof(string),
                  typeof(UserControl),
                  new PropertyMetadata("1*")
              );

        public string USSliderWidth
        {
            get { return (string)GetValue(USSliderWidthProperty); }
            set { SetValue(USSliderWidthProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty USValueWidthProperty
            = DependencyProperty.Register(
                  "USValueWidth",
                  typeof(string),
                  typeof(UserControl),
                  new PropertyMetadata("1*")
              );

        public string USValueWidth
        {
            get { return (string)GetValue(USValueWidthProperty); }
            set { SetValue(USValueWidthProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty USValueHeightProperty
            = DependencyProperty.Register(
                  "USValueHeight",
                  typeof(string),
                  typeof(UserControl),
                  new PropertyMetadata("25")
              );

        public string USValueHeight
        {
            get { return (string)GetValue(USValueHeightProperty); }
            set { SetValue(USValueHeightProperty, value); }
        }
        //---------------------------------------------------------------------------
        // Content Alignment
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty USTitleContentHorizontalAlignProperty
            = DependencyProperty.Register(
                "USTitleContentHorizontalAlign",
                typeof(HorizontalAlignment),
                typeof(UserControl),
                new PropertyMetadata(HorizontalAlignment.Center)
                );

        public HorizontalAlignment USTitleContentHorizontalAlign
        {
            get { return (HorizontalAlignment)GetValue(USTitleContentHorizontalAlignProperty); }
            set { SetValue(USTitleContentHorizontalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty USTitleContentVerticalAlignProperty
            = DependencyProperty.Register(
                "USTitleContentVerticalAlign",
                typeof(VerticalAlignment),
                typeof(UserControl),
                new PropertyMetadata(VerticalAlignment.Center)
                );

        public VerticalAlignment USTitleContentVerticalAlign
        {
            get { return (VerticalAlignment)GetValue(USTitleContentVerticalAlignProperty); }
            set { SetValue(USTitleContentVerticalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty USSliderContentHorizontalAlignProperty
            = DependencyProperty.Register(
                "USSliderContentHorizontalAlign",
                typeof(HorizontalAlignment),
                typeof(UserControl),
                new PropertyMetadata(HorizontalAlignment.Center)
                );

        public HorizontalAlignment USSliderContentHorizontalAlign
        {
            get { return (HorizontalAlignment)GetValue(USSliderContentHorizontalAlignProperty); }
            set { SetValue(USSliderContentHorizontalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty USSliderContentVerticalAlignProperty
            = DependencyProperty.Register(
                "USSliderContentVerticalAlign",
                typeof(VerticalAlignment),
                typeof(UserControl),
                new PropertyMetadata(VerticalAlignment.Center)
                );

        public VerticalAlignment USSliderContentVerticalAlign
        {
            get { return (VerticalAlignment)GetValue(USSliderContentVerticalAlignProperty); }
            set { SetValue(USSliderContentVerticalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty USValueContentHorizontalAlignProperty
            = DependencyProperty.Register(
                "USValueContentHorizontalAlign",
                typeof(HorizontalAlignment),
                typeof(UserControl),
                new PropertyMetadata(HorizontalAlignment.Center)
                );

        public HorizontalAlignment USValueContentHorizontalAlign
        {
            get { return (HorizontalAlignment)GetValue(USValueContentHorizontalAlignProperty); }
            set { SetValue(USValueContentHorizontalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty USValueContentVerticalAlignProperty
            = DependencyProperty.Register(
                "USValueContentVerticalAlign",
                typeof(VerticalAlignment),
                typeof(UserControl),
                new PropertyMetadata(VerticalAlignment.Center)
                );

        public VerticalAlignment USValueContentVerticalAlign
        {
            get { return (VerticalAlignment)GetValue(USValueContentVerticalAlignProperty); }
            set { SetValue(USValueContentVerticalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        // Alignment
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty USTitleHorizontalAlignProperty
            = DependencyProperty.Register(
                "USTitleHorizontalAlign",
                typeof(HorizontalAlignment),
                typeof(UserControl),
                new PropertyMetadata(HorizontalAlignment.Center)
                );

        public HorizontalAlignment USTitleHorizontalAlign
        {
            get { return (HorizontalAlignment)GetValue(USTitleHorizontalAlignProperty); }
            set { SetValue(USTitleHorizontalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty USTitleVerticalAlignProperty
            = DependencyProperty.Register(
                "USTitleVerticalAlign",
                typeof(VerticalAlignment),
                typeof(UserControl),
                new PropertyMetadata(VerticalAlignment.Center)
                );

        public VerticalAlignment USTitleVerticalAlign
        {
            get { return (VerticalAlignment)GetValue(USTitleVerticalAlignProperty); }
            set { SetValue(USTitleVerticalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty USSliderHorizontalAlignProperty
            = DependencyProperty.Register(
                "USSliderHorizontalAlign",
                typeof(HorizontalAlignment),
                typeof(UserControl),
                new PropertyMetadata(HorizontalAlignment.Center)
                );

        public HorizontalAlignment USSliderHorizontalAlign
        {
            get { return (HorizontalAlignment)GetValue(USSliderHorizontalAlignProperty); }
            set { SetValue(USSliderHorizontalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty USSliderVerticalAlignProperty
            = DependencyProperty.Register(
                "USSliderVerticalAlign",
                typeof(VerticalAlignment),
                typeof(UserControl),
                new PropertyMetadata(VerticalAlignment.Center)
                );

        public VerticalAlignment USSliderVerticalAlign
        {
            get { return (VerticalAlignment)GetValue(USSliderVerticalAlignProperty); }
            set { SetValue(USSliderVerticalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty USValueHorizontalAlignProperty
            = DependencyProperty.Register(
                "USValueHorizontalAlign",
                typeof(HorizontalAlignment),
                typeof(UserControl),
                new PropertyMetadata(HorizontalAlignment.Stretch)
                );

        public HorizontalAlignment USValueHorizontalAlign
        {
            get { return (HorizontalAlignment)GetValue(USValueHorizontalAlignProperty); }
            set { SetValue(USValueHorizontalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty USValueVerticalAlignProperty
            = DependencyProperty.Register(
                "USValueVerticalAlign",
                typeof(VerticalAlignment),
                typeof(UserControl),
                new PropertyMetadata(VerticalAlignment.Center)
                );

        public VerticalAlignment USValueVerticalAlign
        {
            get { return (VerticalAlignment)GetValue(USValueVerticalAlignProperty); }
            set { SetValue(USValueVerticalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        // Border Thickness
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty USTitleBorderThicknessProperty
            = DependencyProperty.Register(
                "USTitleBorderThickness",
                typeof(Thickness),
                typeof(UserControl),
                new PropertyMetadata(new Thickness(0))
                );

        public Thickness USTitleBorderThickness
        {
            get { return (Thickness)GetValue(USTitleBorderThicknessProperty); }
            set { SetValue(USTitleBorderThicknessProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty USValueBorderThicknessProperty
            = DependencyProperty.Register(
                "USValueBorderThickness",
                typeof(Thickness),
                typeof(UserControl),
                new PropertyMetadata(new Thickness(1))
                );

        public Thickness USValueBorderThickness
        {
            get { return (Thickness)GetValue(USValueBorderThicknessProperty); }
            set { SetValue(USValueBorderThicknessProperty, value); }
        }
        //---------------------------------------------------------------------------
        #endregion

        #region Color
        //---------------------------------------------------------------------------
        // Border Brush
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty USTitleBorderBrushProperty
            = DependencyProperty.Register(
                "USTitleBorderBrush",
                typeof(Brush),
                typeof(UserControl),
                new PropertyMetadata(Brushes.Transparent)
                );

        public Brush USTitleBorderBrush
        {
            get { return (Brush)GetValue(USTitleBorderBrushProperty); }
            set { SetValue(USTitleBorderBrushProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty USValueBorderBrushProperty
            = DependencyProperty.Register(
                "USValueBorderBrush",
                typeof(Brush),
                typeof(UserControl),
                new PropertyMetadata(Brushes.Black)
                );

        public Brush USValueBorderBrush
        {
            get { return (Brush)GetValue(USValueBorderBrushProperty); }
            set { SetValue(USValueBorderBrushProperty, value); }
        }
        //---------------------------------------------------------------------------
        // BackGround
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty USTitleBackgroundProperty
            = DependencyProperty.Register(
                "USTitleBackground",
                typeof(Brush),
                typeof(UserControl),
                new PropertyMetadata(Brushes.Transparent)
                );

        public Brush USTitleBackground
        {
            get { return (Brush)GetValue(USTitleBackgroundProperty); }
            set { SetValue(USTitleBackgroundProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty USValueBackgroundProperty
            = DependencyProperty.Register(
                "USValueBackground",
                typeof(Brush),
                typeof(UserControl),
                new PropertyMetadata(Brushes.White)
                );

        public Brush USValueBackground
        {
            get { return (Brush)GetValue(USValueBackgroundProperty); }
            set { SetValue(USValueBackgroundProperty, value); }
        }
        //---------------------------------------------------------------------------
        // Foreground
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty USTitleForegroundProperty
           = DependencyProperty.Register(
               "USTitleForeground",
               typeof(Brush),
               typeof(UserControl),
               new PropertyMetadata(Brushes.Black)
               );

        public Brush USTitleForeground
        {
            get { return (Brush)GetValue(USTitleForegroundProperty); }
            set { SetValue(USTitleForegroundProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty USValueForegroundProperty
           = DependencyProperty.Register(
               "USValueForeground",
               typeof(Brush),
               typeof(UserControl),
               new PropertyMetadata(Brushes.Black)
               );

        public Brush USValueForeground
        {
            get { return (Brush)GetValue(USValueForegroundProperty); }
            set { SetValue(USValueForegroundProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty USUnitForegroundProperty
           = DependencyProperty.Register(
               "USUnitForeground",
               typeof(Brush),
               typeof(UserControl),
               new PropertyMetadata(Brushes.Black)
               );

        public Brush USUnitForeground
        {
            get { return (Brush)GetValue(USUnitForegroundProperty); }
            set { SetValue(USUnitForegroundProperty, value); }
        }
        //---------------------------------------------------------------------------
        #endregion

        #region Data
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty USMaximumProperty
            = DependencyProperty.Register(
                  "USMaximum",
                  typeof(double),
                  typeof(UserControl),
                  new PropertyMetadata(0.0)
              );

        public double USMaximum
        {
            get { return (double)GetValue(USMaximumProperty); }
            set { SetValue(USMaximumProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty USMinimumProperty
            = DependencyProperty.Register(
                  "USMinimum",
                  typeof(double),
                  typeof(UserControl),
                  new PropertyMetadata(0.0)
              );

        public double USMinimum
        {
            get { return (double)GetValue(USMinimumProperty); }
            set { SetValue(USMinimumProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty USTickFrequencyProperty
            = DependencyProperty.Register(
                  "USTickFrequency",
                  typeof(double),
                  typeof(UserControl),
                  new PropertyMetadata(5.0)
              );

        public double USTickFrequency
        {
            get { return (double)GetValue(USTickFrequencyProperty); }
            set { SetValue(USTickFrequencyProperty, value); }
        }
        //---------------------------------------------------------------------------
        #endregion

        public event RoutedPropertyChangedEventHandler<double> USValueChanged = null;
        public UserSlider()
        {
            InitializeComponent();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (USValueChanged != null)
            {
                USValueChanged(sender, e);
            }
        }

        private void Textbox_Down(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
//             LPI.KeyPad keypad = new LPI.KeyPad();
//             keypad.Owner = Application.Current.MainWindow;
//             keypad.WindowStartupLocation = WindowStartupLocation.CenterOwner;
//             keypad.keypadResource.KPPrevValue = USValue.ToString();
//             if (keypad.ShowDialog() == true)
//             {
//                 try
//                 {
//                     USValue = Convert.ToDouble(keypad.keypadResource.KPValue);
//                 }
//                 catch (Exception ex)
//                 {
//                     Console.WriteLine(ex.Message);
//                 }
//             }
        }

        private void uc_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            UserSlider us = sender as UserSlider;
            if (e.Key == Key.Enter)
            {
                DependencyObject scope = FocusManager.GetFocusScope(G.MAIN);
                FocusManager.SetFocusedElement(scope, G.MAIN);
                if (us != null)
                {
                    if (us.USValue < us.USMinimum) us.USValue = us.USMinimum;
                    if (us.USValue > us.USMaximum) us.USValue = us.USMaximum;
                }
            }
        }
    }
}
