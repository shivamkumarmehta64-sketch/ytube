using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace BlackTube
{
    public class MainForm : Form
    {
        // ── Win32 / DWM Attributes ──
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_TEXT_COLOR = 36;
        private const int DWMWCP_ROUND = 2;
        private const int SW_RESTORE = 9;

        private static readonly int WM_SHOWME = Program.RegisterWindowMessage("WM_BLACKTUBE_SHOW_YOUTUBE");

        private static void ApplyWindowsTheme(IntPtr handle)
        {
            try
            {
                // Immersive Dark Mode
                int darkMode = 1;
                if (DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int)) != 0)
                {
                    DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref darkMode, sizeof(int));
                }

                // Windows 11 Rounded Corners
                int cornerPref = DWMWCP_ROUND;
                DwmSetWindowAttribute(handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPref, sizeof(int));

                // Dark Title Bar & White Title Text
                int captionColor = 0x000F0F0F; // BGR format for #0F0F0F
                DwmSetWindowAttribute(handle, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));

                int textColor = 0x00FFFFFF;
                DwmSetWindowAttribute(handle, DWMWA_TEXT_COLOR, ref textColor, sizeof(int));
            }
            catch { }
        }

        // ── Global Hotkeys ──
        private const int WM_HOTKEY = 0x0312;
        private const uint VK_MEDIA_NEXT_TRACK = 0xB0;
        private const uint VK_MEDIA_PREV_TRACK = 0xB1;
        private const uint VK_MEDIA_PLAY_PAUSE = 0xB3;

        private const int HOTKEY_PLAY_PAUSE = 9001;
        private const int HOTKEY_NEXT = 9002;
        private const int HOTKEY_PREV = 9003;

        // ── UI Components ──
        private Panel _navBar;
        private FlowLayoutPanel _navLeftFlow;
        private FlowLayoutPanel _navRightFlow;
        private Button _btnBack;
        private Button _btnForward;
        private Button _btnReload;
        private Button _btnHome;
        private Button _btnSearch;
        private Button _btnPin;
        private Button _btnPip;
        private Button _btnFullScreen;
        private Button _btnSleep;
        private Panel _contentPanel;
        private WebView2 _ytWebView;

        private bool _isPipMode = false;
        private bool _isAlwaysOnTop = false;
        private bool _isFullScreen = false;

        // Window state restoration for fullscreen & PiP
        private FormBorderStyle _savedBorderStyle = FormBorderStyle.Sizable;
        private FormWindowState _savedWindowState = FormWindowState.Normal;
        private Rectangle _savedBounds;

        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayMenu;

        private Timer _sleepTimer;
        private int _sleepRemainingSeconds = 0;
        private Timer _gcTimer;
        private string _logPath;

        public MainForm()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

            _logPath = Path.Combine(Application.StartupPath, "debug.log");
            Log("MainForm constructor starting");

            this.Text = "BlackTube - YouTube";
            this.Width = 1280;
            this.Height = 820;
            this.MinimumSize = new Size(480, 320);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(15, 15, 15);
            this.ForeColor = Color.White;
            this.ShowInTaskbar = true;

            string iconPath = Path.Combine(Application.StartupPath, "icon.ico");
            if (File.Exists(iconPath))
            {
                try { this.Icon = new Icon(iconPath); } catch { }
            }

            SetupUI();
            SetupTray();
            SetupTimers();
            SetupHotkeys();

            this.HandleCreated += (s, e) => ApplyWindowsTheme(this.Handle);

            this.Load += async (s, e) =>
            {
                ApplyWindowsTheme(this.Handle);
                await InitializeWebViewAsync();
            };

            this.Shown += (s, e) =>
            {
                ApplyWindowsTheme(this.Handle);
                this.Activate();
                this.BringToFront();
            };

            this.FormClosing += (s, e) =>
            {
                try
                {
                    UnregisterHotKey(this.Handle, HOTKEY_PLAY_PAUSE);
                    UnregisterHotKey(this.Handle, HOTKEY_NEXT);
                    UnregisterHotKey(this.Handle, HOTKEY_PREV);
                }
                catch { }

                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                    _trayIcon.Dispose();
                }
            };
        }

        private void Log(string msg)
        {
            try
            {
                File.AppendAllText(_logPath, "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + msg + Environment.NewLine);
            }
            catch { }
        }

        private void SetupUI()
        {
            // 1. Navigation Header Bar (Compact Windows 11 Fluent Dark UI)
            _navBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = Color.FromArgb(15, 15, 15),
                Padding = new Padding(8, 2, 8, 2)
            };

            _navBar.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(32, 32, 32), 1))
                {
                    e.Graphics.DrawLine(pen, 0, _navBar.Height - 1, _navBar.Width, _navBar.Height - 1);
                }
            };

            // 2. Left Flow (Navigation: Back, Forward, Reload, Home, Brand Badge)
            _navLeftFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };

            _btnBack = CreateNavButton("‹", "Go Back (Alt+Left)", (s, e) => NavigateBack());
            _btnBack.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            _btnBack.Enabled = false;

            _btnForward = CreateNavButton("›", "Go Forward (Alt+Right)", (s, e) => NavigateForward());
            _btnForward.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            _btnForward.Enabled = false;

            _btnReload = CreateNavButton("↻", "Reload (Ctrl+R / F5)", (s, e) => ReloadView());
            _btnReload.Font = new Font("Segoe UI", 10.5f, FontStyle.Regular);

            _btnHome = CreateNavButton("⌂", "Go to YouTube Home (Ctrl+H)", (s, e) => NavigateHome());
            _btnHome.Font = new Font("Segoe UI", 11.5f, FontStyle.Regular);

            var navSeparator = new Panel
            {
                Width = 1,
                Height = 18,
                BackColor = Color.FromArgb(40, 40, 40),
                Margin = new Padding(5, 6, 5, 0)
            };

            _btnSearch = new Button
            {
                Text = "🔍  Search (Ctrl+F)",
                Size = new Size(180, 28),
                Margin = new Padding(2, 2, 4, 0),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(160, 160, 175),
                BackColor = Color.FromArgb(24, 24, 24),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };
            _btnSearch.FlatAppearance.BorderColor = Color.FromArgb(42, 42, 42);
            _btnSearch.FlatAppearance.BorderSize = 1;
            _btnSearch.MouseEnter += (s, e) => { _btnSearch.BackColor = Color.FromArgb(34, 34, 34); _btnSearch.ForeColor = Color.White; };
            _btnSearch.MouseLeave += (s, e) => { _btnSearch.BackColor = Color.FromArgb(24, 24, 24); _btnSearch.ForeColor = Color.FromArgb(160, 160, 175); };
            _btnSearch.Click += (s, e) => FocusYouTubeSearch();

            _navLeftFlow.Controls.Add(_btnBack);
            _navLeftFlow.Controls.Add(_btnForward);
            _navLeftFlow.Controls.Add(_btnReload);
            _navLeftFlow.Controls.Add(_btnHome);
            _navLeftFlow.Controls.Add(navSeparator);
            _navLeftFlow.Controls.Add(_btnSearch);

            // 3. Right Flow (Quick Actions: Pin, PiP, Sleep, Fullscreen)
            _navRightFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };

            _btnFullScreen = CreateToolButton("⛶", "Toggle Full Screen (F11)", (s, e) => ToggleFullScreen());
            _btnPin = CreateToolButton("📌", "Always On Top (Ctrl+Shift+T)", (s, e) => ToggleAlwaysOnTop());
            _btnPip = CreateToolButton("⧉", "Picture-in-Picture Mini Player (Ctrl+Shift+P)", (s, e) => TogglePipMode());
            _btnSleep = CreateToolButton("⏱", "Sleep Timer (Ctrl+Shift+S)", (s, e) => ShowSleepTimerMenu());

            _navRightFlow.Controls.Add(_btnFullScreen);
            _navRightFlow.Controls.Add(_btnPin);
            _navRightFlow.Controls.Add(_btnPip);
            _navRightFlow.Controls.Add(_btnSleep);

            _navBar.Controls.Add(_navRightFlow);
            _navBar.Controls.Add(_navLeftFlow);
            this.Controls.Add(_navBar);

            // 4. Content Area for YouTube WebView2
            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(15, 15, 15)
            };

            _ytWebView = new WebView2
            {
                Dock = DockStyle.Fill,
                Visible = true,
                BackColor = Color.FromArgb(15, 15, 15)
            };
            _contentPanel.Controls.Add(_ytWebView);
            this.Controls.Add(_contentPanel);
        }

        private Button CreateNavButton(string text, string tooltipText, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(30, 28),
                Margin = new Padding(1, 2, 1, 0),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(200, 200, 220),
                BackColor = Color.FromArgb(24, 24, 24),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(42, 42, 42);
            btn.FlatAppearance.BorderSize = 1;
            btn.MouseEnter += (s, e) => { if (btn.Enabled) { btn.BackColor = Color.FromArgb(38, 38, 38); btn.ForeColor = Color.White; } };
            btn.MouseLeave += (s, e) => { if (btn.Enabled) { btn.BackColor = Color.FromArgb(24, 24, 24); btn.ForeColor = Color.FromArgb(200, 200, 220); } };
            btn.EnabledChanged += (s, e) =>
            {
                btn.ForeColor = btn.Enabled ? Color.FromArgb(200, 200, 220) : Color.FromArgb(80, 80, 95);
                btn.BackColor = btn.Enabled ? Color.FromArgb(24, 24, 24) : Color.FromArgb(18, 18, 18);
                btn.FlatAppearance.BorderColor = btn.Enabled ? Color.FromArgb(42, 42, 42) : Color.FromArgb(28, 28, 28);
            };
            btn.Click += onClick;

            var tt = new ToolTip();
            tt.SetToolTip(btn, tooltipText);

            return btn;
        }

        private Button CreateToolButton(string text, string tooltipText, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(32, 28),
                Margin = new Padding(2, 2, 0, 0),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(200, 200, 225),
                BackColor = Color.FromArgb(24, 24, 24),
                Font = new Font("Segoe UI Emoji", 9.5f, FontStyle.Regular),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(42, 42, 42);
            btn.FlatAppearance.BorderSize = 1;
            btn.MouseEnter += (s, e) => { btn.BackColor = Color.FromArgb(38, 38, 38); btn.ForeColor = Color.White; };
            btn.MouseLeave += (s, e) => { btn.BackColor = Color.FromArgb(24, 24, 24); btn.ForeColor = Color.FromArgb(200, 200, 225); };
            btn.Click += onClick;

            var tt = new ToolTip();
            tt.SetToolTip(btn, tooltipText);

            return btn;
        }

        // ── WebView2 Initialization ──
        private async Task InitializeWebViewAsync()
        {
            try
            {
                Log("InitializeWebViewAsync starting for YouTube");

                string userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "BlackTube-webview2");

                if (!Directory.Exists(userDataFolder))
                    Directory.CreateDirectory(userDataFolder);

                var options = new CoreWebView2EnvironmentOptions();
                options.AdditionalBrowserArguments =
                    "--enable-gpu-rasterization " +
                    "--ignore-gpu-blocklist " +
                    "--enable-zero-copy " +
                    "--disk-cache-size=67108864 " +
                    "--enable-features=VaapiVideoDecoder,PlatformHEVCDecoderSupport,HardwareMediaKeyHandling " +
                    "--force-dark-mode";

                CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);
                Log("CoreWebView2Environment created");

                await _ytWebView.EnsureCoreWebView2Async(env);

                _ytWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                _ytWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;

                // Sync window title with current YouTube video/page title
                _ytWebView.CoreWebView2.DocumentTitleChanged += (s, e) =>
                {
                    string title = _ytWebView.CoreWebView2.DocumentTitle;
                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        this.Text = title.EndsWith("YouTube") ? title + " - BlackTube" : title;
                    }
                    else
                    {
                        this.Text = "BlackTube - YouTube";
                    }
                };

                // History navigation sync
                _ytWebView.CoreWebView2.HistoryChanged += (s, e) =>
                {
                    _btnBack.Enabled = _ytWebView.CoreWebView2.CanGoBack;
                    _btnForward.Enabled = _ytWebView.CoreWebView2.CanGoForward;
                };

                // Full Screen Optimization: Hook HTML5 / YouTube video player fullscreen state change
                _ytWebView.CoreWebView2.ContainsFullScreenElementChanged += (s, e) =>
                {
                    bool isElemFullScreen = _ytWebView.CoreWebView2.ContainsFullScreenElement;
                    Log("ContainsFullScreenElementChanged: " + isElemFullScreen);
                    SetFullScreenMode(isElemFullScreen);
                };

                // Attach Multi-Tier AdShield & SponsorBlock Engine
                await AdShieldEngine.AttachAdShieldAsync(_ytWebView);

                _ytWebView.CoreWebView2.Navigate("https://www.youtube.com");
                Log("YouTube WebView successfully initialized");

                _ytWebView.Focus();
            }
            catch (Exception ex)
            {
                Log("FATAL WebView2 Init: " + ex.ToString());
                MessageBox.Show("Failed to initialize YouTube browser engine: " + ex.Message, "BlackTube Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Navigation Methods ──
        private void NavigateBack()
        {
            if (_ytWebView != null && _ytWebView.CoreWebView2 != null && _ytWebView.CoreWebView2.CanGoBack)
            {
                _ytWebView.CoreWebView2.GoBack();
            }
        }

        private void NavigateForward()
        {
            if (_ytWebView != null && _ytWebView.CoreWebView2 != null && _ytWebView.CoreWebView2.CanGoForward)
            {
                _ytWebView.CoreWebView2.GoForward();
            }
        }

        private void ReloadView()
        {
            if (_ytWebView != null && _ytWebView.CoreWebView2 != null)
            {
                _ytWebView.CoreWebView2.Reload();
            }
        }

        private void NavigateHome()
        {
            if (_ytWebView != null && _ytWebView.CoreWebView2 != null)
            {
                _ytWebView.CoreWebView2.Navigate("https://www.youtube.com");
            }
        }

        private void FocusYouTubeSearch()
        {
            if (_ytWebView != null && _ytWebView.CoreWebView2 != null)
            {
                string script = @"(function(){
                    var searchInput = document.querySelector('input#search') || document.querySelector('input[name=""search_query""]');
                    if (searchInput) {
                        searchInput.focus();
                        searchInput.select();
                    }
                })()";
                _ytWebView.CoreWebView2.ExecuteScriptAsync(script);
            }
        }

        // ── Full Screen Optimization ──
        public void ToggleFullScreen()
        {
            SetFullScreenMode(!_isFullScreen);
        }

        public void SetFullScreenMode(bool fullscreen)
        {
            if (fullscreen == _isFullScreen) return;
            _isFullScreen = fullscreen;

            if (_isFullScreen)
            {
                // Save current geometry and state
                _savedBorderStyle = this.FormBorderStyle;
                _savedWindowState = this.WindowState;
                _savedBounds = this.Bounds;

                // Seamless borderless edge-to-edge fullscreen
                _navBar.Visible = false;
                this.FormBorderStyle = FormBorderStyle.None;
                this.WindowState = FormWindowState.Normal;
                this.Bounds = Screen.FromControl(this).Bounds;
                _btnFullScreen.ForeColor = Color.FromArgb(0, 255, 170);
            }
            else
            {
                // Restore standard window styling and bounds
                this.FormBorderStyle = _savedBorderStyle != FormBorderStyle.None ? _savedBorderStyle : FormBorderStyle.Sizable;
                this.WindowState = _savedWindowState;
                this.Bounds = _savedBounds;
                _navBar.Visible = true;
                _btnFullScreen.ForeColor = Color.FromArgb(200, 200, 225);
            }
        }

        // ── Picture-in-Picture & Pin ──
        public void TogglePipMode()
        {
            _isPipMode = !_isPipMode;
            if (_isPipMode)
            {
                if (!_isFullScreen)
                {
                    _savedBounds = this.Bounds;
                    _savedWindowState = this.WindowState;
                }
                this.WindowState = FormWindowState.Normal;
                this.TopMost = true;
                this.Size = new Size(520, 340);
                this.Location = new Point(Screen.PrimaryScreen.WorkingArea.Right - 540, Screen.PrimaryScreen.WorkingArea.Bottom - 360);
                _btnPip.ForeColor = Color.FromArgb(0, 255, 170);
            }
            else
            {
                this.TopMost = _isAlwaysOnTop;
                this.Bounds = _savedBounds;
                this.WindowState = _savedWindowState;
                _btnPip.ForeColor = Color.FromArgb(200, 200, 225);
            }
        }

        public void ToggleAlwaysOnTop()
        {
            _isAlwaysOnTop = !_isAlwaysOnTop;
            this.TopMost = _isAlwaysOnTop;
            _btnPin.ForeColor = _isAlwaysOnTop ? Color.FromArgb(0, 255, 170) : Color.FromArgb(200, 200, 225);
        }

        // ── Media Actions ──
        public async void ExecuteMediaAction(string action)
        {
            if (_ytWebView == null || _ytWebView.CoreWebView2 == null) return;

            string script = "";
            if (action == "toggle")
            {
                script = "(function(){var v=document.querySelector('video');if(v){if(v.paused)v.play();else v.pause();}else{var btn=document.querySelector('.ytp-play-button');if(btn)btn.click();}})()";
            }
            else if (action == "next")
            {
                script = "(function(){var b=document.querySelector('.ytp-next-button');if(b)b.click();})()";
            }
            else if (action == "prev")
            {
                script = "(function(){var b=document.querySelector('.ytp-prev-button');if(b)b.click();})()";
            }

            if (!string.IsNullOrEmpty(script))
            {
                try { await _ytWebView.CoreWebView2.ExecuteScriptAsync(script); }
                catch { }
            }
        }

        // ── System Tray ──
        private void SetupTray()
        {
            _trayMenu = new ContextMenuStrip();
            _trayMenu.BackColor = Color.FromArgb(18, 18, 24);
            _trayMenu.ForeColor = Color.White;

            _trayMenu.Items.Add("▶ Open YouTube", null, (s, e) => ShowApp());
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add("⏯ Play / Pause", null, (s, e) => ExecuteMediaAction("toggle"));
            _trayMenu.Items.Add("⏭ Next Video", null, (s, e) => ExecuteMediaAction("next"));
            _trayMenu.Items.Add("⏮ Previous Video", null, (s, e) => ExecuteMediaAction("prev"));
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add("⛶ Toggle Fullscreen", null, (s, e) => ToggleFullScreen());
            _trayMenu.Items.Add("⧉ PiP Mode", null, (s, e) => TogglePipMode());
            _trayMenu.Items.Add("⏱ Sleep Timer", null, (s, e) => ShowSleepTimerMenu());
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add("✕ Exit BlackTube", null, (s, e) =>
            {
                if (_trayIcon != null) _trayIcon.Visible = false;
                Application.Exit();
            });

            _trayIcon = new NotifyIcon
            {
                Icon = this.Icon,
                Text = "BlackTube - YouTube",
                ContextMenuStrip = _trayMenu,
                Visible = true
            };
            _trayIcon.DoubleClick += (s, e) => ShowApp();
        }

        public void ShowApp()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke((Action)ShowApp);
                return;
            }
            this.Show();
            if (this.WindowState == FormWindowState.Minimized)
                this.WindowState = FormWindowState.Normal;
            ShowWindow(this.Handle, SW_RESTORE);
            this.BringToFront();
            this.Activate();
            this.Focus();
            SetForegroundWindow(this.Handle);
        }

        // ── Sleep Timer & GC ──
        private void SetupTimers()
        {
            _sleepTimer = new Timer { Interval = 1000 };
            _sleepTimer.Tick += (s, e) =>
            {
                if (_sleepRemainingSeconds > 0)
                {
                    _sleepRemainingSeconds--;
                    int m = _sleepRemainingSeconds / 60;
                    int sec = _sleepRemainingSeconds % 60;
                    _btnSleep.Text = string.Format("{0:D2}:{1:D2}", m, sec);
                    _btnSleep.ForeColor = Color.FromArgb(255, 200, 50);

                    if (_sleepRemainingSeconds <= 0)
                    {
                        _sleepTimer.Stop();
                        _btnSleep.Text = "⏱";
                        _btnSleep.ForeColor = Color.FromArgb(200, 200, 225);
                        ExecuteMediaAction("toggle");
                        if (_ytWebView != null && _ytWebView.CoreWebView2 != null)
                            _ytWebView.CoreWebView2.ExecuteScriptAsync("var v=document.querySelector('video');if(v)v.pause();");
                        if (_trayIcon != null)
                            _trayIcon.ShowBalloonTip(2000, "BlackTube Sleep Timer", "YouTube playback paused. Goodnight!", ToolTipIcon.Info);
                    }
                }
            };

            _gcTimer = new Timer { Interval = 60000 };
            _gcTimer.Tick += (s, e) => MemoryTrimmer.Trim();
            _gcTimer.Start();
        }

        private void ShowSleepTimerMenu()
        {
            var menu = new ContextMenuStrip();
            menu.BackColor = Color.FromArgb(24, 24, 34);
            menu.ForeColor = Color.White;

            menu.Items.Add("Turn Off Timer", null, (s, e) =>
            {
                _sleepTimer.Stop();
                _sleepRemainingSeconds = 0;
                _btnSleep.Text = "⏱";
                _btnSleep.ForeColor = Color.FromArgb(200, 200, 225);
            });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("15 Minutes", null, (s, e) => StartSleep(15 * 60));
            menu.Items.Add("30 Minutes", null, (s, e) => StartSleep(30 * 60));
            menu.Items.Add("45 Minutes", null, (s, e) => StartSleep(45 * 60));
            menu.Items.Add("60 Minutes", null, (s, e) => StartSleep(60 * 60));
            menu.Items.Add("90 Minutes", null, (s, e) => StartSleep(90 * 60));

            menu.Show(_btnSleep, new Point(0, _btnSleep.Height));
        }

        private void StartSleep(int seconds)
        {
            _sleepRemainingSeconds = seconds;
            _sleepTimer.Start();
        }

        // ── Hotkeys ──
        private void SetupHotkeys()
        {
            try
            {
                RegisterHotKey(this.Handle, HOTKEY_PLAY_PAUSE, 0, VK_MEDIA_PLAY_PAUSE);
                RegisterHotKey(this.Handle, HOTKEY_NEXT, 0, VK_MEDIA_NEXT_TRACK);
                RegisterHotKey(this.Handle, HOTKEY_PREV, 0, VK_MEDIA_PREV_TRACK);
            }
            catch { }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_SHOWME)
            {
                ShowApp();
                return;
            }
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HOTKEY_PLAY_PAUSE) ExecuteMediaAction("toggle");
                else if (id == HOTKEY_NEXT) ExecuteMediaAction("next");
                else if (id == HOTKEY_PREV) ExecuteMediaAction("prev");
            }
            base.WndProc(ref m);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Fullscreen Toggle (F11)
            if (keyData == Keys.F11)
            {
                ToggleFullScreen();
                return true;
            }

            // Exit Fullscreen / PiP (Escape)
            if (keyData == Keys.Escape)
            {
                if (_isFullScreen)
                {
                    SetFullScreenMode(false);
                    return true;
                }
                if (_isPipMode)
                {
                    TogglePipMode();
                    return true;
                }
            }

            // Search shortcut (Ctrl+F)
            if (keyData == (Keys.Control | Keys.F))
            {
                FocusYouTubeSearch();
                return true;
            }

            // Home shortcut (Ctrl+H)
            if (keyData == (Keys.Control | Keys.H))
            {
                NavigateHome();
                return true;
            }

            // History Navigation
            if (keyData == (Keys.Alt | Keys.Left))
            {
                NavigateBack();
                return true;
            }
            if (keyData == (Keys.Alt | Keys.Right))
            {
                NavigateForward();
                return true;
            }

            // Picture-in-Picture
            if (keyData == (Keys.Control | Keys.Shift | Keys.P))
            {
                TogglePipMode();
                return true;
            }

            // Always on Top
            if (keyData == (Keys.Control | Keys.Shift | Keys.T))
            {
                ToggleAlwaysOnTop();
                return true;
            }

            // Sleep Timer
            if (keyData == (Keys.Control | Keys.Shift | Keys.S))
            {
                ShowSleepTimerMenu();
                return true;
            }

            // Reload
            if (keyData == (Keys.Control | Keys.R) || keyData == Keys.F5)
            {
                ReloadView();
                return true;
            }

            // Minimize to Tray
            if (keyData == (Keys.Control | Keys.M))
            {
                this.WindowState = FormWindowState.Minimized;
                return true;
            }

            // Quit
            if (keyData == (Keys.Control | Keys.Q))
            {
                if (_trayIcon != null) _trayIcon.Visible = false;
                Application.Exit();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
