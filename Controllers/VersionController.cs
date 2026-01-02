using System;
using System.Collections.Generic;
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

        [Obsolete("Use ScanChangesAsync for better UI responsiveness")]
        public void ScanChanges()
        {
            if (ChangeDetector == null || GraphService == null) return;

            var head = GraphService.GetCurrentBranch()?.HeadCommit;

            if (!string.IsNullOrEmpty(_currentRootPath))
            {
                ChangeDetector.ScanForChanges(_currentRootPath, head, _fileService);
            }
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

        public GitImportService CreateGitImporter()
        {
            return new GitImportService(_metadataStore, _hashService, _fileService, GraphService);
        }

        public void LoadProject(string rootPath)
        {
            if (string.IsNullOrEmpty(rootPath)) return;
            _currentRootPath = rootPath;

            // Initialize BlobStore for file content storage
            _fileService.InitializeBlobStore(rootPath);

            // Inject FileService into GraphService for merge operations
            GraphService.SetFileService(_fileService);

            // Re-initialize transports with the correct path
            // (In a more complex DI setup this would be handled differently)

            // Initialize custom Li'nage transports with correct path
            var httpTransport = new LinageHttpTransport(AuthService, rootPath);
            var sshTransport = new LinageSshTransport(AuthService, rootPath);
            _remoteController = new RemoteController(httpTransport, sshTransport, _authController);

            ChangeDetector = new ChangeDetector(rootPath);
            ChangeDetector.StartMonitoring();

            // Load existing branches and commits from database
            var branches = GraphService.GetAllBranches();
            if (branches != null && branches.Count > 0)
            {
                // Try to load 'main' branch first, fallback to 'master', then first available
                var mainBranch = GraphService.GetBranch("main");
                if (mainBranch != null)
                {
                    GraphService.SwitchBranch("main");
                }
                else
                {
                    var masterBranch = GraphService.GetBranch("master");
                    if (masterBranch != null)
                    {
                        GraphService.SwitchBranch("master");
                    }
                    else if (branches.Count > 0)
                    {
                        // Switch to first available branch
                        GraphService.SwitchBranch(branches[0].BranchName);
                    }
                }
            }

            Status = $"Loaded {rootPath}";
        }

        public void CreateCommit(string message, List<string> selectedFiles)
        {
            if (GraphService.GetCurrentBranch() == null)
            {
                // Try to get existing 'main' branch first
                var mainBranch = GraphService.GetBranch("main");
                if (mainBranch != null)
                {
                    GraphService.SwitchBranch("main");
                }
                else if (GraphService.GetCommitHistory().Count == 0)
                {
                    // Create 'main' only if it doesn't exist
                    GraphService.CreateBranch("main");
                    GraphService.SwitchBranch("main");
                }
            }

            // Create Snapshot
            var snapshot = new Snapshot { SnapshotId = Guid.NewGuid(), Timestamp = DateTime.Now };
            snapshot.Files = new List<FileMetadata>();

            // Process files
            foreach (var file in selectedFiles)
            {
                var meta = _fileService.GetMetadata(file, ""); 
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
            
            
            // Link to Parent
            var parent = GraphService.GetCurrentBranch()?.HeadCommit;
            if (parent != null)
            {
                commit.Parents = new List<Commit> { parent };
                
                // Inherit files from parent snapshot
                if (parent.Snapshot?.Files != null)
                {
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
                }
            }
            
            // Add to Graph
            GraphService.AddCommit(commit);

            // Rescan for changes after commit to clear committed files from dirty list
            if (ChangeDetector != null && !string.IsNullOrEmpty(_currentRootPath))
            {
                var head = GraphService.GetCurrentBranch()?.HeadCommit;
                ChangeDetector.ScanForChanges(_currentRootPath, head, _fileService);
            }

            Status = $"Committed: {message}";
        }

        // --- Remote Operations ---

        public async Task Push(string remoteName)
        {
            var remote = RemoteService.GetRemote(remoteName);
            if (remote == null) throw new ArgumentException($"Remote '{remoteName}' not found.");

            var currentBranch = GraphService.GetCurrentBranch();
            if (currentBranch == null) throw new InvalidOperationException("No active branch to push.");

            Status = $"Pushing to {remoteName}...";
            await _remoteController.Push(remote, currentBranch.BranchName);
            Status = $"Pushed to {remoteName}";
        }

        public async Task Pull(string remoteName)
        {
            var remote = RemoteService.GetRemote(remoteName);
            if (remote == null) throw new ArgumentException($"Remote '{remoteName}' not found.");

            var currentBranch = GraphService.GetCurrentBranch();
            if (currentBranch == null) throw new InvalidOperationException("No active branch to pull into.");

            Status = $"Pulling from {remoteName}...";
            await _remoteController.Pull(remote, currentBranch.BranchName);
            Status = $"Pulled from {remoteName}";
        }
    }
}
