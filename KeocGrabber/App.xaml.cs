using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace KeocGrabber
{
    /// <summary>
    /// App.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            this.Dispatcher.UnhandledException +=
          new DispatcherUnhandledExceptionEventHandler(Dispatcher_UnhandledException);

            this.Dispatcher.UnhandledExceptionFilter +=
              new DispatcherUnhandledExceptionFilterEventHandler(Dispatcher_UnhandledExceptionFilter);
        }

        void Dispatcher_UnhandledExceptionFilter(object sender, DispatcherUnhandledExceptionFilterEventArgs e)
        {
            MessageBox.Show(e.Exception.ToString());
            G.WriteLog($"UnHandled Filter Error : {e.Exception}", true);
            e.RequestCatch = true;
        }

        void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.ToString());
            G.WriteLog($"UnHandled Error : {e.Exception}", true);
            e.Handled = true;
        }

        Mutex mutex;
        /// <summary>
        /// 중복 실행 방지 코드
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string mutexName = "KeocGrabber";
            bool createNew;

            mutex = new Mutex(true, mutexName, out createNew);

            if (!createNew)
            {
                MessageBox.Show("Already Running Program.", "KeocGrabber", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }
    }
}
