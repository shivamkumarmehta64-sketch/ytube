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
        // ── DWM Immersive Dark Mode ──
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;

        private static void EnableDarkMode(IntPtr handle)
        {
            try
            {
                int darkMode = 1;
                if (DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int)) != 0)
                {
                    DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref darkMode, sizeof(int));
                }
            }
            catch { }
        }

        // ── Global Hotkeys ──
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WM_HOTKEY = 0x0312;
        private const uint VK_MEDIA_NEXT_TRACK = 0xB0;
        private const uint VK_MEDIA_PREV_TRACK = 0xB1;
        private const uint VK_MEDIA_PLAY_PAUSE = 0xB3;

        private const int HOTKEY_PLAY_PAUSE = 9001;
        private const int HOTKEY_NEXT = 9002;
        private const int HOTKEY_PREV = 9003;

        // ── UI Components ──
        private Panel _navBar;
        private Panel _tabPanel;
        private Button _btnYouTube;
        private Button _btnMusic;
        private Button _btnSleep;
        private Button _btnPip;
        private Button _btnPin;
        private Button _btnReload;
        private Panel _contentPanel;

        private WebView2 _ytWebView;
        private WebView2 _musicWebView;
        private bool _isMusicActive = false;
        private bool _isPipMode = false;
        private bool _isAlwaysOnTop = false;

        private Rectangle _normalBounds;
        private FormWindowState _normalState;

        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayMenu;
        private bool _isExiting = false;

        private Timer _sleepTimer;
        private int _sleepRemainingSeconds = 0;
        private Timer _gcTimer;
        private string _logPath;

        public MainForm()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

            _logPath = Path.Combine(Application.StartupPath, "debug.log");
            Log("MainForm constructor starting");

            this.Text = "BlackTube - YouTube & YT Music";
            this.Width = 1280;
            this.Height = 820;
            this.MinimumSize = new Size(420, 260);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(12, 12, 18);
            this.ForeColor = Color.White;

            string iconPath = Path.Combine(Application.StartupPath, "icon.ico");
            if (File.Exists(iconPath))
            {
                try { this.Icon = new Icon(iconPath); } catch { }
            }

            SetupUI();
            SetupTray();
            SetupTimers();
            SetupHotkeys();

            this.HandleCreated += (s, e) => EnableDarkMode(this.Handle);

            this.Load += async (s, e) =>
            {
                EnableDarkMode(this.Handle);
                await InitializeWebViewsAsync();
            };

            this.Resize += (s, e) =>
            {
                if (this.WindowState == FormWindowState.Minimized)
                {
                    MemoryTrimmer.Trim();
                }
            };

            this.FormClosing += (s, e) =>
            {
                if (!_isExiting && e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    this.Hide();
                    if (_trayIcon != null)
                    {
                        _trayIcon.ShowBalloonTip(1200, "BlackTube", "Minimized to tray. Right-click icon to quit.", ToolTipIcon.Info);
                    }
                }
                else
                {
                    try
                    {
                        UnregisterHotKey(this.Handle, HOTKEY_PLAY_PAUSE);
                        UnregisterHotKey(this.Handle, HOTKEY_NEXT);
                        UnregisterHotKey(this.Handle, HOTKEY_PREV);
                    }
                    catch { }
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
            // 1. Navigation Top Bar
            _navBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Color.FromArgb(16, 16, 24),
                Padding = new Padding(12, 5, 12, 5)
            };

            // 2. Tab Switcher Box
            _tabPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 280,
                BackColor = Color.FromArgb(24, 24, 36)
            };
            _tabPanel.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(45, 45, 65), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, _tabPanel.Width - 1, _tabPanel.Height - 1);
                }
            };

            _btnYouTube = new Button
            {
                Text = "🎬 YouTube",
                Dock = DockStyle.Left,
                Width = 140,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _btnYouTube.FlatAppearance.BorderSize = 0;
            _btnYouTube.Click += (s, e) => SwitchTab(false);

            _btnMusic = new Button
            {
                Text = "🎵 YT Music",
                Dock = DockStyle.Right,
                Width = 140,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            _btnMusic.FlatAppearance.BorderSize = 0;
            _btnMusic.Click += (s, e) => SwitchTab(true);

            _tabPanel.Controls.Add(_btnMusic);
            _tabPanel.Controls.Add(_btnYouTube);
            _navBar.Controls.Add(_tabPanel);

            // 3. Quick Action Buttons (Right Aligned Flow)
            var actionFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            _btnReload = CreateToolButton("↻", "Reload current page (Ctrl+R / F5)", (s, e) => ReloadActiveView());
            _btnPin = CreateToolButton("📌", "Toggle Always On Top (Ctrl+Shift+T)", (s, e) => ToggleAlwaysOnTop());
            _btnPip = CreateToolButton("⧉", "Toggle Picture-in-Picture (Ctrl+Shift+P)", (s, e) => TogglePipMode());
            _btnSleep = CreateToolButton("⏱", "Sleep Timer (Ctrl+Shift+S)", (s, e) => ShowSleepTimerMenu());

            actionFlow.Controls.Add(_btnReload);
            actionFlow.Controls.Add(_btnPin);
            actionFlow.Controls.Add(_btnPip);
            actionFlow.Controls.Add(_btnSleep);

            _navBar.Controls.Add(actionFlow);
            this.Controls.Add(_navBar);

            // 4. Content Area for WebViews
            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(8, 8, 12)
            };
            this.Controls.Add(_contentPanel);

            UpdateTabStyles();
        }

        private Button CreateToolButton(string text, string tooltipText, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(42, 34),
                Margin = new Padding(4, 1, 0, 0),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(190, 190, 215),
                BackColor = Color.FromArgb(24, 24, 36),
                Font = new Font("Segoe UI Emoji", 10f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(45, 45, 65);
            btn.FlatAppearance.BorderSize = 1;
            btn.MouseEnter += (s, e) => { btn.BackColor = Color.FromArgb(40, 40, 60); btn.ForeColor = Color.White; };
            btn.MouseLeave += (s, e) => { btn.BackColor = Color.FromArgb(24, 24, 36); btn.ForeColor = Color.FromArgb(190, 190, 215); };
            btn.Click += onClick;

            var tt = new ToolTip();
            tt.SetToolTip(btn, tooltipText);

            return btn;
        }

        private void UpdateTabStyles()
        {
            if (!_isMusicActive)
            {
                _btnYouTube.BackColor = Color.FromArgb(255, 0, 64);
                _btnYouTube.ForeColor = Color.White;
                _btnMusic.BackColor = Color.Transparent;
                _btnMusic.ForeColor = Color.FromArgb(140, 140, 165);
            }
            else
            {
                _btnMusic.BackColor = Color.FromArgb(168, 85, 247);
                _btnMusic.ForeColor = Color.White;
                _btnYouTube.BackColor = Color.Transparent;
                _btnYouTube.ForeColor = Color.FromArgb(140, 140, 165);
            }
        }

        public void SwitchTab(bool music)
        {
            _isMusicActive = music;
            UpdateTabStyles();

            if (_ytWebView != null && _musicWebView != null)
            {
                _ytWebView.Visible = !_isMusicActive;
                _musicWebView.Visible = _isMusicActive;

                if (_isMusicActive) _musicWebView.Focus();
                else _ytWebView.Focus();
            }

            MemoryTrimmer.Trim();
        }

        private void ReloadActiveView()
        {
            WebView2 active = _isMusicActive ? _musicWebView : _ytWebView;
            if (active != null && active.CoreWebView2 != null)
            {
                active.CoreWebView2.Reload();
            }
        }

        // ── WebView2 Initialization ──
        private async Task InitializeWebViewsAsync()
        {
            try
            {
                Log("InitializeWebViewsAsync starting");

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
                    "--disk-cache-size=33554432 " +
                    "--enable-features=PlatformHEVCDecoderSupport,HardwareMediaKeyHandling";

                CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);
                Log("CoreWebView2Environment created");

                // 1. YouTube WebView
                _ytWebView = new WebView2
                {
                    Dock = DockStyle.Fill,
                    Visible = true
                };
                _contentPanel.Controls.Add(_ytWebView);
                await _ytWebView.EnsureCoreWebView2Async(env);
                await AdShieldEngine.AttachAdShieldAsync(_ytWebView);
                _ytWebView.CoreWebView2.Navigate("https://www.youtube.com");
                Log("YouTube WebView initialized");

                // 2. YouTube Music WebView
                _musicWebView = new WebView2
                {
                    Dock = DockStyle.Fill,
                    Visible = false
                };
                _contentPanel.Controls.Add(_musicWebView);
                await _musicWebView.EnsureCoreWebView2Async(env);
                await AdShieldEngine.AttachAdShieldAsync(_musicWebView);
                _musicWebView.CoreWebView2.Navigate("https://music.youtube.com");
                Log("YouTube Music WebView initialized");
            }
            catch (Exception ex)
            {
                Log("FATAL WebView2 Init: " + ex.ToString());
                MessageBox.Show("Failed to initialize browser engine: " + ex.Message, "BlackTube Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Media Actions ──
        public async void ExecuteMediaAction(string action)
        {
            WebView2 target = _isMusicActive ? _musicWebView : _ytWebView;
            if (target == null || target.CoreWebView2 == null) return;

            string script = "";
            if (action == "toggle")
            {
                script = "(function(){var v=document.querySelector('video');if(v){if(v.paused)v.play();else v.pause();}else{var btn=document.querySelector('#play-pause-button')||document.querySelector('.ytp-play-button');if(btn)btn.click();}})()";
            }
            else if (action == "next")
            {
                script = "(function(){var b=document.querySelector('.next-button')||document.querySelector('.ytp-next-button')||document.querySelector('tp-yt-paper-icon-button[aria-label=\"Next track\"]');if(b)b.click();})()";
            }
            else if (action == "prev")
            {
                script = "(function(){var b=document.querySelector('.previous-button')||document.querySelector('.ytp-prev-button')||document.querySelector('tp-yt-paper-icon-button[aria-label=\"Previous track\"]');if(b)b.click();})()";
            }

            if (!string.IsNullOrEmpty(script))
            {
                try { await target.CoreWebView2.ExecuteScriptAsync(script); }
                catch { }
            }
        }

        // ── System Tray ──
        private void SetupTray()
        {
            _trayMenu = new ContextMenuStrip();
            _trayMenu.BackColor = Color.FromArgb(18, 18, 28);
            _trayMenu.ForeColor = Color.White;

            _trayMenu.Items.Add("🎬 YouTube", null, (s, e) => { ShowApp(); SwitchTab(false); });
            _trayMenu.Items.Add("🎵 YT Music", null, (s, e) => { ShowApp(); SwitchTab(true); });
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add("⏯ Play / Pause", null, (s, e) => ExecuteMediaAction("toggle"));
            _trayMenu.Items.Add("⏭ Next Track", null, (s, e) => ExecuteMediaAction("next"));
            _trayMenu.Items.Add("⏮ Previous Track", null, (s, e) => ExecuteMediaAction("prev"));
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add("⏱ Sleep Timer", null, (s, e) => ShowSleepTimerMenu());
            _trayMenu.Items.Add("⧉ PiP Mode", null, (s, e) => TogglePipMode());
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add("✕ Exit BlackTube", null, (s, e) =>
            {
                _isExiting = true;
                _trayIcon.Visible = false;
                Application.Exit();
            });

            _trayIcon = new NotifyIcon
            {
                Icon = this.Icon,
                Text = "BlackTube (YouTube & YT Music)",
                ContextMenuStrip = _trayMenu,
                Visible = true
            };
            _trayIcon.DoubleClick += (s, e) => ShowApp();
        }

        private void ShowApp()
        {
            this.Show();
            if (this.WindowState == FormWindowState.Minimized)
                this.WindowState = FormWindowState.Normal;
            this.BringToFront();
            this.Activate();
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
                        _btnSleep.ForeColor = Color.FromArgb(190, 190, 215);
                        ExecuteMediaAction("toggle");
                        if (_ytWebView != null && _ytWebView.CoreWebView2 != null)
                            _ytWebView.CoreWebView2.ExecuteScriptAsync("var v=document.querySelector('video');if(v)v.pause();");
                        if (_musicWebView != null && _musicWebView.CoreWebView2 != null)
                            _musicWebView.CoreWebView2.ExecuteScriptAsync("var v=document.querySelector('video');if(v)v.pause();");
                        if (_trayIcon != null)
                            _trayIcon.ShowBalloonTip(2000, "BlackTube Sleep Timer", "Playback paused. Goodnight!", ToolTipIcon.Info);
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
            menu.BackColor = Color.FromArgb(24, 24, 36);
            menu.ForeColor = Color.White;

            menu.Items.Add("Turn Off Timer", null, (s, e) =>
            {
                _sleepTimer.Stop();
                _sleepRemainingSeconds = 0;
                _btnSleep.Text = "⏱";
                _btnSleep.ForeColor = Color.FromArgb(190, 190, 215);
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

        // ── Picture-in-Picture & Pin ──
        public void TogglePipMode()
        {
            _isPipMode = !_isPipMode;
            if (_isPipMode)
            {
                _normalBounds = this.Bounds;
                _normalState = this.WindowState;
                this.WindowState = FormWindowState.Normal;
                this.TopMost = true;
                this.Size = new Size(500, 330);
                this.Location = new Point(Screen.PrimaryScreen.WorkingArea.Right - 520, Screen.PrimaryScreen.WorkingArea.Bottom - 350);
                _btnPip.ForeColor = Color.FromArgb(0, 255, 170);
            }
            else
            {
                this.TopMost = _isAlwaysOnTop;
                this.Bounds = _normalBounds;
                this.WindowState = _normalState;
                _btnPip.ForeColor = Color.FromArgb(190, 190, 215);
            }
        }

        public void ToggleAlwaysOnTop()
        {
            _isAlwaysOnTop = !_isAlwaysOnTop;
            this.TopMost = _isAlwaysOnTop;
            _btnPin.ForeColor = _isAlwaysOnTop ? Color.FromArgb(0, 255, 170) : Color.FromArgb(190, 190, 215);
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
            if (keyData == (Keys.Control | Keys.D1))
            {
                SwitchTab(false);
                return true;
            }
            if (keyData == (Keys.Control | Keys.D2))
            {
                SwitchTab(true);
                return true;
            }
            if (keyData == (Keys.Control | Keys.Shift | Keys.P))
            {
                TogglePipMode();
                return true;
            }
            if (keyData == (Keys.Control | Keys.Shift | Keys.T))
            {
                ToggleAlwaysOnTop();
                return true;
            }
            if (keyData == (Keys.Control | Keys.Shift | Keys.S))
            {
                ShowSleepTimerMenu();
                return true;
            }
            if (keyData == (Keys.Control | Keys.R) || keyData == Keys.F5)
            {
                ReloadActiveView();
                return true;
            }
            if (keyData == (Keys.Control | Keys.M))
            {
                this.WindowState = FormWindowState.Minimized;
                return true;
            }
            if (keyData == (Keys.Control | Keys.Q))
            {
                _isExiting = true;
                _trayIcon.Visible = false;
                Application.Exit();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
