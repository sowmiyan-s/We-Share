using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WeShare.Core.Models;

namespace WeShare.Core.Transfer
{
    public class CaptivePortalService
    {
        private TcpListener? _httpListener;
        private UdpClient? _dnsListener;
        private CancellationTokenSource? _cts;
        private readonly IPAddress _redirectIp;
        private readonly int _targetPort;

        public CaptivePortalService(IPAddress redirectIp, int targetPort = 8080)
        {
            _redirectIp = redirectIp;
            _targetPort = targetPort;
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            
            // Try starting DNS Server on Port 53 specifically on our redirect IP
            try
            {
                var dnsSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                dnsSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                dnsSocket.Bind(new IPEndPoint(_redirectIp, 53));

                _dnsListener = new UdpClient();
                _dnsListener.Client = dnsSocket;

                var token = _cts.Token;
                _ = Task.Run(() => DnsLoop(token), token);
                Console.WriteLine($"[CaptivePortal] DNS server started specifically on {_redirectIp}:53");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CaptivePortal] Failed to start DNS server on {_redirectIp}:53. Error: {ex.Message}");
            }

            // Try starting HTTP Server on Port 80 specifically on our redirect IP
            try
            {
                _httpListener = new TcpListener(_redirectIp, 80);
                _httpListener.Start();
                var token = _cts.Token;
                _ = Task.Run(() => HttpLoop(token), token);
                Console.WriteLine($"[CaptivePortal] HTTP redirect server started specifically on {_redirectIp}:80");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CaptivePortal] Failed to start HTTP redirect server on {_redirectIp}:80. Error: {ex.Message}");
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            try { _dnsListener?.Close(); } catch { }
            try { _httpListener?.Stop(); } catch { }
        }

        private async Task DnsLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var result = await _dnsListener!.ReceiveAsync(token);
                    byte[] query = result.Buffer;
                    string domainName = ParseDnsDomainName(query);
                    Console.WriteLine($"[CaptivePortal] Received DNS query for '{domainName}' from {result.RemoteEndPoint}");

