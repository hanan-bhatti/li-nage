using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Linage.Core;
using Linage.GUI.Theme;
using Linage.GUI.Controls;

namespace Linage.GUI
{
    /// <summary>
    /// Visual conflict resolution interface with 3-pane diff viewer
    /// </summary>
    public class ConflictResolutionView : UserControl
    {
        private SplitContainer _mainSplit;
        private SplitContainer _topSplit;
        private Panel _basePanel;
        private Panel _localPanel;
        private Panel _remotePanel;
        private Panel _mergedPanel;
        
        private RichTextBox _baseEditor;
        private RichTextBox _localEditor;
        private RichTextBox _remoteEditor;
        private RichTextBox _mergedEditor;
        
        private Label _baseLabel;
        private Label _localLabel;
        private Label _remoteLabel;
        private Label _mergedLabel;
        
        private MaterialButton _acceptLocalButton;
        private MaterialButton _acceptRemoteButton;
        private MaterialButton _acceptBothButton;
        private MaterialButton _saveButton;
        private MaterialButton _cancelButton;
        
        private Conflict _currentConflict;
        private List<ConflictSection> _conflictSections = new List<ConflictSection>();
        
        public event EventHandler<ConflictResolutionEventArgs> OnConflictResolved;
        public event EventHandler OnConflictCancelled;
        
        public ConflictResolutionView()
        {
            InitializeComponent();
            ApplyTheme();
        }
        
        private void InitializeComponent()
        {
            this.BackColor = ModernTheme.BackColor;
            
            // Main split: Top (3-pane) | Bottom (merged result)
            _mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterWidth = 2,
                Panel1MinSize = 200,
                Panel2MinSize = 150
            };
            
            // Top split: Base | (Local | Remote)
            _topSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterWidth = 2
            };
            
            // Create panels
            _basePanel = CreateEditorPanel("BASE (Common Ancestor)", out _baseEditor, out _baseLabel);
            
