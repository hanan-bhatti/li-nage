using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Linage.Core;
using Linage.Core.Authentication;
using Linage.Infrastructure;

namespace Linage.Controllers
{
    public class VersionController
    {
        public string Status { get; set; } = "Idle";
        public string GetStatus()
        {
            return Status;
        }

        /// <summary>
        /// Asynchronously scans for file changes with progress reporting.
        /// </summary>
        public async Task ScanChangesAsync(IProgress<string> progress = null, System.Threading.CancellationToken cancellationToken = default)
        {
            if (ChangeDetector == null || GraphService == null) return;

            var head = GraphService.GetCurrentBranch()?.HeadCommit;

            if (!string.IsNullOrEmpty(_currentRootPath))
            {
                await ChangeDetector.ScanForChangesAsync(_currentRootPath, head, _fileService, progress, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        public DateTime LastRunTime { get; set; }

        public VersionGraphService GraphService { get; private set; }
        public ChangeDetector ChangeDetector { get; private set; }
        public RemoteService RemoteService { get; private set; }
        public AuthenticationService AuthService { get; private set; }
        
        private readonly MetadataStore _metadataStore;
        private readonly FileService _fileService;
        private readonly HashService _hashService;
        private readonly CredentialStore _credentialStore;
        private RemoteController _remoteController; // Not readonly - initialized in LoadProject
        private readonly AuthController _authController;
        private string _currentRootPath;

        public VersionController()
        {
            // Production Dependency Injection Root
            try
            {
                var dbContext = new LiNageDbContext();
                
                _metadataStore = new MetadataStore(dbContext);
                _hashService = new HashService();
                _fileService = new FileService(_hashService);
                GraphService = new VersionGraphService(_metadataStore);
                
                // Phase 3 Integration
                _credentialStore = new CredentialStore();
                AuthService = new AuthenticationService(_credentialStore);
                _authController = new AuthController(AuthService);
                
                RemoteService = new RemoteService(_metadataStore);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Failed to initialize database context. Ensure SQL Server is running and configured properly. " +
                    "Check the connection string in App.config (LinageDbContext). " +
                    "If using LocalDB, verify it's installed and the service is running.",
                    ex);
            }
        }

        public GitImportService CreateGitImporter()
        {
            return new GitImportService(_metadataStore, _hashService, _fileService, GraphService);
        }

        public async Task LoadProjectAsync(string rootPath)
        {
            if (string.IsNullOrEmpty(rootPath)) return;
            _currentRootPath = rootPath;

            // Initialize BlobStore for file content storage
            _fileService.InitializeBlobStore(rootPath);

            // Inject FileService into GraphService for merge operations
            GraphService.SetFileService(_fileService);
            
            // Initialize GraphService (async loading of commits)
            await GraphService.InitializeAsync();

            // Re-initialize transports with the correct path
            // (In a more complex DI setup this would be handled differently)

            // Initialize RemoteController with AuthController
            _remoteController = new RemoteController(_authController);

            ChangeDetector = new ChangeDetector(rootPath);
            ChangeDetector.StartMonitoring();

            // Load existing branches and commits from database
            var branches = await GraphService.GetAllBranchesAsync();
            if (branches != null && branches.Count > 0)
            {
                // Try to load 'main' branch first, fallback to 'master', then first available
                var mainBranch = await GraphService.GetBranchAsync("main");
                if (mainBranch != null)
                {
                    await GraphService.SwitchBranchAsync("main");
                }
                else
                {
                    var masterBranch = await GraphService.GetBranchAsync("master");
                    if (masterBranch != null)
                    {
                        await GraphService.SwitchBranchAsync("master");
                    }
                    else if (branches.Count > 0)
                    {
                        // Switch to first available branch
                        await GraphService.SwitchBranchAsync(branches[0].BranchName);
                    }
                }
            }

            Status = $"Loaded {rootPath}";
        }

        public async Task CreateCommitAsync(string message, List<string> selectedFiles)
        {
            DebugLogger.Info($"VersionController.CreateCommitAsync called");
            DebugLogger.Info($"  -> Message: {message}");
            DebugLogger.Info($"  -> Selected files count: {selectedFiles?.Count ?? 0}");

            if (GraphService.GetCurrentBranch() == null)
            {
                DebugLogger.Trace("  -> No current branch, checking for 'main' branch");
                // Try to get existing 'main' branch first
                var mainBranch = await GraphService.GetBranchAsync("main");
                if (mainBranch != null)
                {
                    DebugLogger.Trace("  -> Found 'main' branch, switching to it");
                    await GraphService.SwitchBranchAsync("main");
                }
                else if (GraphService.GetCommitHistory().Count == 0)
                {
                    DebugLogger.Trace("  -> No commits exist, creating 'main' branch");
                    // Create 'main' only if it doesn't exist
                    await GraphService.CreateBranchAsync("main");
                    await GraphService.SwitchBranchAsync("main");
                }
            }

            var currentBranch = GraphService.GetCurrentBranch();
            DebugLogger.Info($"  -> Current branch: {currentBranch?.BranchName ?? "null"}");

            // Create Snapshot
            var snapshot = new Snapshot { SnapshotId = Guid.NewGuid(), Timestamp = DateTime.Now };
            snapshot.Files = new List<FileMetadata>();

            // Process files
            DebugLogger.Trace("  -> Processing selected files:");
            foreach (var file in selectedFiles)
            {
                // file might be a relative path, construct full path
                var fullPath = file;
                if (!Path.IsPathRooted(file) && !string.IsNullOrEmpty(_currentRootPath))
                {
                    fullPath = Path.Combine(_currentRootPath, file.Replace('/', Path.DirectorySeparatorChar));
                }

                DebugLogger.Trace($"     - File: {file} -> FullPath: {fullPath}");

                var meta = _fileService.GetMetadata(fullPath, _currentRootPath);
                DebugLogger.Trace($"       Hash: {meta.FileHash}");

                // Ensure FilePath is stored as relative path with forward slashes
                if (Path.IsPathRooted(meta.FilePath) && !string.IsNullOrEmpty(_currentRootPath))
                {
                    var relativePath = meta.FilePath;
                    if (meta.FilePath.StartsWith(_currentRootPath, StringComparison.OrdinalIgnoreCase))
                    {
                        relativePath = meta.FilePath.Substring(_currentRootPath.Length)
                            .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    }
                    meta.FilePath = relativePath.Replace('\\', '/');
                }

                DebugLogger.Trace($"       Stored path: {meta.FilePath}");
                snapshot.Files.Add(meta);
            }

            // Create Commit
            var commit = new Commit
            {
                CommitId = Guid.NewGuid(),
                Message = message,
                AuthorName = Environment.UserName,
                Timestamp = DateTime.Now,
                Snapshot = snapshot
            };

            commit.CommitHash = commit.CalculateHash();
            DebugLogger.Info($"  -> Created commit: {commit.CommitHash.Substring(0, 8)}...");


            // Link to Parent
            var parent = GraphService.GetCurrentBranch()?.HeadCommit;
            DebugLogger.Trace($"  -> Parent commit: {parent?.CommitHash?.Substring(0, 8) ?? "null"}...");
            if (parent != null)
            {
                commit.Parents = new List<Commit> { parent };

                // Inherit files from parent snapshot
                if (parent.Snapshot?.Files != null)
                {
                    DebugLogger.Trace($"  -> Parent snapshot has {parent.Snapshot.Files.Count} files");
                    // Create a dictionary for fast lookup/replacement
                    var currentFiles = new Dictionary<string, FileMetadata>(StringComparer.OrdinalIgnoreCase);
                    foreach(var f in parent.Snapshot.Files) currentFiles[f.FilePath] = f;

                    // Update/Add selected files
                    foreach(var sFile in snapshot.Files)
                    {
                        currentFiles[sFile.FilePath] = sFile;
                    }

                    // Rebuild snapshot files list with merged state
                    snapshot.Files = currentFiles.Values.ToList();
                    DebugLogger.Trace($"  -> Final snapshot has {snapshot.Files.Count} files");
                }
            }

            // Add to Graph
            await GraphService.AddCommitAsync(commit);
            DebugLogger.Info($"  -> Commit added to graph");

            // Rescan for changes after commit to clear committed files from dirty list
            if (ChangeDetector != null && !string.IsNullOrEmpty(_currentRootPath))
            {
                DebugLogger.Info("  -> Rescanning for changes after commit...");
                var head = GraphService.GetCurrentBranch()?.HeadCommit;
                DebugLogger.Trace($"  -> New HEAD commit: {head?.CommitHash?.Substring(0, 8) ?? "null"}...");
                DebugLogger.Trace($"  -> HEAD snapshot files count: {head?.Snapshot?.Files?.Count ?? 0}");

                // Log HEAD snapshot files for comparison
                if (head?.Snapshot?.Files != null)
                {
                    DebugLogger.Trace("  -> HEAD snapshot files:");
                    foreach (var f in head.Snapshot.Files.Take(10)) // Limit to first 10
                    {
                        DebugLogger.Trace($"     - {f.FilePath} : {f.FileHash?.Substring(0, 8) ?? "null"}...");
                    }
                    if (head.Snapshot.Files.Count > 10)
                    {
                        DebugLogger.Trace($"     ... and {head.Snapshot.Files.Count - 10} more");
                    }
                }

                await ChangeDetector.ScanForChangesAsync(_currentRootPath, head, _fileService);

                var remainingChanges = ChangeDetector.GetChanges();
                DebugLogger.Info($"  -> After rescan: {remainingChanges.Count} dirty files remain");
                foreach (var kvp in remainingChanges.Take(10))
                {
                    DebugLogger.Trace($"     - {kvp.Key} : {kvp.Value}");
                }
                if (remainingChanges.Count > 10)
                {
                    DebugLogger.Trace($"     ... and {remainingChanges.Count - 10} more");
                }
            }

            Status = $"Committed: {message}";
            DebugLogger.Info($"  -> Commit complete");
        }

        // --- Remote Operations ---

        public async Task Push(string remoteName)
        {
            var remote = await RemoteService.GetRemoteAsync(remoteName); 
            if (remote == null) throw new ArgumentException($"Remote '{remoteName}' not found.");

            var currentBranch = GraphService.GetCurrentBranch();
            if (currentBranch == null) throw new InvalidOperationException("No active branch to push.");

            Status = $"Pushing to {remoteName}...";
            await _remoteController.Push(remote, currentBranch.BranchName, _currentRootPath);
            Status = $"Pushed to {remoteName}";
        }

        public async Task Pull(string remoteName)
        {
            var remote = await RemoteService.GetRemoteAsync(remoteName);
            if (remote == null) throw new ArgumentException($"Remote '{remoteName}' not found.");

            var currentBranch = GraphService.GetCurrentBranch();
            if (currentBranch == null) throw new InvalidOperationException("No active branch to pull into.");

            Status = $"Pulling from {remoteName}...";
            await _remoteController.Pull(remote, currentBranch.BranchName, _currentRootPath);
            Status = $"Pulled from {remoteName}";
        }
    }
}
