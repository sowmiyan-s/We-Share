using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using WeShare.Core.Services;

namespace WeShare.UI.Services
{
    public class WindowsPlatformService : IPlatformService
    {
        public bool IsHotspotRunning { get; private set; }
        public string HotspotIp { get; private set; } = "192.168.137.1";

        public string GetDeviceType() => "PC";

        public string GetDefaultSavePath() => 
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "WeShare");

        public async Task<(bool Success, string Message)> StartHotspotAsync(string ssid, string password)
        {
            if (password.Length < 8) return (false, "Password must be at least 8 chars.");

            string script = $"netsh wlan set hostednetwork mode=allow ssid=\"{ssid}\" key=\"{password}\" & netsh wlan start hostednetwork";
            var (launched, err) = await RunElevatedAsync("cmd.exe", $"/c \"{script}\"");
            
            if (!launched) return (false, $"UAC error: {err}");

            await Task.Delay(2000);
            HotspotIp = DetectHotspotIp();
            IsHotspotRunning = true;
            return (true, "Hotspot started.");
        }

        public async Task StopHotspotAsync()
        {
            await RunElevatedAsync("cmd.exe", "/c \"netsh wlan stop hostednetwork\"");
            IsHotspotRunning = false;
        }

        public Task<bool> RequestPermissionsAsync() => Task.FromResult(true);

        public void OpenFile(string path)
        {
            try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); } catch { }
        }

        public void OpenUrl(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
        }

        public void CopyToClipboard(string text)
        {
            // Avalonia handles clipboard via TopLevel
        }

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
