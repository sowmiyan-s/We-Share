# Contributing to We Share

Thank you for your interest in contributing to **We Share**! This project is open-source under the [MIT License](LICENSE), and we welcome contributions from developers of all skill levels.

---

## 1. Code of Conduct

We are committed to providing a welcoming, inclusive, and harassment-free environment for everyone. Please be respectful, constructive, and collaborative in all issues, pull requests, and discussions.

---

## 2. Getting Started

### 2.1. Prerequisites
To develop and build We Share locally, ensure you have:
- **[.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)** (v8.0.100 or later).
- **Windows 10 (Build 19041+) or Windows 11 (64-bit)** (Required for WinRT & wlanapi native APIs).
- **IDE**: [Visual Studio 2022](https://visualstudio.microsoft.com/) (with *.NET desktop development* workload) or [Visual Studio Code](https://code.visualstudio.com/) (with *C# Dev Kit* and *Avalonia for VS Code* extensions).
- **[Inno Setup 6](https://jrsoftware.org/isinfo.php)** (Optional, required only if compiling Windows installer packages).

### 2.2. Clone & Initial Build
```powershell
# Clone the repository
git clone https://github.com/sowmiyan-s/We-Share.git
cd We-Share

# Restore NuGet dependencies
dotnet restore

# Build the solution in Debug mode
dotnet build WeShare.sln

# Launch the desktop application
dotnet run --project src/WeShare.Desktop/WeShare.Desktop.csproj
```

---

## 3. Project Architecture

The solution is organized into three clean layers:

```text
src/
├── WeShare.Core/       # Business logic, networking, raw TCP/UDP, web portal, SQLite
│   ├── Data/           # SQLite database helper and migrations
│   ├── Discovery/      # UDP beacon discovery service & network filters
│   ├── Models/         # DeviceModel, FileItemModel, FileTransferState
│   ├── Network/        # NetworkHelper, WifiConnectorService (P/Invoke), HotspotService
│   ├── Security/       # Encryption and certificate helpers
│   ├── Services/       # Platform services and QRCoder integration
│   └── Transfer/       # TcpTransferManager, WebDashboardService, CaptivePortalService
│
├── WeShare.UI/         # Avalonia 11 presentation layer
│   ├── Assets/         # Logos, icons, and embedded graphics
│   ├── Controls/       # Custom XAML controls and radar canvas math
│   ├── Services/       # UI platform service adapters
│   └── Views/          # MainView.axaml, converters, and view logic
│
└── WeShare.Desktop/    # Windows runtime executable host
    ├── Program.cs      # Entry point & Avalonia desktop builder
    └── app.manifest    # High-DPI and Windows 10/11 compatibility manifest
```

---

## 4. Development Workflow & Guidelines

### 4.1. Coding Standards
- **C# Language Version**: C# 12. Use modern language features (primary constructors, pattern matching, collection expressions).
- **Nullable Reference Types**: `<Nullable>enable</Nullable>` is enforced across all projects. Do not introduce unhandled null references or suppress warnings with `!` unless strictly justified.
- **Asynchronous Programming**: Always use `async`/`await` with `CancellationToken` support for I/O and network operations. Avoid blocking calls (`.Result` or `.Wait()`) that freeze the UI thread.
- **UI Responsiveness**: Never run heavy calculations, socket reads, or disk operations on the UI thread. Dispatch UI updates via Avalonia's `Dispatcher.UIThread.Post(...)`.

### 4.2. Commit Message Conventions
We follow [Conventional Commits](https://www.conventionalcommits.org/):
- `feat: add IPv6 support to UDP beacon scanner`
- `fix: resolve file handle leak on cancelled transfer`
- `docs: update technical architecture specification`
- `style: refine radar canvas pulse animation timings`
- `refactor: extract socket framing logic into helper class`
- `perf: optimize 1MB buffer write loop for NVMe drives`

---

## 5. Building Release Packages

To build production-ready single-file binaries, portable ZIP packages, and the Inno Setup installer:

```powershell
# Run the automated build and packaging script
.\publish.ps1
```

The script performs:
1. Compilation of `WeShare.Desktop` as a self-contained, single-file binary for `win-x64`.
2. Generation of the portable ZIP archive in `setup/WeShare_Portable_win-x64.zip`.
3. Invocation of `iscc.exe` (Inno Setup) to compile the installer into `setup/WeShare_Setup_1.1.0.exe`.

---

## 6. Submitting a Pull Request (PR)

1. Fork the repository on GitHub.
2. Create a feature branch off `main`:
   ```bash
   git checkout -b feat/your-feature-name
   ```
3. Implement your changes, following coding standards and ensuring clean builds.
4. Verify functionality locally:
   - Build cleanly with `dotnet build -c Release`.
   - Test peer-to-peer transfers between two instances or via the Web Portal.
5. Push to your fork:
   ```bash
   git push origin feat/your-feature-name
   ```
6. Open a Pull Request against `main` on the official repository. Provide a descriptive summary of your changes, motivation, and test steps.

Thank you for helping make We Share faster, more reliable, and accessible to everyone!
