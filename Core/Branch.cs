using System;
using System.Collections.Generic;
using System.Linq;

namespace Linage.Core
{
    /// <summary>
    /// Development line representation.
    /// Spec: 4.2.5
    /// </summary>
    public class Branch
    {
        public Guid BranchId { get; set; } = Guid.NewGuid();
        public string BranchName { get; set; }
        public Commit HeadCommit { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public void MoveHead(Commit commit)
        {
            if (commit == null) throw new ArgumentNullException(nameof(commit));
            HeadCommit = commit;
        }

        public List<Commit> GetHistory()
        {
            var history = new List<Commit>();
            var current = HeadCommit;
            
            // Simple linear traversal for now, real implementation might need topological sort 
            // if we want to represent the full graph, but "History" often implies linear ancestry of HEAD.
            // Using a queue for BFS or simple parent traversal.
            // Since a commit can have multiple parents, history is a DAG.
            // This method returns a flattened list or just the primary lineage?
            // Spec says "Returns commit history".
            
            if (current == null) return history;

            var visited = new HashSet<Guid>();
            var stack = new Stack<Commit>();
            stack.Push(current);

            while (stack.Count > 0)
            {
                var c = stack.Pop();
                if (visited.Add(c.CommitId))
                {
                    history.Add(c);
                    if (c.Parents != null)
                    {
                        foreach (var p in c.Parents)
                        {
                            if (!visited.Contains(p.CommitId))
                                stack.Push(p);
                        }
                    }
                }
            }
            
            // Sort by timestamp descending typically
            return history.OrderByDescending(c => c.Timestamp).ToList();
        }

        public bool IsAncestorOf(Commit commit)
        {
            if (commit == null || HeadCommit == null) return false;
            
            // Check if 'HeadCommit' is an ancestor of 'commit'
            // Wait, usually we check if 'commit' is an ancestor of 'HeadCommit' (merged)
            // or if 'HeadCommit' is an ancestor of 'commit' (fast-forwardable).
            // Naming convention "IsAncestorOf(commit)" usually means: Is THIS(Head) an ancestor of PARAM(commit)?
            
            var ancestors = commit.GetAllParents();
            // Include self
            if (commit.CommitId == HeadCommit.CommitId) return true;
            
            return ancestors.Any(a => a.CommitId == HeadCommit.CommitId);
        }

        public Commit GetDivergencePoint(Branch other)
        {
            if (other == null || other.HeadCommit == null || this.HeadCommit == null) return null;

            var myHistory = new HashSet<Guid>(this.GetHistory().Select(c => c.CommitId));
            var otherHistory = other.GetHistory();

            // Find the first commit in other's history that exists in my history
            // (assuming history is ordered by time/topology descending)
            foreach (var commit in otherHistory)
            {
                if (myHistory.Contains(commit.CommitId))
                {
                    return commit;
                }
            }

            return null;
        }
    }
}