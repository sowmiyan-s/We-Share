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
using WeShare.Core.Services;
using WeShare.Core.Discovery;
using System.Linq;

namespace WeShare.Core.Transfer
{
    /// <summary>
    /// Lightweight raw-TCP HTTP server — no HttpListener, no admin rights needed.
    /// </summary>
    public partial class WebDashboardService
    {
        private readonly string _saveDirectory;
        private readonly DeviceModel _localDevice;
        private Func<IReadOnlyList<DeviceModel>>? _getPeers;
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;
        
        private readonly List<SharedFile> _sharedFiles = new();
        private readonly Dictionary<string, List<SharedFile>> _clientSpecificFiles = new(StringComparer.OrdinalIgnoreCase);
        private readonly SemaphoreSlim _filesLock = new(1, 1);
        
        private readonly Dictionary<string, WebClientInfo> _activeWebClients = new(StringComparer.OrdinalIgnoreCase);
        private readonly SemaphoreSlim _webClientsLock = new(1, 1);
        
        public int Port { get; private set; } = 8080;
        public event Action<int>? PortChanged;
        public byte[]? LogoBytes { get; set; }
        public byte[]? SendIconBytes { get; set; }
        public byte[]? ReceiveIconBytes { get; set; }
        
        public event Action<string, string>? WebClientConnected;
        public event Action<WebClientInfo>? WebClientConnectedEx;
        public event Action<string>? WebClientDisconnectedEx;
        public event Action<string, string, string, long>? WebFileShared;
        public event Action<FileTransferState>? WebTransferStarted;
        public event Action<FileTransferState>? WebTransferProgress;
        public event Action<FileTransferState>? WebTransferCompleted;
        public event Action<FileTransferState>? WebTransferFailed;
        public event Action<string>? WebClientHeartbeat;
        public event Action<string, string, string>? WebOfferDeclined;
        public Func<FileTransferState, Task<bool>>? WebFileSharedCallback { get; set; }
        public Func<string, bool>? IsSessionActiveFilter { get; set; }
        public Func<string, string, bool>? IsSessionActiveFilterEx { get; set; }
        private Func<IReadOnlyList<FileTransferState>>? _getHistory;
        public void SetHistoryProvider(Func<IReadOnlyList<FileTransferState>> provider) => _getHistory = provider;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, FileTransferState> _approvedUploads = new();

