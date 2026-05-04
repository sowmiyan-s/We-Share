using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace WeShare.Core.Network
{
    /// <summary>
    /// Creates a Windows Wi-Fi hosted network (hotspot) using netsh.
    /// Requires the PC to have a Wi-Fi adapter that supports hosted networks.
    /// </summary>
    public class HotspotService
    {
        public string Ssid { get; set; } = "WeShare-WiFi";
        public string Password { get; set; } = "weshare123";
        public bool IsRunning { get; private set; }

        /// <summary>IP of the hotspot adapter (usually 192.168.137.1 on Windows).</summary>
        public string HotspotIp { get; private set; } = "192.168.137.1";

        public async Task<(bool Success, string Message)> StartAsync()
        {
            if (Password.Length < 8)
                return (false, "Password must be at least 8 characters.");

            // Chain both commands in one elevated cmd window
            string script = $"netsh wlan set hostednetwork mode=allow ssid=\"{Ssid}\" key=\"{Password}\" & netsh wlan start hostednetwork";

            var (launched, launchErr) = await RunElevatedAsync("cmd.exe", $"/c \"{script}\"");
            if (!launched)
                return (false, $"UAC cancelled or error: {launchErr}");

            // Give Windows a moment to configure the adapter
            await Task.Delay(2000);

            // Verify with a non-elevated status check
            string status = await RunReadAsync("netsh", "wlan show hostednetwork");
            bool running = status.Contains("Started", StringComparison.OrdinalIgnoreCase);

            if (running)
            {
                IsRunning = true;
                HotspotIp = DetectHotspotIp();
                return (true, $"Hotspot started! IP: {HotspotIp}");
            }

            return (false, "Hotspot did not start. Check your Wi-Fi adapter supports hosted networks.");
        }

        public async Task<(bool Success, string Message)> StopAsync()
        {
            var (launched, err) = await RunElevatedAsync("cmd.exe", "/c \"netsh wlan stop hostednetwork\"");
            IsRunning = false;
            return (true, "Hotspot stopped.");
        }

        public Task<string> GetStatusAsync() => RunReadAsync("netsh", "wlan show hostednetwork");

        private static string DetectHotspotIp()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                var desc = ni.Name + ni.Description;
                if (desc.Contains("Virtual", StringComparison.OrdinalIgnoreCase) ||
                    desc.Contains("Hosted",  StringComparison.OrdinalIgnoreCase) ||
                    desc.Contains("Microsoft Wi-Fi Direct", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                            return addr.Address.ToString();
                }
            }
            return "192.168.137.1";
        }

        /// <summary>Run elevated via UAC (no output capture — required for runas).</summary>
        private static Task<(bool Launched, string Error)> RunElevatedAsync(string exe, string args)
        {
            try
            {
                var psi = new ProcessStartInfo(exe, args)
                {
                    UseShellExecute = true,
                    Verb            = "runas",          // triggers UAC prompt
                    WindowStyle     = ProcessWindowStyle.Hidden
                };
                using var proc = Process.Start(psi)!;
                proc.WaitForExit();
                return Task.FromResult<(bool, string)>((true, string.Empty));
            }
            catch (Exception ex)
            {
                // User clicked No on UAC, or no admin rights
                return Task.FromResult<(bool, string)>((false, ex.Message));
            }
        }

        /// <summary>Read-only netsh call — no elevation needed.</summary>
        private static async Task<string> RunReadAsync(string exe, string args)
        {
            try
            {
                var psi = new ProcessStartInfo(exe, args)
                {
                    RedirectStandardOutput = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true
                };
                using var proc = Process.Start(psi)!;
                string output = await proc.StandardOutput.ReadToEndAsync();
                await proc.WaitForExitAsync();
                return output;
            }
            catch (Exception ex) { return ex.Message; }
        }

    }
}
