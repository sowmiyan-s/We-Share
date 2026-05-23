using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Principal;
using System.Threading.Tasks;

#if WINDOWS
using Windows.Networking.NetworkOperators;
#endif

namespace WeShare.Core.Network
{
    /// <summary>
    /// Creates a Wi-Fi hotspot using a multi-strategy approach:
    ///   1. WinRT NetworkOperatorTetheringManager (requires internet connection profile)
    ///   2. netsh wlan hostednetwork — Desert Mode (works with NO internet, no router needed)
    /// Automatically picks the right strategy based on what's available.
    /// </summary>
    public class HotspotService
    {
        public const string TargetSsid     = "WeShare";
        public const string TargetPassword = "weshare1"; // ≥8 chars

        public bool   IsRunning { get; private set; }
        public string HotspotIp { get; private set; } = "192.168.137.1";

        private enum HotspotMode { None, WinRT, Netsh }
        private HotspotMode _activeMode = HotspotMode.None;

        // ── Capability check ─────────────────────────────────────────────────

        /// <summary>
        /// Always returns true on Windows — we can always use netsh hostednetwork as fallback.
        /// </summary>
        public Task<bool> IsSupportedAsync()
        {
#if WINDOWS
            return Task.FromResult(true);
#else
            return Task.FromResult(false);
#endif
        }

        // ── Start ─────────────────────────────────────────────────────────────

        public async Task<(bool Success, string Message)> StartAsync()
        {
#if WINDOWS
            // ── Strategy 1: WinRT Mobile Hotspot ──────────────────────────────
            // Works when an internet connection profile exists.
            try
            {
                var mgr = GetTetheringManager();
                if (mgr != null)
                {
                    if (mgr.TetheringOperationalState == TetheringOperationalState.On)
                    {
                        IsRunning   = true;
                        _activeMode = HotspotMode.WinRT;
                        HotspotIp   = DetectHotspotIp();
                        return (true, HotspotIp);
                    }

                    var cfg = mgr.GetCurrentAccessPointConfiguration();
                    if (cfg.Ssid != TargetSsid || cfg.Passphrase != TargetPassword)
                    {
                        await mgr.ConfigureAccessPointAsync(
                            new NetworkOperatorTetheringAccessPointConfiguration
                            {
                                Ssid       = TargetSsid,
                                Passphrase = TargetPassword
                            });
                    }

                    var result = await mgr.StartTetheringAsync();
                    if (result.Status == TetheringOperationStatus.Success ||
                        result.Status == TetheringOperationStatus.Unknown)
                    {
                        _activeMode = HotspotMode.WinRT;
                        IsRunning   = true;

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

                    // WinRT returned a non-success status — fall through to netsh
                }
            }
            catch
            {
                // WinRT unavailable or failed — fall through to Desert Mode
            }

            // ── Strategy 2: Desert Mode — netsh hostednetwork ────────────────
            // Works WITHOUT any internet connection. Creates a standalone Wi-Fi AP.
            return await StartNetshHotspotAsync();
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
                if (_activeMode == HotspotMode.WinRT)
                {
                    var mgr = GetTetheringManager();
                    if (mgr != null)
                        await mgr.StopTetheringAsync();
                }
                else if (_activeMode == HotspotMode.Netsh)
                {
                    await RunNetshAsync("wlan stop hostednetwork");
                    await RunNetshAsync("wlan set hostednetwork mode=disallow");
                }
            }
            catch { }

            IsRunning   = false;
            _activeMode = HotspotMode.None;
            return (true, "Hotspot stopped.");
#else
            await Task.CompletedTask;
            IsRunning   = false;
            _activeMode = HotspotMode.None;
            return (true, "Hotspot stopped.");
#endif
        }

        // ── Private helpers ───────────────────────────────────────────────────

#if WINDOWS
        private static NetworkOperatorTetheringManager? GetTetheringManager()
        {
            try
            {
                // Prefer internet profile
                var internet = Windows.Networking.Connectivity.NetworkInformation
                                      .GetInternetConnectionProfile();
                if (internet != null)
                    return NetworkOperatorTetheringManager.CreateFromConnectionProfile(internet);

                // No internet? Try any available connection profile (e.g. Ethernet without gateway)
                foreach (var p in Windows.Networking.Connectivity.NetworkInformation
                                         .GetConnectionProfiles())
                {
                    if (p == null) continue;
                    try
                    {
                        return NetworkOperatorTetheringManager.CreateFromConnectionProfile(p);
                    }
                    catch { /* try next */ }
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Desert Mode hotspot using netsh wlan hostednetwork.
        /// Creates a real Wi-Fi AP at 192.168.137.1 with NO internet required.
        /// The adapter runs ICS internally so connected clients get DHCP leases.
        /// </summary>
        private async Task<(bool Success, string Message)> StartNetshHotspotAsync()
        {
            bool ok = await RunNetshElevatedAsync(
                $"wlan set hostednetwork mode=allow ssid=\"{TargetSsid}\" key=\"{TargetPassword}\"");

            if (!ok)
                return (false,
                    "Desert Mode setup failed. Ensure your Wi-Fi adapter supports Virtual AP " +
                    "and try running WeShare as Administrator.");

            bool started = await RunNetshElevatedAsync("wlan start hostednetwork");
            if (!started)
                return (false,
                    "Could not start the hosted network. Your Wi-Fi adapter may not support " +
                    "Virtual AP (common on USB Wi-Fi dongles). Try a different adapter.");

            _activeMode = HotspotMode.Netsh;
            IsRunning   = true;

            // Wait up to 5 seconds for the virtual adapter to receive its IP
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(500);
                string ip = DetectHotspotIp();
                if (ip != "192.168.137.1")
                {
                    HotspotIp = ip;
                    return (true, $"Desert Mode hotspot active. IP: {ip}");
                }
            }

            HotspotIp = "192.168.137.1";
            return (true, $"Desert Mode hotspot active. IP: {HotspotIp}");
        }

        private static async Task<bool> RunNetshAsync(string args)
        {
            try
            {
                var psi = new ProcessStartInfo("netsh", args)
                {
                    UseShellExecute        = false,
                    CreateNoWindow         = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true
                };
                using var proc = Process.Start(psi)!;
                await proc.WaitForExitAsync();
                return proc.ExitCode == 0;
            }
            catch { return false; }
        }

        private static async Task<bool> RunNetshElevatedAsync(string args)
        {
            // If the process is already elevated, skip the UAC prompt
            if (IsRunningAsAdmin())
                return await RunNetshAsync(args);

            try
            {
                var psi = new ProcessStartInfo("netsh", args)
                {
                    UseShellExecute = true,
                    Verb            = "runas",
                    WindowStyle     = ProcessWindowStyle.Hidden
                };
                using var proc = Process.Start(psi)!;
                await proc.WaitForExitAsync();
                return true; // UAC-launched process — exit code not reliably captured
            }
            catch { return false; }
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
#endif

        private static string DetectHotspotIp()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                var label = ni.Name + " " + ni.Description;
                if (label.Contains("Local Area Connection*",  StringComparison.OrdinalIgnoreCase) ||
                    label.Contains("Microsoft Wi-Fi Direct",  StringComparison.OrdinalIgnoreCase) ||
                    label.Contains("Hosted Network",          StringComparison.OrdinalIgnoreCase) ||
                    label.Contains("Virtual",                 StringComparison.OrdinalIgnoreCase))
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
