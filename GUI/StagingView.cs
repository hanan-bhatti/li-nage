using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Linage.Controllers;
using Linage.GUI.Theme;
using Linage.GUI.Controls;

namespace Linage.GUI
{
    public class StagingView : UserControl
    {
        private CheckedListBox _filesList;
        private MaterialTextBox _commitMessage;
        private MaterialButton _commitButton;
        private Label _lblFiles;
        private Label _lblMessage;

        public event EventHandler<CommitEventArgs> OnCommitRequested;

        public StagingView()
        {
            InitializeComponent();
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
            if (files == null)
            {
                _filesList.Items.Clear();
                return;
            }

            var newFiles = new HashSet<string>(files);
            var currentFiles = new HashSet<string>();
            foreach(var item in _filesList.Items) currentFiles.Add(item.ToString());

            // Check if lists are identical
            if (newFiles.SetEquals(currentFiles)) return; // No change, prevent flicker

            _filesList.BeginUpdate();
            _filesList.Items.Clear();
            foreach (var f in files)
            {
                _filesList.Items.Add(f, true); // Default to checked
            }
            _filesList.EndUpdate();
        }

        private void TriggerCommit()
        {
            if (string.IsNullOrWhiteSpace(_commitMessage.Text))
            {
                MessageBox.Show("Please enter a commit message.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedFiles = new List<string>();
            foreach (var item in _filesList.CheckedItems)
            {
                selectedFiles.Add(item.ToString());
            }

            OnCommitRequested?.Invoke(this, new CommitEventArgs 
            { 
                Message = _commitMessage.Text, 
                SelectedFiles = selectedFiles 
            });
            
            _commitMessage.Text = "";
        }
    }

    public class CommitEventArgs : EventArgs
    {
        public string Message { get; set; }
        public List<string> SelectedFiles { get; set; }
    }
}