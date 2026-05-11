using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WeShare.Core.Discovery
{
    /// <summary>
    /// A very minimal mDNS responder for 'weshare.local'.
    /// Allows mobile devices to find the web portal without typing an IP.
    /// </summary>
    public class MdnsService : IDisposable
    {
        private const int MdnsPort = 5353;
        private static readonly IPAddress MdnsGroup = IPAddress.Parse("224.0.0.251");

        private UdpClient? _udp;
        private CancellationTokenSource? _cts;
        private string _hostName = "weshare.local";
        private int _port = 8080;

        public void Start(string name = "weshare", int port = 8080)
        {
            _hostName = name.EndsWith(".local") ? name : name + ".local";
            _port = port;
            try
            {
                _cts = new CancellationTokenSource();
                _udp = new UdpClient();
                
                _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _udp.Client.Bind(new IPEndPoint(IPAddress.Any, MdnsPort));
                _udp.JoinMulticastGroup(MdnsGroup);

                _ = Task.Run(() => ListenLoop(_cts.Token));
                Console.WriteLine($"[mDNS] Started responder for {_hostName} on port {_port}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[mDNS] Failed to start: {ex.Message}");
            }
        }

        private async Task ListenLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var result = await _udp!.ReceiveAsync(token);
                    ProcessPacket(result);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Console.WriteLine($"[mDNS] Loop error: {ex.Message}");
                    await Task.Delay(2000, token);
                }
            }
        }

        private void ProcessPacket(UdpReceiveResult result)
        {
            // Minimal DNS parsing
            // Check if it's a query for weshare.local
            var data = result.Buffer;
            if (data.Length < 12) return;

            // Header: [ID][Flags][Questions][Answers][Authr][Addit]
            // Flags: bit 15 is 0 for query
            ushort flags = (ushort)((data[2] << 8) | data[3]);
            if ((flags & 0x8000) != 0) return; // Ignore responses

            int pos = 12;
            // Parse Question
            string qName = ParseName(data, ref pos);
            if (qName != _hostName) return;

            // Respond!
            SendResponse(result.RemoteEndPoint);
        }

        private string ParseName(byte[] data, ref int pos)
        {
            var sb = new StringBuilder();
            while (pos < data.Length)
            {
                int len = data[pos++];
                if (len == 0) break;
                if (sb.Length > 0) sb.Append(".");
                sb.Append(Encoding.UTF8.GetString(data, pos, len));
                pos += len;
            }
            return sb.ToString();
        }

        private void SendResponse(IPEndPoint target)
        {
            var localIp = UdpDiscoveryService.GetLocalIp();
            if (localIp == "127.0.0.1") return;

            var ipAddr = IPAddress.Parse(localIp);
            var ipBytes = ipAddr.GetAddressBytes();

            // Minimal DNS Answer Packet
            var packet = new List<byte>();
            
            // Header
            packet.AddRange(new byte[] { 0x00, 0x00 }); // ID
            packet.AddRange(new byte[] { 0x84, 0x00 }); // Flags (Response, Authoritative)
            packet.AddRange(new byte[] { 0x00, 0x00 }); // 0 Questions
            packet.AddRange(new byte[] { 0x00, 0x01 }); // 1 Answer
            packet.AddRange(new byte[] { 0x00, 0x00 }); // 0 Authority
            packet.AddRange(new byte[] { 0x00, 0x00 }); // 0 Additional

            // Answer Section
            // Name: weshare.local
            packet.Add(7); packet.AddRange(Encoding.UTF8.GetBytes("weshare"));
            packet.Add(5); packet.AddRange(Encoding.UTF8.GetBytes("local"));
            packet.Add(0);

            packet.AddRange(new byte[] { 0x00, 0x01 }); // Type A
            packet.AddRange(new byte[] { 0x00, 0x01 }); // Class IN
            packet.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x78 }); // TTL 120s
            packet.AddRange(new byte[] { 0x00, 0x04 }); // Data length 4
            packet.AddRange(ipBytes);

            var bytes = packet.ToArray();
            _udp?.SendAsync(bytes, bytes.Length, new IPEndPoint(MdnsGroup, MdnsPort));
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _udp?.Dispose();
        }
    }
}
