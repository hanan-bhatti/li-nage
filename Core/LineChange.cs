using System;

namespace Linage.Core
{
    public enum ChangeType
    {
        ADDED,
        MODIFIED,
        DELETED
    }

    /// <summary>
    /// Represents individual line modifications.
    /// Spec: 4.2.4
    /// </summary>
    public class LineChange
    {
        public Guid ChangeId { get; set; } = Guid.NewGuid();
        public int LineNumber { get; set; }
        public string OldHash { get; set; }
        public string NewHash { get; set; }
        public ChangeType ChangeType { get; set; }
        
        // Associate with a commit for tracking
        public Guid? CommitId { get; set; }
        
        // Spec 3.2.5 mentions Timestamp and Author, adding them as optional or future-proof props
        // though 4.2.4 strictly lists the core logic props.
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public bool IsAddition() => ChangeType == ChangeType.ADDED;
        public bool IsDeletion() => ChangeType == ChangeType.DELETED;
        public bool IsModification() => ChangeType == ChangeType.MODIFIED;
    }
}
