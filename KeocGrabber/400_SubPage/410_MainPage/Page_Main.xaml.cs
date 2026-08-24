/*
 *******************************************************************************
 * 해당 소스는 현장 유지 보수 외에 다른 목적의 사용을 금합니다.
 * - 주식회사 태루 -
 *******************************************************************************
 */
using OpenCvSharp.Flann;
using OpenCvSharp.WpfExtensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using OpenCvSharp;
namespace KeocGrabber
{
    public class Page_MainDataContext : MVVMBase.IPropertyChanged
    {
        Page_Process pageProcess = new Page_Process();
        public Page_Process PageProcess { get { return pageProcess; } set { pageProcess = value; OnPropertyChanged("PageProcess"); } }

        string strCellID = "";
        public string CellID { get { return strCellID; } set { strCellID = value; OnPropertyChanged(); } }

        string strCam1State = "";
        string strCam2State = "";
        string strCam3State = "";
        string strCam4State = "";

        public string Cam1Grab { get { return strCam1State; } set { strCam1State = value; OnPropertyChanged(); } }
        public string Cam2Grab { get { return strCam2State; } set { strCam2State = value; OnPropertyChanged(); } }
        public string Cam3Grab { get { return strCam3State; } set { strCam3State = value; OnPropertyChanged(); } }
        public string Cam4Grab { get { return strCam4State; } set { strCam4State = value; OnPropertyChanged(); } }

        public void SetGrabState(int idx, string strState)
        {
            switch (idx)
            {
                case 0: Cam1Grab = strState; break;
                case 1: Cam2Grab = strState; break;
                case 2: Cam3Grab = strState; break;
                case 3: Cam4Grab = strState; break;
            }
        }

        double dTestBtnHeight = 0;
        public double TestBtnHeight { get { return dTestBtnHeight; } set { dTestBtnHeight = value; OnPropertyChanged(); } }

        // ── Sensor I/O (Euresys 15pin D-Sub #3 = IIN11+, #12 = IIN11-) ──────────
        // 센서 신호(스캔 시작 트리거)가 보드까지 들어오는지 확인하는 표시.
        string strSensorTitle = "Sensor I/O";
        public string SensorTitle { get { return strSensorTitle; } set { if (strSensorTitle == value) return; strSensorTitle = value; OnPropertyChanged(); } }

        bool bSensor1On = false;
        bool bSensor2On = false;
        bool bSensor3On = false;
        bool bSensor4On = false;

        public bool Sensor1On { get { return bSensor1On; } set { if (bSensor1On == value) return; bSensor1On = value; OnPropertyChanged(); } }
        public bool Sensor2On { get { return bSensor2On; } set { if (bSensor2On == value) return; bSensor2On = value; OnPropertyChanged(); } }
        public bool Sensor3On { get { return bSensor3On; } set { if (bSensor3On == value) return; bSensor3On = value; OnPropertyChanged(); } }
        public bool Sensor4On { get { return bSensor4On; } set { if (bSensor4On == value) return; bSensor4On = value; OnPropertyChanged(); } }

        // Info = 램프 옆 짧은 표기("CAM1 12"), Detail = 마우스 오버 시 상세(검출 시각/펄스 폭)
        string strSensor1Info = "-";
        string strSensor2Info = "-";
        string strSensor3Info = "-";
        string strSensor4Info = "-";

        public string Sensor1Info { get { return strSensor1Info; } set { if (strSensor1Info == value) return; strSensor1Info = value; OnPropertyChanged(); } }
        public string Sensor2Info { get { return strSensor2Info; } set { if (strSensor2Info == value) return; strSensor2Info = value; OnPropertyChanged(); } }
        public string Sensor3Info { get { return strSensor3Info; } set { if (strSensor3Info == value) return; strSensor3Info = value; OnPropertyChanged(); } }
        public string Sensor4Info { get { return strSensor4Info; } set { if (strSensor4Info == value) return; strSensor4Info = value; OnPropertyChanged(); } }

        string strSensor1Detail = "";
        string strSensor2Detail = "";
        string strSensor3Detail = "";
        string strSensor4Detail = "";

