<h1 align="center">
  <img src="src/WeShare.UI/Assets/logo.png" width="80" height="80" alt="We Share Logo"><br>
  We Share
</h1>

<p align="center">
  <strong>Fast · Local · Secure file transfers — no internet, no cloud, no limits.</strong>
</p>

<p align="center">
  <a href="https://github.com/sowmiyan-s/We-Share/actions/workflows/build.yml">
    <img src="https://github.com/sowmiyan-s/We-Share/actions/workflows/build.yml/badge.svg" alt="Build Status">
  </a>
  <a href="LICENSE">
    <img src="https://img.shields.io/badge/license-MIT-indigo.svg" alt="MIT License">
  </a>
  <img src="https://img.shields.io/badge/platform-Windows-0C0C0C.svg" alt="Platform">
  <img src="https://img.shields.io/badge/.NET-9.0-FF4B00.svg" alt=".NET 9">
  <img src="https://img.shields.io/badge/Avalonia-11-blue.svg" alt="Avalonia UI">
</p>

<p align="center">
  <a href="https://github.com/sowmiyan-s/We-Share/raw/main/setup/WeShare_Setup.exe">
    <img src="https://img.shields.io/badge/Download-We__Share__Setup.exe-purple?style=for-the-badge&logo=windows&logoColor=white" alt="Download Windows Installer" height="40">
  </a>
</p>

---

## 🚀 What is We Share?

**We Share** is a premium, open-source desktop utility for instant, cable-free file transfers over local Wi-Fi. It uses a high-performance multi-threaded TCP engine allowing any PC on your network to securely send and receive files.

**Zero configuration. Zero data charges. Maximum privacy.**

---

## ✨ Key Features

| Feature | Details |
|---|---|
| ⚡ **Turbo Transfer** | Multi-threaded TCP socket engine optimized for high-speed local transfers. |
| 🔍 **Auto Discovery** | Zero-config peer detection using UDP broadcast—no manual IP entry needed. |
| 🎨 **Premium UI** | A stunning, modern interface powered by Avalonia UI with an Indigo/Slate aesthetic. |
| 🔒 **Local Only** | End-to-end encryption of your workflow—your data never leaves your local network. |
| 🧙 **Transfer Wizard** | A streamlined, step-by-step process for sending and receiving files. |

---

## 🛠️ Tech Stack

- **UI Framework**: [Avalonia UI 11](https://avaloniaui.net/)
- **Runtime**: .NET 9.0 (C#)
- **Networking**: Custom TCP Sockets & UDP Broadcast
- **Database**: SQLite / LiteDB for transfer history and settings

---

## 📖 Getting Started

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Run from Source
```bash
git clone https://github.com/sowmiyan-s/We-Share.git
cd We-Share
dotnet run --project src/WeShare.Desktop/WeShare.Desktop.csproj
```

### Build a Release Binary
```bash
dotnet publish src/WeShare.Desktop/WeShare.Desktop.csproj \
  -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -o ./publish
```

---

## 📱 How to Use



### PC → PC
1. Ensure both PCs are on the same Wi-Fi network and running **We Share**.
2. Drag and drop files into the **Send** area.
3. Select the target device from the discovered list and click **Send**.
4. The receiving PC will automatically notify and save the incoming files.

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
