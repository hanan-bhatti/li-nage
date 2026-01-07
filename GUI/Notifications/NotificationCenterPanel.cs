using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Linage.Core.Notifications;
using Linage.GUI.Theme;
using Linage.Infrastructure.Services;

namespace Linage.GUI.Notifications
{
    public class NotificationCenterPanel : Panel
    {
        private FlowLayoutPanel _listPanel;
        private Label _lblEmpty;

        public NotificationCenterPanel()
        {
            InitializeComponent();
            RefreshList();
            
            // Subscribe to updates
            NotificationManager.Instance.NotificationAdded += (s, e) => { if(this.Visible) RefreshList(); };
            NotificationManager.Instance.HistoryCleared += (s, e) => RefreshList();
        }

        private void InitializeComponent()
        {
            this.BackColor = ModernTheme.SurfaceColor;
            this.Size = new Size(350, 400); // Default size
            this.Padding = new Padding(1); // Border

            // Header
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = ModernTheme.SurfaceLight
            };

            var lblTitle = new Label
            {
                Text = "NOTIFICATIONS",
                Font = new Font(ModernTheme.FontBody.FontFamily, 9f, FontStyle.Bold),
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(10, 10)
            };

            var btnClear = new Label
            {
                Text = "\uE894", // Clear All Icon
                Font = new Font("Segoe MDL2 Assets", 10f),
                ForeColor = ModernTheme.TextSecondary,
                Size = new Size(24, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                Dock = DockStyle.Right
            };
            btnClear.Click += (s, e) => NotificationManager.Instance.ClearHistory();

            header.Controls.Add(lblTitle);
            header.Controls.Add(btnClear);
            this.Controls.Add(header);

            // List
            _listPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = ModernTheme.SurfaceColor
            };
            this.Controls.Add(_listPanel);

            // Empty State
            _lblEmpty = new Label
            {
                Text = "No New Notifications",
                ForeColor = ModernTheme.TextSecondary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };
            _listPanel.Controls.Add(_lblEmpty); // Add to flow? No, flow usually creates list items.
            // Better to add to main panel and bring to front if empty.
            this.Controls.Add(_lblEmpty);
            _lblEmpty.BringToFront();

            // Border Paint
            this.Paint += (s, e) =>
            {
               using (var pen = new Pen(ModernTheme.BorderColor))
               {
                   e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
               }
            };
        }

        public void RefreshList()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(RefreshList));
                return;
            }

            _listPanel.SuspendLayout();
            _listPanel.Controls.Clear();
            _lblEmpty.Visible = false;

            var history = NotificationManager.Instance.GetHistory().ToList();

            if (history.Count == 0)
            {
                _lblEmpty.Visible = true;
            }
            else
            {
                foreach (var item in history)
                {
                    var itemPanel = CreateItemControl(item);
                    _listPanel.Controls.Add(itemPanel);
                }
            }

            _listPanel.ResumeLayout();
        }

        private Control CreateItemControl(NotificationItem item)
        {
            var panel = new Panel
            {
                Width = this.Width - 25, // Scrollbar margin
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(5),
                Margin = new Padding(0, 0, 0, 1) // Separator logic via margin? Or draw line
            };

            // Icon
            var lblIcon = new Label
            {
                Text = GetIcon(item.Severity),
                Font = new Font("Segoe MDL2 Assets", 12f),
                ForeColor = GetColor(item.Severity),
                Size = new Size(20, 20),
                Location = new Point(5, 5)
            };

            // Title/Message
            var lblText = new Label
            {
                Text = string.IsNullOrEmpty(item.Title) ? item.Message : $"{item.Title}\n{item.Message}",
                Font = ModernTheme.FontBody,
                ForeColor = ModernTheme.TextPrimary,
                Location = new Point(30, 5),
                AutoSize = true,
                MaximumSize = new Size(panel.Width - 60, 0)
            };
            
            // Force measurement
            lblText.Size = lblText.GetPreferredSize(new Size(lblText.MaximumSize.Width, 0));
            
            // Delete specific item (X)
             var btnDelete = new Label
            {
                Text = "\uE711",
                Font = new Font("Segoe MDL2 Assets", 8f),
                ForeColor = ModernTheme.TextSecondary,
                Size = new Size(20, 20),
                Location = new Point(panel.Width - 25, 5),
                Cursor = Cursors.Hand
            };
            // Logic to remove single item? Manager doesn't support it yet publicly except via history manipulation
            // For now, hide it or implement later.
            btnDelete.Visible = false; 

            panel.Controls.Add(lblIcon);
            panel.Controls.Add(lblText);
            panel.Controls.Add(btnDelete);

             // Bottom Border
            panel.Paint += (s, e) => {
                 using (var pen = new Pen(ModernTheme.BorderColor))
                 {
                     e.Graphics.DrawLine(pen, 0, panel.Height - 1, panel.Width, panel.Height - 1);
                 }
            };

            return panel;
        }

        private string GetIcon(NotificationSeverity s)
        {
             switch (s)
            {
                case NotificationSeverity.Success: return "\uE73E";
                case NotificationSeverity.Warning: return "\uE7BA";
                case NotificationSeverity.Error: return "\uEA39";
                default: return "\uE946";
            }
        }

        private Color GetColor(NotificationSeverity s)
        {
            switch (s)
            {
                case NotificationSeverity.Success: return ModernTheme.SuccessColor;
                case NotificationSeverity.Warning: return ModernTheme.WarningColor;
                case NotificationSeverity.Error: return ModernTheme.ErrorColor;
                default: return ModernTheme.PrimaryColor;
            }
        }
    }
}
