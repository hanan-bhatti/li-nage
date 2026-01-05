using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Linage.GUI.Theme;

namespace Linage.GUI
{
    /// <summary>
    /// Beautiful welcome screen inspired by JetBrains IDEs with unique creative touches
    /// </summary>
    public class WelcomeView : UserControl
    {
        private Panel _contentPanel;
        private Label _titleLabel;
        private Label _subtitleLabel;
        private LinkLabel _openRepoLink;
        private LinkLabel _cloneRepoLink;
        private LinkLabel _importGitLink;
        private Label _recentLabel;
        private FlowLayoutPanel _recentPanel;

        public event EventHandler OpenRepositoryClicked;
        public event EventHandler CloneRepositoryClicked;
        public event EventHandler ImportGitClicked;

        public WelcomeView()
        {
            InitializeComponent();
            SetupStyles();
            Linage.GUI.Helpers.WatermarkHelper.AddWatermarkLabel(this, "WelcomeView.cs");
        }

        private void InitializeComponent()
        {
            this.BackColor = ModernTheme.BackColor;
            this.Dock = DockStyle.Fill;

            // Central content panel
            _contentPanel = new Panel
            {
                Width = 600,
                Height = 500,
                BackColor = Color.Transparent
            };

            // Title with gradient effect
            _titleLabel = new Label
            {
                Text = "Li'nage",
                Font = new Font("Segoe UI", 42, FontStyle.Bold),
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(0, 40)
            };

            // Subtitle
            _subtitleLabel = new Label
            {
                Text = "Line-Level Version Control",
                Font = new Font("Segoe UI", 14, FontStyle.Regular),
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(5, 110)
            };

            // Accent line (creative touch)
            var accentLine = new Panel
            {
                Width = 120,
                Height = 4,
                BackColor = ModernTheme.PrimaryColor,
                Location = new Point(0, 155)
            };

            // Action buttons section
            var actionsLabel = new Label
            {
                Text = "GET STARTED",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(0, 190)
            };

            // Open Repository
            _openRepoLink = CreateActionLink("📂 Open Repository", 220);
            _openRepoLink.Click += (s, e) => OpenRepositoryClicked?.Invoke(this, EventArgs.Empty);

            // Clone Repository
            _cloneRepoLink = CreateActionLink("⬇ Clone Repository", 265);
            _cloneRepoLink.Click += (s, e) => CloneRepositoryClicked?.Invoke(this, EventArgs.Empty);

            // Import from Git
            _importGitLink = CreateActionLink("🔄 Import Git Repository", 310);
            _importGitLink.Click += (s, e) => ImportGitClicked?.Invoke(this, EventArgs.Empty);

            // Recent projects section
            _recentLabel = new Label
            {
                Text = "RECENT",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(0, 370),
                Visible = false
            };

            _recentPanel = new FlowLayoutPanel
            {
                Location = new Point(0, 400),
                Width = 580,
                Height = 80,
                FlowDirection = FlowDirection.TopDown,
                AutoScroll = false,
                BackColor = Color.Transparent,
                Visible = false
            };

            // Add all controls to content panel
            _contentPanel.Controls.Add(_titleLabel);
            _contentPanel.Controls.Add(_subtitleLabel);
            _contentPanel.Controls.Add(accentLine);
            _contentPanel.Controls.Add(actionsLabel);
            _contentPanel.Controls.Add(_openRepoLink);
            _contentPanel.Controls.Add(_cloneRepoLink);
            _contentPanel.Controls.Add(_importGitLink);
            _contentPanel.Controls.Add(_recentLabel);
            _contentPanel.Controls.Add(_recentPanel);

            this.Controls.Add(_contentPanel);
            this.Resize += OnResize;
        }

        private LinkLabel CreateActionLink(string text, int yPos)
        {
            var link = new LinkLabel
            {
                Text = text,
                Font = new Font("Segoe UI", 13, FontStyle.Regular),
                AutoSize = true,
                Location = new Point(0, yPos),
                LinkColor = ModernTheme.TextPrimary,
                ActiveLinkColor = ModernTheme.PrimaryColor,
                VisitedLinkColor = ModernTheme.TextPrimary,
                LinkBehavior = LinkBehavior.HoverUnderline
            };

            link.MouseEnter += (s, e) => link.ForeColor = ModernTheme.PrimaryColor;
            link.MouseLeave += (s, e) => link.ForeColor = ModernTheme.TextPrimary;

            return link;
        }

        private void SetupStyles()
        {
            // Add subtle gradient background (creative touch)
            this.Paint += (s, e) =>
            {
                using (var brush = new LinearGradientBrush(
                    this.ClientRectangle,
                    ModernTheme.BackColor,
                    Color.FromArgb(25, 25, 28),
                    45f))
                {
                    e.Graphics.FillRectangle(brush, this.ClientRectangle);
                }

                // Add subtle pattern
                using (var pen = new Pen(Color.FromArgb(10, ModernTheme.PrimaryColor), 1))
                {
                    for (int i = 0; i < this.Width; i += 100)
                    {
                        e.Graphics.DrawLine(pen, i, 0, i + 50, this.Height);
                    }
                }
            };
        }

        private void OnResize(object sender, EventArgs e)
        {
            // Center the content panel
            _contentPanel.Location = new Point(
                (this.Width - _contentPanel.Width) / 2,
                (this.Height - _contentPanel.Height) / 2 - 50
            );
        }

        public void AddRecentProject(string path, string name)
        {
            _recentLabel.Visible = true;
            _recentPanel.Visible = true;

            var recentItem = new LinkLabel
            {
                Text = $"  {name}",
                Font = new Font("Segoe UI", 11),
                AutoSize = true,
                LinkColor = ModernTheme.TextSecondary,
                ActiveLinkColor = ModernTheme.PrimaryColor,
                Tag = path,
                Margin = new Padding(0, 5, 0, 5)
            };

            recentItem.Click += (s, e) =>
            {
                // TODO: Open the repository
            };

            _recentPanel.Controls.Add(recentItem);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Custom paint in Paint event
        }
    }
}
