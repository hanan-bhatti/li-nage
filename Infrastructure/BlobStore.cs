using System;
using System.IO;
using System.Text;
using Linage.Core;

namespace Linage.Infrastructure
{
    /// <summary>
    /// Blob storage for file content versioning.
    /// Stores file content by hash for efficient retrieval during merge operations.
    /// </summary>
    public class BlobStore
    {
        private readonly string _blobStorePath;
        private readonly HashService _hashService;

        public BlobStore(string repositoryPath, HashService hashService)
        {
            if (string.IsNullOrEmpty(repositoryPath))
                throw new ArgumentNullException(nameof(repositoryPath));
            
            _hashService = hashService ?? throw new ArgumentNullException(nameof(hashService));
            
            // Store blobs in .linage/objects directory (similar to Git)
            _blobStorePath = Path.Combine(repositoryPath, ".linage", "objects");
            
            // Create directory if it doesn't exist
            if (!Directory.Exists(_blobStorePath))
            {
                Directory.CreateDirectory(_blobStorePath);
            }
        }

        /// <summary>
        /// Store file content and return its hash.
        /// </summary>
        public string StoreContent(string content)
        {
            if (content == null) content = string.Empty;
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
            {
                return StoreContent(stream);
            }
        }

        /// <summary>
        /// Store content from a stream (Large File Support).
        /// </summary>
        public string StoreContent(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            // Compute hash from stream (reset position after)
            var hash = _hashService.ComputeContentHash(stream);
            if (stream.CanSeek) stream.Position = 0;
            
            // Create subdirectory using first 2 chars of hash (like Git)
            var subDir = hash.Substring(0, 2);
            var fileName = hash.Substring(2);
            var dirPath = Path.Combine(_blobStorePath, subDir);
            var filePath = Path.Combine(dirPath, fileName);

            // Only write if doesn't exist (content-addressable storage)
            if (!File.Exists(filePath))
            {
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }
                
                using (var fileStream = File.Create(filePath))
                {
                    stream.CopyTo(fileStream);
                }
            }

            return hash;
        }

        /// <summary>
        /// Store file content from a file path using streams.
        /// </summary>
        public string StoreFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            using (var stream = File.OpenRead(filePath))
            {
                return StoreContent(stream);
            }
        }

        /// <summary>
        /// Retrieve content by hash.
        /// </summary>
        public string GetContent(string hash)
        {
             // For small files, string is fine. For large, use OpenRead.
             using (var stream = OpenRead(hash))
             using (var reader = new StreamReader(stream, Encoding.UTF8))
             {
                 return reader.ReadToEnd();
             }
        }

        /// <summary>
        /// Open a stream to read blob content (Large File Support).
        /// </summary>
        public Stream OpenRead(string hash)
        {
            if (string.IsNullOrEmpty(hash))
                throw new ArgumentException("Hash cannot be empty", nameof(hash));

            var subDir = hash.Substring(0, 2);
            var fileName = hash.Substring(2);
            var filePath = Path.Combine(_blobStorePath, subDir, fileName);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Blob not found for hash: {hash}");
            }

            return File.OpenRead(filePath);
        }

        /// <summary>
        /// Check if blob exists for given hash.
        /// </summary>
        public bool Exists(string hash)
        {
            if (string.IsNullOrEmpty(hash))
                return false;

            var subDir = hash.Substring(0, 2);
            var fileName = hash.Substring(2);
            var filePath = Path.Combine(_blobStorePath, subDir, fileName);

            return File.Exists(filePath);
        }

        /// <summary>
        /// Get the size of stored blob in bytes.
        /// </summary>
        public long GetBlobSize(string hash)
        {
            if (string.IsNullOrEmpty(hash))
                return 0;

            var subDir = hash.Substring(0, 2);
            var fileName = hash.Substring(2);
            var filePath = Path.Combine(_blobStorePath, subDir, fileName);

            if (!File.Exists(filePath))
                return 0;

            var fileInfo = new FileInfo(filePath);
            return fileInfo.Length;
        }

        /// <summary>
        /// Delete a blob by hash (use with caution).
        /// </summary>
        public bool DeleteBlob(string hash)
        {
            if (string.IsNullOrEmpty(hash))
                return false;

            var subDir = hash.Substring(0, 2);
            var fileName = hash.Substring(2);
            var filePath = Path.Combine(_blobStorePath, subDir, fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get total number of blobs stored.
        /// </summary>
        public int GetBlobCount()
        {
            if (!Directory.Exists(_blobStorePath))
                return 0;

            int count = 0;
            var subDirs = Directory.GetDirectories(_blobStorePath);
            
            foreach (var subDir in subDirs)
            {
                count += Directory.GetFiles(subDir).Length;
            }

            return count;
        }

        /// <summary>
        /// Get total size of all blobs in bytes.
        /// </summary>
        public long GetTotalSize()
        {
            if (!Directory.Exists(_blobStorePath))
                return 0;

            long totalSize = 0;
            var subDirs = Directory.GetDirectories(_blobStorePath);
            
            foreach (var subDir in subDirs)
            {
                var files = Directory.GetFiles(subDir);
                foreach (var file in files)
                {
                    totalSize += new FileInfo(file).Length;
                }
            }

            return totalSize;
        }
    }
}
