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
    public class TcpTransferManager
    {
        private readonly int _listenPort;
        private TcpListener? _listener;
        private CancellationTokenSource? _listenerCts;

        public string LocalName { get; set; } = Environment.MachineName;

        public event Action<FileTransferState>? TransferStarted;
        public event Action<FileTransferState>? TransferProgress;
        public event Action<FileTransferState>? TransferCompleted;
        public event Action<FileTransferState>? TransferFailed;

        /// <summary>
        /// Callback to ask the user if they want to accept a transfer. 
        /// Return true to accept, false to reject.
        /// </summary>
        public Func<FileTransferState, Task<bool>>? TransferRequestCallback { get; set; }

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
                    _ = Task.Run(() => HandleIncomingTransferAsync(client, saveDirectory), token);
                }
            }
            catch (OperationCanceledException) { /* listener stopped */ }
            catch (ObjectDisposedException)    { /* listener stopped */ }
            catch (Exception ex)
            {
                Console.WriteLine($"[Transfer] Accept error: {ex.Message}");
            }
        }

        // ── Receive ────────────────────────────────────────────────────────────
        private async Task HandleIncomingTransferAsync(TcpClient client, string saveDirectory)
        {
            using var clientOwner = client;
            using var stream = client.GetStream();
            using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

            FileTransferState? state = null;
            try
            {
                // 1. Read metadata
                int metaLength = reader.ReadInt32();
                byte[] metaBytes = reader.ReadBytes(metaLength);
                var json = Encoding.UTF8.GetString(metaBytes);
                state = JsonSerializer.Deserialize<FileTransferState>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (state == null) return;

                state.RemoteIp = ((IPEndPoint)client.Client.RemoteEndPoint!).Address.ToString();
                state.PeerName = !string.IsNullOrEmpty(state.SenderName) ? state.SenderName : state.RemoteIp;
                state.Status    = TransferStatus.Receiving;
                state.Direction = TransferDirection.Received;
                state.Timestamp = DateTime.UtcNow;

                // 2. Ask for permission
                using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
                if (TransferRequestCallback != null)
                {
                    bool accepted = await TransferRequestCallback(state);
                    writer.Write(accepted ? (byte)1 : (byte)0);
                    writer.Flush();
                    if (!accepted) return;
                }
                else
                {
                    writer.Write((byte)1); // Auto-accept
                    writer.Flush();
                }

                TransferStarted?.Invoke(state);

                // 3. Save file
                Directory.CreateDirectory(saveDirectory);
                string dest = GetUniqueFilePath(saveDirectory, state.FileName);
                state.FilePath = dest;

                using var fs = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

                byte[] buffer = new byte[81920];
                long totalRead = 0;
                var sw = System.Diagnostics.Stopwatch.StartNew();
                long lastReportedBytes = 0;
                DateTime lastReportTime = DateTime.UtcNow;

                int read;
                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fs.WriteAsync(buffer, 0, read);
                    totalRead += read;
                    state.TransferredBytes = totalRead;

                    var now = DateTime.UtcNow;
                    var elapsed = (now - lastReportTime).TotalSeconds;
                    if (elapsed >= 0.25) // update UI ~4x/sec
                    {
                        long bytesSinceLast = totalRead - lastReportedBytes;
                        state.SpeedMbPerSec = bytesSinceLast / elapsed / 1_000_000.0;
                        if (state.SpeedMbPerSec > 0 && state.TotalBytes > totalRead)
                            state.ETA = TimeSpan.FromSeconds((state.TotalBytes - totalRead) / (state.SpeedMbPerSec * 1_000_000.0));
                        else
                            state.ETA = TimeSpan.Zero;

                        lastReportedBytes = totalRead;
                        lastReportTime = now;
                        TransferProgress?.Invoke(state);
                    }
                }

                state.TransferredBytes = totalRead;
                state.SpeedMbPerSec = totalRead / Math.Max(sw.Elapsed.TotalSeconds, 0.001) / 1_000_000.0;
                state.ETA = TimeSpan.Zero;
                state.Status = TransferStatus.Done;
                TransferCompleted?.Invoke(state);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Transfer] Incoming failed: {ex.Message}");
                if (state != null) state.Status = TransferStatus.Failed;
                TransferFailed?.Invoke(state ?? new FileTransferState { Status = TransferStatus.Failed });
            }
        }

        // ── Send ───────────────────────────────────────────────────────────────
        public async Task SendFileAsync(string targetIp, int targetPort, string filePath,
                                        CancellationToken cancellationToken = default)
        {
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists) throw new FileNotFoundException("File not found", filePath);

            var state = new FileTransferState
            {
                FileName   = fileInfo.Name,
                FilePath   = filePath,
                TotalBytes = fileInfo.Length,
                Status     = TransferStatus.Sending,
                Direction  = TransferDirection.Sent,
                SenderName = LocalName,
                Timestamp  = DateTime.UtcNow
            };

            TransferStarted?.Invoke(state);

            try
            {
                using var client = new TcpClient();
                client.SendBufferSize    = 81920;
                client.ReceiveBufferSize = 81920;
                await client.ConnectAsync(targetIp, targetPort, cancellationToken);

                using var stream = client.GetStream();
                using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

                // 1. Metadata
                var json = JsonSerializer.Serialize(state);
                var metaBytes = Encoding.UTF8.GetBytes(json);
                writer.Write(metaBytes.Length);
                writer.Write(metaBytes);
                writer.Flush();

                // 2. Read response
                using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
                byte response = reader.ReadByte();
                if (response == 0)
                {
                    state.Status = TransferStatus.Failed;
                    TransferFailed?.Invoke(state);
                    return; // Rejected by peer
                }

                // 3. File data
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
                byte[] buffer = new byte[81920];
                int read;
                long totalSent = 0;
                long lastReportedBytes = 0;
                DateTime lastReportTime = DateTime.UtcNow;
                var sw = System.Diagnostics.Stopwatch.StartNew();

                while ((read = await fs.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    await stream.WriteAsync(buffer, 0, read, cancellationToken);
                    totalSent += read;
                    state.TransferredBytes = totalSent;

                    var now = DateTime.UtcNow;
                    var elapsed = (now - lastReportTime).TotalSeconds;
                    if (elapsed >= 0.25)
                    {
                        long bytesSinceLast = totalSent - lastReportedBytes;
                        state.SpeedMbPerSec = bytesSinceLast / elapsed / 1_000_000.0;
                        if (state.SpeedMbPerSec > 0 && state.TotalBytes > totalSent)
                            state.ETA = TimeSpan.FromSeconds((state.TotalBytes - totalSent) / (state.SpeedMbPerSec * 1_000_000.0));
                        else
                            state.ETA = TimeSpan.Zero;

                        lastReportedBytes = totalSent;
                        lastReportTime = now;
                        TransferProgress?.Invoke(state);
                    }
                }

                state.TransferredBytes = totalSent;
                state.SpeedMbPerSec = totalSent / Math.Max(sw.Elapsed.TotalSeconds, 0.001) / 1_000_000.0;
                state.ETA = TimeSpan.Zero;
                state.Status = TransferStatus.Done;
                TransferCompleted?.Invoke(state);
            }
            catch (OperationCanceledException)
            {
                state.Status = TransferStatus.Failed;
                TransferFailed?.Invoke(state);
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Transfer] Outgoing failed: {ex.Message}");
                state.Status = TransferStatus.Failed;
                TransferFailed?.Invoke(state);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
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
