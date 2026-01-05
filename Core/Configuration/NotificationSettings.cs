using System.Collections.Generic;
using Linage.Core.Notifications;

namespace Linage.Core.Configuration
{
    public class NotificationSettings
    {
        private static NotificationSettings _instance;
        public static NotificationSettings Instance => _instance ?? (_instance = new NotificationSettings());

        public bool SoundEnabled { get; set; } = true;
        public bool ShowToasts { get; set; } = true;
        public int ToastDurationMs { get; set; } = 5000;
        public int MaxHistoryCount { get; set; } = 100;

        public Dictionary<NotificationSeverity, bool> EnabledSeverities { get; set; }

        private NotificationSettings()
        {
            EnabledSeverities = new Dictionary<NotificationSeverity, bool>
            {
                { NotificationSeverity.Info, true },
                { NotificationSeverity.Success, true },
                { NotificationSeverity.Warning, true },
                { NotificationSeverity.Error, true },
                { NotificationSeverity.Progress, true }
            };
        }

        public bool IsSeverityEnabled(NotificationSeverity severity)
        {
            return EnabledSeverities.TryGetValue(severity, out bool enabled) && enabled;
        }
    }
}
