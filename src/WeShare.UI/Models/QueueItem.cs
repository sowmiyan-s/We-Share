using System;
using System.IO;
using System.Threading.Tasks;
using WeShare.Core.Models;

namespace WeShare.UI.Views
{
    public class QueueItem
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public string RelativePath { get; set; } = "";
        public long Size { get; set; }
        public string SizeDisplay => FileTransferState.FormatBytes(Size);
        public Func<Task<Stream>> OpenStream { get; set; } = null!;
        public Avalonia.Media.Imaging.Bitmap? Thumbnail { get; set; }
    }
}
