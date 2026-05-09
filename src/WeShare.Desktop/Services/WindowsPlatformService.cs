using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices.WindowsRuntime;
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

        private BluetoothLEAdvertisementWatcher? _watcher;
        private BluetoothLEAdvertisementPublisher? _publisher;
        private const ushort ManufacturerId = 0x4747; // "GG" for WeShare

        public string GetDeviceType() => "PC";

        public string GetDefaultSavePath() => 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "WeShare");

        public async Task<(bool Success, string Message)> StartHotspotAsync(string ssid, string password)
        {
            try
            {
                var connectionProfile = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();
                if (connectionProfile == null) return (false, "No active network connection found to share.");

                var manager = Windows.Networking.NetworkOperators.NetworkOperatorTetheringManager.CreateFromConnectionProfile(connectionProfile);
                
                // Configure SSID and Password
                var configuration = new Windows.Networking.NetworkOperators.NetworkOperatorTetheringAccessPointConfiguration();
                configuration.Ssid = ssid;
                configuration.Passphrase = password;
                await manager.ConfigureAccessPointAsync(configuration);

                var result = await manager.StartTetheringAsync();
                if (result.Status == Windows.Networking.NetworkOperators.TetheringOperationStatus.Success)
                {
                    IsHotspotRunning = true;
                    await Task.Delay(2000);
                    HotspotIp = DetectHotspotIp();
                    return (true, "Mobile Hotspot started successfully.");
                }
                else
                {
                    return (false, $"Hotspot failed: {result.Status}");
                }
            }
            catch (Exception ex)
            {
                // Fallback to netsh if WinRT fails (for older versions or specific configs)
                string script = $"netsh wlan set hostednetwork mode=allow ssid=\"{ssid}\" key=\"{password}\" & netsh wlan start hostednetwork";
                var (launched, err) = await RunElevatedAsync("cmd.exe", $"/c \"{script}\"");
                if (!launched) return (false, $"Error: {ex.Message}");
                
                IsHotspotRunning = true;
                return (true, "Started via legacy netsh (Compatibility Mode).");
            }
        }

        public async Task StopHotspotAsync()
        {
            try
            {
                var connectionProfile = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();
                if (connectionProfile != null)
                {
                    var manager = Windows.Networking.NetworkOperators.NetworkOperatorTetheringManager.CreateFromConnectionProfile(connectionProfile);
                    await manager.StopTetheringAsync();
                }
            }
            catch { }
            
            await RunElevatedAsync("cmd.exe", "/c \"netsh wlan stop hostednetwork\"");
            IsHotspotRunning = false;
        }

        public void StartBluetoothDiscovery(Action<DeviceModel> onDeviceFound)
        {
            try
            {
                StopBluetoothDiscovery();
                _watcher = new BluetoothLEAdvertisementWatcher();
                _watcher.ScanningMode = BluetoothLEScanningMode.Active;
                
                _watcher.Received += (s, args) =>
                {
                    string? name = null;
                    string? ip = null;
                    string? ssid = null;
                    string? pwd = null;

                    foreach (var mData in args.Advertisement.ManufacturerData)
                    {
                        if (mData.CompanyId == ManufacturerId)
                        {
                            try
                            {
                                var reader = DataReader.FromBuffer(mData.Data);
                                // Format: [Type(1)][IP(4)][Port(2)][HotspotCode(2)][NameLen(1)][Name...]
                                byte dataType = reader.ReadByte();
                                if (dataType == 0x01) // Unified format
                                {
                                    byte[] ipBytes = new byte[4];
                                    reader.ReadBytes(ipBytes);
                                    ip = new IPAddress(ipBytes).ToString();
                                    ushort port = reader.ReadUInt16();
                                    ushort hotspotCode = reader.ReadUInt16();
                                    
                                    if (hotspotCode > 0)
                                    {
                                        // Generate the standard hotspot details based on the code/flag
                                        // Or in this case we're just assuming the standard SSID/pwd
                                        ssid = "WeShare-WiFi";
                                        pwd = "weshare123";
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
                            Id = args.BluetoothAddress.ToString(),
                            Name = name ?? "Unknown Device",
                            IpAddress = ip,
                            Type = "Nearby Device",
                            LastSeen = DateTime.Now,
                            Port = 45679,
                            Ssid = ssid,
                            Password = pwd
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
                
                // Do NOT set LocalName, it takes up valuable bytes in the 31-byte payload limit
                // _publisher.Advertisement.LocalName = localDevice.Name;

                var writer = new DataWriter();
                writer.WriteByte(0x01); // Type: Unified
                
                // IP Address (4 bytes)
                string ipStr = GetLocalIp();
                if (!IPAddress.TryParse(ipStr, out var ipAddr)) ipAddr = IPAddress.Loopback;
                writer.WriteBytes(ipAddr.GetAddressBytes());
                
                // Port (2 bytes)
                writer.WriteUInt16((ushort)localDevice.Port);
                
                // Hotspot Code (2 bytes) - Assuming "WeShare-XXXX" format
                ushort hotspotCode = 0;
                if (IsHotspotRunning && HotspotIp != null)
                {
                    // For now, let's just use a flag to indicate it's running
                    hotspotCode = 9999; 
                }
                writer.WriteUInt16(hotspotCode);
                
                // Device Name (up to 12 bytes)
                byte[] nameBytes = Encoding.UTF8.GetBytes(localDevice.Name);
                byte nameLen = (byte)Math.Min(nameBytes.Length, 12);
                writer.WriteByte(nameLen);
                writer.WriteBytes(nameBytes[..nameLen]);

                _publisher.Advertisement.ManufacturerData.Add(new BluetoothLEManufacturerData(ManufacturerId, writer.DetachBuffer()));
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

                var network = System.Linq.Enumerable.FirstOrDefault(adapter.NetworkReport.AvailableNetworks, n => n.Ssid == ssid);
                if (network == null) return false;

                var credential = new Windows.Security.Credentials.PasswordCredential();
                if (!string.IsNullOrEmpty(password)) credential.Password = password;

                var result = await adapter.ConnectAsync(network, Windows.Devices.WiFi.WiFiReconnectionKind.Automatic, credential);
                return result.ConnectionStatus == Windows.Devices.WiFi.WiFiConnectionStatus.Success;
            }
            catch { return false; }
        }

        private static string GetLocalIp()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ua.Address))
                        return ua.Address.ToString();
            }
            return "127.0.0.1";
        }

        // ── Other ────────────────────────────────────────────────────────────
        public Task<bool> RequestPermissionsAsync() => Task.FromResult(true);

        public void OpenFile(string path)
        {
            try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); } catch { }
        }

        public void OpenUrl(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
        }

        public void CopyToClipboard(string text) { }

        public void ShareFile(string path)
        {
            try { Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true }); } catch { }
        }

        private static string DetectHotspotIp()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                var desc = ni.Name + ni.Description;
                if (desc.Contains("Virtual") || desc.Contains("Hosted") || desc.Contains("Wi-Fi Direct"))
                {
                    foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                            return addr.Address.ToString();
                }
            }
            return "192.168.137.1";
        }

        private static Task<(bool Launched, string Error)> RunElevatedAsync(string exe, string args)
        {
            try
            {
                var psi = new ProcessStartInfo(exe, args) { UseShellExecute = true, Verb = "runas", WindowStyle = ProcessWindowStyle.Hidden };
                using var proc = Process.Start(psi)!;
                proc.WaitForExit();
                return Task.FromResult((true, ""));
            }
            catch (Exception ex) { return Task.FromResult((false, ex.Message)); }
        }
    }
}
