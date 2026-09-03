using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace BlackTube
{
    public class MainForm : Form
    {
        // ── P/Invoke ──
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;
        private const int WM_HOTKEY = 0x0312;

        private const uint VK_MEDIA_NEXT_TRACK = 0xB0;
        private const uint VK_MEDIA_PREV_TRACK = 0xB1;
        private const uint VK_MEDIA_PLAY_PAUSE = 0xB3;

        private const int HOTKEY_PLAY_PAUSE = 9001;
        private const int HOTKEY_NEXT = 9002;
        private const int HOTKEY_PREV = 9003;

        // ── Controls & State ──
        private Panel _topBar;
        private Panel _tabContainer;
        private Button _btnYouTube;
        private Button _btnMusic;
        private Button _btnSleep;
        private Button _btnPip;
        private Button _btnPin;
        private Button _btnMin;
        private Button _btnMax;
        private Button _btnClose;
        private Label _lblTitle;

        private WebView2 _ytWebView;
        private WebView2 _musicWebView;
        private bool _isMusicActive = false;
        private bool _isPipMode = false;
        private bool _isAlwaysOnTop = false;

        private Rectangle _prePipBounds;
        private FormWindowState _prePipState;

        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayMenu;

        private Timer _sleepTimer;
        private int _sleepRemainingSeconds = 0;
        private Timer _gcTimer;

        public MainForm()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            
            this.Text = "BlackTube - YouTube & YT Music";
            this.Width = 1280;
            this.Height = 800;
            this.MinimumSize = new Size(400, 240);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(8, 8, 14);
            this.ForeColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.None;

            string iconPath = Path.Combine(Application.StartupPath, "icon.ico");
            if (File.Exists(iconPath))
            {
                try { this.Icon = new Icon(iconPath); } catch { }
            }

            SetupUI();
            SetupTray();
            SetupTimers();
            SetupHotkeys();

            this.Load += async (s, e) =>
            {
                await InitializeWebViewsAsync();
            };

            this.Resize += (s, e) =>
            {
                if (this.WindowState == FormWindowState.Minimized)
                {
                    MemoryTrimmer.Trim();
                }
            };
        }

        // ── Custom Titlebar & UI ──
        private void SetupUI()
        {
            _topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(13, 13, 24),
                Padding = new Padding(8, 0, 0, 0)
            };
            _topBar.MouseDown += OnTopBarMouseDown;

            // App title / Logo icon
            _lblTitle = new Label
            {
                Text = "⚡ BlackTube",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 255, 255),
                AutoSize = true,
                Location = new Point(12, 12),
                Cursor = Cursors.Hand
            };
            _lblTitle.MouseDown += OnTopBarMouseDown;
            _topBar.Controls.Add(_lblTitle);

            // Tab bar switcher
            _tabContainer = new Panel
            {
                Location = new Point(140, 4),
                Size = new Size(270, 36),
                BackColor = Color.FromArgb(20, 20, 32)
            };
            _tabContainer.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(40, 40, 60), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, _tabContainer.Width - 1, _tabContainer.Height - 1);
                }
            };

            _btnYouTube = CreateTabButton("🎬 YouTube", 0);
            _btnYouTube.Click += (s, e) => SwitchTab(false);

            _btnMusic = CreateTabButton("🎵 YT Music", 135);
            _btnMusic.Click += (s, e) => SwitchTab(true);

            _tabContainer.Controls.Add(_btnYouTube);
            _tabContainer.Controls.Add(_btnMusic);
            _topBar.Controls.Add(_tabContainer);

            // Action Buttons
            int rightOffset = 10;

            _btnClose = CreateTitleButton("✕", rightOffset, Color.FromArgb(232, 17, 35));
            _btnClose.Click += (s, e) =>
            {
                this.Hide();
                if (_trayIcon != null)
                {
                    _trayIcon.ShowBalloonTip(1500, "BlackTube", "Minimized to tray. Right-click icon to quit.", ToolTipIcon.Info);
                }
            };
            rightOffset += 45;

            _btnMax = CreateTitleButton("▢", rightOffset);
            _btnMax.Click += (s, e) =>
            {
                this.WindowState = this.WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
            };
            rightOffset += 45;

            _btnMin = CreateTitleButton("―", rightOffset);
            _btnMin.Click += (s, e) => { this.WindowState = FormWindowState.Minimized; };
            rightOffset += 45;

            _btnPin = CreateTitleButton("📌", rightOffset);
            _btnPin.Font = new Font("Segoe UI Emoji", 9f);
            _btnPin.Click += (s, e) => ToggleAlwaysOnTop();
            rightOffset += 45;

            _btnPip = CreateTitleButton("⧉", rightOffset);
            _btnPip.Font = new Font("Segoe UI", 11f);
            _btnPip.Click += (s, e) => TogglePipMode();
            rightOffset += 45;

            _btnSleep = CreateTitleButton("⏱", rightOffset);
            _btnSleep.Font = new Font("Segoe UI Emoji", 10f);
            _btnSleep.Click += (s, e) => ShowSleepTimerMenu();

            _topBar.Controls.Add(_btnClose);
            _topBar.Controls.Add(_btnMax);
            _topBar.Controls.Add(_btnMin);
            _topBar.Controls.Add(_btnPin);
            _topBar.Controls.Add(_btnPip);
            _topBar.Controls.Add(_btnSleep);

            this.Controls.Add(_topBar);

            UpdateTabStyles();
        }

        private Button CreateTabButton(string text, int x)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, 0),
                Size = new Size(135, 36),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private Button CreateTitleButton(string text, int rightOffset, Color? hoverColor = null)
        {
            var btn = new Button
            {
                Text = text,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(this.Width - rightOffset, 0),
                Size = new Size(45, 44),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(180, 180, 200),
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) => { btn.BackColor = hoverColor ?? Color.FromArgb(40, 40, 60); btn.ForeColor = Color.White; };
            btn.MouseLeave += (s, e) => { btn.BackColor = Color.Transparent; btn.ForeColor = Color.FromArgb(180, 180, 200); };
            return btn;
        }

        private void OnTopBarMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, (IntPtr)HT_CAPTION, IntPtr.Zero);
            }
        }

        private void UpdateTabStyles()
        {
            if (!_isMusicActive)
            {
                _btnYouTube.BackColor = Color.FromArgb(255, 0, 64);
                _btnYouTube.ForeColor = Color.White;
                _btnMusic.BackColor = Color.Transparent;
                _btnMusic.ForeColor = Color.FromArgb(140, 140, 160);
            }
            else
            {
                _btnMusic.BackColor = Color.FromArgb(168, 85, 247);
                _btnMusic.ForeColor = Color.White;
                _btnYouTube.BackColor = Color.Transparent;
                _btnYouTube.ForeColor = Color.FromArgb(140, 140, 160);
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

        // ── WebView2 Initialization ──
        private async System.Threading.Tasks.Task InitializeWebViewsAsync()
        {
            try
            {
                string userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "BlackTube-webview2");

                var envOptions = new CoreWebView2EnvironmentOptions(
                    "--enable-gpu-rasterization " +
                    "--ignore-gpu-blocklist " +
                    "--enable-zero-copy " +
                    "--disk-cache-size=33554432 " +
                    "--media-cache-size=33554432 " +
                    "--enable-features=PlatformHEVCDecoderSupport,HardwareMediaKeyHandling " +
                    "--js-flags=--max-old-space-size=192"
                );

                CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(null, userDataFolder, envOptions);

                // 1. YouTube WebView
                _ytWebView = new WebView2
                {
                    Dock = DockStyle.Fill,
                    Visible = true
                };
                this.Controls.Add(_ytWebView);
                _ytWebView.BringToFront();
                _topBar.BringToFront();

                await _ytWebView.EnsureCoreWebView2Async(env);
                await AdShieldEngine.AttachAdShieldAsync(_ytWebView);
                _ytWebView.CoreWebView2.Navigate("https://www.youtube.com");

                // 2. YouTube Music WebView
                _musicWebView = new WebView2
                {
                    Dock = DockStyle.Fill,
                    Visible = false
                };
                this.Controls.Add(_musicWebView);

                await _musicWebView.EnsureCoreWebView2Async(env);
                await AdShieldEngine.AttachAdShieldAsync(_musicWebView);
                _musicWebView.CoreWebView2.Navigate("https://music.youtube.com");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error initializing WebView2: " + ex.Message, "BlackTube Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            _trayMenu.RenderMode = ToolStripRenderMode.System;

            _trayMenu.Items.Add("🎬 Switch to YouTube", null, (s, e) => { ShowApp(); SwitchTab(false); });
            _trayMenu.Items.Add("🎵 Switch to YT Music", null, (s, e) => { ShowApp(); SwitchTab(true); });
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add("⏯ Play / Pause", null, (s, e) => ExecuteMediaAction("toggle"));
            _trayMenu.Items.Add("⏭ Next Track", null, (s, e) => ExecuteMediaAction("next"));
            _trayMenu.Items.Add("⏮ Previous Track", null, (s, e) => ExecuteMediaAction("prev"));
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add("⏱ Sleep Timer", null, (s, e) => ShowSleepTimerMenu());
            _trayMenu.Items.Add("⧉ Toggle PiP Mode", null, (s, e) => TogglePipMode());
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add("✕ Quit BlackTube", null, (s, e) => { _trayIcon.Visible = false; Application.Exit(); });

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

        // ── Timers: Sleep & GC ──
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
                        _btnSleep.ForeColor = Color.FromArgb(180, 180, 200);
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
            menu.BackColor = Color.FromArgb(22, 22, 34);
            menu.ForeColor = Color.White;

            menu.Items.Add("Turn Off Timer", null, (s, e) =>
            {
                _sleepTimer.Stop();
                _sleepRemainingSeconds = 0;
                _btnSleep.Text = "⏱";
                _btnSleep.ForeColor = Color.FromArgb(180, 180, 200);
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
                _prePipBounds = this.Bounds;
                _prePipState = this.WindowState;
                this.WindowState = FormWindowState.Normal;
                this.TopMost = true;
                this.Size = new Size(480, 310);
                this.Location = new Point(Screen.PrimaryScreen.WorkingArea.Right - 500, Screen.PrimaryScreen.WorkingArea.Bottom - 330);
                _btnPip.ForeColor = Color.FromArgb(0, 255, 170);
            }
            else
            {
                this.TopMost = _isAlwaysOnTop;
                this.Bounds = _prePipBounds;
                this.WindowState = _prePipState;
                _btnPip.ForeColor = Color.FromArgb(180, 180, 200);
            }
        }

        public void ToggleAlwaysOnTop()
        {
            _isAlwaysOnTop = !_isAlwaysOnTop;
            this.TopMost = _isAlwaysOnTop;
            _btnPin.ForeColor = _isAlwaysOnTop ? Color.FromArgb(0, 255, 170) : Color.FromArgb(180, 180, 200);
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
                WebView2 active = _isMusicActive ? _musicWebView : _ytWebView;
                if (active != null && active.CoreWebView2 != null) active.CoreWebView2.Reload();
                return true;
            }
            if (keyData == (Keys.Control | Keys.M))
            {
                this.WindowState = FormWindowState.Minimized;
                return true;
            }
            if (keyData == (Keys.Control | Keys.Q))
            {
                _trayIcon.Visible = false;
                Application.Exit();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                UnregisterHotKey(this.Handle, HOTKEY_PLAY_PAUSE);
                UnregisterHotKey(this.Handle, HOTKEY_NEXT);
                UnregisterHotKey(this.Handle, HOTKEY_PREV);
            }
            catch { }
            base.OnFormClosing(e);
        }
    }
}
