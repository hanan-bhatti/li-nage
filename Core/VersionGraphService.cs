using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Linage.Infrastructure;

namespace Linage.Core
{
    /// <summary>
    /// Core version management service that maintains the commit DAG.
    /// Spec: 5.2
    /// </summary>
    public class VersionGraphService
    {
        private readonly MetadataStore _metadataStore;
        private Branch _currentBranch;
        
        // In-memory cache of the graph for performance (Spec 10.1.2)
        private readonly Dictionary<Guid, Commit> _commitCache = new Dictionary<Guid, Commit>();
        private readonly Dictionary<string, Branch> _branchCache = new Dictionary<string, Branch>();
        private List<Commit> _cachedHistoryList; // Cache for the sorted history list
        private FileService _fileService;

        public VersionGraphService(MetadataStore metadataStore)
        {
            _metadataStore = metadataStore ?? throw new ArgumentNullException(nameof(metadataStore));
        }

        public void SetFileService(FileService fileService)
        {
            _fileService = fileService;
        }

        public async Task InitializeAsync()
        {
            // Hydrate cache from store
            var commits = await _metadataStore.GetAllCommitsAsync();
            foreach (var c in commits)
            {
                if (!_commitCache.ContainsKey(c.CommitId))
                    _commitCache[c.CommitId] = c;
            }
        }

        public async Task AddCommitAsync(Commit commit)
        {
            if (commit == null) throw new ArgumentNullException(nameof(commit));
            
            // Validate
            if (string.IsNullOrEmpty(commit.CommitHash))
                throw new InvalidOperationException("Commit hash must be calculated before adding.");

            if (_commitCache.ContainsKey(commit.CommitId))
                throw new InvalidOperationException("Commit already exists.");

            // Update DAG
            _commitCache[commit.CommitId] = commit;
            
            // Update current branch HEAD
            if (_currentBranch != null)
            {
                _currentBranch.MoveHead(commit);
                await _metadataStore.SaveBranchAsync(_currentBranch);
            }

            // Invalidate history cache
            _cachedHistoryList = null;

            // Persist
            await _metadataStore.SaveCommitAsync(commit);
        }

        public async Task<Branch> CreateBranchAsync(string name, string repositoryPath = null)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Branch name cannot be empty.");
            
            if (_branchCache.ContainsKey(name))
                 throw new InvalidOperationException($"Branch '{name}' already exists.");

            var existing = await _metadataStore.GetBranchAsync(name);
            if (existing != null)
                throw new InvalidOperationException($"Branch '{name}' already exists.");

            if (_currentBranch == null && _commitCache.Count > 0)
                throw new InvalidOperationException("No active branch to branch off from.");

            var newBranch = new Branch
            {
                BranchName = name,
                RepositoryPath = repositoryPath, // Store repository context
                HeadCommit = _currentBranch?.HeadCommit, // Point to current HEAD
                IsActive = false
            };

            _branchCache[name] = newBranch;
            await _metadataStore.SaveBranchAsync(newBranch);
            return newBranch;
        }

        public async Task<Branch> GetBranchAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
                
            if (_branchCache.TryGetValue(name, out var cached))
                return cached;
            
            var branch = await _metadataStore.GetBranchAsync(name);
            if (branch != null)
                _branchCache[name] = branch;
            
