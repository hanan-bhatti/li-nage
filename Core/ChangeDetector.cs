using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Linage.Infrastructure;

namespace Linage.Core
{
    /// <summary>
    /// Monitors changes and detects conflicts.
    /// Spec: 5.5
    /// </summary>
    public class ChangeDetector : IDisposable
    {
        private readonly FileWatcher _fileWatcher;
        private readonly HashService _hashService;

        // Thread-safe collection to track changed files
        private readonly ConcurrentDictionary<string, string> _dirtyFiles = new ConcurrentDictionary<string, string>();

        public ChangeDetector(string rootPath)
        {
            _fileWatcher = new FileWatcher(rootPath);
            _fileWatcher.OnFileChanged += OnFileChanged;
            _hashService = new HashService();
        }

        public void StartMonitoring()
        {
            _fileWatcher.Start();
        }

        public void StopMonitoring()
        {
            _fileWatcher.Stop();
        }

        private void OnFileChanged(object sender, FileChangeEvent e)
        {
            if (e.EventType == "DELETED")
            {
                // Mark as deleted instead of removing
                _dirtyFiles.AddOrUpdate(e.Path, "DELETED", (k, v) => "DELETED");
            }
            else
            {
                // For Created or Modified, we mark it as dirty.
                _dirtyFiles.AddOrUpdate(e.Path, e.EventType, (k, v) => e.EventType);
            }
        }

        /// <summary>
        /// Returns a list of files that have changed since the last checkpoint.
        /// </summary>
        public List<string> GetChangedFiles()
        {
            return new List<string>(_dirtyFiles.Keys);
        }

        /// <summary>
        /// Returns a dictionary of changed files and their status (NEW, MODIFIED, DELETED).
        /// </summary>
        public IDictionary<string, string> GetChanges()
        {
            return new Dictionary<string, string>(_dirtyFiles);
        }

        /// <summary>
        /// Actively scans directory to find changes compared to the provided snapshot or HEAD.
        /// </summary>
        [Obsolete("Use ScanForChangesAsync for better performance and UI responsiveness")]
        public void ScanForChanges(string rootPath, Commit headCommit, FileService fileService)
        {
            if (string.IsNullOrEmpty(rootPath) || fileService == null) return;

            // Get all files on disk
            var filesOnDisk = fileService.ScanDirectory(rootPath, rootPath);
            var filesOnDiskMap = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(var f in filesOnDisk) filesOnDiskMap.Add(GetRelativePath(rootPath, f.FilePath));

            // Get files in HEAD commit
            var filesInHead = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (headCommit?.Snapshot?.Files != null)
            {
                foreach (var f in headCommit.Snapshot.Files)
                {
                    // Normalize path keys
                    filesInHead[GetRelativePath(rootPath, f.FilePath)] = f.FileHash;
                }
            }

            foreach (var file in filesOnDisk)
            {
                var relativePath = GetRelativePath(rootPath, file.FilePath);

                // Check if new or modified
                if (!filesInHead.TryGetValue(relativePath, out var oldHash))
                {
                    // New file (Untracked)
                    _dirtyFiles.TryAdd(file.FilePath, "NEW");
                }
                else if (oldHash != file.FileHash)
                {
                    // Modified file
                    _dirtyFiles.TryAdd(file.FilePath, "MODIFIED");
                }
                else
                {
                    // File matches HEAD - Remove from dirty list if present
                    _dirtyFiles.TryRemove(file.FilePath, out _);
                }
            }

            // Check for deleted files
            if (headCommit?.Snapshot?.Files != null)
            {
                foreach (var f in headCommit.Snapshot.Files)
                {
                    // If file in HEAD is NOT on disk, it is DELETED
                    if (!filesOnDiskMap.Contains(f.FilePath))
                    {
                        var fullPath = Path.Combine(rootPath, f.FilePath);
                        _dirtyFiles.TryAdd(fullPath, "DELETED");
                    }
                }
            }
        }

        /// <summary>
        /// Asynchronously scans directory to find changes compared to the provided snapshot or HEAD.
        /// Provides progress reporting and cancellation support for better UI responsiveness.
        /// </summary>
        /// <param name="rootPath">Root directory to scan</param>
        /// <param name="headCommit">HEAD commit to compare against</param>
        /// <param name="fileService">File service for scanning operations</param>
        /// <param name="progress">Progress reporter for UI updates (reports status messages)</param>
        /// <param name="cancellationToken">Cancellation token to abort operation</param>
        public async Task ScanForChangesAsync(
            string rootPath,
            Commit headCommit,
            FileService fileService,
            IProgress<string> progress = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(rootPath) || fileService == null) return;

