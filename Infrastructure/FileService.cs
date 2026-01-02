using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Linage.Core;

namespace Linage.Infrastructure
{
    /// <summary>
    /// Progress information for directory scanning operations
    /// </summary>
    public class ScanProgress
    {
        public int ProcessedFiles { get; set; }
        public string CurrentFile { get; set; }
    }

    public class FileService
    {
        private readonly HashService _hashService;
        private BlobStore _blobStore;
        private GitignoreParser _gitignoreParser;

        public FileService(HashService hashService)
        {
            _hashService = hashService ?? throw new ArgumentNullException(nameof(hashService));
        }

        public void InitializeBlobStore(string repositoryPath)
        {
            _blobStore = new BlobStore(repositoryPath, _hashService);

            // Initialize gitignore parser
            _gitignoreParser = new GitignoreParser(repositoryPath);

            // Load .gitignore if exists
            var gitignorePath = Path.Combine(repositoryPath, ".gitignore");
            if (File.Exists(gitignorePath))
            {
                _gitignoreParser.LoadFromFile(gitignorePath);
            }
            else
            {
                // Load default patterns if no .gitignore exists
                _gitignoreParser.LoadDefaultPatterns();
            }
        }

        public string GetFileContents(string filePath)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException("File not found.", filePath);
            return File.ReadAllText(filePath);
        }

        public async Task<string> GetFileContentsAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException("File not found.", filePath);

