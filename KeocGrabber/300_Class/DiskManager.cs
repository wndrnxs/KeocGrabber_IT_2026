/*
 *******************************************************************************
 * 해당 소스는 현장 유지 보수 외에 다른 목적의 사용을 금합니다.
 * - 주식회사 태루 -
 *******************************************************************************
 */
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KeocGrabber
{
    /// <summary>
    /// 디스크 파일 관리.
    /// 1. 디스크 경로 설정
    /// 2. 대상 확장자 설정.
    /// 2. 데이터 유지 기간 설정
    /// 3. 감시 시작
    /// 
    /// 파일 목록 갱신. (파일 생성되면 추가.)
    /// 
    /// </summary>
    class DiskManager
    {
        string m_strDstImgPath = "./";
        string m_strDstLogPath = "./";
        string m_strDstImgExt = "bmp";
        string m_strDstLogExt = "csv";

        Thread m_threadChk = null;
        bool m_bWorkingChk = false;
        int m_nIntervalChk = 0;

        Thread m_threadDel = null;
        bool m_bWorkingDel = false;
        int m_nIntervalDel = 0;

        Mutex m_mutexImg = new Mutex();
        Mutex m_mutexLog = new Mutex();

        List<FileInfo> m_listImg = new List<FileInfo>();
        List<FileInfo> m_listLog = new List<FileInfo>();

        Mutex m_mutexDel = new Mutex();
        Queue<FileInfo> m_queDelImg = new Queue<FileInfo>();

        FileSystemWatcher m_FolderImgWatcher;
        FileSystemWatcher m_FolderImgWatcher2;
        FileSystemWatcher m_FolderLogWatcher;

        int m_nMaintenanceDays = 30;

        public string DstImgPath { get { return m_strDstImgPath; } set { m_strDstImgPath = value; } }
        public string DstLogPath { get { return m_strDstLogPath; } set { m_strDstLogPath = value; } }
        public string DstImgExt { get { return m_strDstImgExt; } set { m_strDstImgExt = value; } }
        public string DstLogExt { get { return m_strDstLogExt; } set { m_strDstLogExt = value; } }

        public int MaintenaceDay { get { return m_nMaintenanceDays; } set { m_nMaintenanceDays = value; } }

        Thread m_threadImageSave = null;
        bool m_bWorking = false;
        string m_strImageSavePath = "/Image/";
        Mutex m_mutexImgSave = new Mutex();
        Queue<Tuple<int, Mat, string>> m_queImg = new Queue<Tuple<int, Mat, string>>();
        public string ImageSavePath { get { return m_strImageSavePath; } set { m_strImageSavePath = value; } }

        List<double> m_dDriveUsage = new List<double>();
        List<double> m_dTotalDriveSize = new List<double>();
        List<string> m_strDriveLetter = new List<string>();

        int m_nSaveDriveIdx = 0;
        int m_nDelDriveIdx = 0;

        double m_dDeleteThreshold = 0.01;
        double m_dSwitchThreshold = 0.9;

        bool m_bIsAddDrive = false;

        public void fn_AddDriveLetter(string[] strLatter)
        {
            m_strDriveLetter.Clear();
            m_dDriveUsage.Clear();
            m_dTotalDriveSize.Clear();
            string strDrive = "";
            var list = DriveInfo.GetDrives();
            for (int i = 0; i < strLatter.Length; i++)
            {
                strDrive = strLatter[i];
                
                if (!strLatter[i].Contains(':')) strDrive += ":";

                m_strDriveLetter.Add(strDrive);

                foreach (var drive in list)
                {
                    if (drive.Name.Contains(strLatter[i]))
                    {
                        m_dDriveUsage.Add((drive.TotalSize - drive.TotalFreeSpace) / (double)drive.TotalSize);
                        m_dTotalDriveSize.Add(drive.TotalSize / 1024.0 / 1024.0 / 1024.0);
                        break;
                    }
                }
            }

            if (m_strDriveLetter.Count != m_dTotalDriveSize.Count)
            {
                // error;
                G.WriteLog($"Invalid Drive Info. Set Drive {m_strDriveLetter.Count} - Add Drive {m_dTotalDriveSize.Count}",true);
                return;
            }

            //G.SYSTEM.SaveDrive

            int nIdx = 0;
            for (int i = 0; i < m_strDriveLetter.Count; i++)
            {
                // System Param에서 SaveDrive 사용.
                if (m_strDriveLetter[i].Contains(G.SYSTEM.SaveDrive))
                {
                    nIdx = i;
                }
            }

            m_nSaveDriveIdx = nIdx;
            m_nDelDriveIdx = m_strDriveLetter.Count - 1 - m_nSaveDriveIdx;
            m_bIsAddDrive = true;
            G.WriteLog($"Save Drive : {m_strDriveLetter[m_nSaveDriveIdx]} | Del Drive : {m_strDriveLetter[m_nDelDriveIdx]}");
        }


        public void fn_StartThread()
        {

            m_threadImageSave = new Thread(new ThreadStart(THREAD_IMAGESAVE));
            m_bWorking = true;
            m_threadImageSave.Start();

            /** 디스크 관리 비활성화. Taeroo-kgseon 2025.04.01 11:05:09 */
            //if (m_bIsAddDrive)
            //{
            //    m_listImg.Clear();
            //    m_listLog.Clear();

            //    fn_GetFileList();
            //    m_nIntervalChk = 1 * 60 * 1000; // 1분에 한번. (1 [m] * 60 [s] * 1000 [ms])
            //    //m_nIntervalChk = 100; // 1분에 한번. (1 [m] * 60 [s] * 1000 [ms])
            //    m_bWorkingChk = true;
            //    m_threadChk = new Thread(new ThreadStart(THREAD_CHECKFILE));
            //    m_threadChk.Start();

            //    m_nIntervalDel = 1000;
            //    m_bWorkingDel = true;
            //    m_threadDel = new Thread(new ThreadStart(THREAD_DELETEFILE));
            //    m_threadDel.Start();

            //    fn_RunWatcher();
            //}
        }

        public void fn_StopThread()
        {
            m_bWorking = false;
            if (m_threadImageSave.IsAlive)
            {
                m_threadImageSave.Abort();
            }
            /** 디스크 관리 비활성화. Taeroo-kgseon 2025.04.01 11:05:19 */
            //fn_StopWatcher();

            //m_bWorkingDel = false;
            //if (m_threadDel != null && m_threadDel.IsAlive)
            //{
            //    m_threadDel.Abort();
            //    m_threadDel = null;
            //}

            //m_bWorkingChk = false;
            //if (m_threadChk != null && m_threadChk.IsAlive)
            //{
            //    m_threadChk.Abort();
            //    m_threadChk = null;
            //}
        }

        private void fn_EnqueDelFile(FileInfo file)
        {
            m_mutexDel.WaitOne();
            m_queDelImg.Enqueue(file);
            m_mutexDel.ReleaseMutex();
        }

        private FileInfo fn_DequeDelFile()
        {
            FileInfo info = null;
            if (m_queDelImg.Count > 0)
            {
                if (m_mutexDel.WaitOne(3000))
                {
                    info = m_queDelImg.Dequeue();
                    m_mutexDel.ReleaseMutex();
                }
            }
            return info;
        }

        private void fn_GetFileList()
        {
            try
            {
                for (int i = 0; i < m_strDriveLetter.Count; i++)
                {
                    if (Directory.Exists(m_strDriveLetter[i] + m_strDstImgPath))
                    {
                        var strImgFiles = Directory.GetFiles(m_strDriveLetter[i] + m_strDstImgPath, $"*.{m_strDstImgExt}");
                        G.WriteLog($"GetImageList : {strImgFiles}");
                        m_listImg.Clear();
                        foreach (var files in strImgFiles)
                        {
                            m_listImg.Add(new FileInfo(files));
                        }
                        G.WriteLog($"GetImageList Done : {m_listImg.Count}");
                    }
                }

                if (Directory.Exists(m_strDstLogPath))
                {
                    var strLogFiles = Directory.GetFiles(m_strDstLogPath, $"*.{m_strDstLogExt}");
                    G.WriteLog($"GetLogList : {strLogFiles}");
                    m_listLog.Clear();
                    foreach (var files in strLogFiles)
                    {
                        m_listLog.Add(new FileInfo(files));
                    }
                    G.WriteLog($"GetLogList Done : {m_listLog.Count}");
                }
            }
            catch (Exception ex)
            {
                G.WriteLog(ex.Message, true);
            }
        }

        private void fn_RunWatcher()
        {
            try
            {
                m_strDstImgPath = m_strDstImgPath.Replace('/', '\\');
                m_strDstImgPath = m_strDstImgPath.Replace(".\\", "\\");
                //m_strDstImgPath = m_strDstImgPath.Replace(":","");
                //if (m_strDstImgPath.IndexOf(".\\") == 0)
                //{
                //    m_strDstImgPath = m_strDstImgPath.Replace(".\\", $"{Directory.GetCurrentDirectory()}\\");
                //}
                m_FolderImgWatcher = new FileSystemWatcher();
                m_FolderImgWatcher.Filter = $"*.{m_strDstImgExt}";
                m_FolderImgWatcher.Path = m_strDriveLetter[m_nSaveDriveIdx] + m_strDstImgPath;
                m_FolderImgWatcher.IncludeSubdirectories = true;
                m_FolderImgWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                m_FolderImgWatcher.Created += new FileSystemEventHandler(ImgWatcher_OnChanged);
                m_FolderImgWatcher.EnableRaisingEvents = true;

                m_FolderImgWatcher2 = new FileSystemWatcher();
                m_FolderImgWatcher2.Filter = $"*.{m_strDstImgExt}";
                m_FolderImgWatcher2.Path = m_strDriveLetter[m_nDelDriveIdx] + m_strDstImgPath;
                m_FolderImgWatcher2.IncludeSubdirectories = true;
                m_FolderImgWatcher2.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                m_FolderImgWatcher2.Created += new FileSystemEventHandler(ImgWatcher_OnChanged);
                m_FolderImgWatcher2.EnableRaisingEvents = true;

                m_FolderLogWatcher = new FileSystemWatcher();
                m_FolderLogWatcher.Filter = $"*.{m_strDstLogExt}";
                m_FolderLogWatcher.Path = m_strDstLogPath;
                m_FolderLogWatcher.IncludeSubdirectories = true;
                m_FolderLogWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                m_FolderLogWatcher.Created += new FileSystemEventHandler(LogWatcher_OnChanged);
                m_FolderLogWatcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                G.WriteLog(ex.Message, true);
            }
        }

        private void fn_StopWatcher()
        {
                if (m_FolderImgWatcher != null)
                    m_FolderImgWatcher.EnableRaisingEvents = false;

                if (m_FolderLogWatcher != null)
                    m_FolderLogWatcher.EnableRaisingEvents = false;
        }

        private void ImgWatcher_OnChanged(object sender, FileSystemEventArgs e) 
        {
            m_mutexImg.WaitOne();
            m_listImg.Add(new FileInfo(e.FullPath));
            m_mutexImg.ReleaseMutex();
        }

        private void LogWatcher_OnChanged(object sender, FileSystemEventArgs e) 
        {
            m_mutexLog.WaitOne();
            m_listLog.Add(new FileInfo(e.FullPath));
            m_mutexLog.ReleaseMutex();
        }

        public void THREAD_CHECKFILE()
        {
            DateTime now;
            double dTotalLengthGB = 0;
            while (m_bWorkingChk)
            {
                now = DateTime.Now;
                Thread.Sleep(m_nIntervalChk);

                // 용량 업데이트.
                var list = DriveInfo.GetDrives();
                for (int i = 0; i < m_strDriveLetter.Count; i++)
                {
                    foreach (var drive in list)
                    {
                        if (drive.Name.Contains(m_strDriveLetter[i]))
                        {
                            m_dDriveUsage[i] = ((drive.TotalSize - drive.TotalFreeSpace) / (double)drive.TotalSize);
                            m_dTotalDriveSize[i] = (drive.TotalSize / 1024.0 / 1024.0 / 1024.0);
                            break;
                        }
                    }
                }

                // 저장 드라이브 + 삭제 드라이브 용량이 90퍼센트가 넘는다면
                if (m_dDriveUsage[m_nDelDriveIdx] + m_dDriveUsage[m_nSaveDriveIdx] > m_dSwitchThreshold)
                {
                    // LOG | CheckFile List : DelDrive[D / 0.90] SaveDrive[E / 0.10]
                    G.WriteLog($"CheckFile List : DelDrive[{m_strDriveLetter[m_nDelDriveIdx]} / {m_dDriveUsage[m_nDelDriveIdx]:F2}] SaveDrive[{m_strDriveLetter[m_nSaveDriveIdx]} / {m_dDriveUsage[m_nSaveDriveIdx]}]");

                    // 삭제대기 큐에 넣은 용량 확인.
                    dTotalLengthGB = 0;
                    foreach (var img in m_listImg)
                    {
                        // img 경로가 해당 문자를 포함 하고 있다면.
                        if (img.FullName.Contains(m_strDriveLetter[m_nDelDriveIdx]))
                        {
                            // 삭제대기 큐에 넣은 이미지 용량 누적.
                            dTotalLengthGB += (img.Length / 1024.0 / 1024.0 / 1024.0); // byte -> GB 변환.
                            if (m_mutexImg.WaitOne(3000))
                            {
                                fn_EnqueDelFile(img);
                                m_listImg.Remove(img);
                                m_mutexImg.ReleaseMutex();
                            }

                            // 전체 용량의 1퍼센트까지만 삭제대기에 추가.
                            if (dTotalLengthGB > m_dDeleteThreshold * m_dTotalDriveSize[m_nDelDriveIdx])
                            {
                                break;
                            }
                        }
                    }
                    //if ((now - img.CreationTime).Days >= m_nMaintenanceDays)
                    //{
                    //    if (m_mutexImg.WaitOne(3000))
                    //    {
                    //        fn_EnqueDelFile(img);
                    //        m_listImg.Remove(img);
                    //        m_mutexImg.ReleaseMutex();
                    //    }
                    //}
                }

                foreach (var log in m_listLog)
                {
                    if ((now - log.CreationTime).Days >= m_nMaintenanceDays)
                    {
                        if (m_mutexLog.WaitOne(3000))
                        {
                            fn_EnqueDelFile(log);
                            m_listLog.Remove(log);
                            m_mutexLog.ReleaseMutex();
                        }
                    }
                }
            }
        }

        public void THREAD_DELETEFILE()
        {
            FileInfo info = null;
            while (m_bWorkingDel)
            {
                info = fn_DequeDelFile();
                if (info != null)
                {
                    // 파일 삭제.
                    info.Delete();

                    // Directory가 비었으면 삭제 시도.
                    if (info.Directory != null && Directory.GetFiles(info.Directory.FullName).Length > 0)
                    {
                        info.Directory.Delete(false);
                    }
                }
                else
                {
                    Thread.Sleep(m_nIntervalDel);
                }
            }
        }


        private void SaveImage(Tuple<int, Mat, string> imgdata)
        {
            try
            {
                // drive 용량이 90프로이면 인덱스 스위치.
                //if (m_dDriveUsage[m_nSaveDriveIdx] > m_dSwitchThreshold)
                //{
                //    int tempidx = m_nDelDriveIdx;
                //    m_nDelDriveIdx = m_nSaveDriveIdx;
                //    m_nSaveDriveIdx = tempidx;
                //    G.SYSTEM.SaveDrive = m_strDriveLetter[m_nSaveDriveIdx];
                //}

                DateTime now = DateTime.Now;
                // Create Directory.
                //string strSavePath = $"{m_strDriveLetter[m_nSaveDriveIdx]}\\{ImageSavePath}/{now:yyyy}/{now:MM}/{now:dd}/";
                DstImgPath = G.GetSavePath();
                if (DstImgPath == "")
                {
                    throw new FileLoadException("");
                }
                string strSavePath = $"{DstImgPath}/";
                Directory.CreateDirectory(strSavePath);

                string strImgName = $"{imgdata.Item3}_{imgdata.Item1}";
                string strImageFileName = $"{strSavePath}{strImgName}.{G.SYSTEM.ImageExt}";

                // 중복 파일명 처리.
                int idx = 1;
                while (File.Exists(strImageFileName))
                {
                    strImageFileName = $"{strSavePath}{strImgName}_{idx}.{G.SYSTEM.ImageExt}";
                    idx++;
                }

                // 파일저장
                //imgdata.Item2.ImWrite(strImageFileName);
                //Cv2.ImWrite(strImageFileName, imgdata.Item2);
                //if (G.SYSTEM.ImageExt == "jpg")
                //    Cv2.ImWrite(strImageFileName, imgdata.Item2, new int[] { (int)ImwriteFlags.JpegQuality, 100 });
                //else
                    Cv2.ImWrite(strImageFileName, imgdata.Item2);

                G.WriteLog($"{strImageFileName} Save Done.");
            }
            catch (Exception ex)
            {
                G.WriteLog(ex.Message, true);
            }
        }

        public void PushSaveImage(int index, Mat img, string lotid)
        {
            m_mutexImgSave.WaitOne();
            m_queImg.Enqueue(new Tuple<int, Mat, string>(index, img, lotid));
            m_mutexImgSave.ReleaseMutex();

            //             m_mutexTransfer.WaitOne();
            //             m_listImg.Add(new Tuple<int, Mat, string>(index, img, lotid));
            //             m_mutexTransfer.ReleaseMutex();

            G.WriteLog($"{lotid} Image Push.");
        }


        private Tuple<int, Mat, string> PopImage()
        {
            Tuple<int, Mat, string> data = null;

            if (m_queImg.Count > 0)
            {
                if (m_mutexImgSave.WaitOne(3000))
                {
                    data = m_queImg.Dequeue();
                    m_mutexImgSave.ReleaseMutex();
                    G.WriteLog($"{data.Item3}_{data.Item1} Image Pop.");
                }
                else
                {
                    G.WriteLog($"Time-Out Image Pop.");
                }
            }

            return data;
        }

        private void THREAD_IMAGESAVE()
        {
            Tuple<int, Mat, string> data = null;
            while (m_bWorking)
            {
                data = PopImage();
                if (data != null)
                {
                    SaveImage(data);
                }
                else
                {
                    Thread.Sleep(500);
                }
            }
        }
    }
}
