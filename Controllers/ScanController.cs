using System;
using Linage.Core;

namespace Linage.Controllers
{
    public class ScanController
    {
        public string Status { get; set; } = "Idle";
        public DateTime LastRunTime { get; set; }

        public FileScanner Scanner { get; set; } = new FileScanner();
        public ChangeDetector Detector { get; set; } = new ChangeDetector();
    }
}
