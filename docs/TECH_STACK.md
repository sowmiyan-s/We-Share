# Technical Stack & Engineering Specifications

This document outlines the complete technology stack, third-party libraries, operating system integrations, and architectural decisions underlying **We Share**.

---

## 1. Stack Overview

| Category | Technology | Version | Purpose |
| :--- | :--- | :--- | :--- |
| **Language** | C# | 12.0 | Core programming language |
| **Target Runtime** | .NET | 8.0 (`net8.0-windows10.0.19041.0`) | Cross-platform runtime with Windows 10 SDK projections |
| **UI Framework** | Avalonia UI | 11.0.10 | Modern, cross-platform XAML GUI framework |
| **Theme System** | Fluent Theme + Custom Styling | 11.0.10 | Obsidian Violet dark glassmorphism styling |
| **Typography** | Inter (Google Fonts) | 11.0.10 | Premium high-legibility UI typography |
| **Local Database** | Microsoft.Data.Sqlite | 8.0.0 | High-performance embedded ACID SQL engine |
| **QR Generation** | QRCoder | 1.4.3 | Pure C# QR code rasterization for mobile pairing |
| **Networking API** | System.Net.Sockets | .NET 8 BCL | Non-blocking TCP/UDP sockets for wire-speed transfers |
| **Native Wi-Fi API** | `wlanapi.dll` (Win32) | Native | P/Invoke interop for zero-elevation Wi-Fi auto-connect |
| **Hotspot Engine** | WinRT Tethering APIs | Windows 10/11 | Programmatic Wi-Fi hotspot management |
| **Embedded Web** | Custom Raw-TCP HTTP 1.1 | In-house | Zero-dependency HTTP & SSE server (no admin rights) |
| **Captive Portal** | Raw UDP 53 / TCP 80 | In-house | DNS redirection & captive portal sheet triggering |
| **Packaging** | Inno Setup | 6.x | Single-file Windows installer compilation |
| **Build Scripts** | PowerShell Core | 7.x / 5.1 | Automated self-contained compilation & distribution |

---

## 2. Component Details

### 2.1. Core Runtime & Language
- **.NET 8 LTS (`net8.0-windows10.0.19041.0`)**: Provides high-performance JIT compilation, Span-based zero-copy memory slicing, asynchronous file I/O pipelines, and modern hardware-accelerated cryptographic primitives.
- **C# 12 Features**: Primary constructors, pattern matching, record types, file-scoped namespaces, collection expressions, and nullable reference types enforced throughout the codebase.
- **WinRT C#/WinRT Projection**: Enables managed invocation of Windows Runtime APIs (`Windows.Networking.NetworkOperators`) without C++/WinRT bridge layers.

### 2.2. User Interface & Presentation (`WeShare.UI`)
- **Avalonia UI 11.0.10**: A cross-platform XAML-based UI framework powered by the Skia graphics library with Direct3D11 hardware acceleration on Windows.
- **Fluent Theme & Inter Font**: Provides smooth micro-animations, acrylic glassmorphic backdrops, modern button states, and font rendering.
- **Mathematical Canvas Controls**: Dynamic radar view calculated in real time using polar-to-Cartesian trigonometry, featuring orbit rings, pulsing beacon waves, and drag-and-drop targeting.