        public string Sensor1Detail { get { return strSensor1Detail; } set { if (strSensor1Detail == value) return; strSensor1Detail = value; OnPropertyChanged(); } }
        public string Sensor2Detail { get { return strSensor2Detail; } set { if (strSensor2Detail == value) return; strSensor2Detail = value; OnPropertyChanged(); } }
        public string Sensor3Detail { get { return strSensor3Detail; } set { if (strSensor3Detail == value) return; strSensor3Detail = value; OnPropertyChanged(); } }
        public string Sensor4Detail { get { return strSensor4Detail; } set { if (strSensor4Detail == value) return; strSensor4Detail = value; OnPropertyChanged(); } }

        public void SetSensor(int idx, bool bOn, string strInfo, string strDetail)
        {
            switch (idx)
            {
                case 0: Sensor1On = bOn; Sensor1Info = strInfo; Sensor1Detail = strDetail; break;
                case 1: Sensor2On = bOn; Sensor2Info = strInfo; Sensor2Detail = strDetail; break;
                case 2: Sensor3On = bOn; Sensor3Info = strInfo; Sensor3Detail = strDetail; break;
                case 3: Sensor4On = bOn; Sensor4Info = strInfo; Sensor4Detail = strDetail; break;
            }
        }

        bool bIsGrabStop = true;
        public bool IsGrabStop { get { return bIsGrabStop; } set { bIsGrabStop = value; OnPropertyChanged(); } }
    }

