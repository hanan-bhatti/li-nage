using System;

namespace Linage.Core
{
    public class AIAccessLog
    {
        public string AccessType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        
        public AITool Tool { get; set; }
        public LineChange ContextChange { get; set; }
    }
}
