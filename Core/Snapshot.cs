using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Linage.Core
{
    /// <summary>
    /// Captures complete filesystem state at commit time.
    /// Spec: 4.2.2
    /// </summary>
    public class Snapshot
    {
        public Guid SnapshotId { get; set; } = Guid.NewGuid();
        public List<FileMetadata> Files { get; set; } = new List<FileMetadata>();
        public string Hash { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public int GetFileCount() => Files.Count;

        /// <summary>
        /// Computes a content-based hash for the snapshot.
        /// Typically a hash of all file hashes sorted by path.
        /// </summary>
        public string GetHash()
        {
            if (Files == null || !Files.Any()) return string.Empty;

            using (var sha256 = SHA256.Create())
            {
                var sb = new StringBuilder();
                foreach (var file in Files.OrderBy(f => f.FilePath))
                {
                    sb.Append(file.FileHash);
                    sb.Append(file.FilePath);
                }
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }
        }

        public bool ValidateIntegrity()
        {
            var calculatedHash = GetHash();
            return string.Equals(Hash, calculatedHash, StringComparison.OrdinalIgnoreCase);
        }
    }
}