    /// <summary>
    /// Page_Main.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Page_Main : Page
    {
        public Page_Main()
        {
            InitializeComponent();
        }

        public void fn_ViewTestArea()
        {
            datacontext.TestBtnHeight = G.USERLEVEL == EN_AUTHORITY.EN_ENGINEER ? 20 : 0;
        }

        public void fn_WriteLog(string strMsg, bool bError)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate () {
                TextBlock tb = new TextBlock();
                tb.Text = strMsg;

                if (bError)
                    tb.Background = Brushes.Tomato;

                listLog.Items.Add(tb);
                if (listLog.Items.Count > 200)
                {
                    listLog.Items.RemoveAt(0);
                }

                listLog.ScrollIntoView(listLog.Items[listLog.Items.Count - 1]);
            }));
        }

        /// <summary>
        /// 센서 I/O 표시 갱신. (MainWindow의 500ms 타이머에서 호출)
        /// 신호는 짧게 지나가므로 SensorIOManager가 SensorLampHold(ms) 동안 램프를 잡아 준다.
        /// </summary>
        public void Update_IOStatus()
        {
            bool bRun = G.SENSORIO.IsRunning;
            datacontext.SensorTitle = bRun
                ? $"SENSOR {G.GRABBER.fn_GetSensorLine(0)}"
                : "SENSOR OFF";

            for (int i = 0; i < Define.CAM_COUNT; i++)
            {
                if (!bRun)
                {
                    datacontext.SetSensor(i, false, $"CAM{i + 1} -", "Sensor I/O 사용 안 함");
                    continue;
                }
                datacontext.SetSensor(i, G.SENSORIO.fn_IsSignalOn(i),
                                         $"CAM{i + 1}", /*{G.SENSORIO.fn_GetCount(i)}*/
                                         G.SENSORIO.fn_GetDetailText(i));
            }
        }

        private void SensorIO_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2) return;

            if (G.USERLEVEL != EN_AUTHORITY.EN_OPERATOR)
            {
                G.SENSORIO.fn_ResetCount();
                Update_IOStatus();
            }
        }

        private void bn_testgrab_click(object sender, RoutedEventArgs e)
        {
            G.IMAGEMANAGER.InitAttachCount(0);
            G.IMAGEMANAGER.InitAttachCount(1);
            G.IMAGEMANAGER.InitAttachCount(2);
            G.IMAGEMANAGER.InitAttachCount(3);
            G.GRABBER.fn_GrabStart();
        }

        private void bn_testgrabstop_click(object sender, RoutedEventArgs e)
        {
            G.GRABBER.fn_GrabStop();
        }

        private void bn_testsave_click(object sender, RoutedEventArgs e)
        {
            OpenCvSharp.Mat mattemp = OpenCvSharp.Mat.Zeros(35715, 16384, OpenCvSharp.MatType.CV_8UC1);
            Task.Run(() => { 
            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 10; i++)
            {
                OpenCvSharp.Cv2.ImWrite($"D:\\Test{i}.jpg", mattemp);
            }
            G.WriteLog($"jpg : {sw.ElapsedMilliseconds:F2} ms");
            sw.Reset();
            sw.Start();
            for (int i = 0; i < 10; i++)
            {
                OpenCvSharp.Cv2.ImWrite($"D:\\Test{i}.bmp", mattemp);
            }
            G.WriteLog($"bmp : {sw.ElapsedMilliseconds:F2} ms");
            sw.Reset();
            sw.Start();
            for (int i = 0; i < 10; i++)
            {
                OpenCvSharp.Cv2.ImWrite($"D:\\Test{i}.png", mattemp);
            }
            G.WriteLog($"png : {sw.ElapsedMilliseconds:F2} ms");
            sw.Stop();
            });
        }

        private void GrabState1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (G.USERLEVEL != EN_AUTHORITY.EN_OPERATOR)
            {
                G.IMAGEMANAGER.SetCompl(0);
            }
        }

        private void GrabState2_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (G.USERLEVEL != EN_AUTHORITY.EN_OPERATOR)
            {
                G.IMAGEMANAGER.SetCompl(1);
            }
        }

        private void GrabState3_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (G.USERLEVEL != EN_AUTHORITY.EN_OPERATOR)
            {
                G.IMAGEMANAGER.SetCompl(2);
            }
        }

        private void GrabState4_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (G.USERLEVEL != EN_AUTHORITY.EN_OPERATOR)
            {
                G.IMAGEMANAGER.SetCompl(3);
            }
        }

        private void bn_Cam4ViewImage_Click(object sender, RoutedEventArgs e)
        {
            if (!G.IMAGEMANAGER.CamImage[3].Empty())
            {
                imgview4.SetLargeImage(G.IMAGEMANAGER.CamImage[3].Clone());
                //imgview4.SetImage(WriteableBitmapConverter.ToWriteableBitmap(G.IMAGEMANAGER.CamImage[3]));
            }
        }

        private void bn_Cam4ViewClear_Click(object sender, RoutedEventArgs e)
        {
            imgview4.ClearCanvas();
        }

        private void bn_Cam3ViewClear_Click(object sender, RoutedEventArgs e)
        {
            imgview3.ClearCanvas();
        }

        private void bn_Cam3ViewImage_Click(object sender, RoutedEventArgs e)
        {
            if (!G.IMAGEMANAGER.CamImage[2].Empty())
            {
                imgview3.SetLargeImage(G.IMAGEMANAGER.CamImage[2].Clone());
                //imgview3.SetImage(WriteableBitmapConverter.ToWriteableBitmap(G.IMAGEMANAGER.CamImage[2]));
            }
        }

        private void bn_Cam2ViewClear_Click(object sender, RoutedEventArgs e)
        {
            imgview2.ClearCanvas();
        }

        private void bn_Cam2ViewImage_Click(object sender, RoutedEventArgs e)
        {
            if (!G.IMAGEMANAGER.CamImage[1].Empty())
            {
                imgview2.SetLargeImage(G.IMAGEMANAGER.CamImage[1].Clone());
                //imgview2.SetImage(WriteableBitmapConverter.ToWriteableBitmap(G.IMAGEMANAGER.CamImage[1]));
            }
        }

        private void bn_Cam1ViewClear_Click(object sender, RoutedEventArgs e)
        {
            imgview1.ClearCanvas();
        }

        private void bn_Cam1ViewImage_Click(object sender, RoutedEventArgs e)
        {
            if (!G.IMAGEMANAGER.CamImage[0].Empty())
            {
                imgview1.SetLargeImage(G.IMAGEMANAGER.CamImage[0].Clone());
                //imgview1.SetImage(WriteableBitmapConverter.ToWriteableBitmap(G.IMAGEMANAGER.CamImage[0]));
            }
        }

        private void bn_ManualGrab_Click(object sender, RoutedEventArgs e)
        {
            datacontext.IsGrabStop = false;

            // state.CHECK 시뮬레이션
            G.GrabStart();

            // ReceiveReady 시뮬레이션: 가상 셀 ID로 요청 큐에 적재
            string fakeCellId = $"MANUAL_{DateTime.Now:yyyyMMddHHmmss}";
            for (int i = 0; i < G.SYSTEM.CamCount; i++)
            {
                G.MSGPROC.fn_PushRequest(fakeCellId, 0, 0, 0, 0, i, new int[] { G.SYSTEM.MyNodeID }, null);
            }
        }
        private void CreateTestImage()
        {
            // 1. 설정: 실제 현장과 동일한 해상도
            int width = 16384;
            int height = 55000;
            string savePath = @"C:\KEOC\TestImage_16k_55k.bmp"; // 저장 경로

            // 2. 초대형 Mat 생성 (CV_8UC1: 흑백 1채널)
            // 주의: 약 900MB 메모리 소요됨
            using (Mat bigMat = new Mat(height, width, MatType.CV_8UC1, new Scalar(50))) // 회색 배경
            {
                // 3. 패턴 그리기 (눈으로 위치 확인용)

                // (1) 1000픽셀 간격으로 격자 무늬 그리기
                for (int y = 0; y < height; y += 1000)
                {
                    Cv2.Line(bigMat, 0, y, width, y, Scalar.White, 5); // 가로줄

                    // 좌표 텍스트 적기 (어디쯤인지 알기 위해)
                    Cv2.PutText(bigMat, $"Line: {y}", new OpenCvSharp.Point(100, y + 50),
                        HersheyFonts.HersheySimplex, 5.0, Scalar.White, 10);
                }

                for (int x = 0; x < width; x += 1000)
                {
                    Cv2.Line(bigMat, x, 0, x, height, Scalar.White, 5); // 세로줄
                }

                // (2) Cell 1, Cell 2 시뮬레이션 (네모 박스 그리기)
                // Cell 1 (앞쪽)
                Cv2.Rectangle(bigMat, new OpenCvSharp.Rect(4000, 2000, 8000, 5000), Scalar.Black, -1); // 검은 박스
                Cv2.PutText(bigMat, "CELL 1 (FRONT)", new OpenCvSharp.Point(5000, 4500),
                    HersheyFonts.HersheySimplex, 10.0, Scalar.White, 20);

                // Cell 2 (뒤쪽) - 아까 문제였던 영역
                Cv2.Rectangle(bigMat, new OpenCvSharp.Rect(4000, 30000, 8000, 5000), Scalar.Black, -1);
                Cv2.PutText(bigMat, "CELL 2 (REAR)", new OpenCvSharp.Point(5000, 32500),
                    HersheyFonts.HersheySimplex, 10.0, Scalar.White, 20);

                // 4. 파일 저장 (시간이 조금 걸립니다)
                // BMP가 가장 빠르고 압축 손실이 없습니다.
                bool result = Cv2.ImWrite(savePath, bigMat);

                if (result)
                    MessageBox.Show($"이미지 생성 완료!\n경로: {savePath}");
                else
                    MessageBox.Show("이미지 저장 실패");
            }
        }

        private void bn_Test_Click(object sender, RoutedEventArgs e)
        {
            CreateTestImage();
            //Stopwatch sw = new Stopwatch();
            //string rtn =  ProtocallManager.Encode("STA.SEND.ANGLEVIEW.SYNC.0.");
            //sw.Restart();
            //G.SyncRecipe("TestRecipe");
            ////ProtocallManager.MessageAnalyzer(rtn);
            //G.WriteLog($"{sw.ElapsedMilliseconds} ms");


            //RequestData request = null;
#if DEBUG
            //G.MSGPROC.m_Que.TryDequeue(out request);
#endif
            //string testImagePath = @"C:\KEOC\Recipe\test.png";

            //if (!G.IMAGEMANAGER.LoadTestImage(request.ImageIndex, testImagePath))
            //{
            //    MessageBox.Show("테스트 이미지를 로드하지 못했습니다. 경로를 확인하세요.");
            //    return;
            //}

            //string testRtnMsg = "STA.SEND.ANGLEVIEW.ReceiveReady.TESTCELL1234.0.2.0.0.0.4.2:3:4:5";

            //string testPacket = ProtocallManager.Encode(testRtnMsg);

            //G.WriteLog($"[테스트] 다음 패킷을 시뮬레이션합니다: {testPacket.Replace("\0", "\\0")}");

            //try
            //{

            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show($"테스트 중 예외 발생: {ex.Message}");
            //}
        }
    }
}
