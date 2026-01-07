using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Linage.GUI.Theme;

namespace Linage.GUI
{
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }

    public class NotificationService
    {
        private static Form _owner;
        private static List<ToastNotification> _activeToasts = new List<ToastNotification>();
        private const int MaxToasts = 5;

        public static void Initialize(Form owner)
        {
            _owner = owner;
        }

        public static void Show(string message, NotificationType type = NotificationType.Info, int durationMs = -1)
        {
            if (_owner == null) return;

            // Set default durations if not specified
            if (durationMs == -1)
            {
                switch (type)
                {
                    case NotificationType.Success: durationMs = 3000; break; // Fast
                    case NotificationType.Warning: durationMs = 8000; break; // Slower
                    case NotificationType.Error: durationMs = 0; break;      // Persistent
                    default: durationMs = 5000; break;
                }
            }

            if (_owner.InvokeRequired)
            {
                _owner.Invoke(new Action(() => Show(message, type, durationMs)));
                return;
            }

            var toast = new ToastNotification(message, type, durationMs);
            _activeToasts.Add(toast);
            toast.Closed += (s, e) => {
                _activeToasts.Remove(toast);
                RepositionToasts();
            };

            RepositionToasts();
            toast.Show(_owner);
        }

        private static void RepositionToasts()
        {
            int bottomMargin = 40; // Above status bar
            int rightMargin = 20;
            int spacing = 10;

            for (int i = 0; i < _activeToasts.Count; i++)
            {
                var toast = _activeToasts[i];
                int x = _owner.Right - toast.Width - rightMargin;
                int y = _owner.Bottom - ((i + 1) * (toast.Height + spacing)) - bottomMargin;
                toast.Location = new Point(x, y);
            }
        }
    }

    public class ToastNotification : Form
    {
        private Timer _timer;
        private NotificationType _type;

        public ToastNotification(string message, NotificationType type, int durationMs)
        {
            _type = type;
            InitializeComponent(message);
            
            if (durationMs > 0)
            {
                _timer = new Timer { Interval = durationMs };
                _timer.Tick += (s, e) => Close();
                _timer.Start();
            }
        }

        private void InitializeComponent(string message)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Size = new Size(320, 70); // Slightly larger
            this.BackColor = ModernTheme.SurfaceLight;
            this.Padding = new Padding(1); // Border

            var accentColor = GetColorForType(_type);

            var panel = new Panel { Dock = DockStyle.Fill, BackColor = ModernTheme.SurfaceLight };
            var lblIcon = new Label { 
                Text = GetIconForType(_type), 
                Font = new Font("Segoe MDL2 Assets", 14f), // Larger icon
                ForeColor = accentColor,
                Size = new Size(50, 70),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Left
            };
            
            var lblMessage = new Label {
                Text = message,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.FontBody,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(5, 5, 5, 5)
            };

            var btnClose = new Label {
                Text = "\uE8BB",
                Font = new Font("Segoe MDL2 Assets", 9f),
                ForeColor = ModernTheme.TextSecondary,
                Size = new Size(30, 25),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                Location = new Point(290, 5)
            };
            btnClose.Click += (s, e) => Close();

            var stripe = new Panel {
                BackColor = accentColor,
                Width = 4,
                Dock = DockStyle.Left
            };

            panel.Controls.Add(lblMessage);
            panel.Controls.Add(lblIcon);
            panel.Controls.Add(stripe);
            panel.Controls.Add(btnClose);
            this.Controls.Add(panel);

            // Add border (Colored if Error)
            this.Paint += (s, e) => {
                var borderColor = _type == NotificationType.Error ? accentColor : ModernTheme.BorderColor;
                using (var pen = new Pen(borderColor))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }
            };
        }

        private Color GetColorForType(NotificationType type)
        {
            switch (type)
            {
                case NotificationType.Success: return ModernTheme.SuccessColor;
                case NotificationType.Warning: return ModernTheme.WarningColor;
                case NotificationType.Error: return ModernTheme.ErrorColor;
                default: return ModernTheme.PrimaryColor;
            }
        }

        private string GetIconForType(NotificationType type)
        {
            switch (type)
            {
                case NotificationType.Success: return "\uE73E";
                case NotificationType.Warning: return "\uE7BA";
                case NotificationType.Error: return "\uEA39";
                default: return "\uE946";
            }
        }

        protected override bool ShowWithoutActivation => true;
    }
}
