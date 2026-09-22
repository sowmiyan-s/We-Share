using System.ComponentModel;
using WeShare.Core.Models;

namespace WeShare.UI.Views
{
    public class BatchCheckItem : INotifyPropertyChanged
    {
        private bool _isSelected = true;
        public string FileId { get; set; } = "";
        public string FileName { get; set; } = "";
        public string RelativePath { get; set; } = "";
        public long Size { get; set; }
        public string FormattedSize => FileTransferState.FormatBytes(Size);

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
