using System;
using System.Collections.Generic;

namespace Linage.Core
{
    public class Commit
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        
        public Snapshot State { get; set; }
        public List<Commit> Parents { get; set; } = new List<Commit>();
    }
}
