using System;

namespace Linage.Infrastructure
{
    public class FileMetadata
    {
        public string Path { get; set; } = string.Empty;
        public string Inode { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime ModifiedTime { get; set; }
        public string Hash { get; set; } = string.Empty;
    }
}
