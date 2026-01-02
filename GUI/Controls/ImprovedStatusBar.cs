using System;
using System.Drawing;
using System.Windows.Forms;
using Linage.Core.Configuration;
using Linage.GUI.Configuration;
using Linage.GUI.Services;

namespace Linage.GUI.Controls
{
    /// <summary>
    /// Modern status bar with proper spacing, typography, and icon usage.
    /// </summary>
    public class ImprovedStatusBar
    {
        private readonly ModernStatusBar _statusBar;
        private readonly IDialogService _dialogService;

        public Label BranchLabel { get; private set; }
        public Label RepositoryLabel { get; private set; }
        public Label StatusLabel { get; private set; }
        public Label FileStatsLabel { get; private set; }
        public ProgressBar ProgressBar { get; private set; }

        public ModernStatusBar StatusBar => _statusBar;

        public ImprovedStatusBar(IDialogService dialogService, UILayoutConfiguration layoutConfig, EventHandler onBranchClick)
        {
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _statusBar = new ModernStatusBar
            {
                Height = layoutConfig?.StatusBarHeight ?? Spacing.Layout.StatusBarHeight
            };

            CreateLabels();
            CreateProgressBar();
            LayoutControls(onBranchClick);
        }

        private void CreateLabels()
        {
            // Branch label
            BranchLabel = new Label
            {
                Text = VersionControlDefaults.DefaultBranchName,
                AutoSize = true,
                Font = new Font(Typography.DefaultFontFamily, Typography.Small),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand,
                Padding = new Padding(Spacing.XXSmall, 0, Spacing.XXSmall, 0)
            };

            // Repository label
            RepositoryLabel = new Label
            {
                Text = "No Repository",
                AutoSize = true,
                Font = new Font(Typography.DefaultFontFamily, Typography.Small),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(Spacing.XXSmall, 0, Spacing.XXSmall, 0)
            };

            // Status label
            StatusLabel = new Label
            {
                Text = "Ready",
                AutoSize = true,
                Font = new Font(Typography.DefaultFontFamily, Typography.Small),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(Spacing.XXSmall, 0, Spacing.XXSmall, 0)
            };

            // File statistics label
            FileStatsLabel = new Label
            {
                Text = "",
                AutoSize = true,
                Font = new Font(Typography.DefaultFontFamily, Typography.Small),
                ForeColor = Color.LightGray,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(Spacing.Small, 0, Spacing.XXSmall, 0)
            };
        }

        private void CreateProgressBar()
        {
            ProgressBar = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                Size = new Size(100, 16),
                Visible = false
            };
        }

        private void LayoutControls(EventHandler onBranchClick)
        {
            // Left section with proper spacing
            var leftFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                Padding = new Padding(Spacing.XSmall, Spacing.XXSmall, 0, Spacing.XXSmall)
            };

            // Branch icon using Icons constant
            var branchIcon = new Label
            {
                Text = Icons.Branch,
                Font = new Font(Typography.IconFontFamily, Typography.Small),
                AutoSize = true,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 2, Spacing.XXSmall, 0)
            };

            // Attach branch click handler
            if (onBranchClick != null)
            {
                BranchLabel.Click += onBranchClick;
            }

            // Add controls with separators
            leftFlow.Controls.Add(branchIcon);
            leftFlow.Controls.Add(BranchLabel);
            leftFlow.Controls.Add(CreateSeparator());
            leftFlow.Controls.Add(RepositoryLabel);
            leftFlow.Controls.Add(CreateSeparator());
            leftFlow.Controls.Add(StatusLabel);
            leftFlow.Controls.Add(FileStatsLabel);

            _statusBar.Controls.Add(leftFlow);

            // Right section with progress bar
            var rightFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = Color.Transparent,
                Padding = new Padding(0, Spacing.XXSmall, Spacing.XSmall, Spacing.XXSmall)
            };
            rightFlow.Controls.Add(ProgressBar);
            _statusBar.Controls.Add(rightFlow);

            // Notification bell
            var bell = new Label
            {
                Text = Icons.Notification,
                Font = new Font(Typography.IconFontFamily, Typography.Small),
                AutoSize = true,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Dock = DockStyle.Right,
                Padding = new Padding(Spacing.XSmall, Spacing.XXSmall + 2, Spacing.Medium, 0),
                Cursor = Cursors.Hand
            };
            bell.Click += (s, e) => _dialogService?.ShowInfo("Notifications", "No new notifications");
            _statusBar.Controls.Add(bell);
        }

        private Label CreateSeparator()
        {
            return new Label
            {
                Text = "|",
                AutoSize = true,
                Font = new Font(Typography.DefaultFontFamily, Typography.Small),
                ForeColor = Color.FromArgb(80, 255, 255, 255), // 30% opacity white
                BackColor = Color.Transparent,
                Padding = new Padding(Spacing.XSmall, 0, Spacing.XSmall, 0)
            };
        }

        public void UpdateFileStats(int added, int modified, int deleted)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (added > 0) parts.Add($"{added} Added");
            if (modified > 0) parts.Add($"{modified} Modified");
            if (deleted > 0) parts.Add($"{deleted} Deleted");

            FileStatsLabel.Text = parts.Count > 0 ? string.Join(", ", parts) : "No Changes";
        }
    }
}
