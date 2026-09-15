# We Share Desktop Application Restoration Log

## Overview
This document preserves the context, UI design specifications, and restoration steps for the **We Share** desktop application layout corresponding to the reference design (captured in `docs/screenshot/screenshot-1.png`).

---

## 1. Visual & Layout Specifications

### Ambient Root Canvas
- **Background Image**: `avares://WeShare.UI/Assets/background.png` with `Stretch="UniformToFill"` and `Opacity="0.36"`.
- **Atmospheric Overlay**: `<Border Background="#D0090A0F" IsHitTestVisible="False"/>` producing a deep, twilight violet starry sky with a crisp city skyline silhouette along the bottom edge.

### Unified Left Sidebar
- **Brand Header**: White vector infinity icon (`brand_symbol.png`) with bold `"We Share"` typography.
- **Primary Navigation**:
  - `Home`: Active pill with violet home icon and white text.
  - `Active Transfers`: Lightning icon with slate text.
  - `File History`: Clock icon with slate text.
  - `Web Transfer`: Mobile device icon with slate text.
- **Nearby Devices Quick List**:
  - Device type indicator (e.g. `PC` badge in cyan/blue circle).
  - Device display name (`DisplayName` binding with nickname and favorite support).
  - Star pin icon (`★` for pinned favorites, `☆` for unpinned).
  - Amber/orange `Send` button (`#F59E0B`).
- **Workstation Identity Card**:
  - Avatar: Circular `profile.png` (28x28 with 14px corner radius).
  - Identity: Device Name (`SidebarDeviceName`) and Network Status (`SidebarNetworkInfo`).
  - Network Indicator: Glowing green online status dot (`#10B981`).
- **System Actions**: `Settings` and `About` buttons at the bottom.

### Home View (Command Center)
- **Send Hero Disc**:
  - Circular action button with purple radial gradient.
  - White vector paper plane icon (`send.png`).
  - Label: `"SEND"` in bold uppercase, subtitle `"Drop files or click to send"`.
- **Receive Hero Disc**:
  - Circular action button with purple radial gradient.
  - White vector cloud download icon (`receive.png`).
  - Label: `"RECEIVE"` in bold uppercase, subtitle `"Ready to accept transfers"`.
- **Mobile QR Shortcut**:
  - Dark pill button with amber phone badge and text `"Connect Phone (QR)"`.

### Radar Discovery Scanner
- Concentric range rings centered within the 480x380 viewport.
- Center local node featuring the circular magnifying glass icon (`find.png`).

### About Dialog
- Application identity card featuring `app_logo.png`.

---

## 2. Compilation & Packaging Verification
- **Solution Build**: `dotnet build WeShare.sln` -> 0 Warning(s), 0 Error(s).
- **Standalone Binary**: `publish/WeShare.Desktop.exe` (Single-file, self-contained win-x64).
- **Portable Distribution**: `setup/WeShare_Portable_win-x64.zip`.
- **Inno Setup Installer**: `setup/WeShare_Setup_1.1.0.exe`.
- **Screenshot Harness**: `dotnet run --project "src\WeShare.Desktop" -- --capture-screenshots` verified all 6 views into `docs/screenshot/`.
