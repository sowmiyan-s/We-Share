# 🛠️ Technical Stack & Architecture

This document outlines the technical implementation details of **We Share**.

## 🏗️ Architecture

The project is built using a platform-agnostic architecture:

- **WeShare.Core**: The heart of the engine (TCP/UDP logic, Web Server, Crypto, Data).
- **WeShare.UI (Shared)**: All UI components and views (Avalonia).
- **WeShare.Desktop**: The Windows host for desktop deployment.

```text
📁 We Share
├── 📁 src
│   ├── 📁 WeShare.Core        (Engine, Protocols & Web Server)
│   ├── 📁 WeShare.UI          (Shared UI Library)
│   └── 📁 WeShare.Desktop     (Windows App)
```

## 🛠️ Tech Stack

- **Framework**: [.NET 8.0](https://dotnet.microsoft.com/download/dotnet/8.0), [Avalonia UI](https://avaloniaui.net/)
- **Networking**: TCP Sockets (Data), UDP Radar (Discovery), Bluetooth LE (Nearby Identity)
- **Web Portal**: Raw TCP HTTP Server (Embedded in Core)
- **Database**: SQLite (History & State Management)
- **Design**: Fluent Design System, Modern Dark Mode
- **Platform Support**: Windows (Desktop), Web (Mobile Portal)

## 🚀 Development & Building

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### 💻 Running on Windows
```powershell
dotnet run --project src/WeShare.Desktop/WeShare.Desktop.csproj
```

> [!NOTE]
> The Web Portal is automatically started on port 8080 when the application runs.
