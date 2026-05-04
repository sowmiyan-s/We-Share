namespace WeShare.Core.Transfer
{
    public class FileChunk
    {
        public string FileId { get; set; } = string.Empty;
        public long Offset { get; set; }
        public int Length { get; set; }
        public byte[] Data { get; set; } = [];
    }
}