            try
            {
                progress?.Report("Starting file scan...");

                // Get all files on disk asynchronously
                var filesOnDisk = await fileService.ScanDirectoryAsync(
                    rootPath,
                    rootPath,
                    progress: new Progress<ScanProgress>(p =>
                    {
                        if (p.ProcessedFiles % 50 == 0)
                        {
                            progress?.Report($"Scanned {p.ProcessedFiles} files...");
                        }
                    }),
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                progress?.Report($"Building file index ({filesOnDisk.Count} files)...");

                // Build lookup structures in parallel
                var filesOnDiskMap = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var filesInHead = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                await Task.Run(() =>
                {
                    // Build disk file map
                    foreach (var f in filesOnDisk)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        filesOnDiskMap.Add(GetRelativePath(rootPath, f.FilePath));
                    }

                    // Build HEAD commit map
                    if (headCommit?.Snapshot?.Files != null)
                    {
                        foreach (var f in headCommit.Snapshot.Files)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            filesInHead[GetRelativePath(rootPath, f.FilePath)] = f.FileHash;
                        }
                    }
                }, cancellationToken).ConfigureAwait(false);

                progress?.Report($"Comparing files ({filesOnDisk.Count} files)...");

                // Process file comparisons in parallel batches
                await Task.Run(() =>
                {
                    var processedCount = 0;
                    var totalFiles = filesOnDisk.Count;

                    // Use Parallel.ForEach for CPU-bound comparison work
                    var partitioner = Partitioner.Create(filesOnDisk, EnumerablePartitionerOptions.NoBuffering);

                    Parallel.ForEach(
                        partitioner,
                        new ParallelOptions
                        {
                            CancellationToken = cancellationToken,
                            MaxDegreeOfParallelism = Environment.ProcessorCount
                        },
                        file =>
                        {
                            var relativePath = GetRelativePath(rootPath, file.FilePath);

                            // Check if new or modified
                            if (!filesInHead.TryGetValue(relativePath, out var oldHash))
                            {
                                // New file (Untracked)
                                _dirtyFiles.TryAdd(file.FilePath, "NEW");
                            }
                            else if (oldHash != file.FileHash)
                            {
                                // Modified file
                                _dirtyFiles.TryAdd(file.FilePath, "MODIFIED");
                            }
                            else
                            {
                                // File matches HEAD - Remove from dirty list if present
                                _dirtyFiles.TryRemove(file.FilePath, out _);
                            }

                            // Report progress every 50 files
                            var currentCount = Interlocked.Increment(ref processedCount);
                            if (currentCount % 50 == 0)
                            {
                                progress?.Report($"Compared {currentCount}/{totalFiles} files...");
                            }
                        });
                }, cancellationToken).ConfigureAwait(false);

                // Check for deleted files
                if (headCommit?.Snapshot?.Files != null)
                {
                    progress?.Report("Checking for deleted files...");

                    await Task.Run(() =>
                    {
                        foreach (var f in headCommit.Snapshot.Files)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            // If file in HEAD is NOT on disk, it is DELETED
                            if (!filesOnDiskMap.Contains(f.FilePath))
                            {
                                var fullPath = Path.Combine(rootPath, f.FilePath);
                                _dirtyFiles.TryAdd(fullPath, "DELETED");
                            }
                        }
                    }, cancellationToken).ConfigureAwait(false);
                }

                var changeCount = _dirtyFiles.Count;
                progress?.Report($"Scan complete: {changeCount} change(s) detected");
            }
            catch (OperationCanceledException)
            {
                progress?.Report("Scan cancelled by user");
                throw;
            }
            catch (Exception ex)
            {
                progress?.Report($"Scan failed: {ex.Message}");
                throw;
            }
        }

        private string GetRelativePath(string root, string fullPath)
        {
            if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return fullPath;
            var rel = fullPath.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            // Normalize to forward slashes for Git compatibility
            return rel.Replace('\\', '/');
        }

        /// <summary>
        /// Detects if a specific file has a conflict.
        /// A conflict occurs if the file on disk is different from what we expect (e.g. during a merge operation).
        /// </summary>
        public bool HasConflict(string filePath, string expectedHash)
        {
            if (!File.Exists(filePath)) return true; // Deleted?

            try
            {
                var currentHash = _hashService.ComputeFileHash(filePath);
                return !string.Equals(currentHash, expectedHash, StringComparison.OrdinalIgnoreCase);
            }
            catch (IOException)
            {
                // File locked or inaccessible
                return false;
            }
        }

        /// <summary>
        /// Asynchronously detects if a specific file has a conflict.
        /// </summary>
        public async Task<bool> HasConflictAsync(string filePath, string expectedHash, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(filePath)) return true; // Deleted?

            try
            {
                var currentHash = await Task.Run(() => _hashService.ComputeFileHash(filePath), cancellationToken)
                    .ConfigureAwait(false);
                return !string.Equals(currentHash, expectedHash, StringComparison.OrdinalIgnoreCase);
            }
            catch (IOException)
            {
                // File locked or inaccessible
                return false;
            }
        }

        public void Dispose()
        {
            if (_fileWatcher != null)
            {
                _fileWatcher.OnFileChanged -= OnFileChanged;
                _fileWatcher.Dispose();
            }
        }
    }
}
