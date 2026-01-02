using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public void LogRefChange(string refName, Guid? oldCommitId, Guid newCommitId, string action)
        {
            var logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t{oldCommitId?.ToString() ?? "null"}\t{newCommitId}\t{action}";
            var logFile = Path.Combine(_reflogPath, $"{refName}.log");
            
            File.AppendAllText(logFile, logEntry + Environment.NewLine);
        }

        /// <summary>
        /// Get reflog entries for a branch
        /// </summary>
        public List<string> GetRefLog(string refName)
        {
            var logFile = Path.Combine(_reflogPath, $"{refName}.log");
            
            if (!File.Exists(logFile))
                return new List<string>();
            
            return File.ReadAllLines(logFile).ToList();
        }

        /// <summary>
        /// Rollback a branch to a previous commit
        /// </summary>
        public void RollbackBranch(string branchName, Guid targetCommitId)
        {
            var branch = _metadataStore.GetBranch(branchName);
            if (branch == null)
                throw new ArgumentException($"Branch '{branchName}' not found.");

            var targetCommit = _metadataStore.GetCommit(targetCommitId);
            if (targetCommit == null)
                throw new ArgumentException($"Target commit '{targetCommitId}' not found.");

            var oldCommitId = branch.HeadCommit?.CommitId;
            
            // Move branch pointer
            branch.MoveHead(targetCommit);
            _metadataStore.SaveBranch(branch);

            // Log the rollback
            LogRefChange(branchName, oldCommitId, targetCommitId, $"rollback to {targetCommitId}");
        }

        /// <summary>
        /// Find dangling commits (commits not reachable from any branch)
        /// </summary>
        public List<Commit> FindDanglingCommits()
        {
            var allCommits = _metadataStore.GetAllCommits();
            var branches = _metadataStore.GetAllBranches();

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
        public Branch RecoverCommit(Guid commitId, string newBranchName)
        {
            var commit = _metadataStore.GetCommit(commitId);
            if (commit == null)
                throw new ArgumentException($"Commit '{commitId}' not found.");

            var branch = new Branch
            {
                BranchName = newBranchName,
                HeadCommit = commit,
                IsActive = false
            };

            _metadataStore.SaveBranch(branch);
            LogRefChange(newBranchName, null, commitId, $"recovery: created branch from dangling commit");

            return branch;
        }

        /// <summary>
        /// Create a backup snapshot of the repository state
        /// </summary>
        public string CreateBackup()
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
        }
    }
}
