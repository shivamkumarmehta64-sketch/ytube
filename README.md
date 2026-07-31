# ytube — Ad-Free YouTube Desktop Client for Windows

A lightweight, high-performance desktop application for YouTube videos and audio built with C# (.NET Framework 4.8) and Microsoft WebView2. Packed with premium features, low RAM consumption, and zero ads.

![YouTube Desktop Icon](icon.png)

---

## ✨ Premium Features

- 🛑 **3-Layer Ad-Blocker**:
  - **Network-Level Filter**: Blocks 25+ ad and tracking domains (`doubleclick.net`, `googlesyndication.com`, `googleadservices.com`, etc.).
  - **API Payload Stripping**: Intercepts YouTube API payloads and removes ad keys (`adPlacements`, `playerAds`, `prerolls`, `masthead`).
  - **DOM Mute & Fast-Forward**: Automatically mutes video ad elements and skips them at 16x speed.

- ⏩ **SponsorBlock Integration**:
  - Seamlessly queries `sponsor.ajay.app` API to auto-skip video sponsors, intros, outros, and self-promotions.

- 🖼️ **Picture-in-Picture (PiP)**:
  - Toggle PiP mode directly from the system tray menu to keep videos floating above all other applications.

- ⚡ **Resource & Memory Optimization**:
  - **Chromium Launch Flags**: Restricted disk/media cache (32MB), single renderer process, and JS heap limit (128MB).
  - **Process Suspension**: WebView2 process suspends when minimized to tray, freeing unused RAM.
  - **Automated GC**: Background garbage collection runs every 60 seconds.
  - **RAM Usage**: ~120–160 MB active | ~60–80 MB minimized in tray.

- 🎛️ **Windows System Tray & Media Keys**:
  - Global hardware Play/Pause, Next Track, and Previous Track hotkeys.
  - Quiet system tray integration (left double-click restore, zero annoying popups).

---

## 🛠️ Build & Install

- **OS**: Windows 10 / Windows 11 (64-bit)
- **Runtime**: [.NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework/net48) (Pre-installed on Windows 10/11) + [Microsoft WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/)

### Compile Command

```cmd
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /reference:Microsoft.Web.WebView2.Core.dll /reference:Microsoft.Web.WebView2.WinForms.dll /out:ytube.exe ytube.cs
```

---

## 📜 License

Distributed under the MIT License.
