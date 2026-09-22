using System;
using System.IO;
using System.Linq;

namespace WeShare.UI.Views
{
    public class StagedWebFile
    {
        public string ClientId { get; set; } = "";
        public string ClientName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string FileName => Path.GetFileName(FilePath);
        public long Size { get; set; }
        public string SizeDisplay
        {
            get
            {
                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                int order = 0;
                double len = Size;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len /= 1024;
                }
                return $"{len:0.##} {sizes[order]}";
            }
        }

        public string FileTypeBadge
        {
            get
            {
                string ext = Path.GetExtension(FilePath).ToLowerInvariant().TrimStart('.');
                if (string.IsNullOrEmpty(ext)) return "DIR";
                return ext.ToUpperInvariant();
            }
        }
 
        public string FileColor
        {
            get
            {
                string ext = Path.GetExtension(FilePath).ToLowerInvariant().TrimStart('.');
                if (string.IsNullOrEmpty(ext)) return "#475569";
                if (ext is "png" or "jpg" or "jpeg" or "gif" or "webp" or "bmp" or "svg") return "#0ea5e9";
                if (ext is "mp4" or "mkv" or "avi" or "mov" or "webm" or "flv" or "wmv") return "#10b981";
                if (ext is "mp3" or "wav" or "flac" or "ogg" or "m4a" or "aac") return "#ec4899";
                if (ext is "pdf" or "doc" or "docx" or "txt" or "rtf" or "md" or "xls" or "xlsx" or "csv" or "ppt" or "pptx") return "#3b82f6";
                if (ext is "zip" or "rar" or "tar" or "gz" or "7z") return "#8b5cf6";
                if (ext is "exe" or "msi" or "bat" or "sh") return "#6366f1";
                return "#64748b";
            }
        }
    }
}