### 2.3. Low-Level Networking (`WeShare.Core`)
- **Multi-Threaded TCP Sockets**: Managed via `System.Net.Sockets.TcpClient` and `TcpListener`. The socket options `NoDelay = true` (disabling Nagle's algorithm) and 1 MB send/receive buffers (`EnterpriseBufferSize = 1048576`) enable high throughput on Gigabit Ethernet and Wi-Fi 6 links.
- **UDP Broadcast Engine**: `System.Net.Sockets.UdpClient` configured with `ReuseAddress = true` and `EnableBroadcast = true` on UDP port `45678`.
- **Intelligent Adapter Detection**: Custom `NetworkHelper` parses `NetworkInterface.GetAllNetworkInterfaces()` to isolate physical 802.11 Wi-Fi and 802.3 Ethernet adapters while filtering out Hyper-V, WSL, Docker, VMware, and VPN adapters.
- **Native Wi-Fi Interop (`WifiConnectorService`)**: P/Invoke bindings into Windows `wlanapi.dll`:
  - `WlanOpenHandle` / `WlanCloseHandle`
  - `WlanEnumInterfaces`
  - `WlanScan` / `WlanGetAvailableNetworkList`
  - `WlanSetProfile` / `WlanConnect` / `WlanDisconnect`
- **Tethering Subsystem (`HotspotService`)**:
  - `Windows.Networking.NetworkOperators.NetworkOperatorTetheringManager` provisions WPA2 hotspots programmatically on Windows 10/11.

### 2.4. Embedded Web Subsystem
- **Custom Raw-TCP HTTP 1.1 Server**: Implemented directly on `TcpListener` without `HttpListener` or ASP.NET Kestrel. Parses HTTP requests, multipart boundaries, URL-encoded query strings, and streaming downloads without administrator privileges.
- **Server-Sent Events (SSE)**: Streams events over `text/event-stream` connections, enabling mobile browsers to react to file offers, progress bars, and batch manifests without polling.
- **Embedded Web Client (`WebDashboardHtml.cs`)**: Single-file bundled responsive web app written in vanilla HTML5, modern CSS3 variables, and vanilla JavaScript (Fetch API, EventSource, FileReader, and Canvas).
- **Captive Portal Engine (`CaptivePortalService`)**:
  - Embedded DNS server listening on UDP port `53` responding to all domain queries with the hotspot gateway IP (`192.168.137.1`).
  - Embedded HTTP server listening on TCP port `80` returning immediate `302 Found` redirects to `http://192.168.137.1:8080/`.

### 2.5. Data Persistence (`WeShare.Core.Data`)
- **Microsoft.Data.Sqlite 8.0.0**: Local embedded database engine.
- **Write-Ahead Logging (WAL)**: Enabled via `PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;` for fast disk writes and non-blocking concurrent reads.
- **Concurrency Locking**: Dedicated `SemaphoreSlim(1, 1)` serializes asynchronous database writes, eliminating SQLite locked file errors under high transfer frequency.

---

## 3. Architectural Decision Records (ADRs)

### ADR 1: Raw TCP Sockets vs. WebSockets / gRPC
- **Context**: File transfers between desktop peers need to move multi-gigabyte files with low CPU overhead and high line-rate speeds.
- **Decision**: Use raw framed TCP sockets (`TcpClient`/`TcpListener`) with custom 9-byte packet headers (`WESH`).
- **Rationale**: WebSockets and gRPC introduce framing overhead, HTTP/2 framing complexity, and serialization/deserialization penalties. Raw TCP streaming with 1 MB memory buffers achieves near-zero CPU copy overhead.

### ADR 2: Custom Raw-TCP HTTP Server vs. Kestrel / HttpListener
- **Context**: Mobile devices need a web interface to send and receive files without installing apps.
- **Decision**: Implement a custom, lightweight HTTP 1.1 / SSE engine using raw `TcpListener`.
- **Rationale**:
  - `HttpListener` relies on `http.sys`, which requires administrative URL reservation commands (`netsh http add urlacl`) to bind on non-localhost interfaces.
  - ASP.NET Core Kestrel adds 15–20 MB of assembly overhead and complex dependency injection pipelines.
  - The custom raw-TCP server compiles into less than 100 KB of code and runs with standard user privileges on any port.

### ADR 3: Avalonia UI 11 vs. WPF, MAUI, or Electron
- **Context**: The desktop client needs a modern UI with 60 FPS animations, low memory footprint, and potential future cross-platform portability (macOS/Linux).
- **Decision**: Select Avalonia UI 11.
- **Rationale**:
  - Electron consumes 150–300 MB of RAM just for the Chromium runtime. Avalonia desktop uses ~45 MB RAM.
  - WPF is locked to Windows and has an aging rendering pipeline.
  - .NET MAUI has limited desktop controls and flexibility.
  - Avalonia uses Skia rendering, Direct3D11 acceleration, and modern XAML styling.

### ADR 4: UDP Broadcast vs. mDNS (Multicast DNS) Alone
- **Context**: Peers need immediate detection on complex local networks.
- **Decision**: Combine targeted UDP broadcast (port 45678) across physical network interfaces with Desert Mode directed unicast pings.
- **Rationale**: Many consumer Wi-Fi routers and guest networks throttle or block 224.0.0.251 mDNS multicast packets. Direct subnet broadcast (`192.168.x.255`) and direct unicast pings guarantee peer discovery across broader router configurations.

---

## 4. Hardware & System Requirements

### Host Requirements (Desktop Application)
- **OS**: Windows 10 (Build 19041 / 20H1 or newer) or Windows 11 (64-bit).
- **CPU**: x64 Dual-Core 1.6 GHz or higher.
- **Memory**: 128 MB free RAM (Application consumes ~45-65 MB during active transfers).
- **Disk**: 60 MB storage space for application binaries.
- **Network**: Wi-Fi adapter (802.11n/ac/ax) or Gigabit Ethernet (1000BASE-T).

### Client Requirements (Universal Web Portal)
- Any device running a modern web browser:
  - **iOS**: Safari 13+ (iPhone / iPad)
  - **Android**: Chrome 80+, Samsung Internet, Firefox
  - **macOS**: Safari, Chrome, Edge, Firefox
  - **Linux**: Chrome, Firefox
