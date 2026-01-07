using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Linage.GUI.Theme;
using Linage.GUI.Controls;

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
        private MaterialButton _openRepoButton; // Changed from LinkLabel
        private LinkLabel _cloneRepoLink;
        private LinkLabel _importGitLink;
        private Label _recentLabel;
        private FlowLayoutPanel _recentPanel;
        private Label _taglineLabel; // New Tagline

        public event EventHandler OpenRepositoryClicked;
        public event EventHandler CloneRepositoryClicked;
        public event EventHandler ImportGitClicked;

        public WelcomeView()
        {
            InitializeComponent();
            SetupStyles();
        }

        private void InitializeComponent()
        {
            this.BackColor = ModernTheme.BackColor;
            this.Dock = DockStyle.Fill;

            // Central content panel
            _contentPanel = new Panel
            {
                Width = 600,
                Height = 550, // Increased height for new elements
                BackColor = Color.Transparent
            };

            // Title with gradient effect
            _titleLabel = new Label
            {
                Text = "Li'nage",
                Font = new Font("Segoe UI", 48, FontStyle.Bold), // Larger
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(0, 20)
            };

            // Subtitle
            _subtitleLabel = new Label
            {
                Text = "Line-Level Version Control",
                Font = new Font("Segoe UI", 16, FontStyle.Regular),
                ForeColor = ModernTheme.TextSecondary, // Will be dimmed further in paint if needed, or stick to theme
                AutoSize = true,
                Location = new Point(5, 100)
            };
            
            // Tagline - "Track changes at the line level, not files."
            _taglineLabel = new Label
            {
                Text = "Track changes at the line level, not files.",
                Font = new Font("Segoe UI", 10, FontStyle.Italic),
                ForeColor = ModernTheme.TextDisabled, // Subtle
                AutoSize = true,
                Location = new Point(5, 135)
            };

            // Accent line (creative touch)
            var accentLine = new Panel
            {
                Width = 80, // Slightly shorter, more minimal
                Height = 3,
                BackColor = ModernTheme.PrimaryColor,
                Location = new Point(5, 165)
            };

            // Action buttons section
            var actionsLabel = new Label
            {
                Text = "GET STARTED",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = ModernTheme.TextDisabled, // Very subtle header
                AutoSize = true,
                Location = new Point(5, 200)
            };

            // Open Repository - Primary Action Button
            _openRepoButton = new MaterialButton
            {
                Text = " OPEN REPOSITORY",
                Size = new Size(220, 45), // Big click target
                Location = new Point(0, 230),
                BackColor = ModernTheme.PrimaryColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            _openRepoButton.Click += (s, e) => OpenRepositoryClicked?.Invoke(this, EventArgs.Empty);

            // Clone Repository - Secondary Link
            _cloneRepoLink = CreateActionLink("⬇ Clone Repository", 295, false); // Lower visual weight
            _cloneRepoLink.Click += (s, e) => CloneRepositoryClicked?.Invoke(this, EventArgs.Empty);

            // Import from Git - Secondary Link
            _importGitLink = CreateActionLink("🔄 Import Git Repository", 335, false);
            _importGitLink.Click += (s, e) => ImportGitClicked?.Invoke(this, EventArgs.Empty);

            // Recent projects section
            _recentLabel = new Label
            {
                Text = "RECENT",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = ModernTheme.TextDisabled,
                AutoSize = true,
                Location = new Point(5, 400),
                Visible = false
            };

            _recentPanel = new FlowLayoutPanel
            {
                Location = new Point(0, 430),
                Width = 580,
                Height = 100,
                FlowDirection = FlowDirection.TopDown,
                AutoScroll = false,
                BackColor = Color.Transparent,
                Visible = false
            };

            // Add all controls to content panel
            _contentPanel.Controls.Add(_titleLabel);
            _contentPanel.Controls.Add(_subtitleLabel);
            _contentPanel.Controls.Add(_taglineLabel);
            _contentPanel.Controls.Add(accentLine);
            _contentPanel.Controls.Add(actionsLabel);
            _contentPanel.Controls.Add(_openRepoButton);
            _contentPanel.Controls.Add(_cloneRepoLink);
            _contentPanel.Controls.Add(_importGitLink);
            _contentPanel.Controls.Add(_recentLabel);
            _contentPanel.Controls.Add(_recentPanel);

            this.Controls.Add(_contentPanel);
            this.Resize += OnResize;
        }

        private LinkLabel CreateActionLink(string text, int yPos, bool primary)
        {
            var link = new LinkLabel
            {
                Text = text,
                Font = new Font("Segoe UI", 11, FontStyle.Regular), // Smaller than before
                AutoSize = true,
                Location = new Point(10, yPos), // Indented slightly relative to button
                LinkColor = primary ? ModernTheme.TextPrimary : ModernTheme.TextSecondary, // Dimmer
                ActiveLinkColor = ModernTheme.PrimaryColor,
                VisitedLinkColor = primary ? ModernTheme.TextPrimary : ModernTheme.TextSecondary,
                LinkBehavior = LinkBehavior.HoverUnderline
            };

            link.MouseEnter += (s, e) => link.ForeColor = ModernTheme.PrimaryColor;
            link.MouseLeave += (s, e) => link.ForeColor = primary ? ModernTheme.TextPrimary : ModernTheme.TextSecondary;

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
