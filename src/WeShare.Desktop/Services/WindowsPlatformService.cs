using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using WeShare.Core.Models;
using WeShare.Core.Services;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Networking.Connectivity;
using Windows.Networking.NetworkOperators;
using Windows.Storage.Streams;

namespace WeShare.Desktop.Services
{
    public class WindowsPlatformService : IPlatformService
    {
        public bool IsHotspotRunning { get; private set; }
        public string HotspotIp { get; private set; } = "192.168.137.1";

        private BluetoothLEAdvertisementWatcher?   _watcher;
        private BluetoothLEAdvertisementPublisher? _publisher;
        private const ushort ManufacturerId = 0x4747;

        // Tracks which strategy was used so we know how to stop it correctly
        private enum HotspotMode { None, WinRT, Netsh }
        private HotspotMode _hotspotMode = HotspotMode.None;

        // Saves the user's original Wi-Fi SSID so we can restore it after Desert Mode
        private string? _originalSsid;

        public string GetDeviceType() => "PC";

        public string GetDefaultSavePath() =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                         "Downloads", "WeShare");

        // ── Hotspot ──────────────────────────────────────────────────────────

        public async Task<(bool Success, string Message)> StartHotspotAsync(string ssid, string password)
        {
            // Save the current Wi-Fi connection so we can restore it after sharing
            _originalSsid = await GetCurrentWifiSsidAsync();

            // ── Strategy 1: WinRT Mobile Hotspot ─────────────────────────────
            // Works when Windows has an active internet connection profile.
            try
            {
                var connectionProfile = NetworkInformation.GetInternetConnectionProfile();
                if (connectionProfile != null)
                {
                    var manager       = NetworkOperatorTetheringManager.CreateFromConnectionProfile(connectionProfile);
                    var configuration = new NetworkOperatorTetheringAccessPointConfiguration
                    {
                        Ssid       = ssid,
                        Passphrase = password
                    };
                    await manager.ConfigureAccessPointAsync(configuration);

                    var result = await manager.StartTetheringAsync();
                    if (result.Status == TetheringOperationStatus.Success)
                    {
                        IsHotspotRunning = true;
                        _hotspotMode     = HotspotMode.WinRT;
                        await Task.Delay(2000);
                        HotspotIp = DetectHotspotIp();
                        return (true, $"Hotspot started. IP: {HotspotIp}");
                    }
                    // Non-success status — fall through to Desert Mode
                }
            }
            catch
            {
                // WinRT failed or not available — fall through to Desert Mode
            }

            // ── Strategy 2: Desert Mode — netsh hostednetwork ────────────────
            // Creates a real standalone Wi-Fi AP with NO internet required.
            // Windows assigns IP 192.168.137.1 on the virtual adapter automatically.
            return await StartDesertModeHotspotAsync(ssid, password);
        }

        private async Task<(bool Success, string Message)> StartDesertModeHotspotAsync(string ssid, string password)
        {
            var (ok1, _) = await RunElevatedAsync("netsh",
                $"wlan set hostednetwork mode=allow ssid=\"{ssid}\" key=\"{password}\"");

            if (!ok1)
                return (false,
                    "Desert Mode setup failed. Try running WeShare as Administrator, " +
                    "and ensure your Wi-Fi adapter supports Virtual AP.");

            var (ok2, _) = await RunElevatedAsync("netsh", "wlan start hostednetwork");
            if (!ok2)
                return (false,
                    "Could not start the Desert Mode hotspot. " +
                    "Your Wi-Fi adapter may not support Virtual AP (common with USB dongles). " +
                    "Try a different adapter or run as Administrator.");

            IsHotspotRunning = true;
            _hotspotMode     = HotspotMode.Netsh;

            // Wait for virtual adapter to receive its IP (up to 5 s)
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(500);
                string ip = DetectHotspotIp();
                if (ip != "192.168.137.1")
                {
                    HotspotIp = ip;
                    return (true, $"Desert Mode active. IP: {ip}");
                }
            }