            var localRemoteSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterWidth = 2
            };
            
            _localPanel = CreateEditorPanel("LOCAL (Your Changes)", out _localEditor, out _localLabel);
            _remotePanel = CreateEditorPanel("REMOTE (Their Changes)", out _remoteEditor, out _remoteLabel);
            
            localRemoteSplit.Panel1.Controls.Add(_localPanel);
            localRemoteSplit.Panel2.Controls.Add(_remotePanel);
            
            _topSplit.Panel1.Controls.Add(_basePanel);
            _topSplit.Panel2.Controls.Add(localRemoteSplit);
            
            // Merged result panel
            _mergedPanel = CreateEditorPanel("MERGED RESULT", out _mergedEditor, out _mergedLabel);
            _mergedEditor.ReadOnly = false; // Allow manual edits
            
            // Action buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = ModernTheme.SurfaceColor,
                Padding = new Padding(10)
            };
            
            _saveButton = new MaterialButton { Text = "SAVE RESOLUTION", Width = 150 };
            _saveButton.Click += OnSaveResolution;
            
            _cancelButton = new MaterialButton { Text = "CANCEL", Width = 100 };
            _cancelButton.Click += OnCancel;
            
            _acceptBothButton = new MaterialButton { Text = "ACCEPT BOTH", Width = 120 };
            _acceptBothButton.Click += (s, e) => AcceptBoth();
            
            _acceptRemoteButton = new MaterialButton { Text = "ACCEPT REMOTE", Width = 140 };
            _acceptRemoteButton.Click += (s, e) => AcceptRemote();
            
            _acceptLocalButton = new MaterialButton { Text = "ACCEPT LOCAL", Width = 130 };
            _acceptLocalButton.Click += (s, e) => AcceptLocal();
            
            buttonPanel.Controls.Add(_saveButton);
            buttonPanel.Controls.Add(_cancelButton);
            buttonPanel.Controls.Add(new Label { Width = 20 }); // Spacer
            buttonPanel.Controls.Add(_acceptBothButton);
            buttonPanel.Controls.Add(_acceptRemoteButton);
            buttonPanel.Controls.Add(_acceptLocalButton);
            
            _mergedPanel.Controls.Add(buttonPanel);
            
            // Assemble layout
            _mainSplit.Panel1.Controls.Add(_topSplit);
            _mainSplit.Panel2.Controls.Add(_mergedPanel);
            
            _mainSplit.SplitterDistance = 400;
            _topSplit.SplitterDistance = 200;
            
            this.Controls.Add(_mainSplit);
        }
        
        private Panel CreateEditorPanel(string title, out RichTextBox editor, out Label label)
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = ModernTheme.BackColor };
            
            label = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 30,
                Font = ModernTheme.FontH2,
                ForeColor = ModernTheme.TextPrimary,
                BackColor = ModernTheme.SurfaceColor,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };
            
            editor = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.BackColor,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.FontCode,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                WordWrap = false
            };
            
            panel.Controls.Add(editor);
            panel.Controls.Add(label);
            
            return panel;
        }
        
        private void ApplyTheme()
        {
            this.BackColor = ModernTheme.BackColor;
        }
        
        public void LoadConflict(Conflict conflict, string baseContent, string localContent, string remoteContent)
        {
            _currentConflict = conflict;
            
            _baseEditor.Text = baseContent ?? "";
            _localEditor.Text = localContent ?? "";
            _remoteEditor.Text = remoteContent ?? "";
            
            // Parse conflict markers from local content if present
            ParseConflictMarkers(localContent);
            
            // Initialize merged editor with local content as starting point
            _mergedEditor.Text = localContent ?? "";
            
            // Highlight conflict sections
            HighlightConflicts();
        }
        
        private void ParseConflictMarkers(string content)
        {
            _conflictSections.Clear();
            
            if (string.IsNullOrEmpty(content)) return;
            
            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            ConflictSection currentSection = null;
            var lineNumber = 0;
            
            foreach (var line in lines)
            {
                if (line.StartsWith("<<<<<<<"))
                {
                    currentSection = new ConflictSection { StartLine = lineNumber };
                }
                else if (line.StartsWith("=======") && currentSection != null)
                {
                    currentSection.MiddleLine = lineNumber;
                }
                else if (line.StartsWith(">>>>>>>") && currentSection != null)
                {
                    currentSection.EndLine = lineNumber;
                    _conflictSections.Add(currentSection);
                    currentSection = null;
                }
                
                lineNumber++;
            }
        }
        
        private void HighlightConflicts()
        {
            // Highlight conflict markers in local editor
            HighlightEditor(_localEditor, Color.FromArgb(255, 100, 100));
            HighlightEditor(_remoteEditor, Color.FromArgb(100, 150, 255));
        }
        
        private void HighlightEditor(RichTextBox editor, Color highlightColor)
        {
            var text = editor.Text;
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            
            editor.SelectAll();
            editor.SelectionBackColor = ModernTheme.BackColor;
            
            var charIndex = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                
                if (line.StartsWith("<<<<<<<") || line.StartsWith("=======") || line.StartsWith(">>>>>>>"))
                {
                    editor.Select(charIndex, line.Length);
                    editor.SelectionBackColor = Color.FromArgb(50, highlightColor);
                }
                
                charIndex += line.Length + Environment.NewLine.Length;
            }
            
            editor.Select(0, 0);
        }
        
        private void AcceptLocal()
        {
            var lines = _localEditor.Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var result = new List<string>();
            
            var inConflict = false;
            var inLocalSection = false;
            
            foreach (var line in lines)
            {
                if (line.StartsWith("<<<<<<<"))
                {
                    inConflict = true;
                    inLocalSection = true;
                    continue;
                }
                else if (line.StartsWith("======="))
                {
                    inLocalSection = false;
                    continue;
                }
                else if (line.StartsWith(">>>>>>>"))
                {
                    inConflict = false;
                    continue;
                }
                
                if (!inConflict || inLocalSection)
                {
                    result.Add(line);
                }
            }
            
            _mergedEditor.Text = string.Join(Environment.NewLine, result);
        }
        
        private void AcceptRemote()
        {
            var lines = _localEditor.Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var result = new List<string>();
            
            var inConflict = false;
            var inRemoteSection = false;
            
            foreach (var line in lines)
            {
                if (line.StartsWith("<<<<<<<"))
                {
                    inConflict = true;
                    continue;
                }
                else if (line.StartsWith("======="))
                {
                    inRemoteSection = true;
                    continue;
                }
                else if (line.StartsWith(">>>>>>>"))
                {
                    inConflict = false;
                    inRemoteSection = false;
                    continue;
                }
                
                if (!inConflict || inRemoteSection)
                {
                    result.Add(line);
                }
            }
            
            _mergedEditor.Text = string.Join(Environment.NewLine, result);
        }
        
        private void AcceptBoth()
        {
            var lines = _localEditor.Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var result = new List<string>();
            
            foreach (var line in lines)
            {
                if (line.StartsWith("<<<<<<<") || line.StartsWith("=======") || line.StartsWith(">>>>>>>"))
                {
                    continue; // Skip conflict markers
                }
                
                result.Add(line);
            }
            
            _mergedEditor.Text = string.Join(Environment.NewLine, result);
        }
        
        private void OnSaveResolution(object sender, EventArgs e)
        {
            if (_currentConflict == null) return;
            
            var resolvedContent = _mergedEditor.Text;
            _currentConflict.Resolve(resolvedContent);
            
            OnConflictResolved?.Invoke(this, new ConflictResolutionEventArgs
            {
                Conflict = _currentConflict,
                ResolvedContent = resolvedContent
            });
        }
        
        private void OnCancel(object sender, EventArgs e)
        {
            OnConflictCancelled?.Invoke(this, EventArgs.Empty);
        }
        
        private class ConflictSection
        {
            public int StartLine { get; set; }
            public int MiddleLine { get; set; }
            public int EndLine { get; set; }
        }
    }
    
    public class ConflictResolutionEventArgs : EventArgs
    {
        public Conflict Conflict { get; set; }
        public string ResolvedContent { get; set; }
    }
}
