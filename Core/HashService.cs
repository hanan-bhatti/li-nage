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

        public string ComputeFileHash(string filePath)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException("File not found.", filePath);

            using (var hasher = CreateHasher())
            using (var stream = File.OpenRead(filePath))
            {
                var bytes = hasher.ComputeHash(stream);
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }
        }

        public string ComputeContentHash(byte[] content)
        {
            if (content == null) return string.Empty;
            using (var hasher = CreateHasher())
            {
                var bytes = hasher.ComputeHash(content);
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }
        }

        public string ComputeContentHash(Stream stream)
        {
            if (stream == null) return string.Empty;
            using (var hasher = CreateHasher())
            {
                var bytes = hasher.ComputeHash(stream);
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
            }
        }

        public string ComputeContentHash(string content)
        {
            if (content == null) return string.Empty;
            return ComputeContentHash(Encoding.UTF8.GetBytes(content));
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
    }
}
