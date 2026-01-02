using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Linage.Core.Diff;

namespace Linage.Core
{
    public class MergeService
    {
        private readonly IDiffStrategy _diffStrategy;

        public MergeService()
        {
            // Default to Patient Diff (Phase 5)
            _diffStrategy = new PatientDiffStrategy();
        }
        
        public MergeService(IDiffStrategy diffStrategy)
        {
            _diffStrategy = diffStrategy ?? new PatientDiffStrategy();
        }

        public class MergeResult
        {
            public bool Success { get; set; }
            public List<Conflict> Conflicts { get; set; } = new List<Conflict>();
            public Dictionary<string, string> MergedFiles { get; set; } = new Dictionary<string, string>();
        }

        /// <summary>
        /// Performs a 3-way merge on a single file.
        /// Implements intelligent line-by-line merging.
        /// </summary>
        public MergeResult MergeFile(string filePath, string baseText, string localText, string remoteText)
        {
            var result = new MergeResult();

            // 1. If local == remote, no change or same change -> Keep local
            if (localText == remoteText)
            {
                result.MergedFiles[filePath] = localText;
                result.Success = true;
                return result;
            }

            // 2. If base == local, only remote changed -> Take remote
            if (baseText == localText)
            {
                result.MergedFiles[filePath] = remoteText;
                result.Success = true;
                return result;
            }

            // 3. If base == remote, only local changed -> Take local
            if (baseText == remoteText)
            {
                result.MergedFiles[filePath] = localText;
                result.Success = true;
                return result;
            }

            // 4. Real 3-way merge required - both sides changed
            try
            {
                var merged = PerformLineBasedMerge(baseText, localText, remoteText);
                if (merged.HasConflict)
                {
                    result.Success = false;
                    result.Conflicts.Add(new Conflict(filePath, baseText, localText, remoteText));
                    // Generate conflict markers
                    result.MergedFiles[filePath] = merged.Content;
                }
                else
                {
                    result.Success = true;
                    result.MergedFiles[filePath] = merged.Content;
                }
            }
            catch (Exception)
            {
                result.Success = false;
                result.Conflicts.Add(new Conflict(filePath, baseText, localText, remoteText));
            }
            
            return result;
        }
        
        private class MergeAttempt
        {
            public string Content { get; set; }
            public bool HasConflict { get; set; }
        }
        
        /// <summary>
        /// Perform intelligent line-based 3-way merge
        /// </summary>
        private MergeAttempt PerformLineBasedMerge(string baseText, string localText, string remoteText)
        {
            var baseLines = SplitLines(baseText);
            var localLines = SplitLines(localText);
            var remoteLines = SplitLines(remoteText);
            
            // Compute diffs from base to local and base to remote
            var localDiff = _diffStrategy.ComputeDiff(baseLines.ToArray(), localLines.ToArray());
            var remoteDiff = _diffStrategy.ComputeDiff(baseLines.ToArray(), remoteLines.ToArray());
            
            var mergedLines = new List<string>();
            
            var localOps = localDiff.ToList();
            var remoteOps = remoteDiff.ToList();
            
            // Simplified merge: Check for overlapping changes
            var localChangedLines = new HashSet<int>();
            var remoteChangedLines = new HashSet<int>();
            
            foreach (var op in localOps)
            {
                for (int i = op.OldStart; i < op.OldEnd; i++)
                    localChangedLines.Add(i);
            }
            
            foreach (var op in remoteOps)
            {
                for (int i = op.OldStart; i < op.OldEnd; i++)
                    remoteChangedLines.Add(i);
            }
            
            // Detect conflicts: lines changed in both
            var conflictingLines = new HashSet<int>(localChangedLines);
            conflictingLines.IntersectWith(remoteChangedLines);
            
            if (conflictingLines.Count > 0)
            {
                // Has conflicts - generate conflict markers
                var sb = new StringBuilder();
                sb.AppendLine("<<<<<<< LOCAL");
                sb.AppendLine(localText);
                sb.AppendLine("=======");
                sb.AppendLine(remoteText);
                sb.AppendLine(">>>>>>> REMOTE");
                
                return new MergeAttempt { Content = sb.ToString(), HasConflict = true };
            }
            
            // No conflicts - merge changes
            // Simple approach: apply local changes, then remote changes
            // In a production system, would need more sophisticated merge algorithm
            
            // For non-conflicting changes, take the union
            // If local changed a line, use local version
            // If remote changed a line, use remote version
            // Otherwise use base
            
            for (int i = 0; i < Math.Max(Math.Max(baseLines.Count, localLines.Count), remoteLines.Count); i++)
            {
                if (localChangedLines.Contains(i))
                {
                    if (i < localLines.Count)
                        mergedLines.Add(localLines[i]);
                }
                else if (remoteChangedLines.Contains(i))
                {
                    if (i < remoteLines.Count)
                        mergedLines.Add(remoteLines[i]);
                }
                else
                {
                    if (i < baseLines.Count)
                        mergedLines.Add(baseLines[i]);
                }
            }
            
            return new MergeAttempt 
            { 
                Content = string.Join(Environment.NewLine, mergedLines),
                HasConflict = false 
            };
        }
        
        private List<string> SplitLines(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new List<string>();
                
            return text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
        }
    }
}
