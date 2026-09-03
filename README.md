# ⚡ BlackTube

> **Unified YouTube + YouTube Music in one ultra-lightweight, futureproof desktop app.**  
> Built with native **C# .NET + Microsoft WebView2**. No heavy Electron runtimes (~98 KB executable, under 60 MB RAM).

---

## ✨ Key Features

| Feature | Shortcut | Description |
|---|---|---|
| 🎬 **Dual Experience** | <kbd>Ctrl</kbd> + <kbd>1</kbd> / <kbd>2</kbd> | Instant toggle between **YouTube** and **YouTube Music** with full background audio continuity. |
| 🛡️ **5-Tier Resilient AdShield** | — | Network-level request blocker, deep /youtubei/v1/ JSON API stripper, DOM cleaner, and 16x video fast-forward fallback. |
| ⏩ **SponsorBlock Integration** | — | Automatically detects and skips sponsored segments, intros, outros, and subscribe reminders via public SponsorBlock API. |
| 🪟 **Picture-in-Picture (PiP)** | <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>P</kbd> | Compact floating frameless window pinned on top for watching videos while working. |
| ⏱️ **Sleep Timer** | <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>S</kbd> | Auto-pauses audio/video after 15, 30, 45, 60, or 90 minutes. |
| 📌 **Always on Top** | <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>T</kbd> | Pin the main window above all other open applications. |
| 🎧 **Background Playback & Tray** | Right-Click Tray | Minimize to system tray and control playback (Play/Pause, Next, Prev) from the tray icon. |
| ⌨️ **Global Media Keys** | Keyboard | Control media with hardware <kbd>Play/Pause</kbd>, <kbd>Next</kbd>, and <kbd>Previous</kbd> buttons system-wide. |
| ⚡ **Ultra-Low Memory Footprint** | — | Automatic memory trimming (~50–80 MB total RAM) calling kernel32 working-set trims and GC compaction on idle/minimize. |

---

## 🛡️ 5-Tier Resilient Ad Blocking Architecture

YouTube frequently breaks basic adblockers by changing CSS classes or rolling out server-side ad injection tests. **BlackTube** implements a multi-tier defense:

`
Tier 1 — Network Level (Fastest)
  └─ Intercepts WebResourceRequested on WebView2
  └─ 250+ known ad, tracking, and analytics domains dropped before reaching the engine

Tier 2 — YouTubei API JSON Stripper (Immune to DOM/CSS Changes)
  └─ Intercepts window.fetch, XMLHttpRequest, and JSON.parse
  └─ Deep-walks responses from /youtubei/v1/ and strips 150+ ad payload keys
  └─ Forces the YouTube player to treat every video as an ad-free stream

Tier 3 — Universal Video Fast-Forward Fallback
  └─ Directly monitors HTML5 <video> elements
  └─ If an ad starts playing, sets muted=true, playbackRate=16.0x, and jumps to completion

Tier 4 — DOM & Banner Cleaner
  └─ Injects high-specificity stylesheets to eliminate mastheads, promo banners, and mealbars
  └─ Automatically dismisses anti-adblock enforcement dialogs

Tier 5 — SponsorBlock API Integration
  └─ Queries sponsor.ajay.app with current video ID
  └─ Skips sponsored segments, self-promos, and non-music portions automatically
`

---

## ⌨️ Keyboard Shortcuts

| Shortcut | Action |
|---|---|
| <kbd>Ctrl</kbd> + <kbd>1</kbd> | Switch to **YouTube** |
| <kbd>Ctrl</kbd> + <kbd>2</kbd> | Switch to **YouTube Music** |
| <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>P</kbd> | Toggle **Picture-in-Picture (PiP)** mode |
| <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>S</kbd> | Open **Sleep Timer** menu |
| <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>T</kbd> | Toggle **Always On Top** |
| <kbd>Ctrl</kbd> + <kbd>R</kbd> / <kbd>F5</kbd> | Reload Current View |
| <kbd>Ctrl</kbd> + <kbd>M</kbd> | Minimize to Tray |
| <kbd>Ctrl</kbd> + <kbd>Q</kbd> | Quit Application |
| <kbd>MediaPlayPause</kbd> | Global Play / Pause |
| <kbd>MediaNextTrack</kbd> | Global Next Track |
| <kbd>MediaPreviousTrack</kbd> | Global Previous Track |

---

## 🚀 Build & Run

### Prerequisites
- Windows 10 or 11
- Microsoft WebView2 Runtime (pre-installed on Windows 10/11)
- Native .NET Framework 4.8 (built-in on Windows)

### 1-Click Build
`cmd
build.bat
`
Generates standalone BlackTube.exe in seconds without needing Visual Studio or external package managers.

### Create Desktop Shortcut
`cmd
setup.bat
`

---

## 🔒 Privacy

- **100% Local**: No cloud telemetry, analytics trackers, or user tracking.
- Cache and profile data stored locally in %LOCALAPPDATA%\BlackTube-webview2.

## 📄 License
MIT License
