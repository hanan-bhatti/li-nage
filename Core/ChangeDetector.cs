using Linage.Infrastructure;

namespace Linage.Core
{
    public class ChangeDetector
    {
        public string Mode { get; set; } = "RealTime";
        public string LastScanHash { get; set; } = string.Empty;
        public RecoveryManager Recovery { get; set; }

        public void ReceiveEvent(FileChangeEvent evt) 
        {
            // Logic
        }
    }
}
