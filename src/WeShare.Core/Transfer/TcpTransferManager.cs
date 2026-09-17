using System;
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
    public class TransferDeclinedException : Exception
    {
        public string PeerName { get; }
        public string FileName { get; }

        public TransferDeclinedException(string peerName, string fileName)
            : base($"{(!string.IsNullOrEmpty(peerName) ? peerName : "The recipient")} declined the transfer request.")
        {
            PeerName = peerName;
            FileName = fileName;
        }
    }

    public class ConnectionRequest
    {
        public string ClientId { get; set; } = Guid.NewGuid().ToString("n");
        public string PeerName { get; set; } = "";
        public string PeerIp { get; set; } = "";
        public string PeerType { get; set; } = "PC";
        public string Role { get; set; } = "Sender"; // "Sender" or "Receiver"
    }

    public class ConnectionResponse
    {
        public bool Accepted { get; set; }
        public string Name { get; set; } = "";
        public string DeviceType { get; set; } = "PC";
        public string Message { get; set; } = "";
    }

    public class BatchFileItem
    {
        public string FileId { get; set; } = Guid.NewGuid().ToString("n");
        public string FileName { get; set; } = "";
        public string RelativePath { get; set; } = "";
        public long FileSize { get; set; }
        public bool IsFolderItem { get; set; }
        public string SizeDisplay => FileTransferState.FormatBytes(FileSize);
        public bool IsSelected { get; set; } = true;
    }

    public class BatchManifest
    {
        public string BatchId { get; set; } = Guid.NewGuid().ToString("n");
        public string SenderName { get; set; } = "";
        public string SenderType { get; set; } = "PC";
        public string SenderIp { get; set; } = "";
        public long TotalBytes { get; set; }
        public System.Collections.Generic.List<BatchFileItem> Files { get; set; } = new();
        public string TotalSizeDisplay => FileTransferState.FormatBytes(TotalBytes);
        public int FileCount => Files.Count;
    }

    public class BatchResponse
    {
        public string BatchId { get; set; } = "";
        public System.Collections.Generic.List<string> AcceptedFileIds { get; set; } = new();
        public bool AllAccepted { get; set; }
        public bool AnyAccepted => AcceptedFileIds.Count > 0;
    }

    public class ResendRequest
    {
        public string BatchId { get; set; } = "";
        public string FileId { get; set; } = "";
        public string FileName { get; set; } = "";
        public string RequesterName { get; set; } = "";
    }

    public class ResendResponse
    {
        public string FileId { get; set; } = "";
        public bool Accepted { get; set; }
    }

    public class TcpTransferManager
    {
        private const int EnterpriseBufferSize = 1048576; // 1MB buffer for gigabit line-rate & 100GB+ large-file throughput
        private static readonly byte[] Magic = new byte[] { 0x57, 0x45, 0x53, 0x48 }; // "WESH"

        private readonly int _listenPort;
        private TcpListener? _listener;
        private CancellationTokenSource? _listenerCts;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, TcpClient> _activeClients = new();

        public int BoundPort { get; private set; }
        public string LocalName { get; set; } = Environment.MachineName;
        public string LocalType { get; set; } = "PC";

        // Active paired session
        public DeviceModel? ActiveSessionDevice { get; set; }
        public bool IsConnected => ActiveSessionDevice != null;

        public void CancelTransfer(string fileId)
        {
            if (_activeClients.TryRemove(fileId, out var client))
            {
                try { client.Close(); } catch { }
            }
        }

        public event Action<FileTransferState>? TransferStarted;
        public event Action<FileTransferState>? TransferProgress;
        public event Action<FileTransferState>? TransferCompleted;
        public event Action<FileTransferState>? TransferFailed;

        /// <summary>
        /// Callback to ask the user if they want to accept a transfer. 
        /// Return true to accept, false to reject.
        /// </summary>
        public Func<FileTransferState, Task<bool>>? TransferRequestCallback { get; set; }

        /// <summary>
        /// Callback to ask user if they want to accept a connection request from a peer.
        /// </summary>
        public Func<ConnectionRequest, Task<bool>>? ConnectionRequestCallback { get; set; }

        /// <summary>
        /// Callback to ask user which files in a batch they want to accept.
        /// Return list of accepted FileId strings.
        /// </summary>
        public Func<BatchManifest, Task<System.Collections.Generic.List<string>>>? BatchManifestCallback { get; set; }

        /// <summary>
        /// Callback to ask user if they want to accept a request from a peer to resend a file.
        /// </summary>
        public Func<ResendRequest, Task<bool>>? ResendRequestCallback { get; set; }

        public event Action<BatchManifest, int, long>? BatchTransferCompleted;

        public event Action<DeviceModel>? DeviceConnected;
        public event Action<DeviceModel>? DeviceDisconnected;

        public TcpTransferManager(int listenPort = 45679)
        {
            _listenPort = listenPort;
        }

        // ── Listen ─────────────────────────────────────────────────────────────
        public void StartListening(string saveDirectory)
        {
            _listenerCts = new CancellationTokenSource();
            _listener = new TcpListener(IPAddress.Any, _listenPort);
            _listener.Start();
            
            BoundPort = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _ = Task.Run(() => AcceptClientsAsync(saveDirectory, _listenerCts.Token));
        }

        public void StopListening()
        {
            _listenerCts?.Cancel();
            _listener?.Stop();
        }

        private async Task AcceptClientsAsync(string saveDirectory, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (_listener == null) break;
                    var client = await _listener.AcceptTcpClientAsync(token);
                    _ = Task.Run(() => HandleIncomingClientAsync(client, saveDirectory), token);
                }
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"[Transfer] Accept error: {ex.Message}");
            }
        }

        // ── Incoming Connection Handler ─────────────────────────────────────────
        private async Task HandleIncomingClientAsync(TcpClient client, string saveDirectory)
        {
            client.NoDelay = true;
            client.SendBufferSize = EnterpriseBufferSize;
            client.ReceiveBufferSize = EnterpriseBufferSize;

            using var clientOwner = client;
            using var stream = client.GetStream();

            string remoteIp = client.Client.RemoteEndPoint is IPEndPoint rep ? rep.Address.ToString() : "unknown";

            try
            {
                // Read 4-byte header to detect framing
                byte[] magicBuf = new byte[4];
                if (!await ReadExactAsync(stream, magicBuf, 4)) return;

                bool isWesh = (magicBuf[0] == Magic[0] && magicBuf[1] == Magic[1] &&
                               magicBuf[2] == Magic[2] && magicBuf[3] == Magic[3]);

                if (isWesh)
                {
                    byte msgType = (byte)stream.ReadByte();
                    byte[] lenBuf = new byte[4];
                    if (!await ReadExactAsync(stream, lenBuf, 4)) return;
                    int length = BitConverter.ToInt32(lenBuf, 0);

                    byte[] payload = new byte[length];
                    if (!await ReadExactAsync(stream, payload, length)) return;

                    if (msgType == 0x01) // CONNECT_REQUEST
                    {
                        await HandleConnectRequestAsync(stream, remoteIp, payload);
                    }
                    else if (msgType == 0x03) // DISCONNECT
                    {
                        HandleDisconnect(remoteIp);
                    }
                    else if (msgType == 0x10) // FILE_MANIFEST
                    {
                        await HandleIncomingFileTransferAsync(client, stream, remoteIp, saveDirectory, payload);
                    }
                    else if (msgType == 0x12) // BATCH_MANIFEST
                    {
                        await HandleBatchManifestAsync(stream, remoteIp, payload);
                    }
                    else if (msgType == 0x14) // RESEND_REQUEST
                    {
                        await HandleResendRequestAsync(stream, remoteIp, payload);
                    }
                }
                else
                {
                    // Fallback for raw legacy transfers (first 4 bytes were length)
                    int encryptedMetaLength = BitConverter.ToInt32(magicBuf, 0);
                    if (encryptedMetaLength > 0 && encryptedMetaLength < 100000)
                    {
                        byte[] metaBytes = new byte[encryptedMetaLength];
                        if (await ReadExactAsync(stream, metaBytes, encryptedMetaLength))
                        {
                            await HandleIncomingFileTransferAsync(client, stream, remoteIp, saveDirectory, metaBytes);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Transfer] Incoming client error from {remoteIp}: {ex.Message}");
            }
        }

        private async Task HandleConnectRequestAsync(NetworkStream stream, string remoteIp, byte[] payload)
        {
            var json = Encoding.UTF8.GetString(payload);
            var req = JsonSerializer.Deserialize<ConnectionRequest>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (req == null) return;

            req.PeerIp = remoteIp;

            bool accepted = true;
            if (ConnectionRequestCallback != null)
            {
                accepted = await ConnectionRequestCallback(req).ConfigureAwait(false);
            }

            var resp = new ConnectionResponse
            {
                Accepted = accepted,
                Name = LocalName,
                DeviceType = LocalType,
                Message = accepted ? "Connected successfully" : "Connection request declined"
            };

            var respBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(resp));
            await stream.WriteAsync(Magic, 0, 4);
            stream.WriteByte(0x02); // CONNECT_RESPONSE
            await stream.WriteAsync(BitConverter.GetBytes(respBytes.Length), 0, 4);
            await stream.WriteAsync(respBytes, 0, respBytes.Length);
            await stream.FlushAsync();

            if (accepted)
            {
                var peer = new DeviceModel
                {
                    Id = req.ClientId,
                    Name = req.PeerName,
                    Type = req.PeerType,
                    IpAddress = remoteIp,
                    Port = _listenPort,
                    ConnectionStatus = "Connected",
                    Role = req.Role == "Receiver" ? "Receiver" : "Sender"
                };
                ActiveSessionDevice = peer;
                DeviceConnected?.Invoke(peer);
            }
        }

        private void HandleDisconnect(string remoteIp)
        {
            if (ActiveSessionDevice != null && IsSameIp(ActiveSessionDevice.IpAddress, remoteIp))
            {
                var dev = ActiveSessionDevice;
                ActiveSessionDevice = null;
                DeviceDisconnected?.Invoke(dev);
            }
        }

        private async Task HandleIncomingFileTransferAsync(TcpClient client, NetworkStream stream, string remoteIp, string saveDirectory, byte[] manifestBytes)
        {
            FileTransferState? state = null;
            try
            {
                var json = Encoding.UTF8.GetString(manifestBytes);
                state = JsonSerializer.Deserialize<FileTransferState>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (state == null) return;

                state.RemoteIp = remoteIp;
                state.PeerName = !string.IsNullOrEmpty(state.SenderName) ? state.SenderName : remoteIp;
                state.Status = TransferStatus.Receiving;
                state.Direction = TransferDirection.Received;
                state.Timestamp = DateTime.UtcNow;

                _activeClients[state.FileId] = client;

                // If paired in active session with this peer, auto-accept!
                bool accepted = false;
                if (ActiveSessionDevice != null && IsSameIp(ActiveSessionDevice.IpAddress, remoteIp))
                {
                    accepted = true;
                }
                else if (TransferRequestCallback != null)
                {
                    accepted = await TransferRequestCallback(state).ConfigureAwait(false);
                }
                else
                {
                    accepted = true;
                }

                // Send response
                await stream.WriteAsync(Magic, 0, 4);
                stream.WriteByte(0x11); // FILE_RESPONSE
                await stream.WriteAsync(BitConverter.GetBytes(1), 0, 4);
                stream.WriteByte(accepted ? (byte)1 : (byte)2);
                await stream.FlushAsync();

                if (!accepted)
                {
                    state.Status = TransferStatus.Failed;
                    state.ErrorMessage = "Transfer declined.";
                    TransferFailed?.Invoke(state);
                    return;
                }

                TransferStarted?.Invoke(state);

                // Prepare save path
                string dest;
                if (!string.IsNullOrEmpty(state.RelativePath))
                {
                    string relDir = Path.GetDirectoryName(state.RelativePath) ?? "";
                    string targetDir = Path.Combine(saveDirectory, relDir);
                    Directory.CreateDirectory(targetDir);
                    dest = Path.Combine(targetDir, Path.GetFileName(state.RelativePath));
                }
                else
                {
                    string category = GetCategoryFolder(Path.GetExtension(state.FileName));
                    string targetDir = Path.Combine(saveDirectory, category);
                    Directory.CreateDirectory(targetDir);
                    dest = GetUniqueFilePath(targetDir, state.FileName);
                }
                state.FilePath = dest;

                using (var fs = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None, EnterpriseBufferSize, true))
                {
                    byte[] buffer = new byte[EnterpriseBufferSize];
                    long totalRead = 0;
                    long lastReportedBytes = 0;
                    DateTime lastReportTime = DateTime.UtcNow;

                    while (totalRead < state.TotalBytes)
                    {
                        int toRead = (int)Math.Min(buffer.Length, state.TotalBytes - totalRead);
                        int read = await stream.ReadAsync(buffer.AsMemory(0, toRead)).ConfigureAwait(false);
                        if (read <= 0) break;

                        await fs.WriteAsync(buffer.AsMemory(0, read)).ConfigureAwait(false);
                        totalRead += read;
                        state.TransferredBytes = totalRead;

                        var now = DateTime.UtcNow;
                        var elapsed = (now - lastReportTime).TotalSeconds;
                        if (elapsed >= 0.2)
                        {
                            long bytesSinceLast = totalRead - lastReportedBytes;
                            state.SpeedMbPerSec = bytesSinceLast / elapsed / 1_000_000.0;
                            if (state.SpeedMbPerSec > 0 && state.TotalBytes > totalRead)
                                state.ETA = TimeSpan.FromSeconds((state.TotalBytes - totalRead) / (state.SpeedMbPerSec * 1_000_000.0));

                            lastReportedBytes = totalRead;
                            lastReportTime = now;
                            TransferProgress?.Invoke(state);
                        }
                    }

                    if (totalRead < state.TotalBytes)
                    {
                        throw new IOException($"Connection lost. Received {totalRead} of {state.TotalBytes} bytes.");
                    }
                }

                // Acknowledge completion to sender so sender knows the file handle has closed
                stream.WriteByte(0x06); // ACK
                await stream.FlushAsync().ConfigureAwait(false);

                state.TransferredBytes = state.TotalBytes;
                state.Status = TransferStatus.Done;
                TransferCompleted?.Invoke(state);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Transfer] Incoming transfer failed: {ex.Message}");
                if (state != null)
                {
                    state.Status = TransferStatus.Failed;
                    state.ErrorMessage = ex.Message;
                }
                TransferFailed?.Invoke(state ?? new FileTransferState { Status = TransferStatus.Failed, ErrorMessage = ex.Message });
            }
            finally
            {
                if (state != null)
                {
                    _activeClients.TryRemove(state.FileId, out _);
                }
            }
        }

        // ── Connect Request (Initiator) ─────────────────────────────────────────
        public async Task<ConnectionResponse> SendConnectRequestAsync(string targetIp, int targetPort, string role = "Sender", CancellationToken cancellationToken = default)
        {
            using var client = new TcpClient();
            client.NoDelay = true;

            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            connectCts.CancelAfter(TimeSpan.FromSeconds(15));
            await client.ConnectAsync(targetIp, targetPort, connectCts.Token).ConfigureAwait(false);

            using var stream = client.GetStream();

            var req = new ConnectionRequest
            {
                ClientId = Guid.NewGuid().ToString("n"),
                PeerName = LocalName,
                PeerIp = targetIp,
                PeerType = LocalType,
                Role = role
            };

            var reqBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(req));
            await stream.WriteAsync(Magic, 0, 4, cancellationToken).ConfigureAwait(false);
            stream.WriteByte(0x01); // CONNECT_REQUEST
            await stream.WriteAsync(BitConverter.GetBytes(reqBytes.Length), 0, 4, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(reqBytes, 0, reqBytes.Length, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

            // Read response
            byte[] magicBuf = new byte[4];
            if (!await ReadExactAsync(stream, magicBuf, 4))
                throw new IOException("Remote device closed connection without response.");

            byte respType = (byte)stream.ReadByte();
            byte[] lenBuf = new byte[4];
            if (!await ReadExactAsync(stream, lenBuf, 4))
                throw new IOException("Remote device sent invalid response header.");

            int length = BitConverter.ToInt32(lenBuf, 0);
            byte[] payload = new byte[length];
            if (!await ReadExactAsync(stream, payload, length))
                throw new IOException("Remote device sent incomplete response payload.");

            var resp = JsonSerializer.Deserialize<ConnectionResponse>(Encoding.UTF8.GetString(payload),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (resp == null)
            {
                throw new IOException("Unable to parse connection response.");
            }

            if (resp.Accepted)
            {
                var peer = new DeviceModel
                {
                    Id = req.ClientId,
                    Name = !string.IsNullOrEmpty(resp.Name) ? resp.Name : targetIp,
                    Type = resp.DeviceType,
                    IpAddress = targetIp,
                    Port = targetPort,
                    ConnectionStatus = "Connected",
                    Role = role == "Receiver" ? "Sender" : "Receiver"
                };
                ActiveSessionDevice = peer;
                DeviceConnected?.Invoke(peer);
            }

            return resp;
        }

        public async Task SendDisconnectAsync(string targetIp, int targetPort)
        {
            try
            {
                using var client = new TcpClient();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await client.ConnectAsync(targetIp, targetPort, cts.Token);
                using var stream = client.GetStream();

                await stream.WriteAsync(Magic, 0, 4);
                stream.WriteByte(0x03); // DISCONNECT
                await stream.WriteAsync(BitConverter.GetBytes(0), 0, 4);
                await stream.FlushAsync();
            }
            catch { }

            DisconnectSession();
        }

        public void DisconnectSession()
        {
            if (ActiveSessionDevice != null)
            {
                var dev = ActiveSessionDevice;
                ActiveSessionDevice = null;
                DeviceDisconnected?.Invoke(dev);
            }
        }

        private async Task HandleBatchManifestAsync(NetworkStream stream, string remoteIp, byte[] payload)
        {
            var json = Encoding.UTF8.GetString(payload);
            var manifest = JsonSerializer.Deserialize<BatchManifest>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (manifest == null) return;

            manifest.SenderIp = remoteIp;
            manifest.SenderName = !string.IsNullOrEmpty(manifest.SenderName) ? manifest.SenderName : remoteIp;

            System.Collections.Generic.List<string> acceptedFileIds = new();
            if (BatchManifestCallback != null)
            {
                acceptedFileIds = await BatchManifestCallback(manifest).ConfigureAwait(false);
            }
            else
            {
                acceptedFileIds = manifest.Files.Select(f => f.FileId).ToList();
            }

            var resp = new BatchResponse
            {
                BatchId = manifest.BatchId,
                AcceptedFileIds = acceptedFileIds,
                AllAccepted = acceptedFileIds.Count == manifest.Files.Count
            };

            var respBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(resp));
            await stream.WriteAsync(Magic, 0, 4);
            stream.WriteByte(0x13); // BATCH_RESPONSE
            await stream.WriteAsync(BitConverter.GetBytes(respBytes.Length), 0, 4);
            await stream.WriteAsync(respBytes, 0, respBytes.Length);
            await stream.FlushAsync();
        }

        private async Task HandleResendRequestAsync(NetworkStream stream, string remoteIp, byte[] payload)
        {
            var json = Encoding.UTF8.GetString(payload);
            var req = JsonSerializer.Deserialize<ResendRequest>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (req == null) return;

            bool accepted = false;
            if (ResendRequestCallback != null)
            {
                accepted = await ResendRequestCallback(req).ConfigureAwait(false);
            }

            var resp = new ResendResponse
            {
                FileId = req.FileId,
                Accepted = accepted
            };

            var respBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(resp));
            await stream.WriteAsync(Magic, 0, 4);
            stream.WriteByte(0x15); // RESEND_RESPONSE
            await stream.WriteAsync(BitConverter.GetBytes(respBytes.Length), 0, 4);
            await stream.WriteAsync(respBytes, 0, respBytes.Length);
            await stream.FlushAsync();
        }

        public async Task<BatchResponse> SendBatchManifestAsync(string targetIp, int targetPort, BatchManifest manifest, CancellationToken cancellationToken = default)
        {
            using var client = new TcpClient();
            client.NoDelay = true;
            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            connectCts.CancelAfter(TimeSpan.FromSeconds(25));
            await client.ConnectAsync(targetIp, targetPort, connectCts.Token).ConfigureAwait(false);

            using var stream = client.GetStream();
            manifest.SenderName = LocalName;
            manifest.SenderType = LocalType;

            var json = JsonSerializer.Serialize(manifest);
            var bytes = Encoding.UTF8.GetBytes(json);

            await stream.WriteAsync(Magic, 0, 4, cancellationToken).ConfigureAwait(false);
            stream.WriteByte(0x12); // BATCH_MANIFEST
            await stream.WriteAsync(BitConverter.GetBytes(bytes.Length), 0, 4, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

            // Read BATCH_RESPONSE (0x13)
            byte[] magicBuf = new byte[4];
            if (!await ReadExactAsync(stream, magicBuf, 4))
                throw new IOException("Remote device closed connection without batch response.");

            byte respType = (byte)stream.ReadByte();
            byte[] lenBuf = new byte[4];
            if (!await ReadExactAsync(stream, lenBuf, 4))
                throw new IOException("Invalid batch response length.");

            int len = BitConverter.ToInt32(lenBuf, 0);
            byte[] payload = new byte[len];
            if (!await ReadExactAsync(stream, payload, len))
                throw new IOException("Incomplete batch response payload.");

            var resp = JsonSerializer.Deserialize<BatchResponse>(Encoding.UTF8.GetString(payload),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return resp ?? new BatchResponse { BatchId = manifest.BatchId };
        }

        public async Task<bool> SendResendRequestAsync(string targetIp, int targetPort, ResendRequest req, CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = new TcpClient();
                client.NoDelay = true;
                using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                connectCts.CancelAfter(TimeSpan.FromSeconds(15));
                await client.ConnectAsync(targetIp, targetPort, connectCts.Token).ConfigureAwait(false);

                using var stream = client.GetStream();
                req.RequesterName = LocalName;
                var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(req));

                await stream.WriteAsync(Magic, 0, 4, cancellationToken).ConfigureAwait(false);
                stream.WriteByte(0x14); // RESEND_REQUEST
                await stream.WriteAsync(BitConverter.GetBytes(bytes.Length), 0, 4, cancellationToken).ConfigureAwait(false);
                await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

                // Read response 0x15
                byte[] magicBuf = new byte[4];
                if (!await ReadExactAsync(stream, magicBuf, 4)) return false;
                byte respType = (byte)stream.ReadByte();
                byte[] lenBuf = new byte[4];
                if (!await ReadExactAsync(stream, lenBuf, 4)) return false;
                int len = BitConverter.ToInt32(lenBuf, 0);
                byte[] payload = new byte[len];
                if (!await ReadExactAsync(stream, payload, len)) return false;

                var resp = JsonSerializer.Deserialize<ResendResponse>(Encoding.UTF8.GetString(payload),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return resp?.Accepted == true;
            }
            catch
            {
                return false;
            }
        }

        public void NotifyBatchCompleted(BatchManifest manifest, int fileCount, long totalBytes)
        {
            BatchTransferCompleted?.Invoke(manifest, fileCount, totalBytes);
        }

        // ── Send File ──────────────────────────────────────────────────────────
        public async Task SendFileAsync(string targetIp, int targetPort, string fileName, Stream fileStream, long totalBytes,
                                        string filePath = "", string relativePath = "", string batchId = "", CancellationToken cancellationToken = default)
        {
            var state = new FileTransferState
            {
                FileName     = fileName,
                FilePath     = filePath,
                RelativePath = relativePath,
                BatchId      = batchId,
                TotalBytes   = totalBytes,
                Status       = TransferStatus.Sending,
                Direction    = TransferDirection.Sent,
                SenderName   = LocalName,
                Timestamp    = DateTime.UtcNow
            };

            TransferStarted?.Invoke(state);

            try
            {
                using var client = new TcpClient();
                client.NoDelay = true;
                _activeClients[state.FileId] = client;
                client.SendBufferSize    = EnterpriseBufferSize;
                client.ReceiveBufferSize = EnterpriseBufferSize;

                using (var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    connectCts.CancelAfter(TimeSpan.FromSeconds(25));
                    await client.ConnectAsync(targetIp, targetPort, connectCts.Token).ConfigureAwait(false);
                }

                using var stream = client.GetStream();

                // 1. Send manifest header
                var json = JsonSerializer.Serialize(state);
                var metaBytes = Encoding.UTF8.GetBytes(json);

                await stream.WriteAsync(Magic, 0, 4, cancellationToken).ConfigureAwait(false);
                stream.WriteByte(0x10); // FILE_MANIFEST
                await stream.WriteAsync(BitConverter.GetBytes(metaBytes.Length), 0, 4, cancellationToken).ConfigureAwait(false);
                await stream.WriteAsync(metaBytes, 0, metaBytes.Length, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

                // 2. Read response
                byte[] magicBuf = new byte[4];
                if (!await ReadExactAsync(stream, magicBuf, 4))
                {
                    state.Status = TransferStatus.Failed;
                    state.ErrorMessage = "The recipient disconnected before the transfer could start.";
                    TransferFailed?.Invoke(state);
                    throw new IOException("The recipient disconnected before the transfer could start.");
                }

                byte respType = (byte)stream.ReadByte();
                byte[] lenBuf = new byte[4];
                if (!await ReadExactAsync(stream, lenBuf, 4))
                    throw new IOException("Invalid response from recipient.");

                byte respCode = (byte)stream.ReadByte();
                if (respCode == 2)
                {
                    state.Status = TransferStatus.Failed;
                    state.ErrorMessage = $"{(!string.IsNullOrEmpty(state.PeerName) ? state.PeerName : "The recipient")} declined the transfer.";
                    TransferFailed?.Invoke(state);
                    throw new TransferDeclinedException(state.PeerName ?? "", state.FileName);
                }

                if (respCode != 1)
                {
                    state.Status = TransferStatus.Failed;
                    state.ErrorMessage = "The recipient was unable to accept the transfer.";
                    TransferFailed?.Invoke(state);
                    throw new IOException("The recipient was unable to accept the transfer.");
                }

                // 3. Stream raw payload at full wire speed
                byte[] buffer = new byte[EnterpriseBufferSize];
                int read;
                long totalSent = 0;
                long lastReportedBytes = 0;
                DateTime lastReportTime = DateTime.UtcNow;

                while ((read = await fileStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await stream.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    totalSent += read;
                    state.TransferredBytes = totalSent;

                    var now = DateTime.UtcNow;
                    var elapsed = (now - lastReportTime).TotalSeconds;
                    if (elapsed >= 0.2)
                    {
                        long bytesSinceLast = totalSent - lastReportedBytes;
                        state.SpeedMbPerSec = bytesSinceLast / elapsed / 1_000_000.0;
                        if (state.SpeedMbPerSec > 0 && state.TotalBytes > totalSent)
                            state.ETA = TimeSpan.FromSeconds((state.TotalBytes - totalSent) / (state.SpeedMbPerSec * 1_000_000.0));

                        lastReportedBytes = totalSent;
                        lastReportTime = now;
                        TransferProgress?.Invoke(state);
                    }
                }

                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

                // Wait for receiver ACK confirming file was completely written to disk and closed
                byte[] ackBuf = new byte[1];
                int ackRead = await stream.ReadAsync(ackBuf.AsMemory(0, 1), cancellationToken).ConfigureAwait(false);
                if (ackRead <= 0 || ackBuf[0] != 0x06)
                {
                    throw new IOException("Receiver failed to acknowledge completion of file transfer.");
                }

                state.TransferredBytes = totalBytes;
                state.Status = TransferStatus.Done;
                TransferCompleted?.Invoke(state);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Transfer] Outgoing failed: {ex.Message}");
                state.Status = TransferStatus.Failed;
                state.ErrorMessage = ex.Message;
                TransferFailed?.Invoke(state);
                throw;
            }
            finally
            {
                _activeClients.TryRemove(state.FileId, out _);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        public static string GetCategoryFolder(string ext)
        {
            ext = ext.ToLower().TrimStart('.');
            switch (ext)
            {
                case "jpg": case "jpeg": case "png": case "gif": case "bmp": case "webp": case "svg":
                    return "Images";
                case "mp4": case "mkv": case "mov": case "avi": case "wmv": case "flv":
                    return "Videos";
                case "mp3": case "wav": case "flac": case "m4a": case "ogg":
                    return "Music";
                case "pdf": case "doc": case "docx": case "txt": case "rtf": case "xls": case "xlsx": case "ppt": case "pptx":
                    return "Documents";
                case "zip": case "rar": case "7z": case "tar": case "gz":
                    return "Archives";
                case "exe": case "msi": case "apk":
                    return "Apps";
                default:
                    return "Others";
            }
        }

        private static async Task<bool> ReadExactAsync(Stream stream, byte[] buffer, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(totalRead, count - totalRead)).ConfigureAwait(false);
                if (read == 0) return false;
                totalRead += read;
            }
            return true;
        }

        private static readonly object _pathLock = new();
        public static string GetUniqueFilePath(string dir, string filename)
        {
            lock (_pathLock)
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

        private static bool IsSameIp(string? ip1, string? ip2)
        {
            if (string.IsNullOrEmpty(ip1) || string.IsNullOrEmpty(ip2)) return false;
            if (string.Equals(ip1, ip2, StringComparison.OrdinalIgnoreCase)) return true;
            if (ip1.StartsWith("::ffff:") && ip1[7..] == ip2) return true;
            if (ip2.StartsWith("::ffff:") && ip2[7..] == ip1) return true;
            return false;
        }
    }
}
