using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace BlackTube
{
    static class Program
    {
        private static Mutex _appMutex = null;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int SetCurrentProcessExplicitAppUserModelID(string AppID);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        private const int SW_RESTORE = 9;

        [STAThread]
        static void Main()
        {
            const string mutexId = "Global\\BlackTube_SingleInstance_Mutex_8c3b1a";
            bool isNewInstance;

            try { SetCurrentProcessExplicitAppUserModelID("com.shivam.blacktube"); } catch { }
            try { SetProcessDPIAware(); } catch { }

            _appMutex = new Mutex(true, mutexId, out isNewInstance);

            if (!isNewInstance)
            {
                // Bring existing instance to front
                Process current = Process.GetCurrentProcess();
                foreach (Process proc in Process.GetProcessesByName(current.ProcessName))
                {
                    if (proc.Id != current.Id && proc.MainWindowHandle != IntPtr.Zero)
                    {
                        ShowWindow(proc.MainWindowHandle, SW_RESTORE);
                        SetForegroundWindow(proc.MainWindowHandle);
                        break;
                    }
                }
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
