using System;
using Linage.Core;

namespace Linage.Controllers
{
    public class DebugController
    {
        public string Status { get; set; } = "Idle";
        public DateTime LastRunTime { get; set; }

        public LineTracker Tracker { get; set; } = new LineTracker();
        public ErrorTrace Trace { get; set; }
        public SolutionIndex Solutions { get; set; }
    }
}
