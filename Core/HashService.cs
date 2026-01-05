using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Linage.Core
{
    /// <summary>
    /// Cryptographic hashing for content integrity.
    /// Spec: 5.6
    /// Thread-safe implementation that creates new hash instances per operation.
    /// All hashes are computed with normalized line endings (LF) for cross-platform consistency.
    /// </summary>
    public class HashService
    {
        private readonly string _algorithm;

        public HashService(string algorithm = "SHA256")
        {
            _algorithm = algorithm;
        }

        private HashAlgorithm CreateHasher()
        {
            if (string.Equals(_algorithm, "SHA1", StringComparison.OrdinalIgnoreCase))
                return SHA1.Create();
            else
                return SHA256.Create(); // Default per spec
        }

        /// <summary>
        /// Compute hash of a file with normalized line endings (CRLF -> LF).
        /// This ensures consistent hashes across platforms.
        /// </summary>
        public string ComputeFileHash(string filePath)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException("File not found.", filePath);

            // Read file content and normalize line endings for consistent hashing
            var content = File.ReadAllText(filePath);
            var normalizedContent = NormalizeLineEndings(content);
            var bytes = Encoding.UTF8.GetBytes(normalizedContent);

            using (var hasher = CreateHasher())
            {
                var hashBytes = hasher.ComputeHash(bytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// Compute hash of raw bytes (no normalization - use for binary data).
        /// </summary>
        public string ComputeContentHash(byte[] content)
        {
            if (content == null) return string.Empty;
            using (var hasher = CreateHasher())
            {
                var bytes = hasher.ComputeHash(content);
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// Compute hash of a stream (no normalization - use for binary data).
        /// </summary>
        public string ComputeContentHash(Stream stream)
        {
            if (stream == null) return string.Empty;
            using (var hasher = CreateHasher())
            {
                var bytes = hasher.ComputeHash(stream);
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// Compute hash of string content with normalized line endings.
        /// </summary>
        public string ComputeContentHash(string content)
        {
            if (content == null) return string.Empty;
            var normalizedContent = NormalizeLineEndings(content);
            return ComputeContentHash(Encoding.UTF8.GetBytes(normalizedContent));
        }

        public string ComputeStringHash(string content)
        {
            return ComputeContentHash(content);
        }

        public bool VerifyHash(string content, string expectedHash)
        {
            var actualHash = ComputeContentHash(content);
            return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Normalize line endings to LF (Unix style) for consistent hashing across platforms.
        /// CRLF (Windows) and CR (old Mac) are converted to LF.
        /// </summary>
        private static string NormalizeLineEndings(string content)
        {
            if (string.IsNullOrEmpty(content)) return content;

            // Replace CRLF with LF first, then replace any remaining CR with LF
            return content.Replace("\r\n", "\n").Replace("\r", "\n");
        }
    }
}
