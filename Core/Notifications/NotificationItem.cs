using System;
using System.Collections.Generic;

namespace Linage.Core.Notifications
{
    public class NotificationItem
    {
        public Guid Id { get; private set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public NotificationSeverity Severity { get; set; }
        public DateTime Timestamp { get; private set; }
        public List<NotificationAction> Actions { get; set; }
        public bool IsRead { get; set; }
        public bool IsArchived { get; set; }
        public string Source { get; set; } // e.g., "Git", "System", "Extension"

        public NotificationItem(string title, string message, NotificationSeverity severity = NotificationSeverity.Info)
        {
            Id = Guid.NewGuid();
            Title = title;
            Message = message;
            Severity = severity;
            Timestamp = DateTime.Now;
            Actions = new List<NotificationAction>();
            IsRead = false;
            IsArchived = false;
        }

        public void MarkAsRead()
        {
            IsRead = true;
        }

        public void Archive()
        {
            IsArchived = true;
        }
    }
}
