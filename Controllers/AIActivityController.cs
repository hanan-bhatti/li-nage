using System;
using Linage.Core;

namespace Linage.Controllers
{
    public class AIActivityController
    {
        public string Status { get; set; } = "Idle";
        public DateTime LastRunTime { get; set; }

        public AIAccessLog LogService { get; set; }
    }
}
