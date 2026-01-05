using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly string _rootPath;

        // Thread-safe collection to track changed files
        private readonly ConcurrentDictionary<string, string> _dirtyFiles = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public ChangeDetector(string rootPath)
        {
            _rootPath = rootPath;
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
            // Normalize path to relative with forward slashes (consistent with ScanForChangesAsync)
            var relativePath = GetRelativePath(_rootPath, e.Path);

            DebugLogger.Trace($"FileWatcher event: {e.EventType} -> {relativePath}");

            if (e.EventType == "DELETED")
            {
                // Mark as deleted instead of removing
                _dirtyFiles.AddOrUpdate(relativePath, "DELETED", (k, v) => "DELETED");
            }
            else
            {
                // For Created or Modified, we mark it as dirty.
                _dirtyFiles.AddOrUpdate(relativePath, e.EventType, (k, v) => e.EventType);
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

            // Get all files on disk - now returns relative paths with forward slashes
            var filesOnDisk = fileService.ScanDirectory(rootPath, rootPath);
            var filesOnDiskMap = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(var f in filesOnDisk) filesOnDiskMap.Add(f.FilePath);

            // Get files in HEAD commit - normalize paths
            var filesInHead = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (headCommit?.Snapshot?.Files != null)
            {
                foreach (var f in headCommit.Snapshot.Files)
                {
                    // Normalize path to forward slashes
                    var normalizedPath = f.FilePath.Replace('\\', '/');
                    filesInHead[normalizedPath] = f.FileHash;
                }
            }

            foreach (var file in filesOnDisk)
            {
                var relativePath = file.FilePath; // Already relative from ScanDirectory

                // Check if new or modified
                if (!filesInHead.TryGetValue(relativePath, out var oldHash))
                {
                    // New file (Untracked)
                    _dirtyFiles.TryAdd(relativePath, "NEW");
                }
                else if (!string.Equals(oldHash, file.FileHash, StringComparison.OrdinalIgnoreCase))
                {
                    // Modified file
                    _dirtyFiles.TryAdd(relativePath, "MODIFIED");
                }
                else
                {
                    // File matches HEAD - Remove from dirty list if present
                    _dirtyFiles.TryRemove(relativePath, out _);
                }
            }

            // Check for deleted files
            if (headCommit?.Snapshot?.Files != null)
            {
                foreach (var f in headCommit.Snapshot.Files)
                {
                    var normalizedPath = f.FilePath.Replace('\\', '/');
                    // If file in HEAD is NOT on disk, it is DELETED
                    if (!filesOnDiskMap.Contains(normalizedPath))
                    {
                        _dirtyFiles.TryAdd(normalizedPath, "DELETED");
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
            DebugLogger.Info("ChangeDetector.ScanForChangesAsync called");
            DebugLogger.Trace($"  -> rootPath: {rootPath}");
            DebugLogger.Trace($"  -> headCommit: {headCommit?.CommitHash?.Substring(0, 8) ?? "null"}...");
            DebugLogger.Trace($"  -> headCommit snapshot files: {headCommit?.Snapshot?.Files?.Count ?? 0}");

            if (string.IsNullOrEmpty(rootPath) || fileService == null)
            {
                DebugLogger.Warn("  -> Aborting: rootPath or fileService is null");
                return;
            }

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

                DebugLogger.Trace($"  -> Files on disk: {filesOnDisk.Count}");
                cancellationToken.ThrowIfCancellationRequested();

                progress?.Report($"Building file index ({filesOnDisk.Count} files)...");

                // Build lookup structures in parallel
                var filesOnDiskMap = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var filesInHead = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                await Task.Run(() =>
                {
                    // Build disk file map - FilePath is already relative from ScanDirectoryAsync
                    foreach (var f in filesOnDisk)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        filesOnDiskMap.Add(f.FilePath);
                    }

                    // Build HEAD commit map - paths should already be relative with forward slashes
                    if (headCommit?.Snapshot?.Files != null)
                    {
                        foreach (var f in headCommit.Snapshot.Files)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            // Normalize path to forward slashes for consistent comparison
                            var normalizedPath = f.FilePath.Replace('\\', '/');
                            filesInHead[normalizedPath] = f.FileHash;
                        }
                    }
                }, cancellationToken).ConfigureAwait(false);

                DebugLogger.Trace($"  -> Files in HEAD map: {filesInHead.Count}");

                // Log a few examples for debugging path comparison issues
                if (filesOnDisk.Count > 0 && filesInHead.Count > 0)
                {
                    var diskSample = filesOnDisk.Take(3).Select(f => f.FilePath).ToList();
                    var headSample = filesInHead.Keys.Take(3).ToList();
                    DebugLogger.Trace($"  -> Sample disk paths: {string.Join(", ", diskSample)}");
                    DebugLogger.Trace($"  -> Sample HEAD paths: {string.Join(", ", headSample)}");
                }

                progress?.Report($"Comparing files ({filesOnDisk.Count} files)...");

                // Clear dirty files before comparison to ensure clean state
                DebugLogger.Trace($"  -> Dirty files before comparison: {_dirtyFiles.Count}");

                // Process file comparisons in parallel batches
                int newCount = 0, modifiedCount = 0, matchedCount = 0;
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
                            // Check if new or modified
                            // filesOnDisk now contains relative paths with forward slashes
                            var relativePath = file.FilePath; // Already relative from ScanDirectoryAsync

                            if (!filesInHead.TryGetValue(relativePath, out var oldHash))
                            {
                                // New file (Untracked)
                                _dirtyFiles.TryAdd(relativePath, "NEW");
                                Interlocked.Increment(ref newCount);
                            }
                            else if (!string.Equals(oldHash, file.FileHash, StringComparison.OrdinalIgnoreCase))
                            {
                                // Modified file
                                _dirtyFiles.TryAdd(relativePath, "MODIFIED");
                                Interlocked.Increment(ref modifiedCount);
                                DebugLogger.Trace($"  -> MODIFIED: {relativePath}");
                                DebugLogger.Trace($"     Disk hash: {file.FileHash?.Substring(0, 8) ?? "null"}...");
                                DebugLogger.Trace($"     HEAD hash: {oldHash?.Substring(0, 8) ?? "null"}...");
                            }
                            else
                            {
                                // File matches HEAD - Remove from dirty list if present
                                if (_dirtyFiles.TryRemove(relativePath, out _))
                                {
                                    DebugLogger.Trace($"  -> MATCHED (removed from dirty): {relativePath}");
                                }
                                Interlocked.Increment(ref matchedCount);
                            }

                            // Report progress every 50 files
                            var currentCount = Interlocked.Increment(ref processedCount);
                            if (currentCount % 50 == 0)
                            {
                                progress?.Report($"Compared {currentCount}/{totalFiles} files...");
                            }
                        });
                }, cancellationToken).ConfigureAwait(false);

                DebugLogger.Info($"  -> Comparison results: NEW={newCount}, MODIFIED={modifiedCount}, MATCHED={matchedCount}");

                // Check for deleted files
                if (headCommit?.Snapshot?.Files != null)
                {
                    progress?.Report("Checking for deleted files...");

                    int deletedCount = 0;
                    await Task.Run(() =>
                    {
                        foreach (var f in headCommit.Snapshot.Files)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            // Normalize path for comparison
                            var normalizedPath = f.FilePath.Replace('\\', '/');

                            // If file in HEAD is NOT on disk, it is DELETED
                            if (!filesOnDiskMap.Contains(normalizedPath))
                            {
                                // Store as relative path (consistent with other entries)
                                _dirtyFiles.TryAdd(normalizedPath, "DELETED");
                                deletedCount++;
                                DebugLogger.Trace($"  -> DELETED: {normalizedPath}");
                            }
                        }
                    }, cancellationToken).ConfigureAwait(false);

                    DebugLogger.Info($"  -> Deleted files: {deletedCount}");
                }

                var changeCount = _dirtyFiles.Count;
                DebugLogger.Info($"  -> Total dirty files after scan: {changeCount}");
                progress?.Report($"Scan complete: {changeCount} change(s) detected");
            }
            catch (OperationCanceledException)
            {
                DebugLogger.Warn("  -> Scan cancelled by user");
                progress?.Report("Scan cancelled by user");
                throw;
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"  -> Scan failed: {ex.Message}");
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
