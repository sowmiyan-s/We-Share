using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WeShare.Core.Models;

namespace WeShare.Core.Discovery
{
    public class UdpDiscoveryService : IDisposable
    {
        private const int DiscoveryPort = 45678;

        private readonly DeviceModel _localDevice;
        private UdpClient? _listener;
        private CancellationTokenSource? _cts;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, DeviceModel> _trackedDevices = new();

        public event Action<DeviceModel>? DeviceDiscovered;
        public event Action<DeviceModel>? DeviceLost;

        public UdpDiscoveryService(DeviceModel localDevice)
        {
            _localDevice = localDevice;
        }

        // ── Listen ───────────────────────────────────────────────────────────
        public void StartListening()
        {
            _cts = new CancellationTokenSource();

            // Separate dedicated socket just for receiving
            _listener = new UdpClient();
            _listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _listener.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));
            _listener.EnableBroadcast = true;

            _ = Task.Run(() => ListenLoop(_cts.Token));
            _ = Task.Run(() => PruneLoop(_cts.Token));
        }

        public void StopListening()
        {
            _cts?.Cancel();
            try { _listener?.Close(); } catch { }
        }

        private async Task PruneLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(2000, token);
                    var now = DateTime.UtcNow;
                    foreach (var kvp in _trackedDevices)
                    {
                        if (now - kvp.Value.LastSeen > TimeSpan.FromSeconds(4.5))
                        {
                            if (_trackedDevices.TryRemove(kvp.Key, out var lost))
                            {
                                DeviceLost?.Invoke(lost);
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch { }
            }
        }

        private async Task ListenLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var result = await _listener!.ReceiveAsync(token);
                    _ = Task.Run(() => ProcessPacket(result), token);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Discovery] Listen error: {ex.Message}");
                    await Task.Delay(1000, token);
                }
            }
        }

        public void ProcessPacket(string json, string remoteIp)
        {
            try
            {
                var device = JsonSerializer.Deserialize<DeviceModel>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (device == null) return;
                if (device.Id == _localDevice.Id) return;

                device.IpAddress = remoteIp;
                device.LastSeen = DateTime.UtcNow;

                if (string.Equals(device.Role, "Idle", StringComparison.OrdinalIgnoreCase))
                {
                    _trackedDevices.TryRemove(device.Id, out _);
                    DeviceLost?.Invoke(device);
                    return;
                }

                _trackedDevices[device.Id] = device;
                Console.WriteLine($"[Discovery] Found: {device.Name} @ {device.IpAddress} (Role: {device.Role})");
                DeviceDiscovered?.Invoke(device);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Discovery] Parse error: {ex.Message}");
            }
        }

        private void ProcessPacket(UdpReceiveResult result)
        {
            try
            {
                // Ignore packets sent from this machine's own network interfaces or loopback
                if (IsOwnAddress(result.RemoteEndPoint.Address)) return;

                var json = Encoding.UTF8.GetString(result.Buffer);
                ProcessPacket(json, result.RemoteEndPoint.Address.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Discovery] Parse error: {ex.Message}");
            }
        }

        public async Task BroadcastRoleAsync(string newRole)
        {
            _localDevice.Role = newRole;
            await BroadcastPresenceAsync();
        }

        // ── Broadcast ────────────────────────────────────────────────────────
        public async Task BroadcastPresenceAsync()
        {
            try
            {
                // Fill in our current IP before broadcasting
                _localDevice.IpAddress = GetLocalIp();

                var json  = JsonSerializer.Serialize(_localDevice);
                var bytes = Encoding.UTF8.GetBytes(json);

                // Collect (localInterfaceIp, subnetBroadcastIp) pairs for all active adapters
                var adapterEndpoints = GetAdapterEndpoints();

                foreach (var (localIp, bcastIp) in adapterEndpoints)
                {
                    try
                    {
                        using var sender = new UdpClient(new IPEndPoint(localIp, 0));
                        sender.EnableBroadcast = true;
                        sender.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                        // Broadcast on adapter's calculated subnet broadcast
                        var ep = new IPEndPoint(bcastIp, DiscoveryPort);
                        await sender.SendAsync(bytes, bytes.Length, ep);

                        // Also broadcast on 255.255.255.255 from this specific adapter
                        var globalEp = new IPEndPoint(IPAddress.Broadcast, DiscoveryPort);
                        await sender.SendAsync(bytes, bytes.Length, globalEp);

                        // Desert Mode directed ping: If on 192.168.137.x, send direct unicast to peers
                        if (localIp.ToString().StartsWith("192.168.137."))
                        {
                            if (localIp.ToString() == "192.168.137.1")
                            {
                                // We are the hotspot host — probe first 15 client addresses
                                for (int host = 2; host <= 15; host++)
                                {
                                    var clientEp = new IPEndPoint(IPAddress.Parse($"192.168.137.{host}"), DiscoveryPort);
                                    await sender.SendAsync(bytes, bytes.Length, clientEp);
                                }
                            }
                            else
                            {
                                // We are a client connected to the hotspot — ping the gateway host directly
                                var hostEp = new IPEndPoint(IPAddress.Parse("192.168.137.1"), DiscoveryPort);
                                await sender.SendAsync(bytes, bytes.Length, hostEp);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Discovery] Broadcast from {localIp} error: {ex.Message}");
                    }
                }

                // Global fallback sender
                try
                {
                    using var globalSender = new UdpClient();
                    globalSender.EnableBroadcast = true;
                    globalSender.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    var bcastEp = new IPEndPoint(IPAddress.Broadcast, DiscoveryPort);
                    await globalSender.SendAsync(bytes, bytes.Length, bcastEp);
                }
                catch { }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Discovery] BroadcastPresenceAsync error: {ex.Message}");
            }
        }

        public static bool IsNoisyVirtual(NetworkInterface ni) =>
            ni.Description.Contains("VMware",      StringComparison.OrdinalIgnoreCase) ||
            ni.Description.Contains("Hyper-V",     StringComparison.OrdinalIgnoreCase) ||
            ni.Description.Contains("Host-Only",   StringComparison.OrdinalIgnoreCase) ||
            ni.Description.Contains("Pseudo",      StringComparison.OrdinalIgnoreCase) ||
            ni.Description.Contains("VPN",         StringComparison.OrdinalIgnoreCase) ||
            ni.Description.Contains("VirtualBox",  StringComparison.OrdinalIgnoreCase) ||
            ni.Description.Contains("WSL",         StringComparison.OrdinalIgnoreCase) ||
            ni.Description.Contains("Docker",      StringComparison.OrdinalIgnoreCase) ||
            ni.Description.Contains("TAP",         StringComparison.OrdinalIgnoreCase) ||
            ni.Name.Contains("vEthernet",          StringComparison.OrdinalIgnoreCase) ||
            ni.Name.Contains("Loopback",           StringComparison.OrdinalIgnoreCase);

        // ── Helpers ───────────────────────────────────────────────────────────
        /// <summary>Returns (localIp, subnetBroadcast) for all active IPv4 interfaces.</summary>
        private static List<(IPAddress LocalIp, IPAddress BroadcastIp)> GetAdapterEndpoints()
        {
            var result = new List<(IPAddress, IPAddress)>();
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                
                // Filter out noisy virtual switches (Hyper-V, WSL, Docker, VMware, VPNs) unless in Desert Mode
                bool isDesert = ni.GetIPProperties().UnicastAddresses
                    .Any(u => u.Address.ToString().StartsWith("192.168.137."));
                if (!isDesert && IsNoisyVirtual(ni)) continue;

                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                    if (!IsValidIpv4(ua.Address)) continue;

                    var ipBytes = ua.Address.GetAddressBytes();
                    var maskBytes = ua.IPv4Mask?.GetAddressBytes();
                    if (maskBytes == null)
                    {
                        result.Add((ua.Address, IPAddress.Broadcast));
                        continue;
                    }

                    var bcast = new byte[4];
                    for (int i = 0; i < 4; i++)
                        bcast[i] = (byte)(ipBytes[i] | ~maskBytes[i]);

                    result.Add((ua.Address, new IPAddress(bcast)));
                }
            }
            return result;
        }

        private static bool IsValidIpv4(IPAddress addr)
        {
            if (IPAddress.IsLoopback(addr)) return false;
            var str = addr.ToString();
            if (str.StartsWith("169.254.")) return false; // Exclude APIPA / Link-Local
            if (str == "0.0.0.0") return false;
            return true;
        }

        /// <summary>Returns true if the address belongs to this machine.</summary>
        public static bool IsOwnAddress(IPAddress address)
        {
            if (IPAddress.IsLoopback(address)) return true;
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                    if (ua.Address.Equals(address)) return true;
            }
            return false;
        }

        /// <summary>Returns true if the string IP address or localhost belongs to this machine.</summary>
        public static bool IsOwnAddress(string? ipString)
        {
            if (string.IsNullOrWhiteSpace(ipString)) return false;
            if (ipString.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return true;
            if (ipString.Equals("::1", StringComparison.OrdinalIgnoreCase)) return true;
            if (IPAddress.TryParse(ipString, out var parsed))
                return IsOwnAddress(parsed);
            return false;
        }

        /// <summary>Get the best local IPv4 address to include in our broadcast payload.</summary>
        public static string GetLocalIp()
        {
            // Pass 1 — Physical wireless or ethernet adapters with valid non-APIPA IP
            var physicalFirst = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                             ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                             !IsNoisyVirtual(ni))
                .OrderByDescending(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                .ThenByDescending(ni => ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet);

            foreach (var ni in physicalFirst)
            {
                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork && IsValidIpv4(ua.Address))
                        return ua.Address.ToString();
                }
            }

            // Pass 2 — Check for WeShare Desert Mode virtual hotspot adapter (192.168.137.1)
            var allUp = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                             ni.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            foreach (var ni in allUp)
            {
                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork && IsValidIpv4(ua.Address))
                    {
                        return ua.Address.ToString();
                    }
                }
            }

            return "127.0.0.1";
        }

        public void Dispose()
        {
            StopListening();
            _listener?.Dispose();
        }
    }
}
