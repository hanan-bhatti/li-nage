using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.Entity;
using Linage.Core;

namespace Linage.Infrastructure
{
    /// <summary>
    /// Metadata store with async database operations to prevent UI thread blocking.
    /// All async methods use ConfigureAwait(false) for optimal performance..
    /// </summary>
    public class MetadataStore
    {
        private readonly LiNageDbContext _context;

        public MetadataStore(LiNageDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        #region Commit Operations

        /// <summary>
        /// Asynchronously save a commit to the database with proper relationship tracking
        /// </summary>
        public async Task SaveCommitAsync(Commit commit)
        {
            if (commit == null) throw new ArgumentNullException(nameof(commit));

            // Ensure Snapshot is tracked or added
            if (commit.Snapshot != null)
            {
                var existingSnapshot = await _context.Snapshots
                    .FindAsync(commit.Snapshot.SnapshotId)
                    .ConfigureAwait(false);

                if (existingSnapshot == null)
                {
                    _context.Snapshots.Add(commit.Snapshot);
                }
                else
                {
                    _context.Entry(existingSnapshot).CurrentValues.SetValues(commit.Snapshot);
                }
            }

            // Ensure parents are tracked (if they exist in DB)
            // This logic is complex in disconnected scenarios, but for local metadata store
            // we assume parents usually exist if we are building on top.
            // For now, simple Add.

            var existingCommit = await _context.Commits
                .FindAsync(commit.CommitId)
                .ConfigureAwait(false);

            if (existingCommit == null)
            {
                _context.Commits.Add(commit);
            }

            await _context.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Synchronously save a commit (OBSOLETE - use SaveCommitAsync instead)
        /// </summary>
        [Obsolete("Use SaveCommitAsync to prevent UI thread blocking")]
        public void SaveCommit(Commit commit)
        {
            if (commit == null) throw new ArgumentNullException(nameof(commit));

            // Ensure Snapshot is tracked or added
            if (commit.Snapshot != null)
            {
                var existingSnapshot = _context.Snapshots.Find(commit.Snapshot.SnapshotId);
                if (existingSnapshot == null)
                {
                    _context.Snapshots.Add(commit.Snapshot);
                }
                else
                {
                    _context.Entry(existingSnapshot).CurrentValues.SetValues(commit.Snapshot);
                }
            }

            var existingCommit = _context.Commits.Find(commit.CommitId);
            if (existingCommit == null)
            {
                _context.Commits.Add(commit);
            }

            _context.SaveChanges();
        }

        /// <summary>
        /// Asynchronously get a commit by ID with eager loading of relationships
        /// </summary>
        public async Task<Commit> GetCommitAsync(Guid commitId)
        {
            return await _context.Commits
                .Include(c => c.Parents)
                .Include(c => c.Snapshot.Files)
                .FirstOrDefaultAsync(c => c.CommitId == commitId)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Synchronously get a commit by ID (OBSOLETE - use GetCommitAsync instead)
        /// </summary>
        [Obsolete("Use GetCommitAsync to prevent UI thread blocking")]
        public Commit GetCommit(Guid commitId)
        {
            return _context.Commits
                .Include(c => c.Parents)
                .Include(c => c.Snapshot.Files)
                .FirstOrDefault(c => c.CommitId == commitId);
        }

        /// <summary>
        /// Asynchronously get all commits with eager loading to prevent N+1 queries
        /// </summary>
        public async Task<List<Commit>> GetAllCommitsAsync()
        {
            return await _context.Commits
                .Include(c => c.Parents)
                .Include(c => c.Snapshot)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Synchronously get all commits (OBSOLETE - use GetAllCommitsAsync instead)
        /// WARNING: This has N+1 query problem. Use GetAllCommitsAsync for better performance.
        /// </summary>
        [Obsolete("Use GetAllCommitsAsync with eager loading to prevent N+1 queries and UI blocking")]
        public List<Commit> GetAllCommits()
        {
            return _context.Commits.ToList();
        }

        /// <summary>
        /// Asynchronously get commits by author with eager loading and ordering
        /// </summary>
        public async Task<List<Commit>> GetCommitsByAuthorAsync(string authorName)
        {
            if (string.IsNullOrWhiteSpace(authorName))
                throw new ArgumentException("Author name cannot be null or empty", nameof(authorName));

            return await _context.Commits
                .Include(c => c.Parents)
                .Include(c => c.Snapshot)
                .Where(c => c.AuthorName == authorName)
                .OrderByDescending(c => c.Timestamp)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Synchronously get commits by author (OBSOLETE - use GetCommitsByAuthorAsync instead)
        /// </summary>
        [Obsolete("Use GetCommitsByAuthorAsync to prevent UI thread blocking")]
        public List<Commit> GetCommitsByAuthor(string authorName)
        {
            return _context.Commits
                .Where(c => c.AuthorName == authorName)
                .OrderByDescending(c => c.Timestamp)
                .ToList();
        }

        /// <summary>
        /// Asynchronously get commits in date range with eager loading and ordering
        /// </summary>
        public async Task<List<Commit>> GetCommitsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
                throw new ArgumentException("Start date must be before or equal to end date");

            return await _context.Commits
                .Include(c => c.Parents)
                .Include(c => c.Snapshot)
                .Where(c => c.Timestamp >= startDate && c.Timestamp <= endDate)
                .OrderByDescending(c => c.Timestamp)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Synchronously get commits in date range (OBSOLETE - use GetCommitsByDateRangeAsync instead)
        /// </summary>
        [Obsolete("Use GetCommitsByDateRangeAsync to prevent UI thread blocking")]
        public List<Commit> GetCommitsByDateRange(DateTime startDate, DateTime endDate)
        {
            return _context.Commits
                .Where(c => c.Timestamp >= startDate && c.Timestamp <= endDate)
                .OrderByDescending(c => c.Timestamp)
                .ToList();
        }

        #endregion

        #region Project Operations

        /// <summary>
        /// Asynchronously save or update a project
        /// </summary>
        public async Task SaveProjectAsync(Project project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));

            var existing = await _context.Projects
                .FindAsync(project.ProjectId)
                .ConfigureAwait(false);

            if (existing == null)
            {
                _context.Projects.Add(project);
            }
            else
            {
                _context.Entry(existing).CurrentValues.SetValues(project);
            }

            await _context.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Synchronously save a project (OBSOLETE - use SaveProjectAsync instead)
        /// </summary>
        [Obsolete("Use SaveProjectAsync to prevent UI thread blocking")]
        public void SaveProject(Project project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));

            var existing = _context.Projects.Find(project.ProjectId);
            if (existing == null)
            {
                _context.Projects.Add(project);
            }
            else
            {
                _context.Entry(existing).CurrentValues.SetValues(project);
            }
            _context.SaveChanges();
        }

        /// <summary>
        /// Asynchronously get a project by ID
        /// </summary>
        public async Task<Project> GetProjectAsync(Guid projectId)
        {
            return await _context.Projects
                .FindAsync(projectId)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously get all projects
        /// </summary>
        public async Task<List<Project>> GetAllProjectsAsync()
        {
            return await _context.Projects
                .ToListAsync()
                .ConfigureAwait(false);
        }

        #endregion

        #region Branch Operations

        /// <summary>
        /// Asynchronously save or update a branch
        /// </summary>
        public async Task SaveBranchAsync(Branch branch)
        {
            if (branch == null) throw new ArgumentNullException(nameof(branch));

            var existing = await _context.Branches
                .FindAsync(branch.BranchId)
                .ConfigureAwait(false);

            if (existing == null)
            {
                _context.Branches.Add(branch);
            }
            else
            {
                _context.Entry(existing).CurrentValues.SetValues(branch);
            }

            await _context.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Synchronously save a branch (OBSOLETE - use SaveBranchAsync instead)
        /// </summary>
        [Obsolete("Use SaveBranchAsync to prevent UI thread blocking")]
        public void SaveBranch(Branch branch)
        {
            if (branch == null) throw new ArgumentNullException(nameof(branch));

            var existing = _context.Branches.Find(branch.BranchId);
            if (existing == null)
            {
                _context.Branches.Add(branch);
            }
            else
            {
                _context.Entry(existing).CurrentValues.SetValues(branch);
            }
            _context.SaveChanges();
        }

        /// <summary>
        /// Asynchronously get a branch by name with eager loading of head commit
        /// </summary>
        public async Task<Branch> GetBranchAsync(string branchName)
        {
            if (string.IsNullOrWhiteSpace(branchName))
                throw new ArgumentException("Branch name cannot be null or empty", nameof(branchName));

            return await _context.Branches
                .Include(b => b.HeadCommit)
                .FirstOrDefaultAsync(b => b.BranchName == branchName)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Synchronously get a branch by name (OBSOLETE - use GetBranchAsync instead)
        /// </summary>
        [Obsolete("Use GetBranchAsync to prevent UI thread blocking")]
        public Branch GetBranch(string branchName)
        {
            return _context.Branches
                .Include(b => b.HeadCommit)
                .FirstOrDefault(b => b.BranchName == branchName);
        }

        /// <summary>
        /// Asynchronously get all branches with eager loading of head commits
        /// </summary>
        public async Task<List<Branch>> GetAllBranchesAsync()
        {
            return await _context.Branches
                .Include(b => b.HeadCommit)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Synchronously get all branches (OBSOLETE - use GetAllBranchesAsync instead)
        /// </summary>
        [Obsolete("Use GetAllBranchesAsync to prevent UI thread blocking")]
        public List<Branch> GetAllBranches()
        {
            return _context.Branches
                .Include(b => b.HeadCommit)
                .ToList();
        }

        /// <summary>
        /// Asynchronously delete a branch from the database
        /// </summary>
        public async Task DeleteBranchAsync(string branchName)
        {
            if (string.IsNullOrWhiteSpace(branchName))
                throw new ArgumentException("Branch name cannot be null or empty", nameof(branchName));

            var branch = await _context.Branches
                .FirstOrDefaultAsync(b => b.BranchName == branchName)
                .ConfigureAwait(false);

            if (branch != null)
            {
                _context.Branches.Remove(branch);
                await _context.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Synchronously delete a branch (OBSOLETE - use DeleteBranchAsync instead)
        /// </summary>
        [Obsolete("Use DeleteBranchAsync to prevent UI thread blocking")]
        public void DeleteBranch(string branchName)
        {
            var branch = _context.Branches.FirstOrDefault(b => b.BranchName == branchName);
            if (branch != null)
            {
                _context.Branches.Remove(branch);
                _context.SaveChanges();
            }
        }

        #endregion

        #region Snapshot Operations

        /// <summary>
        /// Asynchronously save or update a snapshot
        /// </summary>
        public async Task SaveSnapshotAsync(Snapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            var existing = await _context.Snapshots
                .FindAsync(snapshot.SnapshotId)
                .ConfigureAwait(false);

            if (existing == null)
            {
                _context.Snapshots.Add(snapshot);
            }
            else
            {
                _context.Entry(existing).CurrentValues.SetValues(snapshot);
            }

            await _context.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Synchronously save a snapshot (OBSOLETE - use SaveSnapshotAsync instead)
        /// </summary>
        [Obsolete("Use SaveSnapshotAsync to prevent UI thread blocking")]
        public void SaveSnapshot(Snapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            var existing = _context.Snapshots.Find(snapshot.SnapshotId);
            if (existing == null)
            {
                _context.Snapshots.Add(snapshot);
            }
            else
            {
                _context.Entry(existing).CurrentValues.SetValues(snapshot);
            }
            _context.SaveChanges();
        }

        /// <summary>
        /// Asynchronously get snapshot by ID with eager loading of files
        /// </summary>
        public async Task<Snapshot> GetSnapshotAsync(Guid snapshotId)
        {
            return await _context.Snapshots
                .Include(s => s.Files)
                .FirstOrDefaultAsync(s => s.SnapshotId == snapshotId)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Synchronously get snapshot by ID (OBSOLETE - use GetSnapshotAsync instead)
        /// </summary>
        [Obsolete("Use GetSnapshotAsync to prevent UI thread blocking")]
        public Snapshot GetSnapshot(Guid snapshotId)
        {
            return _context.Snapshots
                .Include(s => s.Files)
                .FirstOrDefault(s => s.SnapshotId == snapshotId);
        }

        #endregion

        #region File Metadata Operations

        /// <summary>
        /// Asynchronously save or update file metadata
        /// </summary>
        public async Task SaveFileMetadataAsync(FileMetadata file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));

            var existing = await _context.Files
                .FindAsync(file.FileId)
                .ConfigureAwait(false);

            if (existing == null)
            {
                _context.Files.Add(file);
            }
            else
            {
                _context.Entry(existing).CurrentValues.SetValues(file);
            }

            await _context.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Synchronously save file metadata (OBSOLETE - use SaveFileMetadataAsync instead)
        /// </summary>
        [Obsolete("Use SaveFileMetadataAsync to prevent UI thread blocking")]
        public void SaveFileMetadata(FileMetadata file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));

            var existing = _context.Files.Find(file.FileId);
            if (existing == null)
            {
                _context.Files.Add(file);
            }
            else
            {
                _context.Entry(existing).CurrentValues.SetValues(file);
            }
            _context.SaveChanges();
        }

        /// <summary>
        /// Asynchronously get file metadata by ID
        /// </summary>
        public async Task<FileMetadata> GetFileMetadataAsync(Guid fileId)
        {
            return await _context.Files
                .FindAsync(fileId)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously get file metadata by file path
        /// </summary>
        public async Task<FileMetadata> GetFileMetadataByPathAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            return await _context.Files
                .FirstOrDefaultAsync(f => f.FilePath == filePath)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously get file metadata by hash
        /// </summary>
        public async Task<FileMetadata> GetFileMetadataByHashAsync(string fileHash)
        {
            if (string.IsNullOrWhiteSpace(fileHash))
                throw new ArgumentException("File hash cannot be null or empty", nameof(fileHash));

            return await _context.Files
                .FirstOrDefaultAsync(f => f.FileHash == fileHash)
                .ConfigureAwait(false);
        }

        #endregion

        #region Line Change Operations

        /// <summary>
        /// Asynchronously save line changes in batch for optimal performance
        /// </summary>
        public async Task SaveLineChangesAsync(List<LineChange> changes)
        {
            if (changes == null || changes.Count == 0) return;

            foreach (var change in changes)
            {
                _context.LineChanges.Add(change);
            }

            await _context.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Synchronously save line changes (OBSOLETE - use SaveLineChangesAsync instead)
        /// </summary>
        [Obsolete("Use SaveLineChangesAsync to prevent UI thread blocking")]
        public void SaveLineChanges(List<LineChange> changes)
        {
            if (changes == null || changes.Count == 0) return;

            foreach (var change in changes)
            {
                _context.LineChanges.Add(change);
            }
            _context.SaveChanges();
        }

        /// <summary>
        /// Asynchronously get line changes for a commit ordered by line number
        /// </summary>
        public async Task<List<LineChange>> GetLineChangesByCommitAsync(Guid commitId)
        {
            return await _context.LineChanges
                .Where(lc => lc.CommitId == commitId)
                .OrderBy(lc => lc.LineNumber)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Synchronously get line changes by commit (OBSOLETE - use GetLineChangesByCommitAsync instead)
        /// </summary>
        [Obsolete("Use GetLineChangesByCommitAsync to prevent UI thread blocking")]
        public List<LineChange> GetLineChangesByCommit(Guid commitId)
        {
            return _context.LineChanges
                .Where(lc => lc.CommitId == commitId)
                .OrderBy(lc => lc.LineNumber)
                .ToList();
        }

        #endregion

        #region Remote Operations

        /// <summary>
        /// Asynchronously save or update a remote
        /// </summary>
        public async Task SaveRemoteAsync(Remote remote)
        {
            if (remote == null) throw new ArgumentNullException(nameof(remote));

            var existing = await _context.Remotes
                .FindAsync(remote.RemoteId)
                .ConfigureAwait(false);

            if (existing == null)
            {
                _context.Remotes.Add(remote);
            }
            else
            {
                _context.Entry(existing).CurrentValues.SetValues(remote);
            }

            await _context.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Synchronously save a remote (OBSOLETE - use SaveRemoteAsync instead)
        /// </summary>
        [Obsolete("Use SaveRemoteAsync to prevent UI thread blocking")]
        public void SaveRemote(Remote remote)
        {
            if (remote == null) throw new ArgumentNullException(nameof(remote));

            var existing = _context.Remotes.Find(remote.RemoteId);
            if (existing == null)
            {
                _context.Remotes.Add(remote);
            }
            else
            {
                _context.Entry(existing).CurrentValues.SetValues(remote);
            }
            _context.SaveChanges();
        }

        /// <summary>
        /// Asynchronously get a remote by name
        /// </summary>
        public async Task<Remote> GetRemoteAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Remote name cannot be null or empty", nameof(name));

            return await _context.Remotes
                .FirstOrDefaultAsync(r => r.RemoteName == name)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Synchronously get a remote by name (OBSOLETE - use GetRemoteAsync instead)
        /// </summary>
        [Obsolete("Use GetRemoteAsync to prevent UI thread blocking")]
        public Remote GetRemote(string name)
        {
            return _context.Remotes.FirstOrDefault(r => r.RemoteName == name);
        }

        /// <summary>
        /// Asynchronously get all remotes
        /// </summary>
        public async Task<List<Remote>> GetAllRemotesAsync()
        {
            return await _context.Remotes
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Synchronously get all remotes (OBSOLETE - use GetAllRemotesAsync instead)
        /// </summary>
        [Obsolete("Use GetAllRemotesAsync to prevent UI thread blocking")]
        public List<Remote> GetAllRemotes()
        {
            return _context.Remotes.ToList();
        }

        /// <summary>
        /// Asynchronously delete a remote from the database
        /// </summary>
        public async Task DeleteRemoteAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Remote name cannot be null or empty", nameof(name));

            var remote = await _context.Remotes
                .FirstOrDefaultAsync(r => r.RemoteName == name)
                .ConfigureAwait(false);

            if (remote != null)
            {
                _context.Remotes.Remove(remote);
                await _context.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Synchronously delete a remote (OBSOLETE - use DeleteRemoteAsync instead)
        /// </summary>
        [Obsolete("Use DeleteRemoteAsync to prevent UI thread blocking")]
        public void DeleteRemote(string name)
        {
            var remote = _context.Remotes.FirstOrDefault(r => r.RemoteName == name);
            if (remote != null)
            {
                _context.Remotes.Remove(remote);
                _context.SaveChanges();
            }
        }

        #endregion

        #region AI Activity Operations

        /// <summary>
        /// Asynchronously save AI activity log
        /// </summary>
        public async Task SaveAIActivityAsync(AIActivity activity)
        {
            if (activity == null) throw new ArgumentNullException(nameof(activity));

            _context.AIActivities.Add(activity);
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously get AI activities for a commit
        /// </summary>
        public async Task<List<AIActivity>> GetAIActivitiesByCommitAsync(Guid commitId)
        {
            return await _context.AIActivities
                .Where(a => a.CommitId == commitId)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously get recent AI activities
        /// </summary>
        public async Task<List<AIActivity>> GetRecentAIActivitiesAsync(int count = 50)
        {
            if (count <= 0)
                throw new ArgumentException("Count must be greater than zero", nameof(count));

            return await _context.AIActivities
                .OrderByDescending(a => a.Timestamp)
                .Take(count)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        #endregion

        #region Conflict Operations

        /// <summary>
        /// Asynchronously save conflict information
        /// </summary>
        public async Task SaveConflictAsync(Conflict conflict)
        {
            if (conflict == null) throw new ArgumentNullException(nameof(conflict));

            var existing = await _context.Conflicts
                .FindAsync(conflict.ConflictId)
                .ConfigureAwait(false);

            if (existing == null)
            {
                _context.Conflicts.Add(conflict);
            }
            else
            {
                _context.Entry(existing).CurrentValues.SetValues(conflict);
            }

            await _context.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously get unresolved conflicts
        /// </summary>
        public async Task<List<Conflict>> GetUnresolvedConflictsAsync()
        {
            return await _context.Conflicts
                .Where(c => !c.IsResolved)
                .OrderBy(c => c.FilePath)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously mark conflict as resolved
        /// </summary>
        public async Task ResolveConflictAsync(Guid conflictId)
        {
            var conflict = await _context.Conflicts
                .FindAsync(conflictId)
                .ConfigureAwait(false);

            if (conflict != null)
            {
                conflict.IsResolved = true;
                await _context.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Asynchronously check if a commit exists by hash
        /// </summary>
        public async Task<bool> CommitExistsAsync(string commitHash)
        {
            if (string.IsNullOrWhiteSpace(commitHash))
                throw new ArgumentException("Commit hash cannot be null or empty", nameof(commitHash));

            return await _context.Commits
                .AnyAsync(c => c.CommitHash == commitHash)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously get commit by hash
        /// </summary>
        public async Task<Commit> GetCommitByHashAsync(string commitHash)
        {
            if (string.IsNullOrWhiteSpace(commitHash))
                throw new ArgumentException("Commit hash cannot be null or empty", nameof(commitHash));

            return await _context.Commits
                .Include(c => c.Parents)
                .Include(c => c.Snapshot.Files)
                .FirstOrDefaultAsync(c => c.CommitHash == commitHash)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously get database statistics
        /// </summary>
        public async Task<DatabaseStatistics> GetDatabaseStatisticsAsync()
        {
            var stats = new DatabaseStatistics
            {
                TotalCommits = await _context.Commits.CountAsync().ConfigureAwait(false),
                TotalBranches = await _context.Branches.CountAsync().ConfigureAwait(false),
                TotalSnapshots = await _context.Snapshots.CountAsync().ConfigureAwait(false),
                TotalFiles = await _context.Files.CountAsync().ConfigureAwait(false),
                TotalRemotes = await _context.Remotes.CountAsync().ConfigureAwait(false),
                TotalAIActivities = await _context.AIActivities.CountAsync().ConfigureAwait(false),
                UnresolvedConflicts = await _context.Conflicts.CountAsync(c => !c.IsResolved).ConfigureAwait(false)
            };

            return stats;
        }

        #endregion
    }

    /// <summary>
    /// Database statistics for monitoring and diagnostics
    /// </summary>
    public class DatabaseStatistics
    {
        public int TotalCommits { get; set; }
        public int TotalBranches { get; set; }
        public int TotalSnapshots { get; set; }
        public int TotalFiles { get; set; }
        public int TotalRemotes { get; set; }
        public int TotalAIActivities { get; set; }
        public int UnresolvedConflicts { get; set; }
    }
}
