using System;
using System.Drawing;
using System.Windows.Forms;
using Linage.Core.Notifications;
using Linage.GUI.Theme;

namespace Linage.GUI.Notifications
{
    public class ToastNotification : Form
    {
        private readonly NotificationItem _notification;
        private Timer _timer;
        private bool _isHovered;
        private const int ToastWidth = 350;
        private const int ToastMinHeight = 80;

        public event EventHandler Dismissed;

        public NotificationItem Item => _notification;

        public ToastNotification(NotificationItem notification, int durationMs = 5000)
        {
            _notification = notification;
            InitializeComponent();
            SetupTimer(durationMs);
        }

        private void InitializeComponent()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Size = new Size(ToastWidth, ToastMinHeight);
            this.BackColor = ModernTheme.SurfaceColor;
            this.Padding = new Padding(1); // Border
            this.DoubleBuffered = true;

            // Icon
            var lblIcon = new Label
            {
                Text = GetIconForSeverity(_notification.Severity),
                Font = new Font("Segoe MDL2 Assets", 14f),
                ForeColor = GetColorForSeverity(_notification.Severity),
                Size = new Size(40, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(10, 10)
            };
            this.Controls.Add(lblIcon);

            // Title (Optional, VS Code usually just has message, but we support title)
            int contentLeft = 50;
            int contentWidth = ToastWidth - contentLeft - 40; // Reserved for close button
            
            int currentY = 15;
            if (!string.IsNullOrEmpty(_notification.Title))
            {
                var lblTitle = new Label
                {
                    Text = _notification.Title,
                    Font = new Font(ModernTheme.FontBody.FontFamily, 10f, FontStyle.Bold),
                    ForeColor = ModernTheme.TextPrimary,
                    Location = new Point(contentLeft, currentY),
                    Size = new Size(contentWidth, 20),
                    AutoEllipsis = true
                };
                this.Controls.Add(lblTitle);
                currentY += 20;
            }

            // Message
            var lblMessage = new Label
            {
                Text = _notification.Message,
                Font = ModernTheme.FontBody,
                ForeColor = ModernTheme.TextPrimary,
                Location = new Point(contentLeft, currentY),
                Size = new Size(contentWidth, 100), // Height will adjust
                AutoSize = true,
                MaximumSize = new Size(contentWidth, 0)
            };
            this.Controls.Add(lblMessage);
            
            // Force layout to get accurate height
            var preferredSize = lblMessage.GetPreferredSize(new Size(contentWidth, 0));
            lblMessage.Size = preferredSize;

            // Close Button
            var btnClose = new Label
            {
                Text = "\uE711", // X
                Font = new Font("Segoe MDL2 Assets", 10f),
                ForeColor = ModernTheme.TextSecondary,
                Size = new Size(24, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                Location = new Point(ToastWidth - 30, 8)
            };
            btnClose.Click += (s, e) => CloseToast();
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = ModernTheme.TextPrimary;
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = ModernTheme.TextSecondary;
            this.Controls.Add(btnClose);

            // Adjust Height based on message
            int requiredHeight = lblMessage.Bottom + 15;

            // Actions
            if (_notification.Actions != null && _notification.Actions.Count > 0)
            {
                requiredHeight += 10;
                int actionX = contentLeft;
                foreach (var action in _notification.Actions)
                {
                    var btnAction = new Button
                    {
                        Text = action.Title,
                        AutoSize = true,
                        FlatStyle = FlatStyle.Flat,
                        BackColor = action.IsPrimary ? ModernTheme.PrimaryColor : ModernTheme.SurfaceLight,
                        ForeColor = ModernTheme.TextPrimary,
                        Font = ModernTheme.FontSmall,
                        Location = new Point(actionX, requiredHeight),
                        Cursor = Cursors.Hand
                    };
                    btnAction.FlatAppearance.BorderSize = 0;
                    btnAction.Click += (s, e) =>
                    {
                        action.Action?.Invoke();
                        CloseToast();
                    };
                    this.Controls.Add(btnAction);
                    actionX += btnAction.Width + 10;
                }
                requiredHeight += 40; // Button height + padding
            }

            this.Height = Math.Max(requiredHeight, ToastMinHeight);

            // Paint Border
            this.Paint += (s, e) =>
            {
                using (var pen = new Pen(ModernTheme.BorderColor))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }
                // Draw severity stripe
                // using (var brush = new SolidBrush(GetColorForSeverity(_notification.Severity)))
                // {
                //      e.Graphics.FillRectangle(brush, 0, 0, 4, Height);
                // }
            };

            // Hover effects
            this.MouseEnter += OnMouseEnter;
            this.MouseLeave += OnMouseLeave;
            foreach (Control c in this.Controls)
            {
                c.MouseEnter += OnMouseEnter;
                c.MouseLeave += OnMouseLeave;
            }
        }

        private void SetupTimer(int durationMs)
        {
            if (durationMs > 0 && _notification.Severity != NotificationSeverity.Error)
            {
                _timer = new Timer { Interval = durationMs };
                _timer.Tick += (s, e) =>
                {
                    if (!_isHovered) CloseToast();
                };
                _timer.Start();
            }
        }

        private void OnMouseEnter(object sender, EventArgs e)
        {
            _isHovered = true;
            _timer?.Stop();
        }

        private void OnMouseLeave(object sender, EventArgs e)
        {
            _isHovered = false;
            _timer?.Start();
        }

        public void CloseToast()
        {
            _timer?.Stop();
            Dismissed?.Invoke(this, EventArgs.Empty);
            this.Close();
        }

        private string GetIconForSeverity(NotificationSeverity severity)
        {
            switch (severity)
            {
                case NotificationSeverity.Success: return "\uE73E"; // Check
                case NotificationSeverity.Warning: return "\uE7BA"; // Warning
                case NotificationSeverity.Error: return "\uEA39";   // Error
                case NotificationSeverity.Progress: return "\uE768"; // Sync/Rotate
                case NotificationSeverity.Question: return "\uE9CE"; // Question Info
                default: return "\uE946"; // Info
            }
        }

        private Color GetColorForSeverity(NotificationSeverity severity)
        {
             switch (severity)
            {
                case NotificationSeverity.Success: return ModernTheme.SuccessColor;
                case NotificationSeverity.Warning: return ModernTheme.WarningColor;
                case NotificationSeverity.Error: return ModernTheme.ErrorColor;
                case NotificationSeverity.Question: return ModernTheme.PrimaryColor;
                default: return ModernTheme.PrimaryColor; // Info Blue
            }
        }

        protected override bool ShowWithoutActivation => true;
    }
}
