using System;

namespace Linage.Infrastructure
{
    public class FileChangeEvent : EventArgs
    {
        public string Path { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
