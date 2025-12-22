using System;

namespace Linage.Core
{
    public class ErrorTrace
    {
        public string Message { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public DateTime Timestamp { get; set; }

        public LineChange LinkedChange { get; set; }
        public Commit LinkedCommit { get; set; }
    }
}
