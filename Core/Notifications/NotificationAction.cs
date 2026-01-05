using System;

namespace Linage.Core.Notifications
{
    public class NotificationAction
    {
        public string Title { get; set; }
        public Action Action { get; set; }
        public bool IsPrimary { get; set; }

        public NotificationAction(string title, Action action, bool isPrimary = false)
        {
            Title = title;
            Action = action;
            IsPrimary = isPrimary;
        }
    }
}
