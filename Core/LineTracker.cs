using System;
using System.Collections.Generic;
using System.Linq;
using Linage.Core.Diff;

namespace Linage.Core
{
    /// <summary>
    /// Generates and tracks line-level changes.
    /// Spec: 5.4
    /// </summary>
    public class LineTracker
    {
        private IDiffStrategy _diffStrategy;
        private readonly HashService _hashService;

        public LineTracker(IDiffStrategy diffStrategy = null)
        {
            _diffStrategy = diffStrategy ?? new MyersDiffStrategy();
            _hashService = new HashService();
        }

        public void SetDiffStrategy(IDiffStrategy strategy)
        {
            _diffStrategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        }

        /// <summary>
        /// Compares two versions of a file and returns the list of line changes.
        /// </summary>
        /// <param name="oldContent">Previous file content.</param>
        /// <param name="newContent">New file content.</param>
        /// <returns>List of LineChange objects representing the evolution.</returns>
        public List<LineChange> GenerateLineChanges(string oldContent, string newContent)
        {
            var oldLines = SplitLines(oldContent);
            var newLines = SplitLines(newContent);

            var opcodes = _diffStrategy.ComputeDiff(oldLines, newLines);
            var changes = new List<LineChange>();

            foreach (var op in opcodes)
            {
                switch (op.Type)
                {
                    case OperationType.Equal:
                        // No change recorded for equal lines in this version of the spec,
                        // or we could track them as unmodified if needed.
                        // Typically VCS only stores deltas or full snapshots + delta metadata.
                        // We will skip producing "LineChange" for Equals unless explicitly asked,
                        // but the spec 4.2.4 implies recording "modifications" (Added/Deleted/Modified).
                        // Strictly speaking, "Equal" isn't a "Change".
                        break;

                    case OperationType.Insert:
                        for (int i = op.NewStart; i < op.NewEnd; i++)
                        {
                            var line = newLines[i];
                            changes.Add(new LineChange
                            {
                                ChangeType = ChangeType.ADDED,
                                LineNumber = i + 1, // 1-based line number in the NEW file
                                NewHash = _hashService.ComputeContentHash(line),
                                OldHash = null,
                                Timestamp = DateTime.Now
                            });
                        }
                        break;

                    case OperationType.Delete:
                        for (int i = op.OldStart; i < op.OldEnd; i++)
                        {
                            var line = oldLines[i];
                            changes.Add(new LineChange
                            {
                                ChangeType = ChangeType.DELETED,
                                LineNumber = i + 1, // 1-based line number in the OLD file (where it existed)
                                NewHash = null,
                                OldHash = _hashService.ComputeContentHash(line),
                                Timestamp = DateTime.Now
                            });
                        }
                        break;
                    
                    // Myers usually outputs Insert/Delete/Equal.
                    // If a strategy outputs "Modify", it means a substitution.
                    case OperationType.Modify:
                         // Treat as Delete (Old) + Insert (New) or explicit Modify if logic supports it.
                         // For atomic line tracking, usually represented as a Delete + Insert pair 
                         // unless we are tracking intra-line edits.
                         // Simple mapping:
                         int count = Math.Min(op.OldEnd - op.OldStart, op.NewEnd - op.NewStart);
                         for(int k=0; k<count; k++)
                         {
                             changes.Add(new LineChange
                             {
                                 ChangeType = ChangeType.MODIFIED,
                                 LineNumber = op.NewStart + k + 1,
                                 OldHash = _hashService.ComputeContentHash(oldLines[op.OldStart + k]),
                                 NewHash = _hashService.ComputeContentHash(newLines[op.NewStart + k]),
                                 Timestamp = DateTime.Now
                             });
                         }
                         // Handle remaining as inserts or deletes if lengths differ (rare for explicit Modify opcode)
                        break;
                }
            }

            return changes;
        }

        private string[] SplitLines(string content)
        {
            if (string.IsNullOrEmpty(content)) return new string[0];
            // Split by universal newlines
            return content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        }
    }
}