                    byte[] response = BuildDnsResponse(query, _redirectIp);
                    await _dnsListener.SendAsync(response, response.Length, result.RemoteEndPoint);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CaptivePortal] DNS processing error: {ex.Message}");
                }
            }
        }

        private async Task HttpLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var client = await _httpListener!.AcceptTcpClientAsync(token);
                    _ = Task.Run(() => HandleHttpClient(client), token);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception) { }
            }
        }

        private async Task HandleHttpClient(TcpClient client)
        {
            using (client)
            {
                try
                {
                    using var stream = client.GetStream();
                    byte[] buffer = new byte[4096];
                    int read = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (read == 0) return;

                    string request = Encoding.ASCII.GetString(buffer, 0, read);
                    string host = "";
                    string path = "";
                    var lines = request.Split(new[] { "\r\n" }, StringSplitOptions.None);
                    if (lines.Length > 0)
                    {
                        var parts = lines[0].Split(' ');
                        if (parts.Length > 1) path = parts[1];
                    }
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("Host:", StringComparison.OrdinalIgnoreCase))
                        {
                            host = line.Substring(5).Trim();
                        }
                    }

                    bool isLocalHost = host.Contains(_redirectIp.ToString()) || 
                                       host.Contains("localhost") || 
                                       host.Contains("127.0.0.1");

                    if (isLocalHost)
                    {
                        Console.WriteLine($"[CaptivePortal] Proxying request from {client.Client.RemoteEndPoint} to http://{host}{path} -> 127.0.0.1:{_targetPort}");
                        
                        using var backend = new TcpClient();
                        await backend.ConnectAsync("127.0.0.1", _targetPort);
                        using var backendStream = backend.GetStream();

                        await backendStream.WriteAsync(buffer, 0, read);
                        await backendStream.FlushAsync();

                        var t1 = stream.CopyToAsync(backendStream);
                        var t2 = backendStream.CopyToAsync(stream);

                        await Task.WhenAny(t1, t2);
                    }
                    else
                    {
                        Console.WriteLine($"[CaptivePortal] Intercepted external HTTP request from {client.Client.RemoteEndPoint} to http://{host}{path} -> redirecting to http://{_redirectIp}/");
                        
                        string redirectUrl = $"http://{_redirectIp}/";
                        string response = "HTTP/1.1 302 Found\r\n" +
                                          $"Location: {redirectUrl}\r\n" +
                                          "Content-Length: 0\r\n" +
                                          "Connection: close\r\n" +
                                          "\r\n";
                        byte[] respBytes = Encoding.ASCII.GetBytes(response);
                        await stream.WriteAsync(respBytes, 0, respBytes.Length);
                        await stream.FlushAsync();
                    }
                }
                catch { }
            }
        }

        private static string ParseDnsDomainName(byte[] queryBytes)
        {
            if (queryBytes.Length < 12) return "";
            var sb = new StringBuilder();
            int pos = 12;
            while (pos < queryBytes.Length)
            {
                byte len = queryBytes[pos];
                pos++;
                if (len == 0) break;
                if (sb.Length > 0) sb.Append('.');
                for (int i = 0; i < len; i++)
                {
                    if (pos < queryBytes.Length)
                    {
                        sb.Append((char)queryBytes[pos]);
                        pos++;
                    }
                }
            }
            return sb.ToString();
        }

        private static byte[] BuildDnsResponse(byte[] queryBytes, IPAddress redirectIp)
        {
            if (queryBytes.Length < 12) return queryBytes;

            var response = new List<byte>();

            // Transaction ID
            response.Add(queryBytes[0]);
            response.Add(queryBytes[1]);

            // Flags: Response, Opcode 0, Authoritative, No Truncation, Recursion Desired/Available, No Error
            response.Add(0x81);
            response.Add(0x80);

            // Questions count
            response.Add(queryBytes[4]);
            response.Add(queryBytes[5]);

            // Answer RRs count (initially 0, we'll override at index 6-7 if Type A)
            response.Add(0x00);
            response.Add(0x00);

            // Authority RRs count = 0
            response.Add(0x00);
            response.Add(0x00);

            // Additional RRs count = 0
            response.Add(0x00);
            response.Add(0x00);

            // Copy Questions section
            int pos = 12;
            while (pos < queryBytes.Length)
            {
                byte len = queryBytes[pos];
                response.Add(len);
                pos++;
                if (len == 0) break;

                for (int i = 0; i < len; i++)
                {
                    if (pos < queryBytes.Length)
                    {
                        response.Add(queryBytes[pos]);
                        pos++;
                    }
                }
            }

            // Copy Type and Class from Query
            ushort qType = 0;
            if (pos + 4 <= queryBytes.Length)
            {
                qType = (ushort)((queryBytes[pos] << 8) | queryBytes[pos + 1]);
                response.Add(queryBytes[pos++]);
                response.Add(queryBytes[pos++]);
                response.Add(queryBytes[pos++]);
                response.Add(queryBytes[pos++]);
            }
            else
            {
                qType = 1; // Default to Type A
                response.Add(0x00); response.Add(0x01); // Type A
                response.Add(0x00); response.Add(0x01); // Class IN
            }

            // If query is for IPv4 Address (Type A), respond with our local gateway IP.
            // For AAAA (IPv6) or HTTPS queries, return 0 answers so clients fallback to A queries.
            if (qType == 1)
            {
                // Set Answer count to 1 (index 6 and 7 in response)
                response[6] = 0x00;
                response[7] = 0x01;

                // Answer Section
                response.Add(0xC0);
                response.Add(0x0C);

                // Type A
                response.Add(0x00);
                response.Add(0x01);

                // Class IN
                response.Add(0x00);
                response.Add(0x01);

                // TTL: 60 seconds
                response.Add(0x00);
                response.Add(0x00);
                response.Add(0x00);
                response.Add(0x3C);

                // Data length: 4 bytes
                response.Add(0x00);
                response.Add(0x04);

                // IP Address
                byte[] ipBytes = redirectIp.GetAddressBytes();
                response.AddRange(ipBytes);
            }

            return response.ToArray();
        }
    }
}
