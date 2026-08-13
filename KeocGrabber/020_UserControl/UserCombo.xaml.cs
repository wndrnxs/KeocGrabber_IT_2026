/*
 *******************************************************************************
 * 해당 소스는 현장 유지 보수 외에 다른 목적의 사용을 금합니다.
 * - 주식회사 태루 -
 *******************************************************************************
 */
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FalconWpf
{
    /// <summary>
    /// UserControlTEset.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class UserCombo : UserControl
    {

        #region Content
        public static readonly DependencyProperty UCBItemSourceProperty
           = DependencyProperty.Register(
                 "UCBItemSource",
                 typeof(object),
                 typeof(UserControl),
                 new PropertyMetadata(null)
             );

        public object UCBItemSource { get { return GetValue(UCBItemSourceProperty); } set { SetValue(UCBItemSourceProperty, value); } }

        
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UCBTitleProperty
           = DependencyProperty.Register(
                 "UCBTitle",
                 typeof(string),
                 typeof(UserControl),
                 new PropertyMetadata("Title :")
             );

        public string UCBTitle
        {
            get { return (string)GetValue(UCBTitleProperty); }
            set { SetValue(UCBTitleProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UCBValueProperty
           = DependencyProperty.Register(
                 "UCBValue",
                 typeof(string),
                 typeof(UserControl),
                 new PropertyMetadata("0")
             );

        public string UCBValue
        {
            get { return (string)GetValue(UCBValueProperty); }
            set { SetValue(UCBValueProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UCBUnitProperty
           = DependencyProperty.Register(
                 "UCBUnit",
                 typeof(string),
                 typeof(UserControl),
                 new PropertyMetadata("mm")
             );
        public string UCBUnit
        {
            get { return (string)GetValue(UCBUnitProperty); }
            set { SetValue(UCBUnitProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UCBSelectedIndexProperty
            = DependencyProperty.Register(
                "UCBSelectedIndex",
                typeof(int),
                typeof(UserControl),
                new PropertyMetadata(-1)
                );

        public int UCBSelectedIndex
        {
            get { return (int)GetValue(UCBSelectedIndexProperty); }
            set { SetValue(UCBSelectedIndexProperty, value); }
        }
        //---------------------------------------------------------------------------
        #endregion

        #region Layout
        //---------------------------------------------------------------------------
        // Width
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UCBTitleWidthProperty
           = DependencyProperty.Register(
                 "UCBTitleWidth",
                 typeof(string),
                 typeof(UserControl),
                 new PropertyMetadata("1*")
             );
        public string UCBTitleWidth
        {
            get { return (string)GetValue(UCBTitleWidthProperty); }
            set { SetValue(UCBTitleWidthProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UCBValueWidthProperty
           = DependencyProperty.Register(
                 "UCBValueWidth",
                 typeof(string),
                 typeof(UserControl),
                 new PropertyMetadata("1*")
             );

        public string UCBValueWidth
        {
            get { return (string)GetValue(UCBValueWidthProperty); }
            set { SetValue(UCBValueWidthProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UCBUnitWidthProperty
           = DependencyProperty.Register(
                 "UCBUnitWidth",
                 typeof(string),
                 typeof(UserControl),
                 new PropertyMetadata("1*")
             );

        public string UCBUnitWidth
        {
            get { return (string)GetValue(UCBUnitWidthProperty); }
            set { SetValue(UCBUnitWidthProperty, value); }
        }
        //---------------------------------------------------------------------------
        // Content Alignment
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UCBTitleHorizontalAlignProperty
            = DependencyProperty.Register(
                "UCBTitleHorizontalAlign",
                typeof(HorizontalAlignment),
                typeof(UserControl),
                new PropertyMetadata(HorizontalAlignment.Center)
                );

        public HorizontalAlignment UCBTitleHorizontalAlign
        {
            get { return (HorizontalAlignment)GetValue(UCBTitleHorizontalAlignProperty); }
            set { SetValue(UCBTitleHorizontalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UCBTitleVerticalAlignProperty
            = DependencyProperty.Register(
                "UCBTitleVerticalAlign",
                typeof(VerticalAlignment),
                typeof(UserControl),
                new PropertyMetadata(VerticalAlignment.Center)
                );

        public VerticalAlignment UCBTitleVerticalAlign
        {
            get { return (VerticalAlignment)GetValue(UCBTitleVerticalAlignProperty); }
            set { SetValue(UCBTitleVerticalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UCBValueHorizontalAlignProperty
            = DependencyProperty.Register(
                "UCBValueHorizontalAlign",
                typeof(HorizontalAlignment),
                typeof(UserControl),
                new PropertyMetadata(HorizontalAlignment.Stretch)
                );

        public HorizontalAlignment UCBValueHorizontalAlign
        {
            get { return (HorizontalAlignment)GetValue(UCBValueHorizontalAlignProperty); }
            set { SetValue(UCBValueHorizontalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UCBValueVerticalAlignProperty
            = DependencyProperty.Register(
                "UCBValueVerticalAlign",
                typeof(VerticalAlignment),
                typeof(UserControl),
                new PropertyMetadata(VerticalAlignment.Center)
                );

        public VerticalAlignment UCBValueVerticalAlign
        {
            get { return (VerticalAlignment)GetValue(UCBValueVerticalAlignProperty); }
            set { SetValue(UCBValueVerticalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UCBValueContentHorizontalAlignProperty
            = DependencyProperty.Register(
                "UCBValueContentHorizontalAlign",
                typeof(HorizontalAlignment),
                typeof(UserControl),
                new PropertyMetadata(HorizontalAlignment.Center)
                );

        public HorizontalAlignment UCBValueContentHorizontalAlign
        {
            get { return (HorizontalAlignment)GetValue(UCBValueContentHorizontalAlignProperty); }
            set { SetValue(UCBValueContentHorizontalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UCBValueContentVerticalAlignProperty
            = DependencyProperty.Register(
                "UCBValueContentVerticalAlign",
                typeof(VerticalAlignment),
                typeof(UserControl),
                new PropertyMetadata(VerticalAlignment.Center)
                );

        public VerticalAlignment UCBValueContentVerticalAlign
        {
            get { return (VerticalAlignment)GetValue(UCBValueContentVerticalAlignProperty); }
            set { SetValue(UCBValueContentVerticalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UCBUnitHorizontalAlignProperty
            = DependencyProperty.Register(
                "UCBUnitHorizontalAlign",
                typeof(HorizontalAlignment),
                typeof(UserControl),
                new PropertyMetadata(HorizontalAlignment.Center)
                );

        public HorizontalAlignment UCBUnitHorizontalAlign
        {
            get { return (HorizontalAlignment)GetValue(UCBUnitHorizontalAlignProperty); }
            set { SetValue(UCBUnitHorizontalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UCBUnitVerticalAlignProperty
            = DependencyProperty.Register(
                "UCBUnitVerticalAlign",
                typeof(VerticalAlignment),
                typeof(UserControl),
                new PropertyMetadata(VerticalAlignment.Center)
                );

        public VerticalAlignment UCBUnitVerticalAlign
        {
            get { return (VerticalAlignment)GetValue(UCBUnitVerticalAlignProperty); }
            set { SetValue(UCBUnitVerticalAlignProperty, value); }
        }
        //---------------------------------------------------------------------------
        // Border Thickness
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UCBTitleBorderThicknessProperty
            = DependencyProperty.Register(
                "UCBTitleBorderThickness",
                typeof(Thickness),
                typeof(UserControl),
                new PropertyMetadata(new Thickness(0))
                );

        public Thickness UCBTitleBorderThickness
        {
            get { return (Thickness)GetValue(UCBTitleBorderThicknessProperty); }
            set { SetValue(UCBTitleBorderThicknessProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UCBValueBorderThicknessProperty
            = DependencyProperty.Register(
                "UCBValueBorderThickness",
                typeof(Thickness),
                typeof(UserControl),
                new PropertyMetadata(new Thickness(0))
                );

        public Thickness UCBValueBorderThickness
        {
            get { return (Thickness)GetValue(UCBValueBorderThicknessProperty); }
            set { SetValue(UCBValueBorderThicknessProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UCBUnitBorderThicknessProperty
            = DependencyProperty.Register(
                "UCBUnitBorderThickness",
                typeof(Thickness),
                typeof(UserControl),
                new PropertyMetadata(new Thickness(0))
                );

        public Thickness UCBUnitBorderThickness
        {
            get { return (Thickness)GetValue(UCBUnitBorderThicknessProperty); }
            set { SetValue(UCBUnitBorderThicknessProperty, value); }
        }
        //---------------------------------------------------------------------------
        #endregion

        #region Color
        //---------------------------------------------------------------------------
        // Border Brush
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UCBTitleBorderBrushProperty
            = DependencyProperty.Register(
                "UCBTitleBorderBrush",
                typeof(Brush),
                typeof(UserControl),
                new PropertyMetadata(Brushes.Transparent)
                );

        public Brush UCBTitleBorderBrush
        {
            get { return (Brush)GetValue(UCBTitleBorderBrushProperty); }
            set { SetValue(UCBTitleBorderBrushProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UCBValueBorderBrushProperty
            = DependencyProperty.Register(
                "UCBValueBorderBrush",
                typeof(Brush),
                typeof(UserControl),
                new PropertyMetadata(Brushes.Transparent)
                );

        public Brush UCBValueBorderBrush
        {
            get { return (Brush)GetValue(UCBValueBorderBrushProperty); }
            set { SetValue(UCBValueBorderBrushProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UCBUnitBorderBrushProperty
            = DependencyProperty.Register(
                "UCBUnitBorderBrush",
                typeof(Brush),
                typeof(UserControl),
                new PropertyMetadata(Brushes.Transparent)
                );

        public Brush UCBUnitBorderBrush
        {
            get { return (Brush)GetValue(UCBUnitBorderBrushProperty); }
            set { SetValue(UCBUnitBorderBrushProperty, value); }
        }
        //---------------------------------------------------------------------------
        // BackGround
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UCBTitleBackgroundProperty
            = DependencyProperty.Register(
                "UCBTitleBackground",
                typeof(Brush),
                typeof(UserControl),
                new PropertyMetadata(Brushes.Transparent)
                );

        public Brush UCBTitleBackground
        {
            get { return (Brush)GetValue(UCBTitleBackgroundProperty); }
            set { SetValue(UCBTitleBackgroundProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UCBValueBackgroundProperty
            = DependencyProperty.Register(
                "UCBValueBackground",
                typeof(Brush),
                typeof(UserControl),
                new PropertyMetadata(Brushes.Transparent)
                );

        public Brush UCBValueBackground
        {
            get { return (Brush)GetValue(UCBValueBackgroundProperty); }
            set { SetValue(UCBValueBackgroundProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UCBUnitBackgroundProperty
            = DependencyProperty.Register(
                "UCBUnitBackground",
                typeof(Brush),
                typeof(UserControl),
                new PropertyMetadata(Brushes.Transparent)
                );

        public Brush UCBUnitBackground
        {
            get { return (Brush)GetValue(UCBUnitBackgroundProperty); }
            set { SetValue(UCBUnitBackgroundProperty, value); }
        }
        //---------------------------------------------------------------------------
        // Foreground
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UCBTitleForegroundProperty
           = DependencyProperty.Register(
               "UCBTitleForeground",
               typeof(Brush),
               typeof(UserControl),
               new PropertyMetadata(Brushes.Black)
               );

        public Brush UCBTitleForeground
        {
            get { return (Brush)GetValue(UCBTitleForegroundProperty); }
            set { SetValue(UCBTitleForegroundProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UCBValueForegroundProperty
           = DependencyProperty.Register(
               "UCBValueForeground",
               typeof(Brush),
               typeof(UserControl),
               new PropertyMetadata(Brushes.Black)
               );

        public Brush UCBValueForeground
        {
            get { return (Brush)GetValue(UCBValueForegroundProperty); }
            set { SetValue(UCBValueForegroundProperty, value); }
        }
        //---------------------------------------------------------------------------
        public static readonly DependencyProperty UCBUnitForegroundProperty
           = DependencyProperty.Register(
               "UCBUnitForeground",
               typeof(Brush),
               typeof(UserControl),
               new PropertyMetadata(Brushes.Black)
               );

        public Brush UCBUnitForeground
        {
            get { return (Brush)GetValue(UCBUnitForegroundProperty); }
            set { SetValue(UCBUnitForegroundProperty, value); }
        }
        //---------------------------------------------------------------------------

        #endregion

        public ComboBox combo = null;
        public event SelectionChangedEventHandler UCSelectionChanged;
        public UserCombo()
        {
            InitializeComponent();
            combo = uc_Combo;
        }

        public void Add(object obj)
        {
            uc_Combo.Items.Add(obj);
        }

        public void Clear()
        {
            uc_Combo.Items.Clear();
        }

        private void Uc_Combo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UCSelectionChanged != null)
            {
                UCSelectionChanged(sender, e);
            }
        }
    }
}
