using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Linage.GUI.Configuration;
using Linage.GUI.Theme;

namespace Linage.GUI.Dialogs
{
    /// <summary>
    /// Modern progress dialog with cancellation support.
    /// Shows progress for long-running operations with a cancel button.
    /// </summary>
    public class ProgressDialog : Form
    {
        private readonly CancellationTokenSource _cancellationTokenSource;
        private Label _titleLabel;
        private Label _messageLabel;
        private ProgressBar _progressBar;
        private Button _cancelButton;
        private bool _isCancelled = false;

        public CancellationToken CancellationToken => _cancellationTokenSource.Token;
        public bool IsCancelled => _isCancelled;

        public ProgressDialog(string title, string message, bool showProgressBar = true)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            InitializeComponents(title, message, showProgressBar);
        }

        private void InitializeComponents(string title, string message, bool showProgressBar)
        {
            this.Text = title;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ShowInTaskbar = false;
            this.Size = new Size(400, showProgressBar ? 180 : 150);
            this.BackColor = ModernTheme.BackColor;
            this.ForeColor = ModernTheme.TextPrimary;

            // Title Label
            _titleLabel = new Label
            {
                Text = title,
                Font = new Font(Typography.DefaultFontFamily, Typography.Large, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = false,
                Size = new Size(360, 30),
                Location = new Point(Spacing.Medium, Spacing.Medium),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Message Label
            _messageLabel = new Label
            {
                Text = message,
                Font = new Font(Typography.DefaultFontFamily, Typography.Medium),
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = false,
                Size = new Size(360, 40),
                Location = new Point(Spacing.Medium, Spacing.Medium + 35),
                TextAlign = ContentAlignment.TopLeft
            };

            int currentY = Spacing.Medium + 80;

            // Progress Bar (optional)
            if (showProgressBar)
            {
                _progressBar = new ProgressBar
                {
                    Style = ProgressBarStyle.Marquee,
                    MarqueeAnimationSpeed = 30,
                    Size = new Size(360, 20),
                    Location = new Point(Spacing.Medium, currentY)
                };
                this.Controls.Add(_progressBar);
                currentY += 30;
            }

            // Cancel Button
            _cancelButton = new Button
            {
                Text = "Cancel",
                Size = new Size(100, 32),
                Location = new Point(this.ClientSize.Width - 100 - Spacing.Medium, currentY),
                FlatStyle = FlatStyle.Flat,
                BackColor = ModernTheme.SurfaceColor,
                ForeColor = Color.White,
                Font = new Font(Typography.DefaultFontFamily, Typography.Medium),
                Cursor = Cursors.Hand
            };
            _cancelButton.FlatAppearance.BorderColor = ModernTheme.BorderColor;
            _cancelButton.Click += OnCancelClicked;

            this.Controls.Add(_titleLabel);
            this.Controls.Add(_messageLabel);
            this.Controls.Add(_cancelButton);

            // Prevent closing via X button
            this.FormClosing += (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing && !_isCancelled)
                {
                    OnCancelClicked(s, e);
                    e.Cancel = true;
                }
            };
        }

        public void UpdateMessage(string message)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(UpdateMessage), message);
                return;
            }

            _messageLabel.Text = message;
            _messageLabel.Refresh();
        }

        public void UpdateProgress(int percentage)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<int>(UpdateProgress), percentage);
                return;
            }

            if (_progressBar != null && _progressBar.Style != ProgressBarStyle.Marquee)
            {
                _progressBar.Value = Math.Max(0, Math.Min(100, percentage));
            }
        }

        public void SetProgressBarStyle(ProgressBarStyle style)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<ProgressBarStyle>(SetProgressBarStyle), style);
                return;
            }

            if (_progressBar != null)
            {
                _progressBar.Style = style;
                if (style == ProgressBarStyle.Blocks)
                {
                    _progressBar.Minimum = 0;
                    _progressBar.Maximum = 100;
                    _progressBar.Value = 0;
                }
            }
        }

        private void OnCancelClicked(object sender, EventArgs e)
        {
            if (_isCancelled) return;

            _isCancelled = true;
            _cancelButton.Enabled = false;
            _cancelButton.Text = "Cancelling...";
            UpdateMessage("Cancelling operation...");

            _cancellationTokenSource.Cancel();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource?.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Shows a progress dialog and executes an async operation with cancellation support.
        /// </summary>
        public static void Execute(
            IWin32Window owner,
            string title,
            string message,
            Func<CancellationToken, IProgress<string>, System.Threading.Tasks.Task> operation,
            bool showProgressBar = true)
        {
            using (var dialog = new ProgressDialog(title, message, showProgressBar))
            {
                var progress = new Progress<string>(msg => dialog.UpdateMessage(msg));

                var task = System.Threading.Tasks.Task.Run(
                    async () => await operation(dialog.CancellationToken, progress).ConfigureAwait(false),
                    dialog.CancellationToken);

                // Show dialog modally
                dialog.ShowDialog(owner);

                // Wait for task to complete or be cancelled
                try
                {
                    task.Wait();
                }
                catch (AggregateException ex)
                {
                    // Handle cancellation or other exceptions
                    var innerEx = ex.InnerException;
                    if (innerEx is OperationCanceledException)
                    {
                        // Operation was cancelled, this is expected
                    }
                    else
                    {
                        throw innerEx ?? ex;
                    }
                }
            }
        }

        /// <summary>
        /// Shows a progress dialog with determinate progress (0-100).
        /// </summary>
        public static void ExecuteWithProgress(
            IWin32Window owner,
            string title,
            string message,
            Func<CancellationToken, IProgress<int>, IProgress<string>, System.Threading.Tasks.Task> operation)
        {
            using (var dialog = new ProgressDialog(title, message, true))
            {
                dialog.SetProgressBarStyle(ProgressBarStyle.Blocks);

                var progressPercent = new Progress<int>(percent => dialog.UpdateProgress(percent));
                var progressMessage = new Progress<string>(msg => dialog.UpdateMessage(msg));

                var task = System.Threading.Tasks.Task.Run(
                    async () => await operation(dialog.CancellationToken, progressPercent, progressMessage).ConfigureAwait(false),
                    dialog.CancellationToken);

                // Show dialog modally
                dialog.ShowDialog(owner);

                // Wait for task to complete or be cancelled
                try
                {
                    task.Wait();
                }
                catch (AggregateException ex)
                {
                    var innerEx = ex.InnerException;
                    if (innerEx is OperationCanceledException)
                    {
                        // Operation was cancelled, this is expected
                    }
                    else
                    {
                        throw innerEx ?? ex;
                    }
                }
            }
        }

        private void ProgressDialog_Load(object sender, EventArgs e) {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.SuspendLayout();
            //
            // ProgressDialog
            //
            this.ClientSize = new System.Drawing.Size(282, 253);
            this.Name = "ProgressDialog";
            this.ResumeLayout(false);
        }
    }
}
