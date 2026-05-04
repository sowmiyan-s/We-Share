# 🚀 We Share

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![Avalonia UI](https://img.shields.io/badge/UI-Avalonia-orange.svg)](https://avaloniaui.net/)
[![Platform](https://img.shields.io/badge/Platform-Windows%20|%20Android-green.svg)](https://github.com/sowmiyan-s/We-Share)

**We Share** is a high-performance, cross-platform offline file transfer suite. Inspired by classic sharing utilities, it enables seamless, ultra-fast file transfers between **PCs** and **Android phones** over local Wi-Fi, with zero internet dependency.

---

## ✨ Key Features

- 📱 **Cross-Platform**: Transfer files between PC-to-PC, Phone-to-Phone, and PC-to-Phone.
- 📡 **Instant Discovery**: High-speed UDP radar automatically finds nearby devices.
- 🔗 **QR Handshake**: Scan to connect fallback for cross-network or hotspot transfers.
- 📂 **Multi-File Queue**: Append, remove, and manage entire batches of files before sending.
- ⚡ **Turbo Transfers**: Multi-threaded TCP socket engine with real-time progress tracking.
- 🔒 **Secure**: Local network only, with built-in handshake verification.

---

## 📥 Download

Get the latest stable version for your device:

- 💻 **[Download for Windows (Portable)](https://github.com/sowmiyan-s/We-Share/releases/latest)**
- 📱 **[Download for Android (APK)](https://github.com/sowmiyan-s/We-Share/releases/latest)**

*Or download directly from the repository:*
- [Direct APK Download (LFS)](https://github.com/sowmiyan-s/We-Share/raw/main/src/WeShare.Android/bin/Release/net8.0-android/com.weshare.app-Signed.apk)
- [Direct EXE Download (LFS)](https://github.com/sowmiyan-s/We-Share/raw/main/dist/ShareIt.UI.exe)

---

## 🏗️ Architecture

The project is built using a platform-agnostic architecture:

- **WeShare.Core**: The heart of the engine (TCP/UDP logic, Crypto, Data).
- **WeShare.UI (Shared)**: All UI components and views (Avalonia).
- **WeShare.Desktop**: The Windows host for desktop deployment.
- **WeShare.Android**: The mobile host for Android deployment.

```text
📁 We Share
├── 📁 src
│   ├── 📁 WeShare.Core        (Engine & Protocols)
│   ├── 📁 WeShare.UI          (Shared UI Library)
│   ├── 📁 WeShare.Desktop     (Windows App)
│   └── 📁 WeShare.Android     (Android App)
```

---

## 🚀 Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Android SDK (for mobile build)

### 💻 Running on Windows
```powershell
dotnet run --project src/WeShare.Desktop/WeShare.Desktop.csproj
```

### 📱 Building for Android
```powershell
dotnet build src/WeShare.Android/WeShare.Android.csproj -c Release
```
> [!NOTE]
> The generated APK will be located at: `src/WeShare.Android/bin/Release/net8.0-android/com.weshare.app-Signed.apk`

---

## 🛠️ Tech Stack
- **Framework**: .NET 8, Avalonia UI
- **Networking**: TCP Sockets, UDP Broadcast
- **Database**: SQLite (History & State)
- **Design**: Fluent/Modern Dark Mode

## 📜 License
This project is licensed under the MIT License - see the LICENSE file for details.

---
*Made with ❤️ for high-speed sharing.*
