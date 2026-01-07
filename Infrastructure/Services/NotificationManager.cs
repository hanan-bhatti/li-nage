using System;
using System.Collections.Generic;
using System.Linq;
using Linage.Core.Notifications;

namespace Linage.Infrastructure.Services
{
    public class NotificationManager
    {
        private static NotificationManager _instance;
        public static NotificationManager Instance => _instance ?? (_instance = new NotificationManager());

        private readonly List<NotificationItem> _history;
        private readonly List<NotificationItem> _activeToasts;
        
        // Configuration from Settings
        private int MaxHistoryCount => Linage.Core.Configuration.NotificationSettings.Instance.MaxHistoryCount;

        public event EventHandler<NotificationItem> NotificationAdded;
        public event EventHandler<NotificationItem> NotificationDismissed; // From toast view
        public event EventHandler HistoryCleared;

        private NotificationManager()
        {
            _history = new List<NotificationItem>();
            _activeToasts = new List<NotificationItem>();
        }

        public void Show(string title, string message, NotificationSeverity severity = NotificationSeverity.Info, List<NotificationAction> actions = null, string source = "System")
        {
            var notification = new NotificationItem(title, message, severity)
            {
                Source = source
            };

            if (actions != null)
            {
                notification.Actions = actions;
            }

            // specific logic: Errors stick until manually dismissed? 
            // For now, we add everything to history.
            AddToHistory(notification);
            
            // Add to active toasts (controlled by UI)
            _activeToasts.Add(notification);

            NotificationAdded?.Invoke(this, notification);
        }

        public void ShowError(string title, string message, Exception ex = null)
        {
            var fullMessage = ex != null ? $"{message}\n{ex.Message}" : message;
            Show(title, fullMessage, NotificationSeverity.Error);
        }

        public void ShowSuccess(string title, string message)
        {
            Show(title, message, NotificationSeverity.Success);
        }

        public void ShowWarning(string title, string message)
        {
            Show(title, message, NotificationSeverity.Warning);
        }

        public void ShowConfirmation(string title, string message, Action onYes, Action onNo = null)
        {
            var actions = new List<NotificationAction>
            {
                new NotificationAction("Yes", onYes, true),
                new NotificationAction("No", onNo ?? (() => { }))
            };
            Show(title, message, NotificationSeverity.Question, actions);
        }

        public void DismissToast(NotificationItem item)
        {
            if (_activeToasts.Contains(item))
            {
                _activeToasts.Remove(item);
                NotificationDismissed?.Invoke(this, item);
            }
        }

        public void MarkAllAsRead()
        {
            foreach (var item in _history)
            {
                item.MarkAsRead();
            }
        }

        private void AddToHistory(NotificationItem item)
        {
            _history.Insert(0, item);
            if (_history.Count > MaxHistoryCount)
            {
                _history.RemoveAt(_history.Count - 1);
            }
        }

        public IEnumerable<NotificationItem> GetHistory()
        {
            return _history.AsReadOnly();
        }

        public int GetUnreadCount()
        {
            return _history.Count(n => !n.IsRead);
        }

        public void ClearHistory()
        {
            _history.Clear();
            HistoryCleared?.Invoke(this, EventArgs.Empty);
        }
    }
}
