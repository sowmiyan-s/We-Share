using System;
using System.IO;

namespace WeShare.Core.Models
{
    public class FileItemModel
    {
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public long Size { get; set; }
        public string SizeString => FormatBytes(Size);

        public static string FormatBytes(long bytes)
        {
            string[] Suffix = { "B", "KB", "MB", "GB", "TB" };
            int i;
            double dblSByte = bytes;
            for (i = 0; i < Suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0;
            }

            return $"{dblSByte:0.##} {Suffix[i]}";
        }
    }
}
