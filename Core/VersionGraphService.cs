using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            LoadGraph();
        }

        public void SetFileService(FileService fileService)
        {
            _fileService = fileService;
        }

        private void LoadGraph()
        {
            // Hydrate cache from store
            var commits = _metadataStore.GetAllCommits();
            foreach (var c in commits)
            {
                if (!_commitCache.ContainsKey(c.CommitId))
                    _commitCache[c.CommitId] = c;
            }
            // For now, branches are loaded on demand or we could load all
            // Ideally we'd have _metadataStore.GetAllBranches()
        }

        public void AddCommit(Commit commit)
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
                _metadataStore.SaveBranch(_currentBranch);
            }

            // Invalidate history cache
            _cachedHistoryList = null;

            // Persist
            _metadataStore.SaveCommit(commit);
        }

        public Branch CreateBranch(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Branch name cannot be empty.");
            if (_branchCache.ContainsKey(name) || _metadataStore.GetBranch(name) != null)
                throw new InvalidOperationException($"Branch '{name}' already exists.");

            if (_currentBranch == null && _commitCache.Count > 0)
                throw new InvalidOperationException("No active branch to branch off from.");

            var newBranch = new Branch
            {
                BranchName = name,
                HeadCommit = _currentBranch?.HeadCommit, // Point to current HEAD
                IsActive = false
            };

            _branchCache[name] = newBranch;
            _metadataStore.SaveBranch(newBranch);
            return newBranch;
        }

        public Branch GetBranch(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
                
            if (_branchCache.TryGetValue(name, out var cached))
                return cached;
            
            var branch = _metadataStore.GetBranch(name);
            if (branch != null)
                _branchCache[name] = branch;
            
            return branch;
        }

        public Branch GetCurrentBranch()
        {
            return _currentBranch;
        }

        public List<Branch> GetAllBranches()
        {
            return _metadataStore.GetAllBranches();
        }

        public void SwitchBranch(string name)
        {
            var branch = _metadataStore.GetBranch(name);
            if (branch == null) throw new ArgumentException($"Branch '{name}' not found.");

            _currentBranch = branch;
            _cachedHistoryList = null; // Invalidate cache
            // In a real app, this would also trigger working directory updates (checkout)
        }

        public List<Commit> GetCommitHistory()
        {
            if (_currentBranch == null) return new List<Commit>();
            
            if (_cachedHistoryList != null) return _cachedHistoryList;
            
            _cachedHistoryList = _currentBranch.GetHistory();
            return _cachedHistoryList;
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

        // Real merge implementation with blob storage
        public List<Conflict> Merge(Branch source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (_currentBranch == null) throw new InvalidOperationException("No active branch checked out.");

            // 1. Find common ancestor
            var ancestor = FindCommonAncestor(_currentBranch.HeadCommit, source.HeadCommit);
            if (ancestor == null)
                throw new InvalidOperationException("Branches have no common ancestor.");

            // 2. Prepare Merge Service
            var mergeService = new MergeService();
            var conflicts = new List<Conflict>();

            // 3. Get snapshots
            var snapshotBase = ancestor?.Snapshot;
            var snapshotLocal = _currentBranch.HeadCommit?.Snapshot;
            var snapshotRemote = source.HeadCommit?.Snapshot;
            
            var allFiles = new HashSet<string>();
            if (snapshotBase != null) foreach(var f in snapshotBase.Files) allFiles.Add(f.FilePath);
            if (snapshotLocal != null) foreach(var f in snapshotLocal.Files) allFiles.Add(f.FilePath);
            if (snapshotRemote != null) foreach(var f in snapshotRemote.Files) allFiles.Add(f.FilePath);

            // 4. Retrieve content from blob storage
            foreach (var path in allFiles)
            {
                string baseContent = string.Empty;
                string localContent = string.Empty;
                string remoteContent = string.Empty;

                // Get base content
                if (snapshotBase != null && _fileService != null)
                {
                    var baseFile = snapshotBase.Files.Find(f => f.FilePath == path);
                    if (baseFile != null && _fileService.BlobExists(baseFile.FileHash))
                    {
                        try { baseContent = _fileService.GetContentByHash(baseFile.FileHash); }
                        catch { baseContent = string.Empty; }
                    }
                }

                // Get local content (from working directory or blob)
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

                // Get remote content
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
                {
                    conflicts.AddRange(result.Conflicts);
                }
            }
            
            return conflicts; 
        }

        public void Rebase(Commit onto)
        {
            if (onto == null) throw new ArgumentNullException(nameof(onto));
            if (_currentBranch == null) throw new InvalidOperationException("No active branch checked out.");
            
            var currentHead = _currentBranch.HeadCommit;
            if (currentHead == null) throw new InvalidOperationException("Current branch has no commits.");
            
            // Find the common ancestor (merge base)
            var mergeBase = FindCommonAncestor(currentHead, onto);
            if (mergeBase == null)
                throw new InvalidOperationException("No common ancestor found. Cannot rebase unrelated histories.");
            
            // Get list of commits to replay (from mergeBase to currentHead)
            var commitsToReplay = new List<Commit>();
            var current = currentHead;
            var visited = new HashSet<Guid>();
            
            while (current != null && current.CommitId != mergeBase.CommitId)
            {
                if (!visited.Add(current.CommitId))
                    break; // Cycle detection
                    
                commitsToReplay.Add(current);
                
                // Follow first parent for linear history during rebase
                current = current.Parents?.FirstOrDefault();
            }
            
            // Reverse to get chronological order (oldest first)
            commitsToReplay.Reverse();
            
            // Start rebase: Set HEAD to 'onto'
            var rebasedParent = onto;
            var lineTracker = new LineTracker();
            var fileService = new FileService(new HashService());
            
            // Replay each commit on top of 'onto'
            foreach (var originalCommit in commitsToReplay)
            {
                // Create a new commit with same message but different parent
                var rebasedCommit = new Commit
                {
                    CommitId = Guid.NewGuid(),
                    Message = originalCommit.Message,
                    AuthorName = originalCommit.AuthorName,
                    AuthorEmail = originalCommit.AuthorEmail,
                    Timestamp = DateTime.Now, // New timestamp for rebased commit
                    AiAssisted = originalCommit.AiAssisted,
                    Parents = new List<Commit> { rebasedParent }
                };
                
                // Clone snapshot (simplified - in production would apply diff patches)
                rebasedCommit.Snapshot = new Snapshot
                {
                    SnapshotId = Guid.NewGuid(),
                    Timestamp = DateTime.Now,
                    Files = new List<FileMetadata>(originalCommit.Snapshot?.Files ?? new List<FileMetadata>())
                };
                
                // Calculate hash
                rebasedCommit.CommitHash = rebasedCommit.CalculateHash();
                
                // Add to graph
                AddCommit(rebasedCommit);
                
                // Move forward
                rebasedParent = rebasedCommit;
            }
            
            // Update current branch to point to the last rebased commit
            _currentBranch.MoveHead(rebasedParent);
            _metadataStore.SaveBranch(_currentBranch);
            _cachedHistoryList = null; // Invalidate cache
        }
        
        /// <summary>
        /// Delete a branch (cannot delete active branch)
        /// </summary>
        public void DeleteBranch(string branchName)
        {
            if (string.IsNullOrEmpty(branchName))
                throw new ArgumentException("Branch name cannot be empty.");
                
            if (_currentBranch != null && _currentBranch.BranchName == branchName)
                throw new InvalidOperationException("Cannot delete the currently active branch.");
                
            _metadataStore.DeleteBranch(branchName);
            _branchCache.Remove(branchName);
        }
    }
}