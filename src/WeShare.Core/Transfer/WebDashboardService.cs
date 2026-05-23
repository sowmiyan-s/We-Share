using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WeShare.Core.Models;

namespace WeShare.Core.Transfer
{
    /// <summary>
    /// Lightweight raw-TCP HTTP server â€” no HttpListener, no admin rights needed.
    /// Exposes:
    ///   GET  /             â†’ Mobile dashboard HTML
    ///   GET  /api/devices  â†’ JSON list of discovered peers
    ///   GET  /api/me       â†’ JSON { name, ip }
    ///   POST /upload       â†’ Stream a file; Header: X-File-Name
    /// </summary>
    public partial class WebDashboardService
    {
        private readonly string _saveDirectory;
        private readonly DeviceModel _localDevice;
        private Func<IReadOnlyList<DeviceModel>>? _getPeers;
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;
        private readonly List<SharedFile> _sharedFiles = new();
        private readonly SemaphoreSlim _filesLock = new(1, 1);
        private readonly List<StreamWriter> _eventClients = new();
        private readonly SemaphoreSlim _clientsLock = new(1, 1);
        public int Port { get; } = 8080;
        
        public event Action<string, string, long>? WebFileReceived;
        public event Action<string, string>? WebClientConnected;

        public class SharedFile
        {
            public string Id { get; set; } = Guid.NewGuid().ToString("n");
            public string Name { get; set; } = "";
            public string Path { get; set; } = "";
            public long Size { get; set; }
        }

        public WebDashboardService(string saveDirectory, DeviceModel localDevice)
        {
            _saveDirectory = saveDirectory;
            _localDevice = localDevice;
        }

        /// <summary>Supply a live snapshot of discovered devices.</summary>
        public void SetPeersProvider(Func<IReadOnlyList<DeviceModel>> provider) => _getPeers = provider;

        public void ShareForWeb(string filePath)
        {
            var info = new FileInfo(filePath);
            if (!info.Exists) return;
            _filesLock.Wait();
            try
            {
                if (_sharedFiles.Any(f => f.Path == filePath)) return;
                _sharedFiles.Add(new SharedFile { Name = info.Name, Path = filePath, Size = info.Length });
                NotifyClients("refresh");
            }
            finally { _filesLock.Release(); }
        }

        private async void NotifyClients(string type)
        {
            await _clientsLock.WaitAsync();
            try
            {
                var data = $"data: {type}\n\n";
                var bytes = Encoding.UTF8.GetBytes(data);
                
                var deadClients = new List<StreamWriter>();
                foreach (var client in _eventClients)
                {
                    try
                    {
                        await client.WriteAsync(data);
                        await client.FlushAsync();
                    }
                    catch { deadClients.Add(client); }
                }
                foreach (var dc in deadClients) _eventClients.Remove(dc);
            }
            finally { _clientsLock.Release(); }
        }

        public void ClearSharedFiles() 
        { 
            _filesLock.Wait();
            try { _sharedFiles.Clear(); }
            finally { _filesLock.Release(); }
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            try
            {
                _listener = new TcpListener(IPAddress.Any, Port);
                _listener.Start();
                Console.WriteLine($"[WebDashboard] Listening on all interfaces at port {Port}");
                _ = Task.Run(() => AcceptLoop(_cts.Token));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebDashboard] Failed to start: {ex.Message}");
            }
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();
        }

