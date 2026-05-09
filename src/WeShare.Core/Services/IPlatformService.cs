using System;
using System.Threading.Tasks;
using WeShare.Core.Models;

namespace WeShare.Core.Services
{
    public interface IPlatformService
    {
        string GetDeviceType(); // "PC", "Phone", "Tablet"
        string GetDefaultSavePath();
        
        // Hotspot Management
        Task<(bool Success, string Message)> StartHotspotAsync(string ssid, string password);
        Task StopHotspotAsync();
        bool IsHotspotRunning { get; }
        string HotspotIp { get; }

        // Bluetooth Discovery
        void StartBluetoothDiscovery(Action<DeviceModel> onDeviceFound);
        void StopBluetoothDiscovery();
        void StartBluetoothAdvertising(DeviceModel localDevice);
        void StopBluetoothAdvertising();

        // Wi-Fi Management
        Task<bool> ConnectToWifiAsync(string ssid, string password);

        // Permissions (Mainly for Mobile)
        Task<bool> RequestPermissionsAsync();
        
        // System Actions
        void OpenFile(string path);
        void OpenUrl(string url);
        void ShareFile(string path);
        void CopyToClipboard(string text);
    }
}
