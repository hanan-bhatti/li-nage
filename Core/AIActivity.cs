using System;

namespace Linage.Core
{
    public enum AssistanceLevel
    {
        MINIMAL,
        MODERATE,
        HEAVY,
        COMPLETION
    }

    public class AIActivity
    {
        public Guid ActivityId { get; set; }
        public Guid CommitId { get; set; }
        public string AITool { get; set; }
        public AssistanceLevel AssistanceLevel { get; set; }
        public DateTime Timestamp { get; set; }
        public int FilesAffected { get; set; }
        public int LinesAffected { get; set; }
        public string Description { get; set; }
        public float Confidence { get; set; }

        public AIActivity()
        {
            ActivityId = Guid.NewGuid();
            Timestamp = DateTime.Now;
        }
    }
}
