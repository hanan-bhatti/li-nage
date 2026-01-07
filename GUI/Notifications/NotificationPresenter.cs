using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Linage.Core.Notifications;
using Linage.Infrastructure.Services;

namespace Linage.GUI.Notifications
{
    public class NotificationPresenter
    {
        private readonly Form _mainForm;
        private readonly List<ToastNotification> _activeToasts;
        private NotificationCenterPanel _centerPanel;

        public NotificationPresenter(Form mainForm)
        {
            _mainForm = mainForm;
            _activeToasts = new List<ToastNotification>();

            _centerPanel = new NotificationCenterPanel
            {
                Visible = false,
                Location = new Point(_mainForm.Width - 360, _mainForm.Height - 450), // Initial pos
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            _mainForm.Controls.Add(_centerPanel);
            _centerPanel.BringToFront();

            NotificationManager.Instance.NotificationAdded += OnNotificationAdded;
            NotificationManager.Instance.NotificationDismissed += OnNotificationDismissed;
            
            _mainForm.Resize += (s, e) => RepositionToasts();
        }

        private void OnNotificationAdded(object sender, NotificationItem item)
        {
            if (_mainForm.InvokeRequired)
            {
                _mainForm.Invoke(new Action(() => OnNotificationAdded(sender, item)));
                return;
            }

            ShowToast(item);
        }

        private void OnNotificationDismissed(object sender, NotificationItem item)
        {
             // Handled by toast internal events mostly, but kept for sync
        }

        private void ShowToast(NotificationItem item)
        {
            var toast = new ToastNotification(item);
            _activeToasts.Add(toast);

            toast.Dismissed += (s, e) =>
            {
                _activeToasts.Remove(toast);
                RepositionToasts();
            };

            RepositionToasts();
            toast.Show(_mainForm);
        }

        private void RepositionToasts()
        {
            int bottomMargin = 40; // Above status bar
            int rightMargin = 20;
            int spacing = 10;

            // Stack from bottom up
            int currentBottom = _mainForm.ClientSize.Height - bottomMargin;

            // We iterate in reverse to stack newest at bottom?
            // "Newest at bottom" usually means it spawns at bottom and pushes others up.
            // Let's implement newest-at-bottom stack.
            
            for (int i = _activeToasts.Count - 1; i >= 0; i--)
            {
                var toast = _activeToasts[i];
                int x = _mainForm.ClientSize.Width - toast.Width - rightMargin;
                int y = currentBottom - toast.Height;
                
                toast.Location = new Point(x, y);
                currentBottom -= (toast.Height + spacing);
            }
        }

        public void ToggleCenter()
        {
            _centerPanel.Visible = !_centerPanel.Visible;
            if (_centerPanel.Visible)
            {
                _centerPanel.BringToFront();
                _centerPanel.RefreshList();
                // Position it relative to bell? Or just bottom right fixed.
                _centerPanel.Location = new Point(_mainForm.ClientSize.Width - _centerPanel.Width - 10, _mainForm.ClientSize.Height - _centerPanel.Height - 40);
            }
        }
    }
}
