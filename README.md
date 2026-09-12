# ⚡ BlackTube

> **Ultra-lightweight, dedicated YouTube desktop app with native Windows UI compatibility and edge-to-edge full-screen optimization.**  
> Built with native **C# .NET + Microsoft WebView2**. No heavy Electron runtimes (~99 KB standalone binary, under 50 MB RAM).

---

## ✨ Key Features

| Feature | Shortcut / Action | Description |
|---|---|---|
| 🎬 **Dedicated YouTube Experience** | — | Pure YouTube desktop environment without unnecessary web browser bloat or dual-engine overhead. |
| ⛶ **Full Screen Optimization** | <kbd>F11</kbd> / Video Fullscreen Button | Automatic edge-to-edge borderless fullscreen. Seamlessly hides window frames and top navigation when entering YouTube video fullscreen or pressing <kbd>F11</kbd>. |
| 🪟 **Windows 11/10 UI Compatibility** | — | Native DWM Immersive Dark Mode, Windows 11 rounded corners, Per-Monitor V2 HiDPI scaling, and dark theme caption bar (`#0F0F0F`). |
| 🧭 **Native Navigation Bar** | UI Header | Fluent Windows toolbar with Back (<kbd>Alt</kbd>+<kbd>←</kbd>), Forward (<kbd>Alt</kbd>+<kbd>→</kbd>), Reload (<kbd>Ctrl</kbd>+<kbd>R</kbd> / <kbd>F5</kbd>), Home (<kbd>Ctrl</kbd>+<kbd>H</kbd>), and Quick Search (<kbd>Ctrl</kbd>+<kbd>F</kbd>). |
| 🛡️ **5-Tier Resilient AdShield** | — | Network-level request blocker, deep `/youtubei/v1/` JSON API stripper, DOM cleaner, and 16x video fast-forward fallback. |
| ⏩ **SponsorBlock Integration** | — | Automatically detects and skips sponsored segments, intros, outros, and subscribe reminders via public SponsorBlock API. |
| 🪟 **Picture-in-Picture (PiP)** | <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>P</kbd> | Compact floating frameless window pinned in the corner for watching videos while multitasking. |
| ⏱️ **Sleep Timer** | <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>S</kbd> | Auto-pauses video playback after 15, 30, 45, 60, or 90 minutes. |
| 📌 **Always on Top** | <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>T</kbd> | Pin the main window above all other open applications. |
| 🎧 **System Tray & Media Keys** | Right-Click Tray / Keyboard | Minimize to system tray and control video playback (Play/Pause, Next, Prev) globally. |
| ⚡ **Ultra-Low Memory Footprint** | — | Automatic memory trimming calling `kernel32` working-set trims and GC compaction on idle/minimize. |

---

## 🛡️ 5-Tier Resilient Ad Blocking Architecture

YouTube frequently updates ad delivery mechanisms and enforcement dialogs. **BlackTube** implements a multi-tier defense:

```
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
```

---

## ⌨️ Keyboard Shortcuts

| Shortcut | Action |
|---|---|
| <kbd>F11</kbd> | Toggle **Full Screen Mode** |
| <kbd>Esc</kbd> | Exit **Full Screen** / **PiP Mode** |
| <kbd>Ctrl</kbd> + <kbd>F</kbd> | Focus **YouTube Search Bar** |
| <kbd>Ctrl</kbd> + <kbd>H</kbd> | Go to **YouTube Home** |
| <kbd>Alt</kbd> + <kbd>←</kbd> | Go **Back** in history |
| <kbd>Alt</kbd> + <kbd>→</kbd> | Go **Forward** in history |
| <kbd>Ctrl</kbd> + <kbd>R</kbd> / <kbd>F5</kbd> | Reload View |
| <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>P</kbd> | Toggle **Picture-in-Picture (PiP)** mode |
| <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>S</kbd> | Open **Sleep Timer** menu |
| <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>T</kbd> | Toggle **Always On Top** |
| <kbd>Ctrl</kbd> + <kbd>M</kbd> | Minimize to Tray |
| <kbd>Ctrl</kbd> + <kbd>Q</kbd> | Quit Application |
| <kbd>MediaPlayPause</kbd> | Global Play / Pause |
| <kbd>MediaNextTrack</kbd> | Global Next Video |
| <kbd>MediaPreviousTrack</kbd> | Global Previous Video |

---

## 🚀 Build & Run

### Prerequisites
- Windows 10 or 11
- Microsoft WebView2 Runtime (built-in on Windows 10/11)
- Native .NET Framework 4.8 (built-in on Windows)

### 1-Click Build
```cmd
build.bat
```
Generates standalone `BlackTube.exe` in seconds without needing Visual Studio or external package managers.

### Create Desktop Shortcut
```cmd
setup.bat
```

---

## 🔒 Privacy

- **100% Local**: No telemetry, analytics trackers, or user tracking.
- Cache and profile data stored locally in `%LOCALAPPDATA%\BlackTube-webview2`.

## 📄 License
MIT License
