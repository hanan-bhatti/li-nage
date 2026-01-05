using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Linage.Controllers;
using Linage.GUI.Theme;
using Linage.GUI.Controls;
using Linage.Infrastructure;

namespace Linage.GUI
{
    public class StagingView : UserControl, IThemable
    {
        // ...

        public void ApplyTheme()
        {
            this.BackColor = ModernTheme.BackColor;

            if (_lblFiles != null)
            {
                _lblFiles.ForeColor = ModernTheme.TextPrimary;
                _lblFiles.Font = ModernTheme.FontH2;
            }

            if (_lblMessage != null)
            {
                _lblMessage.ForeColor = ModernTheme.TextPrimary;
                _lblMessage.Font = ModernTheme.FontH2;
            }

            if (_filesList != null)
            {
                _filesList.BackColor = ModernTheme.SurfaceColor;
                _filesList.ForeColor = ModernTheme.TextPrimary;
                _filesList.Font = ModernTheme.FontBody;
            }

            if (_commitMessage != null)
            {
                _commitMessage.BackColor = ModernTheme.SurfaceColor;
                if (_commitMessage.InnerTextBox != null)
                {
                    _commitMessage.InnerTextBox.BackColor = ModernTheme.SurfaceColor;
                    _commitMessage.InnerTextBox.ForeColor = ModernTheme.TextPrimary;
                    _commitMessage.InnerTextBox.Font = ModernTheme.FontBody;
                }
            }

            if (_commitButton != null)
            {
                _commitButton.BackColor = ModernTheme.PrimaryColor;
                _commitButton.ForeColor = Color.White;
                _commitButton.Font = ModernTheme.FontBody;
            }
        }

        private CheckedListBox _filesList;
        private MaterialTextBox _commitMessage;
        private MaterialButton _commitButton;
        private Label _lblFiles;
        private Label _lblMessage;

        public event EventHandler<CommitEventArgs> OnCommitRequested;

        public StagingView()
        {
            InitializeComponent();
            Linage.GUI.Helpers.WatermarkHelper.AddWatermarkLabel(this, "StagingView.cs");
        }

        private void InitializeComponent()
        {
            this.BackColor = ModernTheme.BackColor;

            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(20);
            layout.RowCount = 5;
            // Adjust row styles for better spacing
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f)); // Header
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 60f));  // List
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f)); // Header
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f)); // Input (Fixed height for material box)
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f)); // Button area

            // Files Header
            _lblFiles = new Label { 
                Text = "Staged Changes", 
                Dock = DockStyle.Bottom, 
                AutoSize = true,
                Font = ModernTheme.FontH2,
                ForeColor = ModernTheme.TextPrimary
            };

            // File List
            // Using a panel wrapper to give it a "card" look could be nice, but keeping it simple for now
            _filesList = new CheckedListBox { 
                Dock = DockStyle.Fill, 
                CheckOnClick = true,
                BackColor = ModernTheme.SurfaceColor,
                ForeColor = ModernTheme.TextPrimary,
                BorderStyle = BorderStyle.None,
                Font = ModernTheme.FontBody,
                Padding = new Padding(10)
            };
            
            // Message Header
            _lblMessage = new Label { 
                Text = "Commit Message", 
                Dock = DockStyle.Bottom, 
                AutoSize = true,
                Font = ModernTheme.FontH2,
                ForeColor = ModernTheme.TextPrimary
            };

            // Commit Input
            _commitMessage = new MaterialTextBox { 
                Dock = DockStyle.Top,
                // Text = "" 
            };
            
            // Commit Button
            _commitButton = new MaterialButton { 
                Text = "COMMIT", 
                Dock = DockStyle.Right, // Right align action
                Width = 150
            };
            _commitButton.Click += (s, e) => TriggerCommit();

            // Add controls with spacing
            layout.Controls.Add(_lblFiles, 0, 0);
            layout.Controls.Add(_filesList, 0, 1);
            layout.Controls.Add(_lblMessage, 0, 2);
            layout.Controls.Add(_commitMessage, 0, 3);
            layout.Controls.Add(_commitButton, 0, 4);

            this.Controls.Add(layout);
        }

        public void SetFiles(IEnumerable<string> files)
        {
            DebugLogger.Trace($"StagingView.SetFiles called");

            if (files == null)
            {
                DebugLogger.Trace("  -> files is null, clearing list");
                _filesList.Items.Clear();
                return;
            }

            var fileList = files.ToList();
            DebugLogger.Trace($"  -> Received {fileList.Count} files");
            foreach (var f in fileList)
            {
                DebugLogger.Trace($"     - {f}");
            }

            var newFiles = new HashSet<string>(fileList);
            var currentFiles = new HashSet<string>();
            foreach(var item in _filesList.Items) currentFiles.Add(item.ToString());

            // Check if lists are identical
            if (newFiles.SetEquals(currentFiles))
            {
                DebugLogger.Trace("  -> No change from current list, skipping update");
                return; // No change, prevent flicker
            }

            DebugLogger.Info($"StagingView updating with {fileList.Count} changed files");
            _filesList.BeginUpdate();
            _filesList.Items.Clear();
            foreach (var f in fileList)
            {
                _filesList.Items.Add(f, true); // Default to checked
            }
            _filesList.EndUpdate();
        }

        private void TriggerCommit()
        {
            DebugLogger.Info("StagingView.TriggerCommit called");

            if (string.IsNullOrWhiteSpace(_commitMessage.Text))
            {
                DebugLogger.Warn("  -> Commit aborted: empty message");
                MessageBox.Show("Please enter a commit message.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedFiles = new List<string>();
            foreach (var item in _filesList.CheckedItems)
            {
                selectedFiles.Add(item.ToString());
            }

            DebugLogger.Info($"  -> Committing {selectedFiles.Count} files with message: {_commitMessage.Text}");
            foreach (var f in selectedFiles)
            {
                DebugLogger.Trace($"     - {f}");
            }

            OnCommitRequested?.Invoke(this, new CommitEventArgs
            {
                Message = _commitMessage.Text,
                SelectedFiles = selectedFiles
            });

            DebugLogger.Trace("  -> Clearing commit message textbox");
            _commitMessage.Text = "";
        }
    }

    public class CommitEventArgs : EventArgs
    {
        public string Message { get; set; }
        public List<string> SelectedFiles { get; set; }
    }
}