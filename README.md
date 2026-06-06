<h1 align="center">
  <img src="src/WeShare.UI/Assets/logo.png" width="80" height="80" alt="We Share Logo"><br>
  We Share
</h1>

<p align="center">
  <strong>Fast · Local · Secure file transfers — no internet, no cloud, no limits.</strong><br>
  <em>Works on any network — or even with <strong>no network at all</strong>.</em>
</p>

<p align="center">
  <a href="https://github.com/sowmiyan-s/We-Share/actions/workflows/build.yml">
    <img src="https://github.com/sowmiyan-s/We-Share/actions/workflows/build.yml/badge.svg" alt="Build Status">
  </a>
  <a href="LICENSE">
    <img src="https://img.shields.io/badge/license-MIT-indigo.svg" alt="MIT License">
  </a>
  <img src="https://img.shields.io/badge/platform-Windows-0C0C0C.svg" alt="Platform">
  <img src="https://img.shields.io/badge/.NET-8.0-FF4B00.svg" alt=".NET 8">
  <img src="https://img.shields.io/badge/Avalonia-11-blue.svg" alt="Avalonia UI">
</p>

<p align="center">
  <a href="https://github.com/sowmiyan-s/We-Share/raw/main/setup/WeShare_Setup.exe">
    <img src="https://img.shields.io/badge/Download-We__Share__Setup.exe-purple?style=for-the-badge&logo=windows&logoColor=white" alt="Download Windows Installer" height="40">
  </a>
</p>

---

## 🚀 What is We Share?

**We Share** is a premium, open-source desktop utility for instant, cable-free file transfers — with or without a Wi-Fi router. It uses a high-performance multi-threaded TCP engine allowing any PC on your network to securely send and receive files.

**Zero configuration. Zero data charges. Maximum privacy.**

> **No router? No problem.** We Share automatically creates a Wi-Fi hotspot on one PC and silently connects the other — no passwords to type, no settings to change.

---

## ✨ Key Features

| Feature | Details |
|---|---|
| ⚡ **Turbo Transfer** | Multi-threaded TCP socket engine optimized for high-speed local transfers with Windows-style speed graphs. |
| 🔍 **Auto Discovery** | Zero-config peer detection using UDP broadcast — no manual IP entry needed. |
| 🏜️ **Desert Mode** | No router? App auto-creates a hotspot and connects the other PC silently. Includes Captive Portal redirect. |
| ✅ **Pre-Upload Authorization** | Receiver prompts (Accept / Reject) *before* transfers start for both desktop and mobile web portal uploads. |
| 🛡️ **Single-Session Protection** | Enforces one active transfer session at a time, protecting networks from connection collision. |
| 📈 **Real-time Speed Graphs** | Windows-style green progress bars and canvas-drawn speed graphs on both desktop and web portal UI. |
| 🎨 **Premium Web Portal UI** | Redesigned matte zinc/emerald mobile layout with custom typography (`Bricolage Grotesque`) and marquee banners. |
| 🔒 **Local Only** | End-to-end local — SQLite log data, peer identification, and files never leave your local area network. |

---

## 🏜️ No Wi-Fi? No Problem — Desert Mode

We Share works **even when there's no router or Wi-Fi access point**. Here's exactly what happens automatically:

```
PC A opens We Share (no network detected)
  → Silently starts a Wi-Fi hotspot named "WeShare"

PC B opens We Share (no network detected)
  → Detects "WeShare" hotspot
  → Auto-connects (no password prompt, no manual entry)
  → Discovers PC A on the radar automatically

PC B: Selects PC A → clicks Send
PC A: Gets ACCEPT / REJECT notification
  → Clicks ACCEPT → Transfer begins ✅
```

**The entire flow is automatic.** No passwords to type, no settings to configure, no extra software.

---

## 🛠️ Tech Stack

- **UI Framework**: [Avalonia UI 11](https://avaloniaui.net/)
- **Runtime**: .NET 8.0 (C#)
- **Networking**: Custom TCP Sockets & UDP Broadcast
- **Auto Hotspot**: WinRT `NetworkOperatorTetheringManager` (Windows Mobile Hotspot API)
- **Auto Wi-Fi Connect**: Native `wlanapi.dll` P/Invoke (zero elevation, zero prompts)
- **Database**: SQLite for transfer history and settings

---

## 📖 Getting Started

### Prerequisites
- Windows 10 or Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for building from source)

### Download & Install (Recommended)

Download the latest installer from the badge above — no .NET runtime required, self-contained.

### Run from Source
```bash
git clone https://github.com/sowmiyan-s/We-Share.git
cd We-Share
dotnet run --project src/WeShare.Desktop/WeShare.Desktop.csproj
```

### Build Release Installer
```powershell
# Requires Inno Setup 6 at C:\Program Files (x86)\Inno Setup 6\ISCC.exe
.\publish.ps1
# Output: setup\WeShare_Setup.exe
```

---

## 📱 How to Use

### Normal Mode (Both PCs on Same Wi-Fi)
1. Open **We Share** on both PCs — they auto-discover each other.
2. On the **sender**: drag & drop files → select the receiver from the radar → click **Send**.
3. On the **receiver**: tap **ACCEPT** when the notification appears.
4. Transfer completes at full local network speed. ✅

### Desert Mode (No Router / No Wi-Fi)
1. Open **We Share** on both PCs — no setup needed.
2. The app handles everything automatically (hotspot + auto-connect).
3. Proceed exactly as normal mode above. ✅

---

## 🤝 Contributing

We welcome contributions! Whether it's fixing bugs, adding features, or improving documentation:

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

## 📄 License

Distributed under the MIT License. See `LICENSE` for more information.

<p align="center">
  Built with ❤️ for the local sharing community.<br>
  <b><a href="https://github.com/sowmiyan-s/We-Share">View Repository</a></b>
</p>