        // â”€â”€ Accept loop â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private async Task AcceptLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener!.AcceptTcpClientAsync(token);
                    _ = Task.Run(() => HandleClient(client), token);
                }
                catch { break; }
            }
        }

        // ── Per-connection handler ──────────────────────────────────────────────────

        private async Task HandleClient(TcpClient client)
        {
            using (client)
            {
                try
                {
                    using var stream = new BufferedStream(client.GetStream(), 65536);
                    
                    // --- Parse Request Headers (Avoid StreamReader buffering issues) ---
                    byte[] headerBuffer = new byte[8192];
                    int totalHeaderRead = 0;
                    int headerEndIndex = -1;

                    while (totalHeaderRead < headerBuffer.Length)
                    {
                        int n = await stream.ReadAsync(headerBuffer.AsMemory(totalHeaderRead, 1));
                        if (n == 0) return;
                        totalHeaderRead++;

                        if (totalHeaderRead >= 4 &&
                            headerBuffer[totalHeaderRead - 4] == '\r' && headerBuffer[totalHeaderRead - 3] == '\n' &&
                            headerBuffer[totalHeaderRead - 2] == '\r' && headerBuffer[totalHeaderRead - 1] == '\n')
                        {
                            headerEndIndex = totalHeaderRead;
                            break;
                        }
                    }

                    if (headerEndIndex == -1) return;

                    string headerText = Encoding.ASCII.GetString(headerBuffer, 0, headerEndIndex);
                    var headerLines = headerText.Split(new[] { "\r\n" }, StringSplitOptions.None);
                    if (headerLines.Length == 0) return;

                    var requestLine = headerLines[0];
                    var parts = requestLine.Split(' ');
                    if (parts.Length < 2) return;

                    string method = parts[0].ToUpperInvariant();
                    string path   = parts[1].Split('?')[0];

                    var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 1; i < headerLines.Length; i++)
                    {
                        var line = headerLines[i];
                        if (string.IsNullOrEmpty(line)) continue;
                        int colon = line.IndexOf(':');
                        if (colon > 0)
                            headers[line[..colon].Trim()] = line[(colon + 1)..].Trim();
                    }

                    // --- CORS + route dispatch ---
                    if (method == "GET" && path == "/")
                        await SendResponse(stream, 200, "text/html; charset=utf-8", GetDashboardHtml());

                    else if (method == "GET" && path == "/api/me")
                    {
                        string remoteIp = client.Client.RemoteEndPoint is System.Net.IPEndPoint rep ? rep.Address.ToString() : "unknown";
                        WebClientConnected?.Invoke("Web Portal", remoteIp);
                        await SendJson(stream, new { name = _localDevice.Name, ip = _localDevice.IpAddress });
                    }

                    else if (method == "GET" && path == "/api/devices")
                    {
                        var peers = _getPeers?.Invoke() ?? Array.Empty<DeviceModel>();
                        await SendJson(stream, peers);
                    }

                    else if (method == "GET" && path == "/api/files")
                    {
                        await _filesLock.WaitAsync();
                        try { await SendJson(stream, _sharedFiles); }
                        finally { _filesLock.Release(); }
                    }

                    else if (method == "GET" && path == "/api/events")
                    {
                        var header = "HTTP/1.1 200 OK\r\n" +
                                     "Content-Type: text/event-stream\r\n" +
                                     "Cache-Control: no-cache\r\n" +
                                     "Connection: keep-alive\r\n" +
                                     "Access-Control-Allow-Origin: *\r\n" +
                                     "\r\n";
                        await stream.WriteAsync(Encoding.ASCII.GetBytes(header));
                        await stream.FlushAsync();
                        
                        var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                        await _clientsLock.WaitAsync();
                        try { _eventClients.Add(writer); }
                        finally { _clientsLock.Release(); }

                        try
                        {
                            while (client.Connected)
                            {
                                await writer.WriteAsync(": keepalive\n\n");
                                await Task.Delay(20000);
                            }
                        }
                        catch { }
                        finally
                        {
                            await _clientsLock.WaitAsync();
                            try { _eventClients.Remove(writer); }
                            finally { _clientsLock.Release(); }
                        }
                        return;
                    }

                    else if (method == "GET" && path == "/download")
                    {
                        var query = parts[1].Contains('?') ? parts[1].Split('?')[1] : "";
                        // Split on '=' with max of 2 parts so an empty value (?id=) never throws IndexOutOfRangeException
                        var idPart  = query.Split('&').FirstOrDefault(p => p.StartsWith("id="));
                        var idSplit = idPart?.Split('=', 2);
                        var id      = idSplit?.Length == 2 ? idSplit[1] : null;
                        
                        SharedFile? file = null;
                        await _filesLock.WaitAsync();
                        try { file = _sharedFiles.FirstOrDefault(f => f.Id == id); }
                        finally { _filesLock.Release(); }

                        if (file != null && File.Exists(file.Path))
                        {
                            var info = new FileInfo(file.Path);
                            var header = $"HTTP/1.1 200 OK\r\n" +
                                         $"Content-Type: application/octet-stream\r\n" +
                                         $"Content-Disposition: attachment; filename=\"{Uri.EscapeDataString(file.Name)}\"\r\n" +
                                         $"Content-Length: {info.Length}\r\n" +
                                         "Connection: close\r\n" +
                                         "\r\n";
                            await stream.WriteAsync(Encoding.ASCII.GetBytes(header));
                            using var fs = new FileStream(file.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
                            await fs.CopyToAsync(stream);
                            await stream.FlushAsync();
                        }
                        else
                            await SendResponse(stream, 404, "text/plain", "File Not Found");
                    }

                    else if (method == "POST" && path == "/upload")
                    {
                        string rawName = headers.GetValueOrDefault("X-File-Name", $"upload_{DateTime.Now.Ticks}.dat");
                        string filename = Path.GetFileName(Uri.UnescapeDataString(rawName)); 
                        long contentLength = long.TryParse(headers.GetValueOrDefault("Content-Length", "0"), out var cl) ? cl : 0;

                        Directory.CreateDirectory(_saveDirectory);
                        string dest = GetUniqueFilePath(_saveDirectory, filename);

                        using var fs = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None, 65536, true);
                        byte[] buf = new byte[65536];
                        long written = 0;
                        while (written < contentLength)
                        {
                            int toRead = (int)Math.Min(buf.Length, contentLength - written);
                            int n = await stream.ReadAsync(buf.AsMemory(0, toRead));
                            if (n == 0) break;
                            await fs.WriteAsync(buf.AsMemory(0, n));
                            written += n;
                        }

                        if (written > 0)
                            WebFileReceived?.Invoke("Mobile Web", dest, written);

                        await SendJson(stream, new { success = true, saved = Path.GetFileName(dest), bytes = written });
                    }
                    else
                        await SendResponse(stream, 404, "text/plain", "Not Found");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WebDashboard] Handler error: {ex.Message}");
                }
            }
        }

        // â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private static async Task SendResponse(Stream stream, int status, string contentType, string body)
        {
            var statusText = status switch
            {
                200 => "OK",
                201 => "Created",
                204 => "No Content",
                400 => "Bad Request",
                401 => "Unauthorized",
                403 => "Forbidden",
                404 => "Not Found",
                405 => "Method Not Allowed",
                500 => "Internal Server Error",
                _   => "OK"
            };
            var bodyBytes = Encoding.UTF8.GetBytes(body);
            var header = $"HTTP/1.1 {status} {statusText}\r\n" +
                         $"Content-Type: {contentType}\r\n" +
                         $"Content-Length: {bodyBytes.Length}\r\n" +
                         "Connection: close\r\n" +
                         "Access-Control-Allow-Origin: *\r\n" +
                         "\r\n";
            await stream.WriteAsync(Encoding.ASCII.GetBytes(header));
            await stream.WriteAsync(bodyBytes);
            await stream.FlushAsync();
        }

        private static Task SendJson(Stream stream, object obj)
            => SendResponse(stream, 200, "application/json",
                            JsonSerializer.Serialize(obj, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));

        private static string GetUniqueFilePath(string dir, string filename)
        {
            string dest = Path.Combine(dir, filename);
            if (!File.Exists(dest)) return dest;
            string name = Path.GetFileNameWithoutExtension(filename);
            string ext  = Path.GetExtension(filename);
            int i = 1;
            while (File.Exists(dest))
                dest = Path.Combine(dir, $"{name} ({i++}){ext}");
            return dest;
        }
    }
}