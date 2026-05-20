using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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

    public class FileTransferState : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _fileId = Guid.NewGuid().ToString();
        public string FileId 
        { 
            get => _fileId; 
            set { if (_fileId != value) { _fileId = value; OnPropertyChanged(); } } 
        }

        private string _sessionId = string.Empty;
        public string SessionId 
        { 
            get => _sessionId; 
            set { if (_sessionId != value) { _sessionId = value; OnPropertyChanged(); } } 
        }

        private string _fileName = string.Empty;
        public string FileName 
        { 
            get => _fileName; 
            set { if (_fileName != value) { _fileName = value; OnPropertyChanged(); } } 
        }

        private string _filePath = string.Empty;
        public string FilePath 
        { 
            get => _filePath; 
            set { if (_filePath != value) { _filePath = value; OnPropertyChanged(); } } 
        }

        private string _peerName = string.Empty;
        public string PeerName 
        { 
            get => _peerName; 
            set { if (_peerName != value) { _peerName = value; OnPropertyChanged(); } } 
        }

        private string _senderName = string.Empty;
        public string SenderName 
        { 
            get => _senderName; 
            set { if (_senderName != value) { _senderName = value; OnPropertyChanged(); } } 
        }

        private string _remoteIp = string.Empty;
        public string RemoteIp 
        { 
            get => _remoteIp; 
            set { if (_remoteIp != value) { _remoteIp = value; OnPropertyChanged(); } } 
        }

        private long _totalBytes;
        public long TotalBytes 
        { 
            get => _totalBytes; 
            set 
            { 
                if (_totalBytes != value) 
                { 
                    _totalBytes = value; 
                    OnPropertyChanged(); 
                    OnPropertyChanged(nameof(ProgressPercentage));
                    OnPropertyChanged(nameof(FileSizeDisplay));
                } 
            } 
        }

        private long _transferredBytes;
        public long TransferredBytes 
        { 
            get => _transferredBytes; 
            set 
            { 
                if (_transferredBytes != value) 
                { 
                    _transferredBytes = value; 
                    OnPropertyChanged(); 
                    OnPropertyChanged(nameof(ProgressPercentage));
                } 
            } 
        }

        private TransferStatus _status = TransferStatus.Waiting;
        public TransferStatus Status 
        { 
            get => _status; 
            set { if (_status != value) { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusIcon)); } } 
        }

        private TransferDirection _direction = TransferDirection.Received;
        public TransferDirection Direction 
        { 
            get => _direction; 
            set { if (_direction != value) { _direction = value; OnPropertyChanged(); } } 
        }

        private DateTime _timestamp = DateTime.UtcNow;
        public DateTime Timestamp 
        { 
            get => _timestamp; 
            set { if (_timestamp != value) { _timestamp = value; OnPropertyChanged(); } } 
        }

        private string? _errorMessage;
        public string? ErrorMessage 
        { 
            get => _errorMessage; 
            set { if (_errorMessage != value) { _errorMessage = value; OnPropertyChanged(); } } 
        }

        // Progress percentage (computed)
        public double ProgressPercentage => TotalBytes == 0 ? 0 : (double)TransferredBytes / TotalBytes * 100;

        private double _speedMbPerSec;
        public double SpeedMbPerSec 
        { 
            get => _speedMbPerSec; 
            set { if (_speedMbPerSec != value) { _speedMbPerSec = value; OnPropertyChanged(); } } 
        }

        private TimeSpan _eta;
        public TimeSpan ETA 
        { 
            get => _eta; 
            set { if (_eta != value) { _eta = value; OnPropertyChanged(); } } 
        }

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
