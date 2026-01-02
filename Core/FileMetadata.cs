using System;

namespace Linage.Core
{
    /// <summary>
    /// Tracks individual file properties including path, hash, size, and modification date.
    /// Spec: 4.2.3
    /// </summary>
    public class FileMetadata
    {
        public Guid FileId { get; set; } = Guid.NewGuid();
        public string FilePath { get; set; } // Relative or absolute path
        public string FileHash { get; set; } // Content hash (SHA-256)
        public long FileSize { get; set; } // Size in bytes
        public DateTime ModifiedDate { get; set; }
        public bool IsDeleted { get; set; }

        public FileMetadata(string filePath, string fileHash, long fileSize, DateTime modifiedDate, bool isDeleted = false)
        {
            FilePath = filePath;
            FileHash = fileHash;
            FileSize = fileSize;
            ModifiedDate = modifiedDate;
            IsDeleted = isDeleted;
        }

        public string GetRelativePath(string rootPath)
        {
            if (string.IsNullOrEmpty(rootPath) || !FilePath.StartsWith(rootPath))
                return FilePath;

            var relative = FilePath.Substring(rootPath.Length);
            return relative.TrimStart(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);
        }

        public bool HasChanged(FileMetadata other)
        {
            if (other == null) return true;
            return this.FileHash != other.FileHash || this.IsDeleted != other.IsDeleted;
        }
    }
}