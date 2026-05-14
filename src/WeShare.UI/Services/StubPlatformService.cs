using System;
using System.IO;
using System.Threading.Tasks;
using WeShare.Core.Models;
using WeShare.Core.Services;

namespace WeShare.UI.Services
{
    public class StubPlatformService : IPlatformService
    {
        public bool IsHotspotRunning => false;
        public string HotspotIp => "127.0.0.1";

        public string GetDeviceType() => "PC";
        public string GetDefaultSavePath()
        {
            var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            var weShare = Path.Combine(downloads, "We Share");
            if (!Directory.Exists(weShare)) Directory.CreateDirectory(weShare);
            return weShare;
        }

        public Task<(bool Success, string Message)> StartHotspotAsync(string ssid, string password) => 
            Task.FromResult((false, "Hotspot not supported on this platform."));

        public Task StopHotspotAsync() => Task.CompletedTask;

        public Task<(bool Success, string Message)> RunElevatedAsync(string fileName, string arguments) => Task.FromResult((true, "Stub Success"));

        public void StartBluetoothDiscovery(Action<DeviceModel> onDeviceFound) { }
        public void StopBluetoothDiscovery() { }
        public void StartBluetoothAdvertising(DeviceModel localDevice) { }
        public void StopBluetoothAdvertising() { }

        public Task<bool> ConnectToWifiAsync(string ssid, string password) => Task.FromResult(false);

        public Task<bool> RequestPermissionsAsync() => Task.FromResult(true);

        public void OpenFile(string path)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        public void OpenUrl(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        public void ShareFile(string path) { }
        public void CopyToClipboard(string text) { }
        public void ShowSystemToast(string title, string message, string? url = null) { }
    }
}
