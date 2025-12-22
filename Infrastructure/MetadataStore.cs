using System;
using System.Collections.Generic;

namespace Linage.Infrastructure
{
    public class MetadataStore
    {
        public string StorageType { get; set; } = "SQLite";
        public List<FileMetadata> StoredMetadata { get; set; } = new List<FileMetadata>();
    }
}