        public class WebClientInfo
        {
            public string ClientId { get; set; } = "";
            public string Name { get; set; } = "";
            public string IpAddress { get; set; } = "";
            public StreamWriter? EventWriter { get; set; }
        }

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
            }
            finally { _filesLock.Release(); }
            NotifyAllClients("refresh");
        }

        public bool IsWebClientConnected(string clientId)
        {
            _webClientsLock.Wait();
            try
            {
                return _activeWebClients.TryGetValue(clientId, out var info) && info.EventWriter != null;
            }
            finally { _webClientsLock.Release(); }
        }

        public bool ShareForWebClient(string clientId, string filePath)
        {
            if (!IsWebClientConnected(clientId)) return false;
            var info = new FileInfo(filePath);
            if (!info.Exists) return false;
            SharedFile sharedFile;
            _filesLock.Wait();
            try
            {
                if (!_clientSpecificFiles.TryGetValue(clientId, out var list))
                {
                    list = new List<SharedFile>();
                    _clientSpecificFiles[clientId] = list;
                }
                if (list.Any(f => f.Path == filePath)) return true;
                sharedFile = new SharedFile { Name = info.Name, Path = filePath, Size = info.Length };
                list.Add(sharedFile);
            }
            finally { _filesLock.Release(); }
            
            // Notify client about the specific offer
            NotifyClient(clientId, $"offer:{{\"id\":\"{sharedFile.Id}\",\"name\":\"{Uri.EscapeDataString(sharedFile.Name)}\",\"size\":{sharedFile.Size}}}");
            return true;
        }

        public bool ShareMultipleForWebClient(string clientId, List<string> filePaths)
        {
            if (!IsWebClientConnected(clientId)) return false;
            var sharedFiles = new List<SharedFile>();
            _filesLock.Wait();
            try
            {
                if (!_clientSpecificFiles.TryGetValue(clientId, out var list))
                {
                    list = new List<SharedFile>();
                    _clientSpecificFiles[clientId] = list;
                }
                foreach (var filePath in filePaths)
                {
                    var fInfo = new FileInfo(filePath);
                    if (!fInfo.Exists) continue;
                    if (list.Any(f => f.Path == filePath)) continue;
                    var sf = new SharedFile { Name = fInfo.Name, Path = filePath, Size = fInfo.Length };
                    list.Add(sf);
                    sharedFiles.Add(sf);
                }
            }
            finally { _filesLock.Release(); }

            if (sharedFiles.Count == 0) return true;

            // Build a single batch-offer SSE event
            var filesJson = string.Join(",", sharedFiles.Select(f =>
                $"{{\"id\":\"{f.Id}\",\"name\":\"{Uri.EscapeDataString(f.Name)}\",\"size\":{f.Size}}}" ));
            NotifyClient(clientId, $"batch-offer:[{filesJson}]");
            return true;
        }

        private async void NotifyAllClients(string type)
        {
            await _webClientsLock.WaitAsync();
            try
            {
                var data = $"data: {type}\n\n";
                foreach (var kvp in _activeWebClients)
                {
                    if (kvp.Value.EventWriter != null)
                    {
                        try
                        {
                            await kvp.Value.EventWriter.WriteAsync(data);
                            await kvp.Value.EventWriter.FlushAsync();
                        }
                        catch { kvp.Value.EventWriter = null; }
                    }
                }
            }
            finally { _webClientsLock.Release(); }
        }

        private async void NotifyClient(string clientId, string type)
        {
            await _webClientsLock.WaitAsync();
            try
            {
                if (_activeWebClients.TryGetValue(clientId, out var clientInfo) && clientInfo.EventWriter != null)
                {
                    try
                    {
                        await clientInfo.EventWriter.WriteAsync($"data: {type}\n\n");
                        await clientInfo.EventWriter.FlushAsync();
                    }
                    catch
                    {
                        clientInfo.EventWriter = null;
                    }
                }
            }
            finally { _webClientsLock.Release(); }
        }

        public void ClearSharedFiles() 
        { 
            _filesLock.Wait();
            try 
            { 
                _sharedFiles.Clear(); 
            }
            finally { _filesLock.Release(); }
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            const int startPort = 8080;
            const int maxPort = 8099;

            for (int p = startPort; p <= maxPort; p++)
            {
                try
                {
                    _listener = new TcpListener(IPAddress.Any, p);
                    _listener.Start();
                    if (Port != p)
                    {
                        Port = p;
                        PortChanged?.Invoke(Port);
                    }
                    Console.WriteLine($"[WebDashboard] Successfully listening on all interfaces at port {Port}");
                    _ = Task.Run(() => AcceptLoop(_cts.Token));
                    return;
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse || ex.ErrorCode == 10048)
                {
                    Console.WriteLine($"[WebDashboard] Port {p} in use, trying next port...");
                    try { _listener?.Stop(); } catch { }
                    _listener = null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WebDashboard] Failed to bind to port {p}: {ex.Message}");
                    try { _listener?.Stop(); } catch { }
                    _listener = null;
                    break;
                }
            }

            Console.WriteLine($"[WebDashboard] CRITICAL: Could not find an open port between {startPort} and {maxPort}.");
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();
        }

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

        private async Task HandleClient(TcpClient client)
        {
            using (client)
            {
                try
                {
                    using var stream = new BufferedStream(client.GetStream(), 65536);
                    
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

                    // --- Parse Query Parameters ---
                    var queryParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    string queryString = parts[1].Contains('?') ? parts[1].Split('?')[1] : "";
                    if (!string.IsNullOrEmpty(queryString))
                    {
                        foreach (var pair in queryString.Split('&'))
                        {
                            var kv = pair.Split('=', 2);
                            if (kv.Length == 2)
                            {
                                string k = Uri.UnescapeDataString(kv[0]);
                                string v = Uri.UnescapeDataString(kv[1]);
                                queryParams[k] = v;
                            }
                        }
                    }

                    // --- CORS + route dispatch ---
                    if (method == "OPTIONS")
                    {
                        var header = "HTTP/1.1 204 No Content\r\n" +
                                     "Access-Control-Allow-Origin: *\r\n" +
                                     "Access-Control-Allow-Methods: GET, POST, OPTIONS\r\n" +
                                     "Access-Control-Allow-Headers: *\r\n" +
                                     "Connection: close\r\n\r\n";
                        await stream.WriteAsync(Encoding.ASCII.GetBytes(header));
                        await stream.FlushAsync();
                        return;
                    }

                    if (method == "GET" && path == "/")
                    {
                        var bodyBytes = Encoding.UTF8.GetBytes(GetDashboardHtml());
                        var header = "HTTP/1.1 200 OK\r\n" +
                                     "Content-Type: text/html; charset=utf-8\r\n" +
                                     $"Content-Length: {bodyBytes.Length}\r\n" +
                                     "Cache-Control: no-store, no-cache, must-revalidate, max-age=0\r\n" +
                                     "Pragma: no-cache\r\n" +
                                     "Connection: close\r\n" +
                                     "Access-Control-Allow-Origin: *\r\n" +
                                     "\r\n";
                        await stream.WriteAsync(Encoding.ASCII.GetBytes(header));
                        await stream.WriteAsync(bodyBytes);
                        await stream.FlushAsync();
                    }

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

                    else if (path == "/api/portal-login")
                    {
                        string remoteIp = client.Client.RemoteEndPoint is System.Net.IPEndPoint rep ? rep.Address.ToString() : "unknown";
                        CaptivePortalService.AuthorizeIp(remoteIp);
                        string? cId = queryParams.GetValueOrDefault("clientId");
                        if (!string.IsNullOrEmpty(cId))
                        {
                            WebClientHeartbeat?.Invoke(cId);
                        }
                        await SendJson(stream, new { success = true, ip = remoteIp, authenticated = true });
                    }

                    else if (path == "/api/heartbeat")
                    {
                        string? cId = queryParams.GetValueOrDefault("clientId");
                        if (!string.IsNullOrEmpty(cId))
                        {
                            WebClientHeartbeat?.Invoke(cId);
                        }
                        await SendJson(stream, new { success = true });
                    }

                    else if (method == "GET" && path == "/api/history")
                    {
                        var history = _getHistory?.Invoke() ?? Array.Empty<FileTransferState>();
                        var list = history.Take(25).Select(h => new
                        {
                            id = h.FileId,
                            name = h.FileName,
                            size = h.TotalBytes,
                            sizeDisplay = h.FileSizeDisplay,
                            direction = h.Direction.ToString(),
                            peer = h.PeerName,
                            status = h.Status.ToString(),
                            timestamp = h.Timestamp.ToString("yyyy-MM-dd HH:mm")
                        });
                        await SendJson(stream, list);
                    }

                    else if (method == "GET" && path == "/api/qr")
                    {
                        var ip = UdpDiscoveryService.GetLocalIp();
                        var url = $"http://{ip}:{Port}";
                        var qrBytes = QrCodeService.GenerateQrCodePng(url);
                        
                        var header = $"HTTP/1.1 200 OK\r\n" +
                                     $"Content-Type: image/png\r\n" +
                                     $"Content-Length: {qrBytes.Length}\r\n" +
                                     "Connection: close\r\n" +
                                     "Access-Control-Allow-Origin: *\r\n" +
                                     "\r\n";
                        await stream.WriteAsync(Encoding.ASCII.GetBytes(header));
                        await stream.WriteAsync(qrBytes);
                        await stream.FlushAsync();
                    }

                    else if (method == "GET" && path == "/api/logo")
                    {
                        if (LogoBytes != null)
                        {
                            var header = $"HTTP/1.1 200 OK\r\n" +
                                         $"Content-Type: image/png\r\n" +
                                         $"Content-Length: {LogoBytes.Length}\r\n" +
                                         "Connection: close\r\n" +
                                         "Access-Control-Allow-Origin: *\r\n" +
                                         "\r\n";
                            await stream.WriteAsync(Encoding.ASCII.GetBytes(header));
                            await stream.WriteAsync(LogoBytes);
                            await stream.FlushAsync();
                        }
                        else
                        {
                            var header = "HTTP/1.1 404 Not Found\r\nConnection: close\r\n\r\n";
                            await stream.WriteAsync(Encoding.ASCII.GetBytes(header));
                            await stream.FlushAsync();
                        }
                    }

                    else if (method == "GET" && (path == "/api/assets/send.png" || path == "/assets/send.png"))
                    {
                        if (SendIconBytes != null)
                        {
                            var header = $"HTTP/1.1 200 OK\r\n" +
                                         $"Content-Type: image/png\r\n" +
                                         $"Content-Length: {SendIconBytes.Length}\r\n" +
                                         "Connection: close\r\n" +
                                         "Access-Control-Allow-Origin: *\r\n" +
                                         "\r\n";
                            await stream.WriteAsync(Encoding.ASCII.GetBytes(header));
                            await stream.WriteAsync(SendIconBytes);
                            await stream.FlushAsync();
                        }
                        else
                        {
                            var header = "HTTP/1.1 404 Not Found\r\nConnection: close\r\n\r\n";
                            await stream.WriteAsync(Encoding.ASCII.GetBytes(header));
                            await stream.FlushAsync();
                        }
                    }

                    else if (method == "GET" && (path == "/api/assets/receive.png" || path == "/assets/receive.png"))
                    {
                        if (ReceiveIconBytes != null)
                        {
                            var header = $"HTTP/1.1 200 OK\r\n" +
                                         $"Content-Type: image/png\r\n" +
                                         $"Content-Length: {ReceiveIconBytes.Length}\r\n" +
                                         "Connection: close\r\n" +
                                         "Access-Control-Allow-Origin: *\r\n" +
                                         "\r\n";
                            await stream.WriteAsync(Encoding.ASCII.GetBytes(header));
                            await stream.WriteAsync(ReceiveIconBytes);
                            await stream.FlushAsync();
                        }
                        else
                        {
                            var header = "HTTP/1.1 404 Not Found\r\nConnection: close\r\n\r\n";
                            await stream.WriteAsync(Encoding.ASCII.GetBytes(header));
                            await stream.FlushAsync();
                        }
                    }

                    else if (method == "POST" && path == "/api/decline")
                    {
                        string? clientId = queryParams.GetValueOrDefault("clientId");
                        string? fileId = queryParams.GetValueOrDefault("id");
                        string clientName = "Web Client";
                        string fileName = "";

                        if (!string.IsNullOrEmpty(clientId))
                        {
                            await _webClientsLock.WaitAsync();
                            try
                            {
                                if (_activeWebClients.TryGetValue(clientId, out var clientInfo))
                                    clientName = clientInfo.Name;
                            }
                            finally { _webClientsLock.Release(); }

                            if (!string.IsNullOrEmpty(fileId))
                            {
                                await _filesLock.WaitAsync();
                                try
                                {
                                    if (_clientSpecificFiles.TryGetValue(clientId, out var list))
                                    {
                                        var item = list.FirstOrDefault(f => string.Equals(f.Id, fileId, StringComparison.OrdinalIgnoreCase));
                                        if (item != null)
                                        {
                                            fileName = item.Name;
                                            list.Remove(item);
                                        }
                                    }
                                }
                                finally { _filesLock.Release(); }
                            }
                            WebOfferDeclined?.Invoke(clientId, string.IsNullOrEmpty(fileName) ? (fileId ?? "") : fileName, clientName);
                        }
                        await SendResponse(stream, 200, "text/plain", "OK");
                    }

                    else if (method == "POST" && path == "/api/ask-receive")
                    {
                        string? clientId = queryParams.GetValueOrDefault("clientId");
                        string? targetId = queryParams.GetValueOrDefault("targetId");
                        string? name = queryParams.GetValueOrDefault("name");
                        string? sizeStr = queryParams.GetValueOrDefault("size");
                        long size = long.TryParse(sizeStr, out var s) ? s : 0;
                        string remoteIp = client.Client.RemoteEndPoint is System.Net.IPEndPoint rep ? rep.Address.ToString() : "unknown";

                        if (!string.IsNullOrEmpty(clientId))
                        {
                            WebClientHeartbeat?.Invoke(clientId);
                        }


                        string uploaderName = "Mobile Web";
                        if (!string.IsNullOrEmpty(clientId))
                        {
                            await _webClientsLock.WaitAsync();
                            try
                            {
                                if (_activeWebClients.TryGetValue(clientId, out var clientInfo))
                                {
                                    uploaderName = clientInfo.Name;
                                }
                            }
                            finally { _webClientsLock.Release(); }
                        }

                        string filename = Path.GetFileName(Uri.UnescapeDataString(name ?? "upload.dat"));
                        string webSharedDir = Path.Combine(_saveDirectory, "web_shared");
                        Directory.CreateDirectory(webSharedDir);
                        string dest = GetUniqueFilePath(webSharedDir, filename);

                        var transferState = new FileTransferState
                        {
                            FileId = Guid.NewGuid().ToString("n"),
                            FileName = filename,
                            FilePath = dest,
                            TotalBytes = size,
                            TransferredBytes = 0,
                            Status = TransferStatus.Receiving,
                            Direction = TransferDirection.Received,
                            PeerName = uploaderName,
                            RemoteIp = remoteIp,
                            Timestamp = DateTime.UtcNow
                        };

                        if (!string.IsNullOrEmpty(targetId) && targetId != "pc")
                        {
                            transferState.ErrorMessage = "TARGET:" + targetId;
                            _approvedUploads[transferState.FileId] = transferState;
                            await SendJson(stream, new { accepted = true, id = transferState.FileId });
                        }
                        else
                        {
                            bool accepted = false;
                            if (WebFileSharedCallback != null)
                            {
                                accepted = await WebFileSharedCallback(transferState);
                            }
                            else
                            {
                                accepted = true;
                            }

                            if (accepted)
                            {
                                _approvedUploads[transferState.FileId] = transferState;
                                await SendJson(stream, new { accepted = true, id = transferState.FileId });
                            }
                            else
                            {
                                await SendJson(stream, new { accepted = false });
                            }
                        }
                    }

                    else if (method == "GET" && path == "/api/files")
                    {
                        string? clientId = queryParams.GetValueOrDefault("clientId");
                        var resultFiles = new List<SharedFile>();
                        await _filesLock.WaitAsync();
                        try 
                        { 
                            if (!string.IsNullOrEmpty(clientId) && _clientSpecificFiles.TryGetValue(clientId, out var specFiles))
                            {
                                resultFiles.AddRange(specFiles);
                            }
                        }
                        finally { _filesLock.Release(); }
                        await SendJson(stream, resultFiles);
                    }

                    else if (method == "GET" && path == "/api/events")
                    {
                        string clientId = queryParams.GetValueOrDefault("clientId", Guid.NewGuid().ToString("n"));
                        string clientName = queryParams.GetValueOrDefault("name", "Web Client");
                        string remoteIp = client.Client.RemoteEndPoint is System.Net.IPEndPoint rep ? rep.Address.ToString() : "unknown";

                        var header = "HTTP/1.1 200 OK\r\n" +
                                     "Content-Type: text/event-stream\r\n" +
                                     "Cache-Control: no-cache\r\n" +
                                     "Connection: keep-alive\r\n" +
                                     "Access-Control-Allow-Origin: *\r\n" +
                                     "\r\n";
                        await stream.WriteAsync(Encoding.ASCII.GetBytes(header));
                        await stream.FlushAsync();
                        
                        var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                        var clientInfo = new WebClientInfo
                        {
                            ClientId = clientId,
                            Name = clientName,
                            IpAddress = remoteIp,
                            EventWriter = writer
                        };

                        await _webClientsLock.WaitAsync();
                        try 
                        { 
                            _activeWebClients[clientId] = clientInfo;
                        }
                        finally { _webClientsLock.Release(); }

                        WebClientConnectedEx?.Invoke(clientInfo);

                        try
                        {
                            while (client.Connected && clientInfo.EventWriter != null)
                            {
                                await writer.WriteAsync(": keepalive\n\n");
                                WebClientHeartbeat?.Invoke(clientId);
                                await Task.Delay(10000);
                            }
                        }
                        catch { }
                        finally
                        {
                            await _webClientsLock.WaitAsync();
                            try
                            {
                                if (_activeWebClients.TryGetValue(clientId, out var stored) && stored == clientInfo)
                                {
                                    _activeWebClients.Remove(clientId);
                                }
                            }
                            finally { _webClientsLock.Release(); }

                            await _filesLock.WaitAsync();
                            try
                            {
                                _clientSpecificFiles.Remove(clientId);
                            }
                            finally { _filesLock.Release(); }

                            WebClientDisconnectedEx?.Invoke(clientId);
                        }
                    }

                    else if (method == "GET" && path == "/download")
                    {
                        string? id = queryParams.GetValueOrDefault("id") ?? queryParams.GetValueOrDefault("fileId");
                        string? clientId = queryParams.GetValueOrDefault("clientId");


                        SharedFile? file = null;
                        await _filesLock.WaitAsync();
                        try 
                        { 
                            if (!string.IsNullOrEmpty(id))
                            {
                                if (!string.IsNullOrEmpty(clientId) && _clientSpecificFiles.TryGetValue(clientId, out var list))
                                {
                                    file = list.FirstOrDefault(f => string.Equals(f.Id, id, StringComparison.OrdinalIgnoreCase));
                                }
                                if (file == null)
                                {
                                    file = _sharedFiles.FirstOrDefault(f => string.Equals(f.Id, id, StringComparison.OrdinalIgnoreCase));
                                }
                            }
                        }
                        finally { _filesLock.Release(); }

                        if (file != null && File.Exists(file.Path))
                        {
                            var info = new FileInfo(file.Path);
                            var header = $"HTTP/1.1 200 OK\r\n" +
                                         $"Content-Type: application/octet-stream\r\n" +
                                         $"Content-Disposition: attachment; filename=\"{Uri.EscapeDataString(file.Name)}\"\r\n" +
                                         $"Content-Length: {info.Length}\r\n" +
                                         "Connection: close\r\n" +
                                         "Access-Control-Allow-Origin: *\r\n" +
                                         "\r\n";
                            await stream.WriteAsync(Encoding.ASCII.GetBytes(header));

                            string clientName = "Web Client";
                            if (!string.IsNullOrEmpty(clientId))
                            {
                                await _webClientsLock.WaitAsync();
                                try
                                {
                                    if (_activeWebClients.TryGetValue(clientId, out var cInfo))
                                        clientName = cInfo.Name;
                                }
                                finally { _webClientsLock.Release(); }
                            }

                            var state = new FileTransferState
                            {
                                FileId = file.Id,
                                FileName = file.Name,
                                FilePath = file.Path,
                                TotalBytes = info.Length,
                                TransferredBytes = 0,
                                PeerName = clientName,
                                Direction = TransferDirection.Sent,
                                Status = TransferStatus.Sending,
                                Timestamp = DateTime.UtcNow
                            };
                            WebTransferStarted?.Invoke(state);

                            using var fs = new FileStream(file.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
                            byte[] buf = new byte[65536];
                            int r;
                            long sent = 0;
                            long lastReported = 0;
                            DateTime lastTime = DateTime.UtcNow;

                            while ((r = await fs.ReadAsync(buf, 0, buf.Length)) > 0)
                            {
                                await stream.WriteAsync(buf, 0, r);
                                sent += r;
                                state.TransferredBytes = sent;

                                var now = DateTime.UtcNow;
                                var elapsed = (now - lastTime).TotalSeconds;
                                if (elapsed >= 0.25)
                                {
                                    state.SpeedMbPerSec = (sent - lastReported) / elapsed / 1_000_000.0;
                                    lastReported = sent;
                                    lastTime = now;
                                    WebTransferProgress?.Invoke(state);
                                }
                            }
                            await stream.FlushAsync();
                            state.TransferredBytes = info.Length;
                            state.Status = TransferStatus.Done;
                            WebTransferCompleted?.Invoke(state);
                        }
                        else
                            await SendResponse(stream, 404, "text/plain", "File Not Found");
                    }

                    else if (method == "POST" && path == "/upload")
                    {
                        string? clientId = queryParams.GetValueOrDefault("clientId");
                        string? fileId = queryParams.GetValueOrDefault("id");

                        FileTransferState? transferState = null;
                        if (!string.IsNullOrEmpty(fileId))
                        {
                            _approvedUploads.TryGetValue(fileId, out transferState);
                        }

                        if (transferState == null)
                        {
                            await SendResponse(stream, 400, "application/json", "{\"success\":false,\"error\":\"Upload not pre-approved or invalid id\"}");
                            return;
                        }

                        // Remove from approved uploads so it can't be reused
                        _approvedUploads.TryRemove(fileId!, out _);

                        string uploaderName = transferState.PeerName;
                        string dest = transferState.FilePath;
                        long contentLength = transferState.TotalBytes;

                        if (headers.TryGetValue("Content-Length", out var clHeader) && long.TryParse(clHeader, out var parsedCl) && parsedCl > 0)
                        {
                            contentLength = parsedCl;
                            transferState.TotalBytes = parsedCl;
                        }

                        if (!string.IsNullOrEmpty(clientId))
                        {
                            WebClientHeartbeat?.Invoke(clientId);
                        }

                        WebTransferStarted?.Invoke(transferState);

                        try
                        {
                            using (var fs = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None, 65536, true))
                            {
                                byte[] buf = new byte[65536];
                                long written = 0;
                                long lastReportedBytes = 0;
                                DateTime lastReportTime = DateTime.UtcNow;

                                while (written < contentLength)
                                {
                                    int toRead = (int)Math.Min(buf.Length, contentLength - written);
                                    int n = await stream.ReadAsync(buf.AsMemory(0, toRead));
                                    if (n == 0) break;
                                    await fs.WriteAsync(buf.AsMemory(0, n));
                                    written += n;

                                    transferState.TransferredBytes = written;
                                    var now = DateTime.UtcNow;
                                    var elapsed = (now - lastReportTime).TotalSeconds;
                                    if (elapsed >= 0.25)
                                    {
                                        long bytesSinceLast = written - lastReportedBytes;
                                        transferState.SpeedMbPerSec = bytesSinceLast / elapsed / 1_000_000.0;
                                        if (transferState.SpeedMbPerSec > 0 && transferState.TotalBytes > written)
                                            transferState.ETA = TimeSpan.FromSeconds((transferState.TotalBytes - written) / (transferState.SpeedMbPerSec * 1_000_000.0));

                                        lastReportedBytes = written;
                                        lastReportTime = now;
                                        WebTransferProgress?.Invoke(transferState);

                                        // Push server-side progress to the uploading web client via SSE
                                        if (!string.IsNullOrEmpty(clientId))
                                        {
                                            double speedVal = transferState.SpeedMbPerSec;
                                            NotifyClient(clientId, $"progress:{{\"id\":\"{transferState.FileId}\",\"received\":{written},\"total\":{contentLength},\"speed\":{speedVal:F2}}}");
                                        }
                                    }
                                }

                                if (written != contentLength)
                                {
                                    throw new Exception("Connection closed before complete transfer.");
                                }
                            }

                            transferState.Status = TransferStatus.Done;
                            WebTransferCompleted?.Invoke(transferState);

                            // Notify web client that upload is complete
                            if (!string.IsNullOrEmpty(clientId))
                            {
                                NotifyClient(clientId, $"upload-complete:{{\"id\":\"{transferState.FileId}\",\"name\":\"{Uri.EscapeDataString(transferState.FileName)}\",\"bytes\":{contentLength}}}");
                            }

                            if (contentLength > 0)
                            {
                                if (transferState.ErrorMessage != null && transferState.ErrorMessage.StartsWith("TARGET:"))
                                {
                                    string tId = transferState.ErrorMessage.Substring(7);
                                    ShareForWebClient(tId, transferState.FilePath);
                                    // Optional: still invoke PC callback so it shows in PC library, but maybe skip the "ask receive" popup.
                                    // We already bypassed WebFileSharedCallback in ask-receive, so this just adds it to library.
                                    WebFileShared?.Invoke(clientId ?? "unknown", uploaderName, transferState.FilePath, contentLength);
                                }
                                else
                                {
                                    WebFileShared?.Invoke(clientId ?? "unknown", uploaderName, transferState.FilePath, contentLength);
                                }
                            }

                            await SendJson(stream, new { success = true, saved = Path.GetFileName(transferState.FilePath), bytes = contentLength });
                            try { client.Client.Shutdown(System.Net.Sockets.SocketShutdown.Send); } catch { }
                        }
                        catch (Exception ex)
                        {
                            transferState.Status = TransferStatus.Failed;
                            transferState.ErrorMessage = ex.Message;
                            WebTransferFailed?.Invoke(transferState);
                            throw;
                        }
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