using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WeShare.Core.Network
{
    /// <summary>
    /// Scans for and auto-connects to the "WeShare" hotspot using native wlanapi.dll.
    /// Requires zero elevation — works on all Windows 10/11 PCs via user-scope profiles.
    /// </summary>
    public class WifiConnectorService : IDisposable
    {
        // ── Constants ─────────────────────────────────────────────────────────
        private const string TargetSsid     = "WeShare";
        private const string TargetPassword = "weshare1";
        private const string ProfileName    = "WeShare";

        // ── Native API ────────────────────────────────────────────────────────
        private const uint WLAN_CLIENT_VERSION_VISTA  = 2;
        private const uint WLAN_AVAILABLE_NETWORK_INCLUDE_ALL_ADHOC_PROFILES = 0x00000001;

        [DllImport("wlanapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint WlanOpenHandle(
            uint dwClientVersion, IntPtr pReserved,
            out uint pdwNegotiatedVersion, out IntPtr phClientHandle);

        [DllImport("wlanapi.dll", SetLastError = true)]
        private static extern uint WlanCloseHandle(IntPtr hClientHandle, IntPtr pReserved);

        [DllImport("wlanapi.dll", SetLastError = true)]
        private static extern uint WlanEnumInterfaces(
            IntPtr hClientHandle, IntPtr pReserved,
            out IntPtr ppInterfaceList);

        [DllImport("wlanapi.dll", SetLastError = true)]
        private static extern uint WlanScan(
            IntPtr hClientHandle, ref Guid pInterfaceGuid,
            IntPtr pDot11Ssid, IntPtr pIeData, IntPtr pReserved);

        [DllImport("wlanapi.dll", SetLastError = true)]
        private static extern uint WlanGetAvailableNetworkList(
            IntPtr hClientHandle, ref Guid pInterfaceGuid,
            uint dwFlags, IntPtr pReserved, out IntPtr ppAvailableNetworkList);

        [DllImport("wlanapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint WlanSetProfile(
            IntPtr hClientHandle, ref Guid pInterfaceGuid,
            uint dwFlags, string strProfileXml, string? strAllUserProfileSecurity,
            bool bOverwrite, IntPtr pReserved, out uint pdwReasonCode);

        [DllImport("wlanapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint WlanConnect(
            IntPtr hClientHandle, ref Guid pInterfaceGuid,
            ref WLAN_CONNECTION_PARAMETERS pConnectionParameters, IntPtr pReserved);

        [DllImport("wlanapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint WlanDeleteProfile(
            IntPtr hClientHandle, ref Guid pInterfaceGuid,
            string strProfileName, IntPtr pReserved);

        [DllImport("wlanapi.dll")]
        private static extern void WlanFreeMemory(IntPtr pMemory);

        // ── Native Structs ────────────────────────────────────────────────────
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WLAN_INTERFACE_INFO
        {
            public Guid   InterfaceGuid;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string strInterfaceDescription;
            public int    isState;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WLAN_INTERFACE_INFO_LIST
        {
            public uint dwNumberOfItems;
            public uint dwIndex;
            // WLAN_INTERFACE_INFO items follow inline
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct WLAN_AVAILABLE_NETWORK
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string strProfileName;
            public DOT11_SSID dot11Ssid;
            public int dot11BssType;
            public uint uNumberOfBssids;
            public bool bNetworkConnectable;
            public uint wlanNotConnectableReason;
            public uint uNumberOfPhyTypes;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public uint[] dot11PhyTypes;
            public bool bMorePhyTypes;
            public uint wlanSignalQuality;
            public bool bSecurityEnabled;
            public int dot11DefaultAuthAlgorithm;
            public int dot11DefaultCipherAlgorithm;
            public uint dwFlags;
            public uint dwReserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DOT11_SSID
        {
            public uint uSSIDLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] ucSSID;

            public string GetSsidString()
            {
                if (ucSSID == null || uSSIDLength == 0) return string.Empty;
                return Encoding.UTF8.GetString(ucSSID, 0, (int)uSSIDLength);
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WLAN_CONNECTION_PARAMETERS
        {
            public int wlanConnectionMode;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string strProfile;
            public IntPtr pDot11Ssid;
            public IntPtr pDesiredBssidList;
            public int dot11BssType;
            public uint dwFlags;
        }

        // ── Fields ────────────────────────────────────────────────────────────
        private IntPtr _handle = IntPtr.Zero;
        private Guid   _interfaceGuid;
        private bool   _hasInterface;
        private bool   _disposed;

        // ── Constructor ───────────────────────────────────────────────────────
        public WifiConnectorService()
        {
            try
            {
                if (WlanOpenHandle(WLAN_CLIENT_VERSION_VISTA, IntPtr.Zero,
                                   out _, out _handle) != 0)
                {
                    _handle = IntPtr.Zero;
                    return;
                }
                _hasInterface = TryGetFirstInterface(out _interfaceGuid);
            }
            catch { /* wlanapi not available on this machine */ }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true if the "WeShare" SSID is visible in the scan results.
        /// Triggers a fresh scan and waits up to 6 seconds for results.
        /// </summary>
        public async Task<bool> IsWeShareHotspotVisibleAsync()
        {
            if (!_hasInterface || _handle == IntPtr.Zero) return false;

            try
            {
                // Trigger a new scan
                WlanScan(_handle, ref _interfaceGuid, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

                // Poll for the SSID to appear (up to 6 seconds)
                for (int attempt = 0; attempt < 12; attempt++)
                {
                    await Task.Delay(500);
                    if (SsidVisible(TargetSsid)) return true;
                }
                return false;
            }
            catch { return false; }
        }

        /// <summary>
        /// Creates a user-scope WPA2-PSK profile for "WeShare" and connects.
        /// No elevation required.
        /// </summary>
        public async Task<(bool Success, string Message)> AutoConnectToWeShareAsync()
        {
            if (!_hasInterface || _handle == IntPtr.Zero)
                return (false, "No Wi-Fi adapter found.");

            try
            {
                // 1. Write the WPA2-PSK profile XML
                string xml = BuildProfileXml(TargetSsid, TargetPassword);
                uint reasonCode;
                uint result = WlanSetProfile(_handle, ref _interfaceGuid,
                    0,   // 0 = current user scope (no elevation)
                    xml, null, true, IntPtr.Zero, out reasonCode);

                if (result != 0)
                    return (false, $"WlanSetProfile failed (error {result}, reason {reasonCode}).");

                // 2. Connect to the profile
                var cp = new WLAN_CONNECTION_PARAMETERS
                {
                    wlanConnectionMode = 0,   // wlan_connection_mode_profile
                    strProfile         = ProfileName,
                    pDot11Ssid         = IntPtr.Zero,
                    pDesiredBssidList  = IntPtr.Zero,
                    dot11BssType       = 1,   // dot11_BSS_type_infrastructure
                    dwFlags            = 0
                };
                result = WlanConnect(_handle, ref _interfaceGuid, ref cp, IntPtr.Zero);
                if (result != 0)
                    return (false, $"WlanConnect failed (error {result}).");

                // 3. Wait for IP assignment (up to 8 seconds)
                for (int i = 0; i < 16; i++)
                {
                    await Task.Delay(500);
                    var ip = GetLocalIp();
                    if (ip != "127.0.0.1") return (true, ip);
                }

                return (false, "Connected but no IP assigned in time.");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>Removes the WeShare profile on app shutdown.</summary>
        public void Cleanup()
        {
            if (!_hasInterface || _handle == IntPtr.Zero) return;
            try { WlanDeleteProfile(_handle, ref _interfaceGuid, ProfileName, IntPtr.Zero); }
            catch { }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private bool SsidVisible(string ssid)
        {
            if (WlanGetAvailableNetworkList(_handle, ref _interfaceGuid,
                WLAN_AVAILABLE_NETWORK_INCLUDE_ALL_ADHOC_PROFILES,
                IntPtr.Zero, out IntPtr pList) != 0) return false;

            try
            {
                int count = Marshal.ReadInt32(pList);   // dwNumberOfItems
                int offset = 8;                         // skip dwNumberOfItems + dwIndex
                int structSize = Marshal.SizeOf<WLAN_AVAILABLE_NETWORK>();

                for (int i = 0; i < count; i++)
                {
                    var net = Marshal.PtrToStructure<WLAN_AVAILABLE_NETWORK>(
                        IntPtr.Add(pList, offset + i * structSize));

                    if (net.dot11Ssid.GetSsidString()
                            .Equals(ssid, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                return false;
            }
            finally { WlanFreeMemory(pList); }
        }

        private bool TryGetFirstInterface(out Guid guid)
        {
            guid = Guid.Empty;
            if (WlanEnumInterfaces(_handle, IntPtr.Zero, out IntPtr pList) != 0) return false;

            try
            {
                uint count = (uint)Marshal.ReadInt32(pList);
                if (count == 0) return false;

                // Items start at offset 8 (dwNumberOfItems + dwIndex)
                IntPtr first = IntPtr.Add(pList, 8);
                var info = Marshal.PtrToStructure<WLAN_INTERFACE_INFO>(first);
                guid = info.InterfaceGuid;
                return true;
            }
            finally { WlanFreeMemory(pList); }
        }

        private static string BuildProfileXml(string ssid, string password) => $@"<?xml version=""1.0""?>
<WLANProfile xmlns=""http://www.microsoft.com/networking/WLAN/profile/v1"">
    <name>{ssid}</name>
    <SSIDConfig>
        <SSID>
            <name>{ssid}</name>
        </SSID>
    </SSIDConfig>
    <connectionType>ESS</connectionType>
    <connectionMode>auto</connectionMode>
    <MSM>
        <security>
            <authEncryption>
                <authentication>WPA2PSK</authentication>
                <encryption>AES</encryption>
                <useOneX>false</useOneX>
            </authEncryption>
            <sharedKey>
                <keyType>passPhrase</keyType>
                <protected>false</protected>
                <keyMaterial>{password}</keyMaterial>
            </sharedKey>
        </security>
    </MSM>
</WLANProfile>";

        // ── IDisposable ───────────────────────────────────────────────────────
        /// <summary>Inline IP helper — returns the best non-loopback IPv4 address.</summary>
        private static string GetLocalIp()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(addr.Address))
                        return addr.Address.ToString();
                }
            }
            return "127.0.0.1";
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_handle != IntPtr.Zero)
            {
                WlanCloseHandle(_handle, IntPtr.Zero);
                _handle = IntPtr.Zero;
            }
        }
    }
}
