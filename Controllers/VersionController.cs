using System;
using Linage.Core;

namespace Linage.Controllers
{
    public class VersionController
    {
        public string Status { get; set; } = "Idle";
        public DateTime LastRunTime { get; set; }

        public VersionGraphService GraphService { get; set; } = new VersionGraphService();
    }
}
