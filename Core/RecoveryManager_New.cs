using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Linage.Infrastructure;

namespace Linage.Core
{
    /// <summary>
    /// Manages repository recovery operations - rollback, reflog, and data recovery
    /// </summary>
    public class RecoveryManager
    {
        private readonly MetadataStore _metadataStore;
        private readonly string _repositoryPath;
        private readonly string _reflogPath;
        public int RetryCount { get; set; }

        public RecoveryManager()
        {
            RetryCount = 3;
        }

        public RecoveryManager(MetadataStore metadataStore, string repositoryPath)
        {
            _metadataStore = metadataStore ?? throw new ArgumentNullException(nameof(metadataStore));
            _repositoryPath = repositoryPath ?? throw new ArgumentNullException(nameof(repositoryPath));
            
            _reflogPath = Path.Combine(repositoryPath, ".linage", "logs");
            
            if (!Directory.Exists(_reflogPath))
            {
                Directory.CreateDirectory(_reflogPath);
            }
            RetryCount = 3;
        }

        /// <summary>
        /// Log a reference change (like Git reflog)
        /// </summary>
        public async Task LogRefChangeAsync(string refName, Guid? oldCommitId, Guid newCommitId, string action)
        {
            var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t{oldCommitId?.ToString() ?? "null"}\t{newCommitId}\t{action}";
            var logFile = Path.Combine(_reflogPath, $"{refName}.log");
            
            await Task.Run(() => File.AppendAllText(logFile, logEntry + Environment.NewLine)).ConfigureAwait(false);
        }

        /// <summary>
        /// Get reflog entries for a branch
        /// </summary>
        public async Task<List<string>> GetRefLogAsync(string refName)
        {
            var logFile = Path.Combine(_reflogPath, $"{refName}.log");
            
            if (!File.Exists(logFile))
                return new List<string>();
            
            // File.ReadAllLinesAsync is .NET Core 2.1+, assume we might be on older framework or can use Task.Run
            // Using Task.Run for compatibility if needed, or straightforward await if acceptable.
            // Safe bet involves Task.Run for file I/O wrapper.
            return await Task.Run(() => File.ReadAllLines(logFile).ToList()).ConfigureAwait(false);
        }

        /// <summary>
        /// Rollback a branch to a previous commit
        /// </summary>
        public async Task RollbackBranchAsync(string branchName, Guid targetCommitId)
        {
            var branch = await _metadataStore.GetBranchAsync(branchName);
            if (branch == null)
                throw new ArgumentException($"Branch '{branchName}' not found.");

            var targetCommit = await _metadataStore.GetCommitAsync(targetCommitId);
            if (targetCommit == null)
                throw new ArgumentException($"Target commit '{targetCommitId}' not found.");

            var oldCommitId = branch.HeadCommit?.CommitId;
            
            // Move branch pointer
            branch.MoveHead(targetCommit);
            await _metadataStore.SaveBranchAsync(branch);

            // Log the rollback
            await LogRefChangeAsync(branchName, oldCommitId, targetCommitId, $"rollback to {targetCommitId}");
        }

        /// <summary>
        /// Find dangling commits (commits not reachable from any branch)
        /// </summary>
        public async Task<List<Commit>> FindDanglingCommitsAsync()
        {
            var allCommits = await _metadataStore.GetAllCommitsAsync();
            var branches = await _metadataStore.GetAllBranchesAsync();

            // Find all reachable commits
            var reachable = new HashSet<Guid>();
            foreach (var branch in branches)
            {
                if (branch.HeadCommit != null)
                {
                    TraverseCommits(branch.HeadCommit, reachable);
                }
            }

            // Return unreachable commits
            return allCommits.Where(c => !reachable.Contains(c.CommitId)).ToList();
        }

        private void TraverseCommits(Commit commit, HashSet<Guid> visited)
        {
            if (commit == null || !visited.Add(commit.CommitId))
                return;

            if (commit.Parents != null)
            {
                foreach (var parent in commit.Parents)
                {
                    TraverseCommits(parent, visited);
                }
            }
        }

        /// <summary>
        /// Recover a dangling commit by creating a new branch
        /// </summary>
        public async Task<Branch> RecoverCommitAsync(Guid commitId, string newBranchName)
        {
            var commit = await _metadataStore.GetCommitAsync(commitId);
            if (commit == null)
                throw new ArgumentException($"Commit '{commitId}' not found.");

            var branch = new Branch
            {
                BranchName = newBranchName,
                HeadCommit = commit,
                IsActive = false
            };

            await _metadataStore.SaveBranchAsync(branch);
            await LogRefChangeAsync(newBranchName, null, commitId, $"recovery: created branch from dangling commit");

            return branch;
        }

        /// <summary>
        /// Create a backup snapshot of the repository state
        /// </summary>
        public async Task<string> CreateBackupAsync()
        {
            return await Task.Run(() =>
            {
                var backupDir = Path.Combine(_repositoryPath, ".linage", "backups");
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupPath = Path.Combine(backupDir, $"backup_{timestamp}");
                
                Directory.CreateDirectory(backupPath);

                // Copy database file (if using SQLite) or export metadata
                // For SQL Server, we'd need to use SQL backup commands
                // This is a simplified implementation
                
                return backupPath;
            });
        }
    }
}
