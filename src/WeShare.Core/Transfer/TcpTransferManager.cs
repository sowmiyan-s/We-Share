using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WeShare.Core.Models;
using WeShare.Core.Security;

namespace WeShare.Core.Transfer
{
    public class TcpTransferManager
    {
        private readonly int _listenPort;
        private TcpListener? _listener;
        private CancellationTokenSource? _listenerCts;

        public int BoundPort { get; private set; }
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
            
            // Capture the actual port (useful if _listenPort was 0)
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
            using var rawStream = client.GetStream();

            FileTransferState? state = null;
            try
            {

                // 1. Read encrypted metadata length
                byte[] lenBuffer = new byte[4];
                if (!await ReadExactAsync(rawStream, lenBuffer, 4)) return;
                int encryptedMetaLength = BitConverter.ToInt32(lenBuffer, 0);

                // 2. Read encrypted metadata bytes
                byte[] encryptedMeta = new byte[encryptedMetaLength];
                if (!await ReadExactAsync(rawStream, encryptedMeta, encryptedMetaLength)) return;

                // 3. Decrypt metadata
                byte[] metaBytes = EncryptionHelper.Decrypt(encryptedMeta);
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
                bool accepted = true;
                if (TransferRequestCallback != null)
                {
                    accepted = await TransferRequestCallback(state).ConfigureAwait(false);
                }
                
                await rawStream.WriteAsync(new byte[] { accepted ? (byte)1 : (byte)0 }, 0, 1).ConfigureAwait(false);
                await rawStream.FlushAsync().ConfigureAwait(false);
                
                if (!accepted) return;

                TransferStarted?.Invoke(state);

                // 3. Save file
                string category = GetCategoryFolder(Path.GetExtension(state.FileName));
                string targetDir = Path.Combine(saveDirectory, category);
                Directory.CreateDirectory(targetDir);
                
                string dest = GetUniqueFilePath(targetDir, state.FileName);
                state.FilePath = dest;

                using var fs = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
                
                // 4. Decrypt file data
                using var fileCryptoReader = EncryptionHelper.CreateDecryptionStream(rawStream);

                byte[] buffer = new byte[81920];
                long totalRead = 0;
                long lastReportedBytes = 0;
                DateTime lastReportTime = DateTime.UtcNow;
                int read;

                while ((read = await fileCryptoReader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                {
                    await fs.WriteAsync(buffer, 0, read).ConfigureAwait(false);
                    totalRead += read;
                    state.TransferredBytes = totalRead;

                    var now = DateTime.UtcNow;
                    var elapsed = (now - lastReportTime).TotalSeconds;
                    if (elapsed >= 0.25)
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

                state.TransferredBytes = totalRead;
                state.Status = TransferStatus.Done;
                TransferCompleted?.Invoke(state);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Transfer] Incoming failed: {ex.Message}");
                if (state != null) 
                {
                    state.Status = TransferStatus.Failed;
                    state.ErrorMessage = ex.Message;
                }
                TransferFailed?.Invoke(state ?? new FileTransferState { Status = TransferStatus.Failed, ErrorMessage = ex.Message });
            }
        }

        // ── Send ───────────────────────────────────────────────────────────────
        public async Task SendFileAsync(string targetIp, int targetPort, string fileName, Stream fileStream, long totalBytes,
                                        CancellationToken cancellationToken = default)
        {
            var state = new FileTransferState
            {
                FileName   = fileName,
                TotalBytes = totalBytes,
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

                // Apply a 30-second timeout ONLY for the connection handshake.
                // Do NOT apply it to the entire transfer — large files would always time out.
                using (var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    connectCts.CancelAfter(TimeSpan.FromSeconds(30));
                    await client.ConnectAsync(targetIp, targetPort, connectCts.Token).ConfigureAwait(false);
                }

                using var rawStream = client.GetStream();

                // 1. Encrypt and send metadata
                var json = JsonSerializer.Serialize(state);
                var metaBytes = Encoding.UTF8.GetBytes(json);
                var encryptedMeta = EncryptionHelper.Encrypt(metaBytes);
                
                byte[] lenBytes = BitConverter.GetBytes(encryptedMeta.Length);
                await rawStream.WriteAsync(lenBytes, 0, 4, cancellationToken).ConfigureAwait(false);
                await rawStream.WriteAsync(encryptedMeta, 0, encryptedMeta.Length, cancellationToken).ConfigureAwait(false);
                await rawStream.FlushAsync(cancellationToken).ConfigureAwait(false);

                // 2. Read response (unencrypted handshake response)
                byte[] respBuffer = new byte[1];
                int r = await rawStream.ReadAsync(respBuffer, 0, 1, cancellationToken).ConfigureAwait(false);
                if (r == 0 || respBuffer[0] == 0)
                {
                    state.Status = TransferStatus.Failed;
                    TransferFailed?.Invoke(state);
                    return;
                }

                // 3. Encrypt and send file data
                using (var fileCryptoWriter = EncryptionHelper.CreateEncryptionStream(rawStream))
                {
                    byte[] buffer = new byte[81920];
                    int read;
                    long totalSent = 0;
                    long lastReportedBytes = 0;
                    DateTime lastReportTime = DateTime.UtcNow;

                    while ((read = await fileStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
                    {
                        await fileCryptoWriter.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false);
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

                            lastReportedBytes = totalSent;
                            lastReportTime = now;
                            TransferProgress?.Invoke(state);
                        }
                    }
                    await fileCryptoWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
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
                if (ex is OperationCanceledException) throw;
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static string GetCategoryFolder(string ext)
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
                int read = await stream.ReadAsync(buffer, totalRead, count - totalRead).ConfigureAwait(false);
                if (read == 0) return false;
                totalRead += read;
            }
            return true;
        }

        private static readonly object _pathLock = new();
        private static string GetUniqueFilePath(string dir, string filename)
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
    }
}
