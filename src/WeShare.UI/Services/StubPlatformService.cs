using System;
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
        public string GetDefaultSavePath() => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        public Task<(bool Success, string Message)> StartHotspotAsync(string ssid, string password) => 
            Task.FromResult((false, "Hotspot not supported on this platform."));

        public Task StopHotspotAsync() => Task.CompletedTask;

        public void StartBluetoothDiscovery(Action<DeviceModel> onDeviceFound) { }
        public void StopBluetoothDiscovery() { }
        public void StartBluetoothAdvertising(DeviceModel localDevice) { }
        public void StopBluetoothAdvertising() { }

        public Task<bool> ConnectToWifiAsync(string ssid, string password) => Task.FromResult(false);

        public Task<bool> RequestPermissionsAsync() => Task.FromResult(true);

        public void OpenFile(string path) { }
        public void OpenUrl(string url) { }
        public void ShareFile(string path) { }
        public void CopyToClipboard(string text) { }
    }
}
