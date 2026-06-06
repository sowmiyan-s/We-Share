using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace WeShare.Core.Network
{
    public class NetworkAdapterInfo
    {
        public string Name        { get; set; } = "";
        public string Description { get; set; } = "";
        public string IpAddress   { get; set; } = "";
        public string Type        { get; set; } = "";   // WiFi, Ethernet, USB, Loopback
    }

    public static class NetworkHelper
    {
        /// <summary>Returns all active IPv4 adapters with their addresses.</summary>
        public static List<NetworkAdapterInfo> GetActiveAdapters()
        {
            var result = new List<NetworkAdapterInfo>();

            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                    if (IPAddress.IsLoopback(addr.Address)) continue;

                    result.Add(new NetworkAdapterInfo
                    {
                        Name        = ni.Name,
                        Description = ni.Description,
                        IpAddress   = addr.Address.ToString(),
                        Type        = DetectType(ni)
                    });
                }
            }

            return result;
        }

        private static string DetectType(NetworkInterface ni)
        {
            var desc = ni.Description.ToLower() + ni.Name.ToLower();
 
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                desc.Contains("wi-fi") || desc.Contains("wifi") || desc.Contains("wireless") || desc.Contains("wlan"))
                return "Wi-Fi";
 
            if (desc.Contains("usb") || desc.Contains("rndis") || desc.Contains("tether") || desc.Contains("android"))
                return "USB Tethering";
 
            if (ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                desc.Contains("ethernet") || desc.Contains("local area"))
                return "Ethernet";
 
            if (desc.Contains("virtual") || desc.Contains("hyper-v") || desc.Contains("vmware"))
                return "Virtual";
 
            return "Network";
        }
    }
}
