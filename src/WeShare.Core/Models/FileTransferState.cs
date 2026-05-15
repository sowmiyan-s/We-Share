using System;

namespace WeShare.Core.Models
{
    public enum TransferStatus
    {
        Waiting,
        Sending,
        Receiving,
        Paused,
        Failed,
        Done
    }

    public enum TransferDirection
    {
        Sent,
        Received
    }

    public class FileTransferState
    {
        public string FileId { get; set; } = Guid.NewGuid().ToString();
        public string SessionId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string PeerName { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty; // Added
        public string RemoteIp { get; set; } = string.Empty;   // Added
        public long TotalBytes { get; set; }
        public long TransferredBytes { get; set; }
        public TransferStatus Status { get; set; } = TransferStatus.Waiting;
        public TransferDirection Direction { get; set; } = TransferDirection.Received;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? ErrorMessage { get; set; }

        // Progress metadata (computed)
        public double ProgressPercentage => TotalBytes == 0 ? 0 : (double)TransferredBytes / TotalBytes * 100;
        public double SpeedMbPerSec { get; set; }
        public TimeSpan ETA { get; set; }

        // Display helpers
        public string FileSizeDisplay => FormatBytes(TotalBytes);
        public string StatusIcon => Status switch
        {
            TransferStatus.Done      => "✓",
            TransferStatus.Failed    => "✕",
            TransferStatus.Sending   => "↑",
            TransferStatus.Receiving => "↓",
            TransferStatus.Paused    => "⏸",
            _                        => "…"
        };

        public static string FormatBytes(long bytes)
        {
            if (bytes < 1024)         return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024L * 1024 * 1024) return $"{bytes / 1024.0 / 1024:F1} MB";
            return $"{bytes / 1024.0 / 1024 / 1024:F2} GB";
        }
    }
}
