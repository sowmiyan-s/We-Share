<div align="center">

<img src="src/WeShare.UI/Assets/logo.png" width="96" height="96" alt="We Share Logo" />

# We Share

**High-Performance, Local-First File Transfer Utility for Windows and Mobile**

Fast, private, and cable-free data transfers across your local network &mdash; no internet, no cloud intermediaries, and zero bandwidth limits.

[![Build Status](https://github.com/sowmiyan-s/We-Share/actions/workflows/build.yml/badge.svg)](https://github.com/sowmiyan-s/We-Share/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-7C3AED.svg)](LICENSE)
[![Platform: Windows](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-06B6D4.svg)](https://sowmiyan-s.github.io/We-Share/)
[![Runtime: .NET 8](https://img.shields.io/badge/.NET-8.0-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![UI: Avalonia 11](https://img.shields.io/badge/Avalonia%20UI-11.0-purple.svg)](https://avaloniaui.net/)
[![Version](https://img.shields.io/badge/Release-v1.1.0-10B981.svg)](https://github.com/sowmiyan-s/We-Share/releases)

<br />

[**Official Website**](https://sowmiyan-s.github.io/We-Share/) &bull;
[**Download Installer (.exe)**](https://github.com/sowmiyan-s/We-Share/raw/main/setup/WeShare_Setup_1.1.0.exe) &bull;
[**Download Portable (.zip)**](https://github.com/sowmiyan-s/We-Share/raw/main/setup/WeShare_Portable_win-x64.zip) &bull;
[**Brand Assets Kit**](https://github.com/sowmiyan-s/We-Share/tree/main/assets/brand) &bull;
[**Documentation**](https://sowmiyan-s.github.io/We-Share/how-to-use.html) &bull;
[**Release Notes**](https://sowmiyan-s.github.io/We-Share/patch-notes.html)

</div>

---

## Overview

**We Share** is an open-source desktop application engineered for instantaneous local network file sharing. Built with **C# / .NET 8** and **Avalonia UI 11**, it bypasses the internet entirely, utilizing raw multi-threaded TCP sockets and local UDP broadcast discovery to achieve wire-speed transfers between computers and mobile devices.

Whether sending 50 GB 4K video reels across an office, moving vacation photos from an iPhone without cloud compression, or transferring files between laptops in an airplane with no Wi-Fi router, We Share provides seamless, zero-configuration local networking.

### Core Capabilities

- **Turbo Multi-Threaded TCP Sockets**: Maximizes physical network bandwidth (Gigabit Ethernet and Wi-Fi 6) with chunked binary streaming and live transfer telemetry.
- **AirDrop-Grade Radar Discovery**: Zero-configuration UDP broadcast detection on port 45678. Devices orbit an interactive radar scanner with instant tap-to-send dispatch.
- **Apple-Style Radar Receive Station**: Full-screen radar listening station with pulsing concentric rings and live status indicator.
- **Universal Web Transfer (Zero Client Apps)**: Bidirectional transfer with iOS, Android, macOS, and Linux devices via an embedded HTTP 1.1 / SSE server and instant camera QR code pairing.
- **Desert Mode (Off-Grid Hotspot)**: Automatically provisions an ad-hoc Wi-Fi network and silently handshakes peer laptops when no router is available.
- **Pre-Upload Authorization & Consent**: Explicit receiver prompts display sender identity, filename, and byte metrics before any file payload is transferred.
- **Single-Session Concurrency Lock**: Prevents connection collisions and protects socket buffers by enforcing dedicated one-to-one transfer channels.
- **Obsidian Violet Signature Aesthetics**: Custom dark glassmorphism interface with fluent button micro-animations and zero command prompt windows.

---

## Visual Showcase

<div align="center">

### Home Command Center & Transfer Staging
<p align="center">
  <img src="docs/screenshot/screenshot-1.png" width="48%" alt="Home Dashboard" />
  <img src="docs/screenshot/screenshot-2.png" width="48%" alt="File Staging Queue" />
</p>

### AirDrop-Style Radar Discovery & Listening Station
<p align="center">
  <img src="docs/screenshot/screenshot-3.png" width="48%" alt="Radar Discovery" />
  <img src="docs/screenshot/screenshot-4.png" width="48%" alt="Radar Receive Station" />
</p>

### Dedicated Device Session Hub & Web Transfer
<p align="center">
  <img src="docs/screenshot/screenshot-5.png" width="48%" alt="Device Session Hub" />
  <img src="docs/screenshot/screenshot-6.png" width="48%" alt="Web Transfer Hub" />
</p>

</div>

---

## Technical Architecture & How It Works

```mermaid
flowchart TD
    subgraph Discovery ["1. Peer Discovery Layer (UDP 45678)"]
        A[UDP Broadcast Beacon] -->|Every 5s| B[Subnet Peer Listener]
        B --> C[NIC Virtual Adapter Filter\nExclude Hyper-V, WSL, Docker, VPN]
        C --> D[Active Device Registry\nPC, Mac, iOS, Android]
    end

    subgraph TransferEngine ["2. Direct Transfer Engine (TCP Sockets)"]
        E[Sender File Queue] --> F[Pre-Transfer Metadata Handshake]
        F -->|Prompt Receiver| G{Accept / Reject}
        G -->|Reject| H[Instant Rejection Signal]
        G -->|Accept| I[Multi-Threaded Binary Chunk Streaming]
        I --> J[CRC64 Stream Integrity Verification]
        J --> K[Disk Write to Downloads Folder]
    end

    subgraph WebPortal ["3. Universal Web Transfer (HTTP 1.1 + SSE)"]
        L[Embedded HTTP Server\nPorts 8080-8099] --> M[QR Code Generation]
        M --> N[Mobile Browser Safari / Chrome]
        N -->|Server-Sent Events| O[Bidirectional Session Hub]
        O -->|Push / Pull| E
    end

    subgraph Desert ["4. Off-Grid Hotspot (Desert Mode)"]
        P[Zero Network Detected] --> Q[WinRT Tethering Manager\nSSID: WeShare]
        Q --> R[Native wlanapi.dll P/Invoke\nSilent Client Auto-Connect]
        R --> A
    end
```

### 1. Peer Discovery Protocol (UDP Broadcast)
- Emits periodic JSON-encoded beacons over UDP port `45678` across local broadcast addresses.
- Built-in NIC filter identifies and excludes non-physical network adapters (WSL, Hyper-V, Docker, VMware, and VPN tunnels), preventing discovery dropouts.
- Maintains an active peer list with timestamp tracking; devices absent for >15 seconds are cleanly removed.

### 2. Multi-Threaded TCP Transfer Engine
- Dedicated TCP server listens for incoming binary transfer requests.
- Framed protocol header:
  ```text
  [Magic: 4B][ProtocolVer: 2B][SessionId: 16B][FileCount: 4B][TotalBytes: 8B][Manifest JSON]
  ```
- File streaming uses 64 KB memory chunks with real-time rolling average speed and completion calculations.
- Dedicated receiver confirmation dialog displays file name, count, and size prior to socket data ingestion.

### 3. Web Transfer Subsystem
- Lightweight HTTP 1.1 server running locally without external web server dependencies (IIS or Kestrel).
- Automatic port scanning fallback (`8080` &ndash; `8099`) if the primary port is occupied.
- Uses Server-Sent Events (`/api/events`) for real-time mobile push notifications, transfer progress bars, and batch dispatch.
- Uploads from mobile devices require desktop host approval via `/api/request-upload`.

### 4. Desert Mode Hotspot Subsystem
- Uses Windows Runtime (`WinRT`) APIs (`NetworkOperatorTetheringManager`) to provision a Wi-Fi hotspot on demand.
- Client devices running We Share detect the specific SSID and invoke Windows Native Wi-Fi API (`wlanapi.dll`) P/Invoke calls to join silently without password prompts.
- When the application closes, the Wi-Fi adapter is returned to its previous configuration.

---

## Sharing Scenarios

### Scenario A: Computer to Computer (Same Network)
1. Launch **We Share** on both Windows computers.
2. Discovered computers appear on the **Radar Scan** and **Your Network** sidebar roster.
3. Click **Select & Send** or drag files directly into the window.
4. Tap the destination computer on the radar to initiate transmission.
5. The receiving PC displays an **Accept / Reject** prompt. Upon acceptance, the transfer streams at wire speed.

### Scenario B: Computer to Smartphone (Web Transfer)
1. Click **Web Transfer** in the sidebar on the PC.
2. Point any iPhone or Android camera at the on-screen QR code.
3. Tap the browser notification to open the local transfer session in Safari, Chrome, or Firefox.
4. On the PC, click **Choose Files to Send** to stage files for the mobile device.
5. Tap the incoming download link on the phone to save photos, videos, or documents directly into mobile storage.

### Scenario C: Smartphone to Computer
1. Scan the PC's on-screen QR code with your mobile camera.
2. Tap **Choose Files** on the mobile webpage and pick images, videos, or documents.
3. On the PC, click **Accept** on the incoming transfer prompt.
4. Files stream directly to the computer's designated Downloads folder.

### Scenario D: Off-Grid (Airplane, Vehicle, Outdoors)
1. When no Wi-Fi router is present, click **Start Desert Hotspot** on Laptop A.
2. Open We Share on Laptop B &mdash; the client auto-connects to the ad-hoc network within seconds.
3. Both computers appear on each other's radar and transfer files without cellular or internet data.

---

## Installation & Distribution

### Requirements
- **Operating System**: Windows 10 (Build 19041+) or Windows 11 (64-bit)
- **Runtime**: None required (all packages are self-contained)
- **Network**: Wi-Fi adapter or local Ethernet network

### 1. Windows Installer (Recommended)
Download the latest installer from the release assets:
- **File**: `WeShare_Setup_1.1.0.exe`
- **Silent Installation (IT / Enterprise)**:
  ```powershell
  .\WeShare_Setup_1.1.0.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART
  ```

### 2. Standalone Portable Edition
Download the zero-install portable package:
- **File**: `WeShare_Portable_win-x64.zip`
- Extract anywhere (e.g., USB drive) and run `WeShare.Desktop.exe` directly without administrative privileges.

---

## Building from Source

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 or VS Code with C# Dev Kit
- [Inno Setup 6](https://jrsoftware.org/isinfo.php) (optional, for installer compilation)

### Clone & Build
```powershell
# Clone repository
git clone https://github.com/sowmiyan-s/We-Share.git
cd We-Share

# Restore dependencies
dotnet restore

# Build solution
dotnet build WeShare.sln -c Release

# Run desktop application
dotnet run --project src/WeShare.Desktop/WeShare.Desktop.csproj
```

### Packaging Release Binaries
To compile the single-file self-contained binary, portable ZIP, and Inno Setup installer:
```powershell
.\publish.ps1
```
Output binaries are generated in the `setup/` directory:
- `setup/WeShare_Setup_1.1.0.exe`
- `setup/WeShare_Portable_win-x64.zip`

---

## Security & Privacy

- **Local Network Isolation**: All data packets flow strictly over local layer-2/layer-3 network paths. No external servers or cloud services are involved.
- **Pre-Upload Verification**: Transfers require explicit destination acceptance before payload bytes are sent over the wire.
- **Single-Session Lock**: Transfer sessions are locked to one peer at a time, eliminating connection interference or rogue injections.
- **Encrypted Local Storage**: Transfer logs, device preferences, and file metadata are maintained in a local SQLite database (`weshare.db`) that never leaves the machine.
- **Zero Analytics & Telemetry**: We Share collects zero diagnostics, telemetry, or user analytics.

---

## Brand Assets & Media Kit

Official high-resolution logos, application icons, banners, and typography guides are maintained in the repository:
- **Directory**: [`assets/brand/`](https://github.com/sowmiyan-s/We-Share/tree/main/assets/brand)
- **Included Assets**:
  - High-resolution application logos (`APP LOGO.png`, `full png.png`)
  - Vector action glyphs (`send.png`, `receive.png`, `find.png`, `profile.png`)
  - Obsidian ecosystem background assets (`background.png`)

---

## Contributing

Contributions are welcomed. To contribute:

1. Fork the repository: `https://github.com/sowmiyan-s/We-Share`
2. Create a feature branch: `git checkout -b feature/NewFeature`
3. Commit your changes: `git commit -m 'feat: implement NewFeature'`
4. Push to the branch: `git push origin feature/NewFeature`
5. Open a Pull Request for review.

---

## License

This project is licensed under the **MIT License** &mdash; see the [LICENSE](LICENSE) file for details.

Developed with precision by [Sowmiyan-S](https://github.com/sowmiyan-s).