            using (var reader = new StreamReader(filePath))
            {
                return await reader.ReadToEndAsync().ConfigureAwait(false);
            }
        }

        public FileMetadata GetMetadata(string path, string rootPath)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("File not found.", path);

            var info = new FileInfo(path);
            var hash = _hashService.ComputeFileHash(path);

            // Store content in blob store if initialized
            if (_blobStore != null)
            {
                try
                {
                    _blobStore.StoreFile(path);
                }
                catch (Exception)
                {
                    // Log error but don't fail metadata retrieval
                }
            }

            // Note: Spec 4.2.3 requires FileId (Guid), FilePath, FileHash, FileSize, ModifiedDate, IsDeleted
            var metadata = new FileMetadata(
                filePath: path,
                fileHash: hash,
                fileSize: info.Length,
                modifiedDate: info.LastWriteTime
            );

            // Adjust path to be relative if needed, handled by FileMetadata.GetRelativePath logic
            // but we store the full path initially usually, or strictly relative?
            // Spec says "Relative or absolute path". Keeping as absolute from input, usually relative is better for portable repos.

            return metadata;
        }

        public async Task<FileMetadata> GetMetadataAsync(string path, string rootPath, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("File not found.", path);

            var info = new FileInfo(path);

            // Compute hash asynchronously
            var hash = await Task.Run(() => _hashService.ComputeFileHash(path), cancellationToken)
                .ConfigureAwait(false);

            // Store content in blob store if initialized
            if (_blobStore != null)
            {
                try
                {
                    await Task.Run(() => _blobStore.StoreFile(path), cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Log error but don't fail metadata retrieval
                }
            }

            var metadata = new FileMetadata(
                filePath: path,
                fileHash: hash,
                fileSize: info.Length,
                modifiedDate: info.LastWriteTime
            );

            return metadata;
        }

        [Obsolete("Use ScanDirectoryAsync for better performance and UI responsiveness")]
        public List<FileMetadata> ScanDirectory(string directoryPath, string rootPath, List<string> ignorePatterns = null)
        {
            var results = new List<FileMetadata>();
            if (!Directory.Exists(directoryPath)) return results;

            var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                // Explicitly ignore .git and .linage metadata folders
                if (file.IndexOf(".git", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    file.IndexOf(".linage", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                // Use GitignoreParser for ignore checking
                if (_gitignoreParser != null && _gitignoreParser.IsIgnored(file, false))
                    continue;

                results.Add(GetMetadata(file, rootPath));
            }
            return results;
        }

        /// <summary>
        /// Asynchronously scans a directory and computes file metadata with parallel hash computation.
        /// </summary>
        /// <param name="directoryPath">Directory to scan</param>
        /// <param name="rootPath">Root path for relative path calculation</param>
        /// <param name="ignorePatterns">Optional ignore patterns (currently unused, gitignore is used instead)</param>
        /// <param name="progress">Progress reporter for UI updates</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of file metadata</returns>
        public async Task<List<FileMetadata>> ScanDirectoryAsync(
            string directoryPath,
            string rootPath,
            List<string> ignorePatterns = null,
            IProgress<ScanProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(directoryPath))
                return new List<FileMetadata>();

            // Get all files first (this is I/O bound but fast)
            var allFiles = await Task.Run(() =>
            {
                try
                {
                    return Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);
                }
                catch (UnauthorizedAccessException)
                {
                    // Handle permission errors gracefully
                    return Array.Empty<string>();
                }
            }, cancellationToken).ConfigureAwait(false);

            // Filter files based on gitignore and exclusion rules
            var filesToProcess = await Task.Run(() =>
            {
                var filtered = new List<string>();
                foreach (var file in allFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Explicitly ignore .git and .linage metadata folders
                    if (file.IndexOf(".git", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        file.IndexOf(".linage", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;

                    // Use GitignoreParser for ignore checking
                    if (_gitignoreParser != null && _gitignoreParser.IsIgnored(file, false))
                        continue;

                    filtered.Add(file);
                }
                return filtered;
            }, cancellationToken).ConfigureAwait(false);

            // Process files in parallel with progress reporting
            var results = new ConcurrentBag<FileMetadata>();
            var processedCount = 0;

            await Task.Run(() =>
            {
                // Use Parallel.ForEach for CPU-bound hash computation
                var partitioner = Partitioner.Create(filesToProcess, EnumerablePartitionerOptions.NoBuffering);

                Parallel.ForEach(
                    partitioner,
                    new ParallelOptions
                    {
                        CancellationToken = cancellationToken,
                        MaxDegreeOfParallelism = Environment.ProcessorCount
                    },
                    file =>
                    {
                        try
                        {
                            if (!File.Exists(file))
                                return;

                            var info = new FileInfo(file);
                            var hash = _hashService.ComputeFileHash(file);

                            // Store content in blob store if initialized
                            if (_blobStore != null)
                            {
                                try
                                {
                                    _blobStore.StoreFile(file);
                                }
                                catch (Exception)
                                {
                                    // Log error but don't fail metadata retrieval
                                }
                            }

                            var metadata = new FileMetadata(
                                filePath: file,
                                fileHash: hash,
                                fileSize: info.Length,
                                modifiedDate: info.LastWriteTime
                            );

                            results.Add(metadata);

                            // Report progress
                            var currentCount = Interlocked.Increment(ref processedCount);
                            if (progress != null)
                            {
                                progress.Report(new ScanProgress
                                {
                                    ProcessedFiles = currentCount,
                                    CurrentFile = file
                                });
                            }
                        }
                        catch (IOException)
                        {
                            // Skip files that are locked or inaccessible
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // Skip files we don't have permission to read
                        }
                    });
            }, cancellationToken).ConfigureAwait(false);

            return results.ToList();
        }

        public void SaveFile(string path, string content)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            File.WriteAllText(path, content);
        }

        public async Task SaveFileAsync(string path, string content, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
            {
                await Task.Run(() => Directory.CreateDirectory(dir), cancellationToken)
                    .ConfigureAwait(false);
            }

            using (var writer = new StreamWriter(path))
            {
                await writer.WriteAsync(content).ConfigureAwait(false);
            }
        }

        public string GetContentByHash(string hash)
        {
            if (_blobStore == null)
                throw new InvalidOperationException("BlobStore not initialized. Call InitializeBlobStore first.");

            return _blobStore.GetContent(hash);
        }

        public async Task<string> GetContentByHashAsync(string hash, CancellationToken cancellationToken = default)
        {
            if (_blobStore == null)
                throw new InvalidOperationException("BlobStore not initialized. Call InitializeBlobStore first.");

            return await Task.Run(() => _blobStore.GetContent(hash), cancellationToken)
                .ConfigureAwait(false);
        }

        public bool BlobExists(string hash)
        {
            return _blobStore != null && _blobStore.Exists(hash);
        }

        public string StoreContent(string content)
        {
            if (_blobStore == null) return null;
            return _blobStore.StoreContent(content);
        }

        public string StoreContent(Stream content)
        {
            if (_blobStore == null) return null;
            return _blobStore.StoreContent(content);
        }

        public async Task<string> StoreContentAsync(string content, CancellationToken cancellationToken = default)
        {
            if (_blobStore == null) return null;
            return await Task.Run(() => _blobStore.StoreContent(content), cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<string> StoreContentAsync(Stream content, CancellationToken cancellationToken = default)
        {
            if (_blobStore == null) return null;
            return await Task.Run(() => _blobStore.StoreContent(content), cancellationToken)
                .ConfigureAwait(false);
        }

        public Stream GetContentStream(string hash)
        {
            if (_blobStore == null)
                throw new InvalidOperationException("BlobStore not initialized. Call InitializeBlobStore first.");

            return _blobStore.OpenRead(hash);
        }
    }
}
