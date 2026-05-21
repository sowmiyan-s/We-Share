using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

#if WINDOWS
using Windows.Networking.NetworkOperators;
#endif

namespace WeShare.Core.Network
{
    /// <summary>
    /// Creates a Windows Mobile Hotspot using the WinRT NetworkOperatorTetheringManager.
    /// This is the same engine as Settings → Network → Mobile Hotspot.
    /// Requires zero elevation, works on all Windows 10/11 PCs without adapter errors.
    /// </summary>
    public class HotspotService
    {
        public const string TargetSsid     = "WeShare";
        public const string TargetPassword = "weshare1";   // ≥8 chars

        public bool   IsRunning  { get; private set; }
        public string HotspotIp  { get; private set; } = "192.168.137.1";

        // ── Capability check ─────────────────────────────────────────────────

        /// <summary>Returns true if this device supports the Mobile Hotspot feature.</summary>
        public Task<bool> IsSupportedAsync()
        {
#if WINDOWS
            try
            {
                var mgr = NetworkOperatorTetheringManager.CreateFromConnectionProfile(
                    Windows.Networking.Connectivity.NetworkInformation
                           .GetInternetConnectionProfile()
                    ?? GetAnyConnectionProfile());

                return Task.FromResult(mgr != null);
            }
            catch
            {
                return Task.FromResult(false);
            }
#else
            return Task.FromResult(false);
#endif
        }

        // ── Start ─────────────────────────────────────────────────────────────

        public async Task<(bool Success, string Message)> StartAsync()
        {
#if WINDOWS
            try
            {
                var mgr = GetTetheringManager();
                if (mgr == null)
                    return (false, "Hotspot not supported on this device.");

                // Already running?
                if (mgr.TetheringOperationalState ==
                    TetheringOperationalState.On)
                {
                    IsRunning = true;
                    HotspotIp = DetectHotspotIp();
                    return (true, HotspotIp);
                }

                // Configure SSID + password
                var cfg = mgr.GetCurrentAccessPointConfiguration();
                if (cfg.Ssid != TargetSsid || cfg.Passphrase != TargetPassword)
                {
                    var newCfg = new NetworkOperatorTetheringAccessPointConfiguration
                    {
                        Ssid       = TargetSsid,
                        Passphrase = TargetPassword
                    };
                    await mgr.ConfigureAccessPointAsync(newCfg);
                }

                // Start the hotspot
                var result = await mgr.StartTetheringAsync();
                if (result.Status == TetheringOperationStatus.Success ||
                    result.Status == TetheringOperationStatus.Unknown)
                {
                    IsRunning = true;

                    // Wait for the adapter to get an IP (up to 6 seconds)
                    for (int i = 0; i < 12; i++)
                    {
                        await Task.Delay(500);
                        string ip = DetectHotspotIp();
                        if (ip != "192.168.137.1" || i >= 4)
                        {
                            HotspotIp = ip;
                            return (true, ip);
                        }
                    }

                    HotspotIp = "192.168.137.1";
                    return (true, HotspotIp);
                }

                return (false, $"Hotspot start failed: {result.Status}");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
#else
            await Task.CompletedTask;
            return (false, "Windows-only feature.");
#endif
        }

        // ── Stop ─────────────────────────────────────────────────────────────

        public async Task<(bool Success, string Message)> StopAsync()
        {
#if WINDOWS
            try
            {
                var mgr = GetTetheringManager();
                if (mgr != null)
                    await mgr.StopTetheringAsync();
                IsRunning = false;
                return (true, "Hotspot stopped.");
            }
            catch (Exception ex)
            {
                IsRunning = false;
                return (false, ex.Message);
            }
#else
            await Task.CompletedTask;
            IsRunning = false;
            return (true, "Hotspot stopped.");
#endif
        }

        // ── Helpers ──────────────────────────────────────────────────────────

#if WINDOWS
        private static NetworkOperatorTetheringManager? GetTetheringManager()
        {
            try
            {
                var profile = Windows.Networking.Connectivity.NetworkInformation
                                     .GetInternetConnectionProfile()
                              ?? GetAnyConnectionProfile();
                if (profile == null) return null;
                return NetworkOperatorTetheringManager
                       .CreateFromConnectionProfile(profile);
            }
            catch { return null; }
        }

        private static Windows.Networking.Connectivity.ConnectionProfile? GetAnyConnectionProfile()
        {
            var profiles = Windows.Networking.Connectivity.NetworkInformation
                                  .GetConnectionProfiles();
            foreach (var p in profiles)
                if (p != null) return p;
            return null;
        }
#endif

        /// <summary>Detects the IP address of the hotspot adapter (Microsoft Hosted Network).</summary>
        private static string DetectHotspotIp()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                var name = ni.Name + " " + ni.Description;
                if (name.Contains("Local Area Connection* ", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Microsoft Wi-Fi Direct", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Hosted Network",         StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Virtual",                StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                            !System.Net.IPAddress.IsLoopback(addr.Address))
                            return addr.Address.ToString();
                    }
                }
            }
            return "192.168.137.1";
        }
    }
}
