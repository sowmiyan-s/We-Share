# 🛠️ Technical Stack & Architecture

This document outlines the technical implementation details of **We Share**.

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

## 🛠️ Tech Stack

- **Framework**: [.NET 8.0](https://dotnet.microsoft.com/download/dotnet/8.0), [Avalonia UI](https://avaloniaui.net/)
- **Networking**: TCP Sockets (Data), UDP Radar (Discovery), Bluetooth LE (Nearby Identity)
- **Database**: SQLite (History & State Management)
- **Design**: Fluent Design System, Modern Dark Mode
- **Platform Support**: Windows (Desktop), Android (Mobile)

## 🚀 Development & Building

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