            return branch;
        }

        public Branch GetCurrentBranch()
        {
            return _currentBranch;
        }

        public async Task<List<Branch>> GetAllBranchesAsync(string repositoryPath = null)
        {
            return await _metadataStore.GetAllBranchesAsync(repositoryPath);
        }

        public async Task SwitchBranchAsync(string name)
        {
            var branch = await GetBranchAsync(name);
            if (branch == null) throw new ArgumentException($"Branch '{name}' not found.");

            _currentBranch = branch;
            _cachedHistoryList = null; // Invalidate cache
        }

        public List<Commit> GetCommitHistory()
        {
            if (_currentBranch == null) return new List<Commit>();

            if (_cachedHistoryList != null) return _cachedHistoryList;

            _cachedHistoryList = _currentBranch.GetHistory();
            return _cachedHistoryList;
        }

        /// <summary>
        /// Gets blame information for a specific line in a file.
        /// Returns the LineChange with the CommitId of the commit that last modified this line.
        /// </summary>
        public async Task<LineChange> GetLineBlameAsync(string filePath, int lineNumber)
        {
            return await _metadataStore.GetLineBlameAsync(filePath, lineNumber);
        }

        /// <summary>
        /// Gets blame information for all lines in a file.
        /// Returns line changes with CommitIds for each modified line.
        /// </summary>
        public async Task<List<LineChange>> GetFileBlameAsync(string filePath)
        {
            return await _metadataStore.GetFileBlameAsync(filePath);
        }

        /// <summary>
        /// Gets a commit by its ID.
        /// </summary>
        public Commit GetCommitById(Guid commitId)
        {
            if (_commitCache.TryGetValue(commitId, out var commit))
                return commit;
            return null;
        }

        public Commit FindCommonAncestor(Commit a, Commit b)
        {
            if (a == null || b == null) return null;

            // Get all ancestors of A with their distance/depth
            var ancestorsA = a.GetAllParents(); 
            var setA = new HashSet<Guid>(ancestorsA.Select(x => x.CommitId));
            setA.Add(a.CommitId);

            // Traverse B's ancestors until we find one in setA
            var queue = new Queue<Commit>();
            queue.Enqueue(b);
            var visited = new HashSet<Guid>();

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (setA.Contains(current.CommitId))
                    return current;

                if (visited.Add(current.CommitId))
                {
                    if (current.Parents != null)
                    {
                        foreach (var p in current.Parents)
                            queue.Enqueue(p);
                    }
                }
            }

            return null;
        }

        public List<Conflict> Merge(Branch source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (_currentBranch == null) throw new InvalidOperationException("No active branch checked out.");

            var ancestor = FindCommonAncestor(_currentBranch.HeadCommit, source.HeadCommit);
            if (ancestor == null)
                throw new InvalidOperationException("Branches have no common ancestor.");

            var mergeService = new MergeService();
            var conflicts = new List<Conflict>();

            var snapshotBase = ancestor?.Snapshot;
            var snapshotLocal = _currentBranch.HeadCommit?.Snapshot;
            var snapshotRemote = source.HeadCommit?.Snapshot;
            
            var allFiles = new HashSet<string>();
            if (snapshotBase != null) foreach(var f in snapshotBase.Files) allFiles.Add(f.FilePath);
            if (snapshotLocal != null) foreach(var f in snapshotLocal.Files) allFiles.Add(f.FilePath);
            if (snapshotRemote != null) foreach(var f in snapshotRemote.Files) allFiles.Add(f.FilePath);

            foreach (var path in allFiles)
            {
                string baseContent = string.Empty;
                string localContent = string.Empty;
                string remoteContent = string.Empty;

                if (snapshotBase != null && _fileService != null)
                {
                    var baseFile = snapshotBase.Files.Find(f => f.FilePath == path);
                    if (baseFile != null && _fileService.BlobExists(baseFile.FileHash))
                    {
                        try { baseContent = _fileService.GetContentByHash(baseFile.FileHash); }
                        catch { baseContent = string.Empty; }
                    }
                }

                if (snapshotLocal != null)
                {
                    var localFile = snapshotLocal.Files.Find(f => f.FilePath == path);
                    if (localFile != null)
                    {
                        try
                        {
                            if (File.Exists(path))
                                localContent = File.ReadAllText(path);
                            else if (_fileService != null && _fileService.BlobExists(localFile.FileHash))
                                localContent = _fileService.GetContentByHash(localFile.FileHash);
                        }
                        catch { localContent = string.Empty; }
                    }
                }

                if (snapshotRemote != null && _fileService != null)
                {
                    var remoteFile = snapshotRemote.Files.Find(f => f.FilePath == path);
                    if (remoteFile != null && _fileService.BlobExists(remoteFile.FileHash))
                    {
                        try { remoteContent = _fileService.GetContentByHash(remoteFile.FileHash); }
                        catch { remoteContent = string.Empty; }
                    }
                }
                
                var result = mergeService.MergeFile(path, baseContent, localContent, remoteContent);
                if (!result.Success)
                    conflicts.AddRange(result.Conflicts);
            }
            
            return conflicts; 
        }

        public async Task RebaseAsync(Commit onto)
        {
            if (onto == null) throw new ArgumentNullException(nameof(onto));
            if (_currentBranch == null) throw new InvalidOperationException("No active branch checked out.");
            
            var currentHead = _currentBranch.HeadCommit;
            if (currentHead == null) throw new InvalidOperationException("Current branch has no commits.");
            
            var mergeBase = FindCommonAncestor(currentHead, onto);
            if (mergeBase == null)
                throw new InvalidOperationException("No common ancestor found. Cannot rebase unrelated histories.");
            
            var commitsToReplay = new List<Commit>();
            var current = currentHead;
            var visited = new HashSet<Guid>();
            
            while (current != null && current.CommitId != mergeBase.CommitId)
            {
                if (!visited.Add(current.CommitId))
                    break;
                    
                commitsToReplay.Add(current);
                current = current.Parents?.FirstOrDefault();
            }
            
            commitsToReplay.Reverse();
            
            var rebasedParent = onto;
            
            foreach (var originalCommit in commitsToReplay)
            {
                var rebasedCommit = new Commit
                {
                    CommitId = Guid.NewGuid(),
                    Message = originalCommit.Message,
                    AuthorName = originalCommit.AuthorName,
                    AuthorEmail = originalCommit.AuthorEmail,
                    Timestamp = DateTime.Now,
                    AiAssisted = originalCommit.AiAssisted,
                    Parents = new List<Commit> { rebasedParent }
                };
                
                rebasedCommit.Snapshot = new Snapshot
                {
                    SnapshotId = Guid.NewGuid(),
                    Timestamp = DateTime.Now,
                    Files = new List<FileMetadata>(originalCommit.Snapshot?.Files ?? new List<FileMetadata>())
                };
                
                rebasedCommit.CommitHash = rebasedCommit.CalculateHash();
                
                await AddCommitAsync(rebasedCommit);
                
                rebasedParent = rebasedCommit;
            }
            
            _currentBranch.MoveHead(rebasedParent);
            await _metadataStore.SaveBranchAsync(_currentBranch);
            _cachedHistoryList = null; 
        }
        
        public async Task DeleteBranchAsync(string branchName)
        {
            if (string.IsNullOrEmpty(branchName))
                throw new ArgumentException("Branch name cannot be empty.");
                
            if (_currentBranch != null && _currentBranch.BranchName == branchName)
                throw new InvalidOperationException("Cannot delete the currently active branch.");
                
            await _metadataStore.DeleteBranchAsync(branchName);
            _branchCache.Remove(branchName);
        }
    }
}