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

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int RegisterWindowMessage(string lpString);

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int SetCurrentProcessExplicitAppUserModelID(string AppID);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiFlag);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);
        private static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff);
        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;

        [STAThread]
        static void Main()
        {
            const string mutexId = "Global\\BlackTube_YouTube_SingleInstance_8c3b1a";
            bool isNewInstance = false;

            try { SetCurrentProcessExplicitAppUserModelID("com.shivam.blacktube.youtube"); } catch { }

            try
            {
                if (!SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2))
                {
                    SetProcessDPIAware();
                }
            }
            catch
            {
                try { SetProcessDPIAware(); } catch { }
            }

            try
            {
                _appMutex = new Mutex(true, mutexId, out isNewInstance);
            }
            catch
            {
                isNewInstance = true;
            }

            if (!isNewInstance)
            {
                // Find existing instance process
                Process current = Process.GetCurrentProcess();
                Process[] existing = Process.GetProcessesByName(current.ProcessName);
                bool restored = false;

                foreach (Process proc in existing)
                {
                    if (proc.Id != current.Id)
                    {
                        uint targetPid = (uint)proc.Id;
                        EnumWindows((hWnd, lParam) =>
                        {
                            uint wPid;
                            GetWindowThreadProcessId(hWnd, out wPid);
                            if (wPid == targetPid)
                            {
                                ShowWindow(hWnd, SW_RESTORE);
                                ShowWindow(hWnd, SW_SHOW);
                                SetForegroundWindow(hWnd);
                                restored = true;
                            }
                            return true;
                        }, IntPtr.Zero);
                    }
                }

                int wmShow = RegisterWindowMessage("WM_BLACKTUBE_SHOW_YOUTUBE");
                if (wmShow != 0)
                {
                    PostMessage(HWND_BROADCAST, wmShow, IntPtr.Zero, IntPtr.Zero);
                }

                if (restored)
                {
                    return;
                }

                // If existing instance was a zombie process without windows, kill it and start fresh
                foreach (Process proc in existing)
                {
                    if (proc.Id != current.Id)
                    {
                        try { proc.Kill(); } catch { }
                    }
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