            HotspotIp = "192.168.137.1";
            return (true, $"Desert Mode active. IP: {HotspotIp}");
        }

        public async Task StopHotspotAsync()
        {
            try
            {
                if (_hotspotMode == HotspotMode.WinRT)
                {
                    var profile = NetworkInformation.GetInternetConnectionProfile();
                    if (profile != null)
                    {
                        var mgr = NetworkOperatorTetheringManager.CreateFromConnectionProfile(profile);
                        await mgr.StopTetheringAsync();
                    }
                }
                else if (_hotspotMode == HotspotMode.Netsh)
                {
                    await RunElevatedAsync("netsh", "wlan stop hostednetwork");
                    await RunElevatedAsync("netsh", "wlan set hostednetwork mode=disallow");
                }
            }
            catch { }

            IsHotspotRunning = false;
            _hotspotMode     = HotspotMode.None;

            // Restore the original Wi-Fi connection the user had before the hotspot
            if (!string.IsNullOrEmpty(_originalSsid))
            {
                await RestoreWifiConnectionAsync(_originalSsid);
                _originalSsid = null;
            }
        }

        // ── Bluetooth ─────────────────────────────────────────────────────────

        public void StartBluetoothDiscovery(Action<DeviceModel> onDeviceFound)
        {
            try
            {
                StopBluetoothDiscovery();
                _watcher = new BluetoothLEAdvertisementWatcher
                {
                    ScanningMode = BluetoothLEScanningMode.Active
                };

                _watcher.Received += (s, args) =>
                {
                    string? name = null;
                    string? ip   = null;
                    string? ssid = null;
                    string? pwd  = null;

                    foreach (var mData in args.Advertisement.ManufacturerData)
                    {
                        if (mData.CompanyId == ManufacturerId)
                        {
                            try
                            {
                                var reader   = DataReader.FromBuffer(mData.Data);
                                byte dataType = reader.ReadByte();
                                if (dataType == 0x01)
                                {
                                    byte[] ipBytes = new byte[4];
                                    reader.ReadBytes(ipBytes);
                                    ip = new IPAddress(ipBytes).ToString();

                                    ushort port        = reader.ReadUInt16();
                                    ushort hotspotCode = reader.ReadUInt16();

                                    if (hotspotCode > 0)
                                    {
                                        ssid = WeShare.Core.Network.HotspotService.TargetSsid;
                                        pwd  = WeShare.Core.Network.HotspotService.TargetPassword;
                                    }

                                    byte nameLen = reader.ReadByte();
                                    if (nameLen > 0) name = reader.ReadString(nameLen);
                                }
                            }
                            catch { }
                        }
                    }

                    if (!string.IsNullOrEmpty(ip))
                    {
                        onDeviceFound?.Invoke(new DeviceModel
                        {
                            Id       = args.BluetoothAddress.ToString(),
                            Name     = name ?? "Unknown Device",
                            IpAddress = ip,
                            Type     = "Nearby Device",
                            LastSeen = DateTime.Now,
                            Port     = 45679,
                            Ssid     = ssid,
                            Password = pwd,
                            IsReceiver = true
                        });
                    }
                };

                _watcher.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BLE Watcher Error: {ex.Message}");
            }
        }

        public void StopBluetoothDiscovery()
        {
            _watcher?.Stop();
            _watcher = null;
        }

        public void StartBluetoothAdvertising(DeviceModel localDevice)
        {
            try
            {
                StopBluetoothAdvertising();
                _publisher = new BluetoothLEAdvertisementPublisher();

                var writer = new DataWriter();
                writer.WriteByte(0x01);

                string ipStr = GetLocalIp();
                if (!IPAddress.TryParse(ipStr, out var ipAddr)) ipAddr = IPAddress.Loopback;
                writer.WriteBytes(ipAddr.GetAddressBytes());
                writer.WriteUInt16((ushort)localDevice.Port);

                ushort hotspotCode = (IsHotspotRunning && HotspotIp != null) ? (ushort)9999 : (ushort)0;
                writer.WriteUInt16(hotspotCode);

                byte[] nameBytes = Encoding.UTF8.GetBytes(localDevice.Name);
                byte   nameLen   = (byte)Math.Min(nameBytes.Length, 12);
                writer.WriteByte(nameLen);
                writer.WriteBytes(nameBytes[..nameLen]);

                _publisher.Advertisement.ManufacturerData.Add(
                    new BluetoothLEManufacturerData(ManufacturerId, writer.DetachBuffer()));
                _publisher.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"BLE Adv Error: {ex.Message}");
            }
        }

        public void StopBluetoothAdvertising()
        {
            _publisher?.Stop();
            _publisher = null;
        }

        // ── Wi-Fi ─────────────────────────────────────────────────────────────

        public async Task<bool> ConnectToWifiAsync(string ssid, string password)
        {
            try
            {
                var access = await Windows.Devices.WiFi.WiFiAdapter.RequestAccessAsync();
                if (access != Windows.Devices.WiFi.WiFiAccessStatus.Allowed) return false;

                var adapters = await Windows.Devices.WiFi.WiFiAdapter.FindAllAdaptersAsync();
                if (adapters.Count == 0) return false;

                var adapter = adapters[0];
                await adapter.ScanAsync();

                var network = adapter.NetworkReport.AvailableNetworks
                                     .FirstOrDefault(n => n.Ssid == ssid);
                if (network == null) return false;

                var credential = new Windows.Security.Credentials.PasswordCredential();
                if (!string.IsNullOrEmpty(password)) credential.Password = password;

                var result = await adapter.ConnectAsync(
                    network,
                    Windows.Devices.WiFi.WiFiReconnectionKind.Automatic,
                    credential);

                return result.ConnectionStatus == Windows.Devices.WiFi.WiFiConnectionStatus.Success;
            }
            catch { return false; }
        }

        // ── Wi-Fi state save / restore ────────────────────────────────────────

        /// <summary>
        /// Reads the current connected Wi-Fi SSID via "netsh wlan show interfaces".
        /// Returns null if no Wi-Fi network is currently connected.
        /// </summary>
        public async Task<string?> GetCurrentWifiSsidAsync()
        {
            try
            {
                var psi = new ProcessStartInfo("netsh", "wlan show interfaces")
                {
                    UseShellExecute        = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow         = true
                };
                using var proc = Process.Start(psi)!;
                string output = await proc.StandardOutput.ReadToEndAsync();
                await proc.WaitForExitAsync();

                foreach (var line in output.Split('\n'))
                {
                    var t = line.Trim();
                    // Match lines like "    SSID                   : MyNetwork"
                    if (t.StartsWith("SSID", StringComparison.OrdinalIgnoreCase) &&
                        !t.StartsWith("BSSID", StringComparison.OrdinalIgnoreCase) &&
                        t.Contains(':'))
                    {
                        var parts = t.Split(':', 2);
                        if (parts.Length == 2)
                        {
                            var ssid = parts[1].Trim();
                            if (!string.IsNullOrEmpty(ssid)) return ssid;
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Reconnects to a previously saved Wi-Fi profile by name (SSID = profile name on most systems).
        /// </summary>
        private static async Task RestoreWifiConnectionAsync(string ssid)
        {
            try
            {
                // "netsh wlan connect name=..." connects to an existing saved profile
                var psi = new ProcessStartInfo("netsh", $"wlan connect name=\"{ssid}\"")
                {
                    UseShellExecute        = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow         = true
                };
                using var proc = Process.Start(psi)!;
                await proc.WaitForExitAsync();
            }
            catch { }
        }

        // ── Elevated process helper ───────────────────────────────────────────

        public async Task<(bool Success, string Message)> RunElevatedAsync(string fileName, string arguments)
        {
            // If already admin, run directly so we can capture the exit code
            if (IsRunningAsAdmin())
            {
                try
                {
                    var psi = new ProcessStartInfo(fileName, arguments)
                    {
                        UseShellExecute        = false,
                        CreateNoWindow         = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError  = true
                    };
                    using var proc = Process.Start(psi)!;
                    await proc.WaitForExitAsync();
                    return (proc.ExitCode == 0, "");
                }
                catch (Exception ex) { return (false, ex.Message); }
            }

            // Otherwise request elevation via UAC
            try
            {
                var psi = new ProcessStartInfo(fileName, arguments)
                {
                    UseShellExecute = true,
                    Verb            = "runas",
                    WindowStyle     = ProcessWindowStyle.Hidden
                };
                using var proc = Process.Start(psi)!;
                await proc.WaitForExitAsync();
                return (true, ""); // exit code unreliable for UAC-spawned processes
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        private static bool IsRunningAsAdmin()
        {
            try
            {
                using var id = WindowsIdentity.GetCurrent();
                return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

        // ── Misc helpers ──────────────────────────────────────────────────────

        private static string GetLocalIp()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(ua.Address))
                        return ua.Address.ToString();
            }
            return "127.0.0.1";
        }

        private static string DetectHotspotIp()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                var desc = ni.Name + ni.Description;
                if (desc.Contains("Virtual",     StringComparison.OrdinalIgnoreCase) ||
                    desc.Contains("Hosted",      StringComparison.OrdinalIgnoreCase) ||
                    desc.Contains("Wi-Fi Direct", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                            return addr.Address.ToString();
                }
            }
            return "192.168.137.1";
        }

        // ── System actions ────────────────────────────────────────────────────

        public Task<bool> RequestPermissionsAsync() => Task.FromResult(true);

        public void OpenUrl(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { }
        }

        public void OpenFile(string path)
        {
            try { Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true }); }
            catch { }
        }

        public void ShareFile(string path) => OpenFile(path);

        public void CopyToClipboard(string text)
        {
            // Handled in UI layer via Avalonia
        }

        public void ShowSystemToast(string title, string message, string? url = null)
        {
            string script =
                $"[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null; " +
                $"[Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null; " +
                $"$xml = [Windows.Data.Xml.Dom.XmlDocument]::new(); " +
                $"$template = '<toast><visual><binding template=\"ToastGeneric\"><text>{title}</text><text>{message}</text></binding></visual></toast>'; " +
                $"$xml.LoadXml($template); " +
                $"$toast = [Windows.UI.Notifications.ToastNotification]::new($xml); " +
                $"[Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier(\"We Share\").Show($toast);";

            _ = RunElevatedAsync("powershell.exe", $"-Command \"{script}\"");
        }
    }
}
