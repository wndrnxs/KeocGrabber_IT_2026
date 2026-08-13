/*
 *******************************************************************************
 * 해당 소스는 현장 유지 보수 외에 다른 목적의 사용을 금합니다.
 * - 주식회사 태루 -
 *******************************************************************************
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using MVVMBase;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace FalconWpf
{
    public class ImageViewInfo : IPropertyChanged
    {
        private int m_nWidth;
        private int m_nHeight;
        private int m_nChannel;
        private int m_nStride;
        private string m_strFileName = string.Empty;
        private string m_strPath = string.Empty;
        private string m_strExtension = string.Empty;
        private string m_strCreationTime = string.Empty;
        private double m_dDpiX;
        private double m_dDpiY;
        private double m_dImageScale;
        private byte m_ColorR;
        private byte m_ColorG;
        private byte m_ColorB;
        private Brush m_brsColorRGB;
        private bool m_bIsEditor = false;
        private bool m_bIsHalftone = false;
        private bool m_bIsDraw = false;
        private double m_dFps;

        private double m_dPosX;
        private double m_dPosY;
        public string FileName
        {
            get { return m_strFileName; }
            set { m_strFileName = value; OnPropertyChanged(); }
        }
        public int Width
        {
            get { return m_nWidth; }
            set { m_nWidth = value; OnPropertyChanged(); }
        }
        public int Height
        {
            get { return m_nHeight; }
            set { m_nHeight = value; OnPropertyChanged(); }
        }
        public int Channel
        {
            get { return m_nChannel; }
            set { m_nChannel = value; OnPropertyChanged(); }
        }
        public string Path
        {
            get { return m_strPath; }
            set { m_strPath = value; OnPropertyChanged(); }
        }
        public string Extension
        {
            get { return m_strExtension; }
            set { m_strExtension = value; OnPropertyChanged(); }
        }
        public string CreationTime
        {
            get { return m_strCreationTime; }
            set { m_strCreationTime = value; OnPropertyChanged(); }
        }
        public int Stride
        {
            get { return m_nStride; }
            set { m_nStride = value; OnPropertyChanged(); }
        }
        public double DpiX
        {
            get { return m_dDpiX; }
            set { m_dDpiX = value; OnPropertyChanged(); }
        }
        public double DpiY
        {
            get { return m_dDpiY; }
            set { m_dDpiY = value; OnPropertyChanged(); }
        }

        public double PosX
        {
            get { return m_dPosX; }
            set { m_dPosX = value; OnPropertyChanged(); }
        }

        public double PosY
        {
            get { return m_dPosY; }
            set { m_dPosY = value; OnPropertyChanged(); }
        }

        public double ImageScale
        {
            get { return m_dImageScale; }
            set { m_dImageScale = value; OnPropertyChanged(); }
        }

        public bool IsEditor
        {
            get { return m_bIsEditor; }
            set { m_bIsEditor = value; OnPropertyChanged(); }
        }

        public bool IsHalftone
        {
            get { return m_bIsHalftone; }
            set { m_bIsHalftone = value; OnPropertyChanged(); }
        }

        public bool IsDraw
        {
            get { return m_bIsDraw; }
            set { m_bIsDraw = value; OnPropertyChanged(); }
        }
        public byte ColorR
        {
            get { return m_ColorR; }
            set { m_ColorR = value; ColorRGB = new SolidColorBrush(Color.FromRgb(m_ColorR, m_ColorG, m_ColorB)); OnPropertyChanged(); }
        }
        public byte ColorG
        {
            get { return m_ColorG; }
            set { m_ColorG = value; OnPropertyChanged(); }
        }
        public byte ColorB
        {
            get { return m_ColorB; }
            set { m_ColorB = value; OnPropertyChanged(); }
        }

        public Brush ColorRGB
        {
            get { return m_brsColorRGB; }
            set { m_brsColorRGB = value; OnPropertyChanged(); }
        }

        public double Fps
        {
            get { return m_dFps; }
            set { m_dFps = value; OnPropertyChanged(); }
        }
    }

    /// <summary>
    /// Align.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ImageViewer : UserControl, IDisposable
    {
        #region DLL
        //const string strDLLPath = "./FrontInspCore.dll";
        //[DllImport(strDLLPath, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        //public extern static IntPtr libOpenImage(StringBuilder filename, ref int width, ref int height, ref int channel);
        #endregion

        #region Sync Other Control
        public Func<double, double, bool> delUpdateScroll = null;
        public Func<double, bool> delUpdateZoom = null;
        #endregion

        public Func<string, bool> delWriteLog = null;

        public Func<int, double, double, double, double, bool> delUpdateRect = null;
        public Func<bool> delImageOpenEvent = null;
        string Title = "ImageViewer";

        bool bUpdatePrevPoint = false;

        Image OverlapImage = new Image();

        #region define enum
        //---------------------------------------------------------------------------
        /**
        @enum   EnRoiMode	
        @brief	UserControl의 Draw 모드 구분 열거형.
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/3/9  16:32
        */
        public enum EnRoiMode
        {
            ModeMove = 0,
            ModeAddRectangle
        }

        public enum EnGrip
        {
            None = -1,
            LT,
            CT,
            RT,
            LC,
            RC,
            LB,
            CB,
            RB,
            All,
            Count
        }

        #endregion

        #region define Member Variable
        const double ZOOM_MIN = 0.01;
        const double ZOOM_MAX = 10;

        double m_dScale = 1.0;
        public double ImageScale
        {
            get { return m_dScale; }
            set
            {
                m_dScale = value <= ZOOM_MIN ? ZOOM_MIN : value >= ZOOM_MAX ? ZOOM_MAX : value;
                m_imgInfo.ImageScale = m_dScale * 100;
                ZoomScale(m_dScale);
            }
        }
        int m_nChildCount = 0;
        System.Windows.Point m_pntPrev;
        System.Windows.Point m_pntNowPos;

        public EnRoiMode m_enMode = EnRoiMode.ModeMove;
        EnGrip m_enGrip = EnGrip.Count;

        ImageBrush m_ImgBrs;

        System.Windows.Rect[] m_rectROI = new System.Windows.Rect[4];

        int m_nSelectedObject = 0;
        public int SelectedObject
        {
            get { return m_nSelectedObject; }
            set
            {
                m_nSelectedObject = value;
                fn_SelectObject();
            }
        }


        public bool IsEditor
        {
            get { return m_imgInfo.IsEditor; }
            set { m_imgInfo.IsEditor = value; }
        }

        double m_dGripWidth = 12.0;
        double m_dGripHeight = 12.0;


        System.Windows.Rect[] m_rectGrid = new System.Windows.Rect[(int)EnGrip.Count];
        Rectangle[] m_rectangleGrip = new Rectangle[(int)EnGrip.Count];

        const int MAX_ROI_COUNT = 4;

        Grid[] m_grid = new Grid[MAX_ROI_COUNT];
        Rectangle[] m_rectangle = new Rectangle[MAX_ROI_COUNT];
        TextBlock[] m_textbox = new TextBlock[MAX_ROI_COUNT];

        Grid m_gridRef = new Grid();
        Rectangle m_rectangleRef = new Rectangle();
        Brush m_brsLine = Brushes.Yellow;
        Brush m_brsFill = new SolidColorBrush(Color.FromArgb(75, 255, 255, 0));
        Brush m_brsLine2 = Brushes.Lime;
        Brush m_brsFill2 = new SolidColorBrush(Color.FromArgb(75, 0, 255, 0));
        Brush m_brsLine3 = Brushes.Blue;
        Brush m_brsFill3 = new SolidColorBrush(Color.FromArgb(75, 0, 0, 255));
        Brush m_brsLine4 = Brushes.Red;
        Brush m_brsFill4 = new SolidColorBrush(Color.FromArgb(75, 255, 0, 0));

        ImageViewInfo m_imgInfo = new ImageViewInfo();

        Brush m_brsGrip = Brushes.Yellow;
        Brush m_brsLineAlign = Brushes.Yellow;
        Brush m_brsFillAlign = new SolidColorBrush(Color.FromArgb(75, 255, 255, 0));
        Brush m_brsLineProcessing = Brushes.Lime;
        Brush m_brsFillProcessing = new SolidColorBrush(Color.FromArgb(75, 0, 255, 0));
        Brush m_brsLineIgnore = Brushes.Red;
        Brush m_brsFillIgnore = new SolidColorBrush(Color.FromArgb(75, 255, 0, 0));
        Brush m_brsLineRef = Brushes.Cyan;

        bool bLoaded = false;
        #endregion

        Cursor m_oldCursor;

        byte[] imageData = null;
        WriteableBitmap wbm = null;
        Mat m_matView = null;   // View(대용량 이미지) 표시 시 마우스 위치 픽셀값 조회용 원본 보관

        DateTime timePrevUpdateTime = DateTime.Now;
        TimeSpan spanFrame;

        double dScrollOffsetX = 0;
        double dScrollOffsetY = 0;

        const int ALIGNROI_MAX_SIZEX = 300;
        const int ALIGNROI_MAX_SIZEY = 300;

        int iReverseX = 1;
        int iReverseY = 1;

        Stopwatch swFPS = new Stopwatch();
        int iFrameCount = 0;

        public ImageViewer()
        {
            InitializeComponent();
            this.DataContext = m_imgInfo;
            ComponentDispatcher.ThreadIdle += fn_InitUI;
        }

        /**	
        @fn		public void fn_InitUI(object sender, EventArgs e)
        @brief	UI Load된 후 Class 초기화.
        @return	void
        @param	object    sender :
        @param	EventArgs e      :
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/11/15  14:56
        */
        public void fn_InitUI(object sender, EventArgs e)
        {
            ComponentDispatcher.ThreadIdle -= fn_InitUI;

            // Grip Rect
            for (int i = 0; i < m_rectangleGrip.Length; i++)
            {
                m_rectangleGrip[i] = new Rectangle();
                lib_Canvas.Children.Add(m_rectangleGrip[i]);
            }

            // Align Rect
            for (int i = 0; i < m_grid.Length; i++)
            {
                m_grid[i] = new Grid();
                m_rectangle[i] = new Rectangle();

                switch (i)
                {
                    case 0:
                        m_rectangle[i].Fill = m_brsFill;
                        m_rectangle[i].Stroke = m_brsLine;
                        break;

                    case 1:
                        m_rectangle[i].Fill = m_brsFill2;
                        m_rectangle[i].Stroke = m_brsLine2;
                        break;

                    case 2:
                        m_rectangle[i].Fill = m_brsFill3;
                        m_rectangle[i].Stroke = m_brsLine3;
                        break;

                    case 3:
                        m_rectangle[i].Fill = m_brsFill4;
                        m_rectangle[i].Stroke = m_brsLine4;
                        break;
                }
                //if (i == 0)
                //{
                //    m_rectangle[i].Fill = m_brsFill;
                //    m_rectangle[i].Stroke = m_brsLine;
                //}
                //else
                //{
                //    m_rectangle[i].Fill = m_brsFill2;
                //    m_rectangle[i].Stroke = m_brsLine2;
                //}
                m_rectangle[i].StrokeDashArray = new DoubleCollection();
                m_rectangle[i].StrokeDashArray.Add(2);
                m_rectangle[i].StrokeDashArray.Add(2);

                m_textbox[i] = new TextBlock();
                m_textbox[i].Text = $"Cell {i + 1}";
                m_textbox[i].VerticalAlignment = VerticalAlignment.Top;
                m_textbox[i].HorizontalAlignment = HorizontalAlignment.Left;
                //if (i == 0) m_textbox[i].Foreground = m_brsLine;
                //else m_textbox[i].Foreground = m_brsLine2;
                switch (i)
                {
                    case 0:
                        m_textbox[i].Foreground = m_brsLine;
                        break;

                    case 1:
                        m_textbox[i].Foreground = m_brsLine2;
                        break;

                    case 2:
                        m_textbox[i].Foreground = m_brsLine3;
                        break;

                    case 3:
                        m_textbox[i].Foreground = m_brsLine4;
                        break;
                }
                m_textbox[i].Margin = new Thickness(5);

                m_grid[i].Children.Add(m_rectangle[i]);
                m_grid[i].Children.Add(m_textbox[i]);
                m_grid[i].Width = 0;
                m_grid[i].Height = 0;
                lib_Canvas.Children.Add(m_grid[i]);
            }

            // Ref Rect
            m_rectangleRef.Stroke = m_brsLineRef;
            m_rectangleRef.Fill = Brushes.Transparent;
            m_gridRef.Children.Add(m_rectangleRef);
            m_gridRef.Width = 0;
            m_gridRef.Height = 0;
            lib_Canvas.Children.Add(m_gridRef);
            m_nChildCount = lib_Canvas.Children.Count;
            VisibleOverlapImage(false);
            lib_Canvas.Children.Add(OverlapImage);


            SetMode(EnRoiMode.ModeMove);

            //swFPS.Start();
            //wbm = new WriteableBitmap(m_imgInfo.Width, m_imgInfo.Height, 96, 96, PixelFormats.Bgr24, null);
        }

        public void SetOverlapImage(WriteableBitmap wb)
        {
            OverlapImage.Source = wb;
        }

        public void MoveOverlapImage(double offsetx, double offsety)
        {
            if (OverlapImage != null)
            {
                Canvas.SetLeft(OverlapImage, offsetx);
                Canvas.SetTop(OverlapImage, offsety);
            }
        }

        public void OpacityOverlapImage(double opacity)
        {
            if (OverlapImage != null)
            {
                OverlapImage.Opacity = opacity;
            }
        }

        public void VisibleOverlapImage(bool bvisible)
        {
            if (OverlapImage != null)
            {
                OverlapImage.Visibility = bvisible ? Visibility.Visible : Visibility.Hidden;
            }
        }

        public void UnLoadImage()
        {
            lib_Canvas.Background = null;
            OverlapImage = null;
        }

        public void Dispose()
        {
            if (m_ImgBrs != null) m_ImgBrs.ImageSource = null;
            m_ImgBrs = null;
            try { m_matView?.Dispose(); } catch { }
            m_matView = null;
            m_rectROI = null;

            for (int i = 0; i < m_rectangleGrip.Length; i++)
            {
                m_rectangleGrip[i] = null;
            }

            for (int i = 0; i < m_rectangle.Length; i++)
            {
                m_rectangle[i] = null;
            }
            for (int i = 0; i < m_textbox.Length; i++)
            {
                m_textbox[i] = null;
            }

            for (int i = 0; i < m_grid.Length; i++)
            {
                m_grid[i] = null;
            }

            lib_Canvas.Background = null;
        }

        //---------------------------------------------------------------------------
        /**	
        @fn		public void OpenImage(string strPath)
        @brief	Align Control에 string path로 이미지 Open.
        @return	
        @param	
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/3/9  16:39
        */
        public void OpenImage(string strPath)
        {
            Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(strPath)) return;

                    // 대용량 파일은 BitmapImage 대신 OpenCV로 읽는 게 훨씬 안정적입니다.
                    // 흑백(Grayscale) 모드로 로드하여 메모리 절약 (필요시 ImreadModes.Color)
                    // 사용자 요청: 흑백, 16k*55k -> Grayscale 필수
                    Mat mat = Cv2.ImRead(strPath, ImreadModes.Grayscale);

                    if (mat.Empty()) return;

                    Dispatcher.Invoke(() =>
                    {
                        m_imgInfo.Path = strPath;
                        m_imgInfo.FileName = System.IO.Path.GetFileName(strPath);

                        // 위에서 만든 함수 호출
                        SetLargeImage(mat);
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => delWriteLog?.Invoke($"Open Fail: {ex.Message}"));
                }
            });
            //try
            //{
            //    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            //    {
            //        try
            //        {
            //            if (!File.Exists(strPath))
            //            {
            //                m_ImgBrs = null;
            //                lib_Canvas.Background = Brushes.Transparent;
            //                return;
            //            }
            //            FileInfo fi = new FileInfo(strPath);
            //            m_imgInfo.Path = strPath;
            //            m_imgInfo.FileName = strPath.Substring(strPath.LastIndexOf('\\') + 1);
            //            m_imgInfo.Extension = fi.Extension;
            //            m_imgInfo.CreationTime = fi.CreationTime.ToString("yyyy.MM.dd HH:mm:ss");

            //            //int width = 0;
            //            //int height = 0;
            //            //int channel = 0;
            //            WriteableBitmap wbm = OpenFile(strPath);
            //            //IntPtr ptr = libOpenImage(new StringBuilder(m_imgInfo.Path), ref width, ref height, ref channel);

            //            //m_imgInfo.Width = width;
            //            //m_imgInfo.Height = height;
            //            //m_imgInfo.Channel = channel;
            //            //
            //            //m_imgInfo.Stride = (m_imgInfo.Width * m_imgInfo.Channel + 3) & ~3;
            //            //int nImageLength = (int)(m_imgInfo.Stride * m_imgInfo.Height * m_imgInfo.Channel);
            //            //byte[] imageData = new byte[nImageLength];
            //            //for (int i = 0; i < m_imgInfo.Height; i++)
            //            //{
            //            //    Marshal.Copy(ptr + i * m_imgInfo.Width * m_imgInfo.Channel, imageData, i * m_imgInfo.Stride, m_imgInfo.Width * m_imgInfo.Channel);
            //            //}
            //            //
            //            //PixelFormat format = PixelFormats.Bgr32;
            //            //switch (m_imgInfo.Channel)
            //            //{
            //            //    case 1: format = PixelFormats.Gray8; break;
            //            //    case 2: format = PixelFormats.Gray16; break;
            //            //    case 3: format = PixelFormats.Bgr24; break;
            //            //    case 4: format = PixelFormats.Bgr32; break;
            //            //}
            //            //WriteableBitmap wbm = new WriteableBitmap(m_imgInfo.Width, m_imgInfo.Height, 96, 96, format, null);
            //            //wbm.Lock();
            //            //wbm.WritePixels(new Int32Rect(0, 0, m_imgInfo.Width, m_imgInfo.Height), imageData, m_imgInfo.Stride, 0);
            //            //wbm.AddDirtyRect(new Int32Rect(0, 0, m_imgInfo.Width, m_imgInfo.Height));
            //            //wbm.Unlock();
            //            if (wbm != null)
            //            {
            //                m_ImgBrs = new ImageBrush(wbm);
            //                m_ImgBrs.Stretch = Stretch.None;
            //                lib_Canvas.Width = m_ImgBrs.ImageSource.Width;
            //                lib_Canvas.Height = m_ImgBrs.ImageSource.Height;
            //                lib_Canvas.Background = m_ImgBrs;
            //                m_imgInfo.Width = wbm.PixelWidth;
            //                m_imgInfo.Height = wbm.PixelHeight;
            //                m_imgInfo.Channel = (int)(wbm.Format.BitsPerPixel / 8.0);
            //                m_imgInfo.Stride = (m_imgInfo.Width * m_imgInfo.Channel + 3) & ~3;
            //                m_imgInfo.DpiX = wbm.DpiX;
            //                m_imgInfo.DpiY = wbm.DpiY;


            //                m_dScale = myScaleTransform.ScaleX;
            //            }
            //        }
            //        catch (Exception ex)
            //        {
            //            KeocGrabber.G.WriteLog($"Image Open Fail : {ex.Message}", true);
            //        }
            //    }));

            //}
            //catch { }
        }

        public WriteableBitmap OpenFile(string strpath)
        {
            BitmapImage bmpImg = new BitmapImage();
            FileStream source = File.OpenRead(strpath);
            //bmpImg.Format = PixelFormats.Bgr32;
            bmpImg.BeginInit();
            bmpImg.CacheOption = BitmapCacheOption.OnLoad;
            bmpImg.StreamSource = source;
            bmpImg.EndInit();

            WriteableBitmap wbm = null;
            if (bmpImg.Format == PixelFormats.Indexed8)
            {
                FormatConvertedBitmap newFormatedBitmapSource = new FormatConvertedBitmap();

                // BitmapSource objects like FormatConvertedBitmap can only have their properties
                // changed within a BeginInit/EndInit block.
                newFormatedBitmapSource.BeginInit();

                // Use the BitmapSource object defined above as the source for this new
                // BitmapSource (chain the BitmapSource objects together).
                newFormatedBitmapSource.Source = bmpImg;

                // Set the new format to Gray32Float (grayscale).
                newFormatedBitmapSource.DestinationFormat = PixelFormats.Gray8;
                newFormatedBitmapSource.EndInit();

                wbm = new WriteableBitmap(newFormatedBitmapSource);
            }
            else
                wbm = new WriteableBitmap(bmpImg);

            return wbm;
        }

        //---------------------------------------------------------------------------
        /**	
        @fn		public double GetFitScale()
        @brief	Control의 화면에 이미지 크기 맞게 배율 계산.
        @return	
        @param	
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/3/9  16:39
        */
        public double GetFitScale()
        {
            double dScaleX = 1.0;
            double dScaleY = 1.0;
            
            if (m_imgInfo.Width > 0 && m_imgInfo.Height > 0)
            {
                try
                {
                    if (lib_ScrollViewer.ActualWidth > 0 && lib_ScrollViewer.ActualHeight > 0)
                    {
                        dScaleX = lib_ScrollViewer.ActualWidth / (double)m_imgInfo.Width;
                        dScaleY = lib_ScrollViewer.ActualHeight / (double)m_imgInfo.Height;
                    }
                }
                catch (System.Exception ex)
                {
                    delWriteLog?.Invoke($"{this.Title} : {ex.Message}");
                }
            }
            return (dScaleY > dScaleX) ? dScaleX : dScaleY;

            //double dScaleX = 1.0;
            //double dScaleY = 1.0;
            //if (m_ImgBrs != null)
            //{
            //    try
            //    {
            //        if (lib_ScrollViewer.ActualWidth > 0 && lib_ScrollViewer.ActualHeight > 0)
            //        {
            //            dScaleX = lib_ScrollViewer.ActualWidth / m_ImgBrs.ImageSource.Width;
            //            dScaleY = lib_ScrollViewer.ActualHeight / m_ImgBrs.ImageSource.Height;
            //        }
            //    }
            //    catch (System.Exception ex)
            //    {
            //        delWriteLog?.Invoke($"{this.Title} : {ex.Message}");
            //    }
            //}
            //return (dScaleY > dScaleX) ? dScaleX : dScaleY;
        }

        /**	
        @fn		public void SetFitScale()
        @brief	Fit Scale.
        @return	void
        @param	void
        @remark	
         - Canvas Fit Scale.
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/11/15  14:58
        */
        public void SetFitScale()
        {
            ImageScale = GetFitScale();
        }

        /**	
        @fn		public void SetHalftone(bool bHalftone = false)
        @brief	Canvas Halftone Setting.
        @return	void
        @param	bool bHalftone : Halftone 여부.
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/11/15  14:59
        */
        public void SetHalftone(bool? bHalftone = false)
        {
            if (bHalftone == true)
                RenderOptions.SetBitmapScalingMode(lib_Canvas, BitmapScalingMode.HighQuality);
            else
                RenderOptions.SetBitmapScalingMode(lib_Canvas, BitmapScalingMode.NearestNeighbor);
        }

        /**	
        @fn		public bool GetHalftone()
        @brief	Halftone 여부 얻기.
        @return	bool : Halftone 여부.
        @param	void
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/11/15  15:00
        */
        public bool GetHalftone()
        {
            bool bRtn = false;
            if (RenderOptions.GetBitmapScalingMode(lib_Canvas) != BitmapScalingMode.NearestNeighbor)
                bRtn = true;
            else
                bRtn = false;
            return bRtn;
        }

        /**	
        @fn		public void SetScale(double dScale)
        @brief	원하는 Scale값으로 설정.
        @return	void
        @param	double dScale : 원하는 Scale.
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/11/15  15:02
        */
        public bool SetScale(double dScale)
        {
            ImageScale = dScale;
            return true;
        }

        //---------------------------------------------------------------------------
        /**	
        @fn		internal void SetImage(WriteableBitmap wb, Stretch stretch = Stretch.None)
        @brief	WriteableBitmap Type을 Align Control에 Set.
        @return	void
        @param	WriteableBitmap wb      : Source Image.
        @param	Stretch         stretch : Stretch Option.
        @remark	
         - Stretch Option을 Fill로 할 경우, 배율이 맞지 않을 수 있음.
         - 배율 조정 + Fit을 원하면 GetFitScale을 사용하여 수동으로 화면 Scale 조정 할 것.
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/3/9  16:40
        */
        public void SetImage(WriteableBitmap wb, Stretch stretch = Stretch.None)
        {
            if (wb == null)
                return;

            Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(delegate ()
            {
                m_ImgBrs = new ImageBrush(wb);

                m_ImgBrs.Stretch = stretch;

                m_imgInfo.Width = wb.PixelWidth;
                m_imgInfo.Height = wb.PixelHeight;
                m_imgInfo.Channel = (int)(wb.Format.BitsPerPixel / 8.0);
                if (m_ImgBrs.Stretch == Stretch.None)
                {
                    lib_Canvas.Width = m_imgInfo.Width;
                    lib_Canvas.Height = m_imgInfo.Height;
                }
                bool bFirst = lib_Canvas.Background == null;
                lib_Canvas.Background = m_ImgBrs;
                m_dScale = myScaleTransform.ScaleX;

                UpdateFps();
                
                if (bFirst) SetFitScale();
            }));
        }

        private void UpdateFps()
        {
            if (!swFPS.IsRunning) swFPS.Start();
            iFrameCount++;
            if (swFPS.ElapsedMilliseconds > 1000)
            {
                m_imgInfo.Fps = (iFrameCount / (double)swFPS.ElapsedMilliseconds) * 1000;
                iFrameCount = 0;
                swFPS.Reset();
            }
        }

        /**	
        @fn		public void SetImage(byte[] buff, int width, int height)
        @brief	byte array를 이미지 설정.
        @return	void
        @param	byte[]  buff    : Image Buffer
        @param	int     width   : Image Width
        @param	int     height  : Image Height
        @remark	
         - GrayScale Image Set.
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/11/15  15:04
        */
        public void SetImage(byte[] buff, int width, int height)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(delegate ()
            {
                if (this.IsLoaded)
                {
                    WriteableBitmap wbm = new WriteableBitmap(width, height, 96, 96, PixelFormats.Gray8, null);
                    wbm.Lock();
                    wbm.WritePixels(new Int32Rect(0, 0, width, height), buff, width, 0);
                    wbm.AddDirtyRect(new Int32Rect(0, 0, width, height));
                    wbm.Unlock();
                    SetImage(wbm);
                    SetFitScale();
                }
            }));
        }

        //---------------------------------------------------------------------------
        /**	
        @fn		public void SetImage(BitmapImage bmpimg, Stretch stretch = Stretch.None)
        @brief	WriteableBitmap Type을 Align Control에 Set.
        @return	void
        @param	BitmapImage     bmpimg  : Source Image.
        @param	Stretch         stretch : Stretch Option.
        @remark	
         - Stretch Option을 Fill로 할 경우, 배율이 맞지 않을 수 있음.
         - 배율 조정 + Fit을 원하면 GetFitScale을 사용하여 수동으로 화면 Scale 조정 할 것.
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/3/9  16:40
        */
        public void SetImage(BitmapImage bmpimg, Stretch stretch = Stretch.None)
        {
            m_ImgBrs = new ImageBrush(bmpimg);
            m_ImgBrs.Stretch = stretch;
            if (m_ImgBrs.Stretch == Stretch.None)
            {
                lib_Canvas.Width = m_ImgBrs.ImageSource.Width;
                lib_Canvas.Height = m_ImgBrs.ImageSource.Height;
            }

            m_imgInfo.Width = (int)m_ImgBrs.ImageSource.Width;
            m_imgInfo.Height = (int)m_ImgBrs.ImageSource.Height;
            m_imgInfo.Channel = (int)(bmpimg.Format.BitsPerPixel / 8.0);

            lib_Canvas.Background = new ImageBrush(bmpimg);
            m_dScale = myScaleTransform.ScaleX;
            UpdateFps();
        }

        //---------------------------------------------------------------------------
        /**	
        @fn		public void SetImage(IntPtr ptr, double dWidth, double dHeight, double dChannel)
        @brief	IntPtr 이미지를 Contorl에 Set.
        @return	void
        @param	IntPtr ptr          : Source Image Integer Pointer
        @param	double dWidth       : Image Width
        @param	double dHeight      : Image Height
        @param	double dChannel     : Image Channel
        @remark	
         - Image Pointer를 Byte Array로 변환.
         - 변환된 ByteArray를 Writeable Bitmap으로 변환.
         - Writeable Bitmap을 Control에 Set.
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/3/9  16:40
        */
        public void SetImage(IntPtr ptr, double dWidth, double dHeight, double dChannel)
        {
            bool bFirstUpdate = m_ImgBrs == null;
            //Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(delegate ()
            Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate ()
            {
                int nStride = ((int)dWidth * (int)dChannel + 3) & ~3;
                int nImageLength = (int)(nStride * dHeight);
                if (imageData == null || nImageLength != imageData.Length)
                    imageData = new byte[nImageLength];
                Marshal.Copy(ptr, imageData, 0, nImageLength);
                if (wbm == null || (wbm.PixelWidth != (int)dWidth || wbm.PixelHeight != (int)dHeight))
                    wbm = new WriteableBitmap((int)dWidth, (int)dHeight, 96, 96, PixelFormats.Bgr24, null);
                wbm.Lock();
                wbm.WritePixels(new Int32Rect(0, 0, (int)dWidth, (int)dHeight), imageData, (int)nStride, 0);
                wbm.AddDirtyRect(new Int32Rect(0, 0, (int)dWidth, (int)dHeight));
                wbm.Unlock();

                lib_Canvas.Background = null;

                m_ImgBrs = new ImageBrush(wbm);
                m_ImgBrs.Stretch = Stretch.None;
                m_imgInfo.Width = wbm.PixelWidth;
                m_imgInfo.Height = wbm.PixelHeight;
                m_imgInfo.Channel = (int)(wbm.Format.BitsPerPixel / 8.0);
                if (m_ImgBrs.Stretch == Stretch.None)
                {
                    lib_Canvas.Width = m_imgInfo.Width;
                    lib_Canvas.Height = m_imgInfo.Height;
                }
                lib_Canvas.Background = m_ImgBrs;
                m_dScale = myScaleTransform.ScaleX;
                spanFrame = DateTime.Now - timePrevUpdateTime;
                timePrevUpdateTime = DateTime.Now;
                UpdateFps();
                //SetImage(wbm);
                if (bFirstUpdate)
                    SetFitScale();
            }));
        }

        public double GetFrameElapse()
        {
            return spanFrame.TotalMilliseconds;
        }

        //---------------------------------------------------------------------------
        /**	
        @fn		private void onMouseDown(object sender, MouseButtonEventArgs e)
        @brief	마우스 Down 이벤트.
        @return	void
        @param	object                  sender
        @param	MouseButtonEventArgs    e
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/3/9  16:44
        */
        private void onMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                bUpdatePrevPoint = true;
                switch (m_enMode)
                {
                    case EnRoiMode.ModeMove:
                        this.Cursor = Cursors.SizeAll;
                        m_pntPrev = e.GetPosition(this);
                        break;
                    case EnRoiMode.ModeAddRectangle:
                        {
                            m_enGrip = EnGrip.None;
                            m_pntPrev = e.GetPosition(lib_Canvas);
                            if (m_nChildCount > 0)
                            {
                                if (CheckRectInsidePoint(m_rectGrid[(int)EnGrip.LT], m_pntPrev)) m_enGrip = EnGrip.LT;
                                else if (CheckRectInsidePoint(m_rectGrid[(int)EnGrip.CT], m_pntPrev)) m_enGrip = EnGrip.CT;
                                else if (CheckRectInsidePoint(m_rectGrid[(int)EnGrip.RT], m_pntPrev)) m_enGrip = EnGrip.RT;
                                else if (CheckRectInsidePoint(m_rectGrid[(int)EnGrip.LC], m_pntPrev)) m_enGrip = EnGrip.LC;
                                else if (CheckRectInsidePoint(m_rectGrid[(int)EnGrip.RC], m_pntPrev)) m_enGrip = EnGrip.RC;
                                else if (CheckRectInsidePoint(m_rectGrid[(int)EnGrip.LB], m_pntPrev)) m_enGrip = EnGrip.LB;
                                else if (CheckRectInsidePoint(m_rectGrid[(int)EnGrip.CB], m_pntPrev)) m_enGrip = EnGrip.CB;
                                else if (CheckRectInsidePoint(m_rectGrid[(int)EnGrip.RB], m_pntPrev)) m_enGrip = EnGrip.RB;
                                else if (CheckRectInsidePoint(m_rectGrid[(int)EnGrip.All], m_pntPrev)) m_enGrip = EnGrip.All;
                            }
                        }
                        break;
                }
            }
        }

        //---------------------------------------------------------------------------
        /**	
        @fn		private void onMouseMove(object sender, MouseEventArgs e)
        @brief	마우스 Move 이벤트.
        @return	void
        @param	object          sender
        @param	MouseEventArgs  e
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/3/9  16:46
        */
        private void onMouseMove(object sender, MouseEventArgs e)
        {
            m_pntNowPos = e.GetPosition(lib_Canvas);
            m_imgInfo.PosX = m_pntNowPos.X;
            m_imgInfo.PosY = m_pntNowPos.Y;
            try
            {
                if (m_enMode == EnRoiMode.ModeAddRectangle)
                {
                    if (CheckRectInsidePoint(m_rectGrid[(int)EnGrip.LT], e.GetPosition(lib_Canvas))) this.Cursor = Cursors.SizeNWSE;
                    else if (CheckRectInsidePoint(m_rectGrid[(int)EnGrip.CT], e.GetPosition(lib_Canvas))) this.Cursor = Cursors.SizeNS;
                    else if (CheckRectInsidePoint(m_rectGrid[(int)EnGrip.RT], e.GetPosition(lib_Canvas))) this.Cursor = Cursors.SizeNESW;
                    else if (CheckRectInsidePoint(m_rectGrid[(int)EnGrip.LC], e.GetPosition(lib_Canvas))) this.Cursor = Cursors.SizeWE;
                    else if (CheckRectInsidePoint(m_rectGrid[(int)EnGrip.RC], e.GetPosition(lib_Canvas))) this.Cursor = Cursors.SizeWE;
                    else if (CheckRectInsidePoint(m_rectGrid[(int)EnGrip.LB], e.GetPosition(lib_Canvas))) this.Cursor = Cursors.SizeNESW;
                    else if (CheckRectInsidePoint(m_rectGrid[(int)EnGrip.CB], e.GetPosition(lib_Canvas))) this.Cursor = Cursors.SizeNS;
                    else if (CheckRectInsidePoint(m_rectGrid[(int)EnGrip.RB], e.GetPosition(lib_Canvas))) this.Cursor = Cursors.SizeNWSE;
                    else if (CheckRectInsidePoint(m_rectGrid[(int)EnGrip.All], e.GetPosition(lib_Canvas))) this.Cursor = Cursors.SizeAll;
                    else this.Cursor = m_oldCursor;
                }

                WriteableBitmap bitmapImage = m_ImgBrs?.ImageSource as WriteableBitmap;
                if (bitmapImage != null)
                {
                    int height = bitmapImage.PixelHeight;
                    int width = bitmapImage.PixelWidth;
                    int nStride = (bitmapImage.PixelWidth * bitmapImage.Format.BitsPerPixel + 7) / 8;
                    double dDPIScaleX = bitmapImage.DpiX / 96.0;
                    double dDPIScaleY = bitmapImage.DpiY / 96.0;
                    byte[] pixelByteArray = new byte[4];
                    try
                    {
                        bitmapImage.CopyPixels(new Int32Rect((int)(e.GetPosition(lib_Canvas).X * dDPIScaleX), (int)(e.GetPosition(lib_Canvas).Y * dDPIScaleY), 1, 1), pixelByteArray, nStride, 0);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    switch (bitmapImage.Format.BitsPerPixel)
                    {
                        case 8:
                            pixelByteArray[1] = pixelByteArray[0];
                            pixelByteArray[2] = pixelByteArray[0];
                            pixelByteArray[3] = 255;
                            break;
                        case 24:
                            pixelByteArray[3] = 255;
                            break;
                        case 32:
                            break;
                    }
                    m_imgInfo.ColorR = pixelByteArray[2];
                    m_imgInfo.ColorG = pixelByteArray[1];
                    m_imgInfo.ColorB = pixelByteArray[0];
                }
                else if (m_matView != null && !m_matView.Empty())
                {
                    // 대용량 이미지(View)는 m_ImgBrs(WriteableBitmap)가 없으므로
                    // 보관된 원본 Mat에서 마우스 위치(=이미지 픽셀좌표)의 값을 직접 읽는다.
                    int ix = (int)m_pntNowPos.X;
                    int iy = (int)m_pntNowPos.Y;
                    if (ix >= 0 && iy >= 0 && ix < m_matView.Width && iy < m_matView.Height)
                    {
                        if (m_matView.Channels() == 1)
                        {
                            byte g = m_matView.At<byte>(iy, ix);
                            m_imgInfo.ColorR = g; m_imgInfo.ColorG = g; m_imgInfo.ColorB = g;
                        }
                        else if (m_matView.Channels() >= 3)
                        {
                            Vec3b v = m_matView.At<Vec3b>(iy, ix);
                            m_imgInfo.ColorB = v.Item0;
                            m_imgInfo.ColorG = v.Item1;
                            m_imgInfo.ColorR = v.Item2;
                        }
                    }
                }

                if (bUpdatePrevPoint)
                {
                    if (e.LeftButton == MouseButtonState.Pressed)
                    {
                        switch (m_enMode)
                        {
                            case EnRoiMode.ModeMove:
                                double dOffsetX = lib_ScrollViewer.HorizontalOffset;
                                double dOffsetY = lib_ScrollViewer.VerticalOffset;
                                System.Windows.Point pnt = (System.Windows.Point)(m_pntPrev - e.GetPosition(this));
                                lib_ScrollViewer.ScrollToHorizontalOffset(dOffsetX + pnt.X);
                                lib_ScrollViewer.ScrollToVerticalOffset(dOffsetY + pnt.Y);
                                m_pntPrev = e.GetPosition(this);
                                break;
                            case EnRoiMode.ModeAddRectangle:
                                {
                                    double dGapX = m_pntNowPos.X - m_pntPrev.X;
                                    double dGapY = m_pntNowPos.Y - m_pntPrev.Y;

                                    Grid SelRectangle = lib_Canvas.Children[SelectedObject + (int)EnGrip.Count] as Grid;

                                    fn_CheckGridSelect(SelRectangle, dGapX, dGapY);

                                    fn_DrawGrip();
                                }
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                delWriteLog?.Invoke($"{this.Title} : {ex.Message}");
            }
        }

        private void fn_CheckGridSelect(Grid SelRectangle, double dGapX, double dGapY)
        {
            switch (m_enGrip)
            {
                case EnGrip.LT:
                    {
                        
                        {
                            Canvas.SetLeft(SelRectangle, Canvas.GetLeft(SelRectangle) + dGapX);
                            Canvas.SetTop(SelRectangle, Canvas.GetTop(SelRectangle) + dGapY);
                            SelRectangle.Width += dGapX * -1;
                            SelRectangle.Height += dGapY * -1;
                        }
                        m_pntPrev = m_pntNowPos;
                    }
                    break;
                case EnGrip.CT:
                    {
                       
                        {
                            Canvas.SetTop(SelRectangle, Canvas.GetTop(SelRectangle) + dGapY);
                            SelRectangle.Height += dGapY * -1;
                        }
                        m_pntPrev = m_pntNowPos;
                    }
                    break;
                case EnGrip.RT:
                    {
                        {
                            Canvas.SetTop(SelRectangle, Canvas.GetTop(SelRectangle) + dGapY);
                            SelRectangle.Width += dGapX;
                            SelRectangle.Height += dGapY * -1;
                        }
                        m_pntPrev = m_pntNowPos;
                    }
                    break;
                case EnGrip.LC:
                    {
                        {
                            Canvas.SetLeft(SelRectangle, Canvas.GetLeft(SelRectangle) + dGapX);
                            SelRectangle.Width += dGapX * -1;
                        }
                        m_pntPrev = m_pntNowPos;
                    }
                    break;
                case EnGrip.RC:
                    {
                        SelRectangle.Width += dGapX;
                        m_pntPrev = m_pntNowPos;
                    }
                    break;
                case EnGrip.LB:
                    {
                        {
                            Canvas.SetLeft(SelRectangle, Canvas.GetLeft(SelRectangle) + dGapX);
                            SelRectangle.Width += dGapX * -1;
                            SelRectangle.Height += dGapY;
                        }
                        m_pntPrev = m_pntNowPos;
                    }
                    break;
                case EnGrip.CB:
                    {
                        SelRectangle.Height += dGapY;
                        m_pntPrev = m_pntNowPos;
                    }
                    break;
                case EnGrip.RB:
                    {
                        SelRectangle.Width += dGapX;
                        SelRectangle.Height += dGapY;
                        m_pntPrev = m_pntNowPos;
                    }
                    break;
                case EnGrip.All:
                    {
                        Canvas.SetLeft(SelRectangle, Canvas.GetLeft(SelRectangle) + dGapX);
                        Canvas.SetTop(SelRectangle, Canvas.GetTop(SelRectangle) + dGapY);
                        m_pntPrev = m_pntNowPos;
                    }
                    break;
                case EnGrip.None:
                    if (dGapX > 0)
                    {
                        Canvas.SetLeft(SelRectangle, m_pntPrev.X);
                        SelRectangle.Width = dGapX;
                    }
                    else
                    {
                        Canvas.SetLeft(SelRectangle, m_pntNowPos.X);
                        SelRectangle.Width = Math.Abs(dGapX);
                    }

                    if (dGapY > 0)
                    {
                        Canvas.SetTop(SelRectangle, m_pntPrev.Y);
                        SelRectangle.Height = dGapY;
                    }
                    else
                    {
                        Canvas.SetTop(SelRectangle, m_pntNowPos.Y);
                        SelRectangle.Height = Math.Abs(dGapY);
                    }
                    break;
            }
        }


        public bool SetScroll(double dHorizontal, double dVertical)
        {
            lib_ScrollViewer.ScrollToHorizontalOffset(dHorizontal);
            lib_ScrollViewer.ScrollToVerticalOffset(dVertical);
            return true;
        }

        /**	
        @fn		private void onMouseUp(object sender, MouseButtonEventArgs e)
        @brief	Mouse Up (Preview Mouse Up Event)
        @return	void
        @param	object               sender :
        @param	MouseButtonEventArgs e      :
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/11/15  15:11
        */
        private void onMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                try
                {
                    switch (m_enMode)
                    {
                        case EnRoiMode.ModeAddRectangle:
//                             for (int i = 0; i < m_rectROI.Length; i++)
//                             {
//                                 if (m_rectROI[i].Width > 0 && m_rectROI[i].Height > 0)
//                                 {
//                                     delUpdateRect?.Invoke(i, m_rectROI[i].X, m_rectROI[i].Y, m_rectROI[i].Width, m_rectROI[i].Height);
//                                 }
//                             }
                            delUpdateRect?.Invoke(SelectedObject, m_rectROI[SelectedObject].X, m_rectROI[SelectedObject].Y, m_rectROI[SelectedObject].Width, m_rectROI[SelectedObject].Height);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    delWriteLog?.Invoke($"{this.Title} : {ex.Message}");
                }
                bUpdatePrevPoint = false;
            }
        }

        /**	
        @fn		public void SetMode(EnRoiMode mode, EnDrawMode drawmode = 0)
        @brief	Draw Mode 설정.
        @return	void
        @param	mode     : ROI Mode
        @param	drawmode : Draw Mode
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/11/15  15:13
        */
        public void SetMode(EnRoiMode mode)
        {
            m_enMode = mode;
            switch (m_enMode)
            {
                case EnRoiMode.ModeMove: m_oldCursor = this.Cursor = Cursors.SizeAll; break;
                case EnRoiMode.ModeAddRectangle: m_oldCursor = this.Cursor = Cursors.Cross; break;
            }
            fn_SelectObject();
        }

        //---------------------------------------------------------------------------
        /**	
        @fn		private bool CheckRectInsidePoint(Rect rect, Point pnt)
        @brief	인자로 넘어온 사각형 안에 포인트가 속해 있는지 bool형으로 리턴.
        @return	bool : Point가 Rect 안에 있는지.
        @param	Rect rect : 확인할 영역.
        @param	Point pnt : 확인할 점.
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/3/9  16:49
        */
        private bool CheckRectInsidePoint(System.Windows.Rect rect, System.Windows.Point pnt)
        {
            return (rect.Left <= pnt.X) && (rect.Top <= pnt.Y) && (rect.Right >= pnt.X) && (rect.Bottom >= pnt.Y);
        }

        //---------------------------------------------------------------------------
        /**	
        @fn		public void ZoomScale(double dScale)
        @brief	Zoom Scale 설정.
        @return	void
        @param	double dScale : 실수형 줌 값.
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/3/9  16:51
        */
        private void ZoomScale(double dScale)
        {
            if (myScaleTransform != null)
            {
                // 이미지가 로드되지 않았으면 리턴
                if (m_imgInfo.Width <= 0 || m_imgInfo.Height <= 0) return;

                System.Windows.Point pnt = Mouse.GetPosition(lib_ScrollViewer);
                double dCurPosRateX = 0.0;
                double dCurPosRateY = 0.0;

                // [수정] m_ImgBrs.ImageSource.Width -> m_imgInfo.Width
                double width = m_imgInfo.Width;
                double height = m_imgInfo.Height;

                if (lib_ScrollViewer.ExtentWidth > 0)
                    dCurPosRateX = (lib_ScrollViewer.HorizontalOffset + pnt.X) / lib_ScrollViewer.ExtentWidth;
                if (lib_ScrollViewer.ExtentHeight > 0)
                    dCurPosRateY = (lib_ScrollViewer.VerticalOffset + pnt.Y) / lib_ScrollViewer.ExtentHeight;

                myScaleTransform.ScaleX = iReverseX * dScale;
                myScaleTransform.ScaleY = iReverseY * dScale;

                // [중요] m_dScale 변수 업데이트 (무한 루프 방지)
                // 기존 코드에 'if (myScaleTransform.ScaleX == m_dScale) return;'이 있다면
                // 여기서 값을 맞춰줘야 다음에 호출될 때 문제가 없습니다.
                // m_dScale = dScale; // (필요 시 주석 해제)

                double dOffsetX = (width * dScale) * dCurPosRateX - pnt.X;
                double dOffsetY = (height * dScale) * dCurPosRateY - pnt.Y;

                lib_ScrollViewer.ScrollToHorizontalOffset(dOffsetX);
                lib_ScrollViewer.ScrollToVerticalOffset(dOffsetY);

                fn_UpdateObject();
                delUpdateZoom?.Invoke(ImageScale);
            }

            //if (myScaleTransform.ScaleX == m_dScale) return;
            //if (myScaleTransform != null && m_ImgBrs != null)
            //{
            //    System.Windows.Point pnt = Mouse.GetPosition(lib_ScrollViewer);
            //    double dCurPosRateX = 0.0;
            //    double dCurPosRateY = 0.0;
            //    double width = m_ImgBrs.ImageSource.Width;
            //    double height = m_ImgBrs.ImageSource.Height;
            //    if (lib_ScrollViewer.ExtentWidth > 0)
            //        dCurPosRateX = (lib_ScrollViewer.HorizontalOffset + pnt.X) / lib_ScrollViewer.ExtentWidth;
            //    if (lib_ScrollViewer.ExtentHeight > 0)
            //        dCurPosRateY = (lib_ScrollViewer.VerticalOffset + pnt.Y) / lib_ScrollViewer.ExtentHeight;

            //    myScaleTransform.ScaleX = iReverseX * dScale;
            //    myScaleTransform.ScaleY = iReverseY * dScale;
            //    //m_dScale = dScale;

            //    double dOffsetX = (width * dScale) * dCurPosRateX - pnt.X;
            //    double dOffsetY = (height * dScale) * dCurPosRateY - pnt.Y;
            //    lib_ScrollViewer.ScrollToHorizontalOffset(dOffsetX);
            //    lib_ScrollViewer.ScrollToVerticalOffset(dOffsetY);

            //    fn_UpdateObject();
            //    delUpdateZoom?.Invoke(ImageScale);
            //}
        }

        //---------------------------------------------------------------------------
        /**	
        @fn		public double GetZoomScale()
        @brief	Control에 설정된 Zoom Scale 반환.
        @return	double : 현재 Zoom Scale.
        @param	void
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/3/9  16:52
        */
        public double GetZoomScale()
        {
            return m_dScale;
        }


        //---------------------------------------------------------------------------
        /**	
        @fn		public CroppedBitmap GetModelImage()
        @brief	Model ROI에서 이미지 얻기.
        @return	CroppedBitmap : Model ROI Image
        @param	void
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/3/9  17:03
        */
        public CroppedBitmap GetROIImage(int index)
        {
            System.Windows.Rect rectROI = m_rectROI[index];
            CroppedBitmap cb = null;
            if (rectROI.Width > 0 && rectROI.Height > 0 && lib_Canvas.Background != null)
            {
                ImageBrush ib = lib_Canvas.Background as ImageBrush;
                if (ib != null)
                {
                    WriteableBitmap wb = ib.ImageSource as WriteableBitmap;
                    if (wb != null)
                    {
                        double dDPIScaleX = wb.DpiX / 96.0;
                        double dDPIScaleY = wb.DpiY / 96.0;
                        cb = new CroppedBitmap(
                            wb,
                            new Int32Rect((int)(rectROI.X * dDPIScaleX), (int)(rectROI.Y * dDPIScaleY),
                            (int)(rectROI.Width * dDPIScaleX), (int)(rectROI.Height * dDPIScaleY)));       //select region rect
                    }
                }
            }

            return cb;
        }

        /**	
        @fn		public Rect GetROIRect()
        @brief	Select ROI 얻기.
        @return	Rect : ROI 객체.
        @param	void
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/11/15  15:16
        */
        public System.Windows.Rect GetROIRect(int index)
        {
            return m_rectROI[index];
        }

        public bool SetROIRect(double x, double y, double width, double height)
        {
            try
            {
                if (!bUpdatePrevPoint)
                {
                    Grid SelRectangle = lib_Canvas.Children[SelectedObject + (int)EnGrip.Count] as Grid;

                    Canvas.SetLeft(SelRectangle, x);
                    Canvas.SetTop(SelRectangle, y);
                    SelRectangle.Width = width;
                    SelRectangle.Height = height;

                    fn_DrawGrip();
                }
            }
            catch (Exception ex)
            {
                delWriteLog?.Invoke(ex.Message);
            }
            return false;
        }

        //---------------------------------------------------------------------------
        /**	
        @fn		public IntPtr? fn_GetIamgePtr()
        @brief	Image IntPtr 얻기.
        @return	IntPtr? : nullable Image Pointer
        @param	void
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/3/9  18:32
        */
        public IntPtr? fn_GetImagePtr()
        {
            if (m_ImgBrs != null)
            {
                try
                {
                    WriteableBitmap bmp = m_ImgBrs.ImageSource as WriteableBitmap;
                    return bmp.BackBuffer;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return null;
                }
            }
            return null;
        }


        //---------------------------------------------------------------------------
        /**	
        @fn		public WriteableBitmap fn_GetImageStream()
        @brief	Image Steam 얻기.
        @return	WriteableBitmap : Control의 이미지.
        @param	void
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/3/9  18:33
        */
        public WriteableBitmap fn_GetImageStream()
        {
            if (m_ImgBrs != null)
            {
                try
                {
                    WriteableBitmap bmp = null;
                    Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
                    {
                        bmp = m_ImgBrs.ImageSource as WriteableBitmap;
                        if (bmp == null)
                        {
                            BitmapImage img = m_ImgBrs.ImageSource as BitmapImage;
                            if (img != null)
                                bmp = new WriteableBitmap(img);
                        }
                    }));
                    return bmp;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return null;
                }
            }
            return null;
        }

        //---------------------------------------------------------------------------
        /**	
        @fn		public void fn_SaveImage(string strPath)
        @brief	Image Save.
        @return	void
        @param	string strPath : Image Path.
        @remark	
         - 
        @author	선경규(Kyeong Kyu - Seon)
        @date	2020/3/9  18:34
        */
        public void fn_SaveImage(string strPath)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                if (m_ImgBrs != null)
                {
                    try
                    {
                        WriteableBitmap bmp;

                        bmp = m_ImgBrs.ImageSource as WriteableBitmap;

                        using (FileStream stream = new FileStream(strPath, FileMode.Create))
                        {
                            BmpBitmapEncoder encoder = new BmpBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(bmp));
                            encoder.Save(stream);
                        }
                    }
                    catch (Exception ex)
                    {
                        delWriteLog?.Invoke($"{this.Title} : {ex.Message}");
                    }
                }
            }));
        }

        private void UserControl_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            double dZoom = GetZoomScale();
            double dFitScale = GetFitScale();
            double zoomStep = 0.025;

            if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control) return;

            if (e.Delta > 0)
            {
                // 줌 인
                ImageScale = (Math.Floor(m_dScale / zoomStep) + 1) * zoomStep;
            }
            else
            {
                // 줌 아웃
                double nextScale = (Math.Floor(m_dScale / zoomStep) - 1) * zoomStep;
                if (nextScale < dFitScale) nextScale = dFitScale;
                ImageScale = nextScale;
            }
            e.Handled = true;
        }

        public void ClearCanvas()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(delegate ()
            {
            if (lib_Canvas != null)
            {
                lib_Canvas.Background = Brushes.Transparent;
                m_ImgBrs = null;
                try { m_matView?.Dispose(); } catch { }
                m_matView = null;
            }
            }));
        }

        private void bn_FitScale_Click(object sender, RoutedEventArgs e)
        {
            SetFitScale();
        }

        private void bn_Scale1_Click(object sender, RoutedEventArgs e)
        {
            ImageScale = 1.0;
        }


        private void bn_Halftone_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton tb = sender as ToggleButton;
            if (tb != null)
            {
                SetHalftone(tb.IsChecked);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!bLoaded && m_ImgBrs != null)
            {
                SetFitScale();
                bLoaded = true;
            }
        }

        private void lib_ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            delUpdateScroll?.Invoke(e.HorizontalOffset, e.VerticalOffset);
        }

        public void SetScrollOffset(double x, double y)
        {
            dScrollOffsetX = x;
            dScrollOffsetY = y;
        }

        private void bn_Mode_Click(object sender, RoutedEventArgs e)
        {
            m_imgInfo.IsDraw ^= true;
            if (m_imgInfo.IsDraw)
            {
                SetMode(EnRoiMode.ModeAddRectangle);
            }
            else
            {
                SetMode(EnRoiMode.ModeMove);
            }
        }

        public void SetDrawMode(bool bDraw)
        {
            m_imgInfo.IsDraw = bDraw;
            if (m_imgInfo.IsDraw)
            {
                SetMode(EnRoiMode.ModeAddRectangle);
            }
            else
            {
                SetMode(EnRoiMode.ModeMove);
            }
        }

        private void bn_ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            ImageScale = GetZoomScale() / 0.75;
        }

        private void bn_ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            ImageScale = GetZoomScale() / 1.25;
        }

        private void bn_Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fdlg = new OpenFileDialog();
            if (fdlg.ShowDialog() == true)
            {
                OpenImage(fdlg.FileName);
                SetFitScale();
                delImageOpenEvent?.Invoke();
            }
        }

        private void bn_Save_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog fdlg = new SaveFileDialog();
            if (fdlg.ShowDialog() == true)
            {
                fn_SaveImage(fdlg.FileName);
            }
        }

        private void fn_SelectObject()
        {
            UIElement element;

            for (int i = (int)EnGrip.Count; i < m_nChildCount - 1; i++)
            {
                element = lib_Canvas.Children[i];
                element.Visibility = Visibility.Hidden;
            }

            switch (SelectedObject)
            {
                case 0:
                    m_rectangle[SelectedObject].Fill = m_brsFill;
                    m_rectangle[SelectedObject].Stroke = m_brsLine;
                    break;

                case 1:
                    m_rectangle[SelectedObject].Fill = m_brsFill2;
                    m_rectangle[SelectedObject].Stroke = m_brsLine2;
                    break;

                case 2:
                    m_rectangle[SelectedObject].Fill = m_brsFill3;
                    m_rectangle[SelectedObject].Stroke = m_brsLine3;
                    break;

                case 3:
                    m_rectangle[SelectedObject].Fill = m_brsFill4;
                    m_rectangle[SelectedObject].Stroke = m_brsLine4;
                    break;
            }

            //if (SelectedObject == 0)
            //    m_brsGrip = m_brsLine;
            //else
            //    m_brsGrip = m_brsLine2;

            for (int i = 0; i < (int)EnGrip.Count; i++)
            {
                element = lib_Canvas.Children[i];
                element.Visibility = m_enMode == EnRoiMode.ModeAddRectangle ? Visibility.Visible : Visibility.Hidden;
            }

            //for (int i = (int)EnGrip.Count; i < m_nChildCount - 1; i++)
            //{
            //    element = lib_Canvas.Children[i];
            //    element.Visibility = Visibility.Visible;
            //}
            if (SelectedObject < 2)
            {
                element = lib_Canvas.Children[9];
                element.Visibility = Visibility.Visible;
                element = lib_Canvas.Children[10];
                element.Visibility = Visibility.Visible;
            }
            else
            {
                element = lib_Canvas.Children[11];
                element.Visibility = Visibility.Visible;
                element = lib_Canvas.Children[12];
                element.Visibility = Visibility.Visible;
            }
            fn_DrawGrip();
        }

        private void fn_ROIInsideImageCheck(Grid rect)
        {
            double dleft = Canvas.GetLeft(rect);
            double dtop = Canvas.GetTop(rect);
            if (!(dleft >= 0))
                Canvas.SetLeft(rect, 0);
            if (dleft + rect.Width >= lib_Canvas.ActualWidth)
                Canvas.SetLeft(rect, lib_Canvas.ActualWidth - rect.Width);
            if (!(dtop >= 0))
                Canvas.SetTop(rect, 0);
            if (dtop + rect.Height >= lib_Canvas.ActualHeight)
                Canvas.SetTop(rect, lib_Canvas.ActualHeight - rect.Height);

            Console.WriteLine($"rectX:{dleft} rectY:{dtop} canvaswidth : {lib_Canvas.ActualWidth} canvasheight : {lib_Canvas.ActualHeight}");
            
        }

        private void fn_DrawGrip()
        {
            Grid SelRectangle = (lib_Canvas.Children[SelectedObject + (int)EnGrip.Count] as Grid);
            if (SelRectangle != null)
            {
                //if (SelectedObject == REFROI_INDEX) fn_ROIInsideImageCheck(SelRectangle);

                double dGripWidth = m_dGripWidth / m_dScale;
                double dGripHeight = m_dGripHeight / m_dScale;

                m_rectGrid[(int)EnGrip.All].X = Canvas.GetLeft(SelRectangle);
                m_rectGrid[(int)EnGrip.All].Y = Canvas.GetTop(SelRectangle);
                m_rectGrid[(int)EnGrip.All].Width = SelRectangle.Width;
                m_rectGrid[(int)EnGrip.All].Height = SelRectangle.Height;

                m_rectGrid[(int)EnGrip.LT].X = m_rectGrid[(int)EnGrip.All].Left - dGripWidth / 2.0;
                m_rectGrid[(int)EnGrip.LT].Y = m_rectGrid[(int)EnGrip.All].Top - dGripHeight / 2.0;

                m_rectGrid[(int)EnGrip.CT].X = (m_rectGrid[(int)EnGrip.All].Left + m_rectGrid[(int)EnGrip.All].Right - dGripWidth) / 2.0;
                m_rectGrid[(int)EnGrip.CT].Y = m_rectGrid[(int)EnGrip.All].Top - dGripHeight / 2.0;

                m_rectGrid[(int)EnGrip.RT].X = m_rectGrid[(int)EnGrip.All].Right - dGripWidth / 2.0;
                m_rectGrid[(int)EnGrip.RT].Y = m_rectGrid[(int)EnGrip.All].Top - dGripHeight / 2.0;

                m_rectGrid[(int)EnGrip.LC].X = m_rectGrid[(int)EnGrip.All].Left - dGripWidth / 2.0;
                m_rectGrid[(int)EnGrip.LC].Y = (m_rectGrid[(int)EnGrip.All].Top + m_rectGrid[(int)EnGrip.All].Bottom - dGripHeight) / 2.0;

                m_rectGrid[(int)EnGrip.RC].X = m_rectGrid[(int)EnGrip.All].Right - dGripWidth / 2.0;
                m_rectGrid[(int)EnGrip.RC].Y = (m_rectGrid[(int)EnGrip.All].Top + m_rectGrid[(int)EnGrip.All].Bottom - dGripHeight) / 2.0;

                m_rectGrid[(int)EnGrip.LB].X = m_rectGrid[(int)EnGrip.All].Left - dGripWidth / 2.0;
                m_rectGrid[(int)EnGrip.LB].Y = m_rectGrid[(int)EnGrip.All].Bottom - dGripHeight / 2.0;

                m_rectGrid[(int)EnGrip.CB].X = (m_rectGrid[(int)EnGrip.All].Left + m_rectGrid[(int)EnGrip.All].Right - dGripWidth) / 2.0;
                m_rectGrid[(int)EnGrip.CB].Y = m_rectGrid[(int)EnGrip.All].Bottom - dGripHeight / 2.0;

                m_rectGrid[(int)EnGrip.RB].X = m_rectGrid[(int)EnGrip.All].Right - dGripWidth / 2.0;
                m_rectGrid[(int)EnGrip.RB].Y = m_rectGrid[(int)EnGrip.All].Bottom - dGripHeight / 2.0;


                for (int i = 0; i < 8; i++)
                {
                    m_rectGrid[i].Width = dGripWidth;
                    m_rectGrid[i].Height = dGripHeight;

                    m_rectangleGrip[i].Width = m_rectGrid[i].Width;
                    m_rectangleGrip[i].Height = m_rectGrid[i].Height;
                    m_rectangleGrip[i].StrokeThickness = 0.0;
                    m_rectangleGrip[i].Fill = m_brsGrip;

                    Canvas.SetLeft(m_rectangleGrip[i], m_rectGrid[i].X);
                    Canvas.SetTop(m_rectangleGrip[i], m_rectGrid[i].Y);
                }

                // Update Rectangle Infomation
                Grid grid = null;
                for (int i = 0; i < m_rectROI.Length; i++)
                {
                    grid = lib_Canvas.Children[i + (int)EnGrip.Count] as Grid;
                    if (grid != null)
                    {
                        m_rectROI[i].X = Canvas.GetLeft(grid);
                        m_rectROI[i].Y = Canvas.GetTop(grid);
                        m_rectROI[i].Width = grid.Width;
                        m_rectROI[i].Height = grid.Height;
                    }
                }
            }
        }

        private void fn_UpdateObject()
        {
            for (int i = 0; i < m_textbox.Length; i++)
            {
                m_textbox[i].FontSize = 14 / m_dScale;
                m_rectangle[i].StrokeThickness = 1 / m_dScale;
            }

            fn_DrawGrip();
        }

        public void Inverse(bool inverseX, bool inverseY)
        {
            iReverseX = inverseX ? -1 : 1;
            iReverseY = inverseY ? -1 : 1;

            myScaleTransform.ScaleX = iReverseX * m_dScale;
            myScaleTransform.ScaleY = iReverseY * m_dScale;
        }

        public void AlignObject(int mode)
        {
            Grid SelRectangle = lib_Canvas.Children[SelectedObject + (int)EnGrip.Count] as Grid;

            int otheridx = (m_rectROI.Length - 1) - SelectedObject;
            Grid otherRectangle = lib_Canvas.Children[otheridx + (int)EnGrip.Count] as Grid;

            //! 1 : left, 2 : center, 3 : right, 4 : top, 5 : middle, 6 : bottom, 7 : vertical, 8 : horizontal.  Taeroo-kgseon - 2024/08/26  18:48
            switch (mode)
            {
                case 1: // left
                    Canvas.SetLeft(SelRectangle, Canvas.GetLeft(otherRectangle));
                    break;
                case 2: // center
                    Canvas.SetLeft(SelRectangle, Canvas.GetLeft(otherRectangle) + (otherRectangle.Width - SelRectangle.Width) / 2.0);
                    break;
                case 3: // right
                    Canvas.SetLeft(SelRectangle, Canvas.GetLeft(otherRectangle) + (otherRectangle.Width - SelRectangle.Width));
                    break;
                case 4: // top
                    Canvas.SetTop(SelRectangle, Canvas.GetTop(otherRectangle));
                    break;
                case 5: // middle
                    Canvas.SetTop(SelRectangle, Canvas.GetTop(otherRectangle) + (otherRectangle.Height - SelRectangle.Height) / 2.0);
                    break;
                case 6: // bottom
                    Canvas.SetTop(SelRectangle, Canvas.GetTop(otherRectangle) + (otherRectangle.Height - SelRectangle.Height));
                    break;
                case 7: // vertical
                    SelRectangle.Height = otherRectangle.Height;
                    break;
                case 8: // horizontal
                    SelRectangle.Width = otherRectangle.Width;
                    break;
            }
            fn_DrawGrip();
            delUpdateRect?.Invoke(SelectedObject, m_rectROI[SelectedObject].X, m_rectROI[SelectedObject].Y, m_rectROI[SelectedObject].Width, m_rectROI[SelectedObject].Height);
        }

        public void SetLargeImage(Mat bigMat)
        {
            if (bigMat == null || bigMat.Empty()) return;

            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                try
                {
                    // 1. 기존 Canvas 배경 초기화 (이제 안 씀)
                    //   ※ Background를 null로 두면 Canvas가 히트테스트에서 빠져
                    //     마우스 이벤트(onMouseMove)가 발생하지 않는다 → 좌표/gray 미표시.
                    //     반드시 Transparent로 유지해야 마우스 이벤트가 들어온다.
                    lib_Canvas.Background = Brushes.Transparent;
                    m_ImgBrs = null;

                    // 마우스 위치 픽셀값(gray) 조회용으로 원본 Mat 보관 (이전 것 해제 후 소유권 인수)
                    try { m_matView?.Dispose(); } catch { }
                    m_matView = bigMat;

                    // 2. 정보 업데이트
                    m_imgInfo.Width = bigMat.Width;
                    m_imgInfo.Height = bigMat.Height;
                    m_imgInfo.Channel = bigMat.Channels();

                    // ROI 캔버스 크기를 이미지 전체 크기에 맞춤
                    lib_Canvas.Width = m_imgInfo.Width;
                    lib_Canvas.Height = m_imgInfo.Height;

                    // 3. 이미지 슬라이싱 (GPU 제한 회피: 높이 8000px씩 자름)
                    int sliceHeight = 8000;
                    List<BitmapSource> listBitmaps = new List<BitmapSource>();

                    int currentY = 0;
                    int imgWidth = bigMat.Width;
                    int imgHeight = bigMat.Height;

                    // (옵션) 흑백 이미지인 경우 최적화
                    // bigMat가 1채널인지 확인

                    while (currentY < imgHeight)
                    {
                        int h = Math.Min(sliceHeight, imgHeight - currentY);

                        // 메모리 복사 없이 ROI 생성
                        OpenCvSharp.Rect roi = new OpenCvSharp.Rect(0, currentY, imgWidth, h);

                        using (Mat subMat = bigMat.SubMat(roi))
                        {
                            // Mat -> BitmapSource 변환
                            BitmapSource bmp = subMat.ToBitmapSource();
                            bmp.Freeze(); // UI 스레드 접근 허용
                            listBitmaps.Add(bmp);
                        }
                        currentY += h;
                    }

                    // 4. UI 바인딩
                    icImageList.ItemsSource = listBitmaps;

                    // 5. 초기 배율 설정
                    m_dScale = myScaleTransform.ScaleX;
                    SetFitScale();
                    UpdateFps();
                }
                catch (Exception ex)
                {
                    delWriteLog?.Invoke($"{this.Title} Error: {ex.Message}");
                }
            }));
        }
    }
}
