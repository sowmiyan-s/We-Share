using System;
using System.ComponentModel;
using WeShare.Core.Models;

namespace WeShare.UI.Views
{
    public enum TransferFileItemStatus
    {
        Waiting,
        Transferring,
        Completed,
        Failed
    }

    public class ActiveBatchFileItem : INotifyPropertyChanged
    {
        private TransferFileItemStatus _status = TransferFileItemStatus.Waiting;
        private double _progressPercentage;
        private string _statusText = "Waiting in queue";

        public string FileId { get; set; } = "";
        public string FileName { get; set; } = "";
        public long FileSize { get; set; }
        public string FormattedSize => FileTransferState.FormatBytes(FileSize);
        public string RelativePath { get; set; } = "";

        public TransferFileItemStatus Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(IsWaiting));
                    OnPropertyChanged(nameof(IsTransferring));
                    OnPropertyChanged(nameof(IsCompleted));
                    OnPropertyChanged(nameof(IsFailed));
                    OnPropertyChanged(nameof(StatusBadgeColor));
                    OnPropertyChanged(nameof(StatusBadgeText));
                }
            }
        }

        public double ProgressPercentage
        {
            get => _progressPercentage;
            set
            {
                if (Math.Abs(_progressPercentage - value) > 0.01)
                {
                    _progressPercentage = value;
                    OnPropertyChanged(nameof(ProgressPercentage));
                    OnPropertyChanged(nameof(ProgressDisplay));
                }
            }
        }

        public string StatusText
        {
            get => _statusText;
            set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

        public bool IsWaiting => _status == TransferFileItemStatus.Waiting;
        public bool IsTransferring => _status == TransferFileItemStatus.Transferring;
        public bool IsCompleted => _status == TransferFileItemStatus.Completed;
        public bool IsFailed => _status == TransferFileItemStatus.Failed;

        public string StatusBadgeColor => _status switch
        {
            TransferFileItemStatus.Completed => "#10B981",
            TransferFileItemStatus.Transferring => "#A855F7",
            TransferFileItemStatus.Failed => "#EF4444",
            _ => "#64748B"
        };

        public string StatusBadgeText => _status switch
        {
            TransferFileItemStatus.Completed => "Completed",
            TransferFileItemStatus.Transferring => $"{ProgressPercentage:F0}%",
            TransferFileItemStatus.Failed => "Failed",
            _ => "Waiting"
        };

        public string ProgressDisplay => $"{ProgressPercentage:F0}%";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
