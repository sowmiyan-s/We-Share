using System;
using System.Threading.Tasks;

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

        // Permissions (Mainly for Mobile)
        Task<bool> RequestPermissionsAsync();
        
        // System Actions
        void OpenFile(string path);
        void CopyToClipboard(string text);
    }
}
