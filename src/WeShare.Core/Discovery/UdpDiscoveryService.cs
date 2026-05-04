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

        public event Action<DeviceModel>? DeviceDiscovered;

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
        }

        public void StopListening()
        {
            _cts?.Cancel();
            try { _listener?.Close(); } catch { }
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

        private void ProcessPacket(UdpReceiveResult result)
        {
            try
            {
                var json   = Encoding.UTF8.GetString(result.Buffer);
                var device = JsonSerializer.Deserialize<DeviceModel>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (device == null) return;

                // Ignore packets from ourselves (match by Id OR by source IP being our own)
                if (device.Id == _localDevice.Id) return;
                if (IsOwnAddress(result.RemoteEndPoint.Address)) return;

                // Always set IP from the packet's real source address
                device.IpAddress = result.RemoteEndPoint.Address.ToString();

                Console.WriteLine($"[Discovery] Found: {device.Name} @ {device.IpAddress}");
                DeviceDiscovered?.Invoke(device);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Discovery] Parse error: {ex.Message}");
            }
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

                // Broadcast on ALL active network adapters' subnets for max reach
                var broadcasts = GetBroadcastAddresses();
                broadcasts.Add(IPAddress.Broadcast); // also try 255.255.255.255

                using var sender = new UdpClient();
                sender.EnableBroadcast = true;
                sender.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                foreach (var bcast in broadcasts)
                {
                    try
                    {
                        var ep = new IPEndPoint(bcast, DiscoveryPort);
                        await sender.SendAsync(bytes, bytes.Length, ep);
                        Console.WriteLine($"[Discovery] Broadcast → {bcast}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Discovery] Broadcast failed to {bcast}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Discovery] BroadcastPresenceAsync error: {ex.Message}");
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        /// <summary>Get subnet broadcast addresses for all active IPv4 adapters.</summary>
        private static List<IPAddress> GetBroadcastAddresses()
        {
            var result = new List<IPAddress>();
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                    if (IPAddress.IsLoopback(ua.Address)) continue;

                    // subnet broadcast = ip | (~mask)
                    var ipBytes   = ua.Address.GetAddressBytes();
                    var maskBytes = ua.IPv4Mask?.GetAddressBytes();
                    if (maskBytes == null) continue;

                    var bcast = new byte[4];
                    for (int i = 0; i < 4; i++)
                        bcast[i] = (byte)(ipBytes[i] | ~maskBytes[i]);

                    result.Add(new IPAddress(bcast));
                }
            }
            return result;
        }

        /// <summary>Returns true if the address belongs to this machine.</summary>
        private static bool IsOwnAddress(IPAddress address)
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

        /// <summary>Get the best local IPv4 address to include in our broadcast payload.</summary>
        public static string GetLocalIp()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(ua.Address))
                        return ua.Address.ToString();
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
