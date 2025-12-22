using System;
using System.Collections.Generic;
using Linage.Infrastructure;

namespace Linage.Core
{
    public class Snapshot
    {
        public DateTime Timestamp { get; set; }
        public string Hash { get; set; } = string.Empty;
        public List<FileMetadata> Files { get; set; } = new List<FileMetadata>();
    }
}
