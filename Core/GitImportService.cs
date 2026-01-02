using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using Linage.Core;
using Linage.Infrastructure;

namespace Linage.Core
{
    /// <summary>
    /// Imports Git repositories into Li'nage format.
    /// Converts file-based Git commits to line-based Li'nage commits.
    /// </summary>
    public class GitImportService
    {
        private readonly MetadataStore _metadataStore;
        private readonly HashService _hashService;
        private readonly FileService _fileService;
        private readonly VersionGraphService _graphService;

        public GitImportService(MetadataStore metadataStore, HashService hashService, 
            FileService fileService, VersionGraphService graphService)
        {
            _metadataStore = metadataStore ?? throw new ArgumentNullException(nameof(metadataStore));
            _hashService = hashService ?? throw new ArgumentNullException(nameof(hashService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _graphService = graphService ?? throw new ArgumentNullException(nameof(graphService));
            
            // Force database initialization to ensure tables exist
            try
            {
                using (var db = new LiNageDbContext())
                {
                    db.Database.Initialize(force: false);
                }
            }
            catch
            {
                // Ignore initialization errors - database will be created on first use
            }
        }

        /// <summary>
        /// Import a Git repository into Li'nage.
        /// Creates .linage directory and converts commits.
        /// </summary>
        public ImportResult ImportRepository(string gitRepoPath, IProgress<string> progress = null)
        {
            if (!Repository.IsValid(gitRepoPath))
                throw new ArgumentException($"Not a valid Git repository: {gitRepoPath}");

            progress?.Report($"Scanning repository: {gitRepoPath}...");
            
            var result = new ImportResult { RepositoryPath = gitRepoPath };

            try
            {
                // 1. Create .linage directory
                var linageDir = Path.Combine(gitRepoPath, ".linage");
                if (!Directory.Exists(linageDir))
                    Directory.CreateDirectory(linageDir);

                // 2. Initialize blob store
                _fileService.InitializeBlobStore(gitRepoPath);

                // 3. Create Project entity (required for foreign key constraints)
                var project = new Project
                {
                    ProjectId = Guid.NewGuid(),
                    ProjectName = Path.GetFileName(gitRepoPath),
                    RepositoryPath = gitRepoPath,
                    CreatedDate = DateTime.Now
                };
                _metadataStore.SaveProject(project);

                using (var repo = new Repository(gitRepoPath))
                {
                    // 4. Import branches
                    foreach (var gitBranch in repo.Branches)
                    {
                        if (gitBranch.IsRemote) continue; // Skip remote branches for now

                        result.BranchesImported++;
                        progress?.Report($"Importing branch: {gitBranch.FriendlyName} ({result.BranchesImported}/{repo.Branches.Count(b => !b.IsRemote)})");
                        ImportBranch(repo, gitBranch, gitRepoPath, progress);
                    }

                    // 5. Import remotes (now that Project exists)
                    foreach (var remote in repo.Network.Remotes)
                    {
                        ImportRemote(remote, project.ProjectId);
                        result.RemotesImported++;
                    }

                    result.Success = true;
                }
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
            {
                // Entity Framework validation errors - show which fields are invalid
                result.Success = false;
                var errorMsg = "Database validation failed:\n\n";
                
                foreach (var validationErrors in dbEx.EntityValidationErrors)
                {
                    errorMsg += $"Entity: {validationErrors.Entry.Entity.GetType().Name}\n";
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        errorMsg += $"  • {validationError.PropertyName}: {validationError.ErrorMessage}\n";
                    }
                    errorMsg += "\n";
                }
                
                result.ErrorMessage = errorMsg;
            }
            catch (Exception ex)
            {
                result.Success = false;
                
                // Build comprehensive error message
                var errorMsg = $"Error Type: {ex.GetType().Name}\n";
                errorMsg += $"Message: {ex.Message}\n\n";
                
                // Inner exception
                if (ex.InnerException != null)
                {
                    errorMsg += $"Inner Exception Type: {ex.InnerException.GetType().Name}\n";
                    errorMsg += $"Inner Message: {ex.InnerException.Message}\n\n";
                    
                    // Inner inner exception
                    if (ex.InnerException.InnerException != null)
                    {
                        errorMsg += $"Inner Inner Exception: {ex.InnerException.InnerException.GetType().Name}\n";
                        errorMsg += $"Inner Inner Message: {ex.InnerException.InnerException.Message}\n\n";
                    }
                }
                
                // Stack trace (first 500 chars)
                if (ex.StackTrace != null && ex.StackTrace.Length > 0)
                {
                    errorMsg += $"Stack Trace:\n{ex.StackTrace.Substring(0, Math.Min(500, ex.StackTrace.Length))}\n";
                }
                
                result.ErrorMessage = errorMsg;
                
                // Log to debug
                System.Diagnostics.Debug.WriteLine("=== GIT IMPORT ERROR ===");
                System.Diagnostics.Debug.WriteLine(errorMsg);
                System.Diagnostics.Debug.WriteLine("========================");
            }

            return result;
        }

        private void ImportBranch(Repository gitRepo, LibGit2Sharp.Branch gitBranch, string repoPath, IProgress<string> progress)
        {
            // Check if Li'nage branch already exists
            var existingBranch = _graphService.GetBranch(gitBranch.FriendlyName);
            Branch linageBranch;

            if (existingBranch != null)
            {
                // Use existing branch
                linageBranch = existingBranch;
            }
            else
            {
                // For Git import, we need to create branches without requiring an active branch
                // Create branch directly via MetadataStore to bypass the active branch check
                linageBranch = new Branch
                {
                    BranchName = gitBranch.FriendlyName,
                    HeadCommit = null, // Will be updated as commits are imported
                    IsActive = false
                };

                // Save to database via MetadataStore
                _metadataStore.SaveBranch(linageBranch);

                // Set as current branch if this is the first branch being imported
                if (_graphService.GetCurrentBranch() == null)
                {
                    _graphService.SwitchBranch(gitBranch.FriendlyName);
                }
            }

            // Walk commits from oldest to newest
            var commits = gitBranch.Commits.Reverse().ToList();
            var commitMap = new Dictionary<string, Commit>(); // Git SHA -> Li'nage Commit

            foreach (var gitCommit in commits)
            {
                var linageCommit = ConvertCommit(gitCommit, repoPath, commitMap);

                // Add to graph
                _graphService.AddCommit(linageCommit);
                commitMap[gitCommit.Sha] = linageCommit;

                progress?.Report($"Imported commit {linageCommit.CommitHash.Substring(0,7)}: {linageCommit.Message}");

                // Update branch head
                linageBranch.MoveHead(linageCommit);
            }
        }

        private Commit ConvertCommit(LibGit2Sharp.Commit gitCommit, string repoPath, 
            Dictionary<string, Commit> commitMap)
        {
            var linageCommit = new Commit
            {
                CommitId = Guid.NewGuid(),
                AuthorName = gitCommit.Author.Name,
                AuthorEmail = gitCommit.Author.Email,
                Message = gitCommit.Message,
                Timestamp = gitCommit.Author.When.DateTime,
                Parents = new List<Commit>()
            };

            // Map parents
            foreach (var gitParent in gitCommit.Parents)
            {
                if (commitMap.TryGetValue(gitParent.Sha, out var linageParent))
                {
                    linageCommit.Parents.Add(linageParent);
                }
            }

            // Create snapshot from Git tree
            var snapshot = new Snapshot
            {
                SnapshotId = Guid.NewGuid(),
                Timestamp = gitCommit.Author.When.DateTime,
                Files = new List<FileMetadata>()
            };

            // Convert Git tree to file metadata
            foreach (var entry in gitCommit.Tree)
            {
                try
                {
                    if (entry.TargetType == TreeEntryTargetType.Blob)
                    {
                        var blob = (Blob)entry.Target;
                        
                        // Skip binary files or very large files
                        if (blob.IsBinary || blob.Size > 10 * 1024 * 1024) // Skip files > 10MB
                            continue;

                        var content = blob.GetContentText();
                        if (content == null)
                            continue;

                        // Store in blob store (Li'nage object storage)
                        _fileService.StoreContent(content);

                        // Store in blob store
                        var hash = _hashService.ComputeContentHash(content);
                        
                        // Create file metadata (Li'nage style)
                        var metadata = new FileMetadata(
                            filePath: entry.Path, // Relative path
                            fileHash: hash,
                            fileSize: blob.Size,
                            modifiedDate: gitCommit.Author.When.DateTime,
                            isDeleted: false
                        );

                        snapshot.Files.Add(metadata);
                    }
                }
                catch (Exception ex)
                {
                    // Skip files that can't be processed
                    System.Diagnostics.Debug.WriteLine($"Failed to process file {entry.Path}: {ex.Message}");
                }
            }

            // Calculate snapshot hash
            snapshot.Hash = snapshot.GetHash();
            linageCommit.Snapshot = snapshot;

            // Calculate commit hash
            linageCommit.CommitHash = linageCommit.CalculateHash();

            return linageCommit;
        }

        private void ImportRemote(LibGit2Sharp.Remote gitRemote, Guid projectId)
        {
            var protocol = gitRemote.Url.StartsWith("https://") 
                ? RemoteProtocol.HTTPS 
                : RemoteProtocol.SSH;

            var remote = new Remote
            {
                RemoteName = gitRemote.Name,
                RemoteUrl = gitRemote.Url,
                Protocol = protocol,
                IsDefault = gitRemote.Name == "origin",
                ProjectId = projectId  // Set the foreign key
            };

            _metadataStore.SaveRemote(remote);
        }

        /// <summary>
        /// Quick import - only import HEAD commit and current files.
        /// Faster for large repos.
        /// </summary>
        public ImportResult QuickImport(string gitRepoPath)
        {
            if (!Repository.IsValid(gitRepoPath))
                throw new ArgumentException($"Not a valid Git repository: {gitRepoPath}");

            var result = new ImportResult { RepositoryPath = gitRepoPath };

            try
            {
                // Create .linage directory
                var linageDir = Path.Combine(gitRepoPath, ".linage");
                if (!Directory.Exists(linageDir))
                    Directory.CreateDirectory(linageDir);

                _fileService.InitializeBlobStore(gitRepoPath);

                // Create Project entity
                var project = new Project
                {
                    ProjectId = Guid.NewGuid(),
                    ProjectName = Path.GetFileName(gitRepoPath),
                    RepositoryPath = gitRepoPath,
                    CreatedDate = DateTime.Now
                };
                _metadataStore.SaveProject(project);

                using (var repo = new Repository(gitRepoPath))
                {
                    // Only import current branch HEAD
                    var head = repo.Head;
                    if (head?.Tip != null)
                    {
                        var commitMap = new Dictionary<string, Commit>();
                        var linageCommit = ConvertCommit(head.Tip, gitRepoPath, commitMap);
                        
                        // Check if branch exists
                        var branch = _graphService.GetBranch(head.FriendlyName);
                        if (branch == null)
                        {
                            branch = _graphService.CreateBranch(head.FriendlyName);
                            result.BranchesImported = 1;
                        }
                        
                        _graphService.AddCommit(linageCommit);
                        branch.MoveHead(linageCommit);

                        result.CommitsImported = 1;
                    }

                    // Import remotes
                    foreach (var remote in repo.Network.Remotes)
                    {
                        ImportRemote(remote, project.ProjectId);
                        result.RemotesImported++;
                    }

                    result.Success = true;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }
    }

    public class ImportResult
    {
        public bool Success { get; set; }
        public string RepositoryPath { get; set; }
        public int CommitsImported { get; set; }
        public int BranchesImported { get; set; }
        public int RemotesImported { get; set; }
        public string ErrorMessage { get; set; }

        public override string ToString()
        {
            if (!Success)
                return $"Import failed: {ErrorMessage}";

            return $"Imported {CommitsImported} commits, {BranchesImported} branches, {RemotesImported} remotes from {RepositoryPath}";
        }
    }
}
