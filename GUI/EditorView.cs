using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Linage.Controllers;
using Linage.Core;
using Linage.GUI.Controls;
using Linage.GUI.Helpers;
using Linage.GUI.Services;
using Linage.GUI.Theme;

namespace Linage.GUI
{
    public class EditorView : UserControl, IThemable
    {
        // ... (existing fields)

        public void ApplyTheme()
        {
            this.BackColor = ModernTheme.BackColor;
            
            if (_editorContainer != null)
                _editorContainer.BackColor = ModernTheme.BackColor;

            if (_lblCurrentFile != null)
            {
                _lblCurrentFile.ForeColor = ModernTheme.TextSecondary;
                _lblCurrentFile.BackColor = ModernTheme.SurfaceColor;
                _lblCurrentFile.Font = ModernTheme.MainFont;
            }

            if (_codeEditor != null)
            {
                _codeEditor.BackColor = ModernTheme.BackColor;
                _codeEditor.ForeColor = ModernTheme.TextPrimary;
                _codeEditor.SelectionColor = ModernTheme.TextPrimary;
                _codeEditor.Font = ModernTheme.CodeFont;
            }

            if (_lineHistoryGrid != null)
            {
                _lineHistoryGrid.BackgroundColor = ModernTheme.SurfaceColor;
                _lineHistoryGrid.GridColor = ModernTheme.BorderColor;
                
                _lineHistoryGrid.ColumnHeadersDefaultCellStyle.BackColor = ModernTheme.SurfaceColor;
                _lineHistoryGrid.ColumnHeadersDefaultCellStyle.ForeColor = ModernTheme.TextSecondary;
                _lineHistoryGrid.ColumnHeadersDefaultCellStyle.Font = ModernTheme.MainFont;
                
                _lineHistoryGrid.DefaultCellStyle.BackColor = ModernTheme.SurfaceColor;
                _lineHistoryGrid.DefaultCellStyle.ForeColor = ModernTheme.TextSecondary;
                _lineHistoryGrid.DefaultCellStyle.Font = ModernTheme.MainFont;
                _lineHistoryGrid.DefaultCellStyle.SelectionBackColor = ModernTheme.SurfaceLight;
            }
            
            // Re-highlight syntax if needed or just let it update naturally on edit
            // _highlighter?.ReapplyTheme(); // If highlighter supports it
        }

        // ... (rest of class)
        private Panel _editorContainer;
                private EnhancedRichTextBox _codeEditor;
                private DataGridView _lineHistoryGrid;
                private Label _lblCurrentFile;
                private Panel _searchPanel;
                
        // Missing Fields
        private TextBox _txtSearch;
        private string _currentFilePath;
        private string _repositoryRoot;
        private VersionController _versionController;
        private SyntaxHighlighter _highlighter;
        private Timer _typingTimer;
        private int _lastLineCount;
        private bool _isDirty;
        private bool _isVirtual; // If true, file is not on disk

        // Events
        public event EventHandler FileSaved;
        public event EventHandler ContentChanged;

        public bool IsDirty
        {
            get => _isDirty;
            private set
            {
                if (_isDirty != value)
                {
                    _isDirty = value;
                    ContentChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
                
                // ...
        
                private System.Windows.Forms.Timer _historyDebounceTimer;

        public EditorView()
        {
            InitializeComponent();
            
            _historyDebounceTimer = new System.Windows.Forms.Timer { Interval = 250 };
            _historyDebounceTimer.Tick += async (s, e) => 
            { 
                _historyDebounceTimer.Stop(); 
                await UpdateLineHistory(); 
            };
        }

        private void InitializeComponent()
        {
            this.BackColor = ModernTheme.BackColor;
            
            // 3. Editor Container
            _editorContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.BackColor
            };

            // Top Label
            _lblCurrentFile = new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Font = ModernTheme.MainFont,
                ForeColor = ModernTheme.TextSecondary,
                BackColor = ModernTheme.SurfaceColor,
                Text = "No file open"
            };
            _editorContainer.Controls.Add(_lblCurrentFile); // Top label first

            // Initialize History Grid
            InitializeHistoryGrid();

            // Split Container for Code and History
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 800, // Default width for code
                SplitterWidth = 4,
                FixedPanel = FixedPanel.Panel2 // Keep history panel size fixed
            };
            splitContainer.BackColor = ModernTheme.SplitterColor;
            splitContainer.Panel1.BackColor = ModernTheme.BackColor;
            splitContainer.Panel2.BackColor = ModernTheme.SurfaceColor;

            // Code Editor
            _codeEditor = new EnhancedRichTextBox
            {
                Dock = DockStyle.Fill,
                Font = ModernTheme.CodeFont,
                WordWrap = false,
                BackColor = ModernTheme.BackColor,
                ForeColor = ModernTheme.TextPrimary,
                BorderStyle = BorderStyle.None,
                AcceptsTab = true,
                ScrollBars = RichTextBoxScrollBars.Vertical // Use native scrollbar
            };
            
            // Ensure text is visible by setting selection colors
            _codeEditor.SelectionColor = ModernTheme.TextPrimary;
            
            _codeEditor.TextChanged += OnTextChanged;
            // Debounce selection change for history update
            _codeEditor.SelectionChanged += (s, e) => 
            { 
                _historyDebounceTimer.Stop();
                _historyDebounceTimer.Start();
            };
            
            // No gutter - simplified for performance
            _codeEditor.VScrollHappened += (s, e) => {
                    // No-op
            };
            _codeEditor.Resize += (s, e) => {
                    // No-op
            };
            
            // Add controls to split container
            splitContainer.Panel1.Controls.Add(_codeEditor);
            splitContainer.Panel2.Controls.Add(_lineHistoryGrid);
            
            _editorContainer.Controls.Add(splitContainer); // Fill control last 

            // Initialize Helpers
            _highlighter = new SyntaxHighlighter(_codeEditor);
            _typingTimer = new Timer { Interval = 500 };
            _typingTimer.Tick += (s, e) => { _typingTimer.Stop(); };
            
            // CRITICAL: Add the editor container to this UserControl
            this.Controls.Add(_editorContainer);
            
            // Setup Search Panel (after _codeEditor is created)
            SetupSearchPanel();
        }
                    
                            // ...
                    
        // --- Gutter Logic ---

        private void InitializeHistoryGrid()
        {
            _lineHistoryGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = ModernTheme.SurfaceColor,
                GridColor = ModernTheme.BorderColor,
                BorderStyle = BorderStyle.None,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ReadOnly = true
            };

            // Styles
            _lineHistoryGrid.ColumnHeadersDefaultCellStyle.BackColor = ModernTheme.SurfaceColor;
            _lineHistoryGrid.ColumnHeadersDefaultCellStyle.ForeColor = ModernTheme.TextSecondary;
            _lineHistoryGrid.ColumnHeadersDefaultCellStyle.Font = ModernTheme.MainFont;
            
            _lineHistoryGrid.DefaultCellStyle.BackColor = ModernTheme.SurfaceColor;
            _lineHistoryGrid.DefaultCellStyle.ForeColor = ModernTheme.TextSecondary;
            _lineHistoryGrid.DefaultCellStyle.Font = ModernTheme.MainFont;
            _lineHistoryGrid.DefaultCellStyle.SelectionBackColor = ModernTheme.SurfaceLight;
            _lineHistoryGrid.DefaultCellStyle.SelectionForeColor = Color.White;

            _lineHistoryGrid.Columns.Add("Version", "Ver");
            _lineHistoryGrid.Columns.Add("Author", "Author");
            _lineHistoryGrid.Columns.Add("Date", "Date");
        }

        private void SetupSearchPanel()
        {
            _searchPanel = new Panel
            {
                Size = new Size(300, 40),
                BackColor = ModernTheme.SurfaceLight,
                Visible = false,
                BorderStyle = BorderStyle.FixedSingle
            };
            
            _txtSearch = new TextBox
            {
                Location = new Point(10, 10),
                Width = 200,
                BackColor = ModernTheme.BackColor,
                ForeColor = ModernTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };
            _txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) PerformSearch(_txtSearch.Text); };

            var btnClose = new Button
            {
                Text = "X",
                Location = new Point(220, 8),
                Size = new Size(25, 25),
                FlatStyle = FlatStyle.Flat,
                ForeColor = ModernTheme.TextPrimary
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => _searchPanel.Visible = false;

            _searchPanel.Controls.Add(_txtSearch);
            _searchPanel.Controls.Add(btnClose);
            
            _codeEditor.Controls.Add(_searchPanel); 
        }

        // --- File Operations ---

        public async Task LoadFile(string filePath)
        {
            if (!File.Exists(filePath)) return;

            // Temporarily disable events to prevent flooding during load
            _codeEditor.TextChanged -= OnTextChanged;
            // Note: We can't easily unsubscribe the lambda for SelectionChanged unless we stored it.
            // But _historyDebounceTimer handles the SelectionChanged spam nicely now.
            
            // Suspend drawing for performance
            NativeMethods.SuspendDrawing(_codeEditor);

            try
            {
                _currentFilePath = filePath;
                _lblCurrentFile.Text = $"📄 {Path.GetFileName(filePath)}";
                
                // Read file asynchronously
                string content = await Task.Run(() => File.ReadAllText(filePath)).ConfigureAwait(true);
                
                // Set text (this is the heavy UI operation)
                _codeEditor.Text = content;
                
                // Reset Selection/View
                _codeEditor.SelectionStart = 0;
                _codeEditor.SelectionLength = 0;
                _codeEditor.SelectionColor = ModernTheme.TextPrimary;
                
                // Resume drawing before highlighting so the user sees *something*
                NativeMethods.ResumeDrawing(_codeEditor);
                
                // Async Highlighting
                _isHighlighting = true;
                if (_highlighter != null)
                {
                    // This is now safer as it runs on background and applies on UI
                    await _highlighter.HighlightAllAsync().ConfigureAwait(true); 
                }
                
                IsDirty = false;
                _lastLineCount = _codeEditor.Lines.Length;
            }
            catch (Exception ex)
            {
                NativeMethods.ResumeDrawing(_codeEditor); // Ensure we resume if error
                Linage.Infrastructure.DebugLogger.Error($"Error loading file {filePath}: {ex.Message}");
                Linage.Infrastructure.Services.NotificationManager.Instance.ShowError("Error", $"Error loading file: {ex.Message}");
            }
            finally 
            { 
                _isHighlighting = false; 
                _codeEditor.TextChanged += OnTextChanged;
            }
        }

        public void LoadContent(string title, string content, bool readOnly = false)
        {
            _currentFilePath = title; // Virtual path/name
            _isVirtual = true;
            _lblCurrentFile.Text = $"ℹ️ {title}";

            NativeMethods.SuspendDrawing(_codeEditor);
            try
            {
                _codeEditor.Text = content;
                _codeEditor.ReadOnly = readOnly;
                _codeEditor.SelectionStart = 0;
                _codeEditor.SelectionLength = 0;
                _codeEditor.BackColor = readOnly ? ModernTheme.SurfaceLight : ModernTheme.BackColor;
                
                IsDirty = false;
                _lastLineCount = _codeEditor.Lines.Length;
            }
            finally
            {
                NativeMethods.ResumeDrawing(_codeEditor);
            }
        }

        public void SaveFile()
        {
            if (string.IsNullOrEmpty(_currentFilePath) || _isVirtual || _codeEditor.ReadOnly) return;

            try
            {
                // Note: RichTextBox.Text returns plain text. RTF is hidden.
                // If we want to save formatting, we'd use SaveFile, but for code we want plain text.
                File.WriteAllText(_currentFilePath, _codeEditor.Text);
                IsDirty = false;
                FileSaved?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Linage.Infrastructure.Services.NotificationManager.Instance.ShowError("Error", $"Error saving: {ex.Message}");
            }
        }

        // --- Editor Logic ---

        private bool _isHighlighting = false; // Guard against recursive events

        // ...

        private void OnTextChanged(object sender, EventArgs e)
        {
            if (_isHighlighting) return; // STOP RECURSION

            IsDirty = true;
            
            if (_codeEditor.Lines.Length != _lastLineCount)
            {
                _lastLineCount = _codeEditor.Lines.Length;
            }

            // Highlighting Strategy:
            // Highlight current line immediately for responsiveness
            try 
            {
                _isHighlighting = true; // Block events
                int currentLineIndex = _codeEditor.GetLineFromCharIndex(_codeEditor.SelectionStart);
                _highlighter.HighlightLine(currentLineIndex);
            }
            catch { /* Ignore */ }
            finally 
            { 
                _isHighlighting = false; // Unblock
            }
                
            // Restart debounce timer for visible range update
            _typingTimer.Stop();
            _typingTimer.Start();
        }

        private void PerformSearch(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            
            int index = _codeEditor.Find(text, _codeEditor.SelectionStart + _codeEditor.SelectionLength, RichTextBoxFinds.None);
            if (index >= 0)
            {
                _codeEditor.Select(index, text.Length);
                _codeEditor.ScrollToCaret();
                _codeEditor.Focus();
            }
            else
            {
                index = _codeEditor.Find(text, 0, RichTextBoxFinds.None);
                if (index >= 0)
                {
                    _codeEditor.Select(index, text.Length);
                    _codeEditor.ScrollToCaret();
                    _codeEditor.Focus();
                }
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.F))
            {
                _searchPanel.Visible = true;
                _searchPanel.Location = new Point(_editorContainer.Width - _searchPanel.Width - 20, 10);
                _searchPanel.BringToFront();
                _txtSearch.Focus();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private async Task UpdateLineHistory()
        {
            // Guard against null reference - grid may not be initialized
            if (_lineHistoryGrid == null || _codeEditor.IsDisposed)
                return;

            if (_versionController == null || string.IsNullOrEmpty(_currentFilePath))
                return;

            try
            {
                // Must get values on UI thread before async
                int currentLineIndex = _codeEditor.GetLineFromCharIndex(_codeEditor.SelectionStart);
                int lineNumber = currentLineIndex + 1; // 1-based
                string filePath = _currentFilePath;
                
                // Clear grid on UI thread
                _lineHistoryGrid.Rows.Clear();

                // Get the relative path for the file
                string relativePath = filePath;
                if (!string.IsNullOrEmpty(_repositoryRoot) && filePath.StartsWith(_repositoryRoot))
                {
                    relativePath = filePath.Substring(_repositoryRoot.Length).TrimStart('\\', '/');
                }

                // ASYNC CALL - Non-blocking
                var lineBlame = await _versionController.GraphService.GetLineBlameAsync(relativePath, lineNumber);
                
                // Back on UI Context (await handles this in WinForms)
                if (_lineHistoryGrid.IsDisposed) return;

                if (lineBlame != null && lineBlame.CommitId.HasValue)
                {
                    // Found line-specific blame data
                    var commit = _versionController.GraphService.GetCommitById(lineBlame.CommitId.Value);
                    if (commit != null)
                    {
                        _lineHistoryGrid.Rows.Add(
                            commit.CommitHash?.Substring(0, Math.Min(7, commit.CommitHash?.Length ?? 0)) ?? "N/A",
                            commit.AuthorName ?? "Unknown",
                            commit.Timestamp.ToString("yyyy-MM-dd HH:mm")
                        );
                    }
                    else
                    {
                        // Commit not in cache, show change info
                        _lineHistoryGrid.Rows.Add(
                            lineBlame.CommitId?.ToString().Substring(0, 7) ?? "N/A",
                            "Unknown",
                            lineBlame.Timestamp.ToString("yyyy-MM-dd HH:mm")
                        );
                    }
                }
                else
                {
                    // Fallback: show commits that touched this file (file-level history)
                    // We can also make this async if GetCommitHistory() is slow
                    var commits = await Task.Run(() => _versionController.GraphService.GetCommitHistory());

                    foreach (var commit in commits.Take(10))
                    {
                        if (commit.Snapshot?.Files == null) continue;

                        var fileInCommit = commit.Snapshot.Files
                            .FirstOrDefault(f => f.FilePath.Equals(relativePath, StringComparison.OrdinalIgnoreCase) ||
                                                f.FilePath.EndsWith(Path.GetFileName(filePath), StringComparison.OrdinalIgnoreCase));

                        if (fileInCommit != null)
                        {
                            _lineHistoryGrid.Rows.Add(
                                commit.CommitHash?.Substring(0, Math.Min(7, commit.CommitHash?.Length ?? 0)) ?? "N/A",
                                commit.AuthorName ?? "Unknown",
                                commit.Timestamp.ToString("yyyy-MM-dd HH:mm")
                            );
                        }
                    }
                }

                // If no history found, show placeholder
                if (_lineHistoryGrid.Rows.Count == 0)
                {
                    _lineHistoryGrid.Rows.Add("---", $"Line {lineNumber}: Not tracked", "---");
                }
            }
            catch (Exception ex)
            {
                if (!_lineHistoryGrid.IsDisposed)
                {
                    _lineHistoryGrid.Rows.Clear();
                    _lineHistoryGrid.Rows.Add("Error", "History Unavailable", "---");
                }
                Linage.Infrastructure.DebugLogger.Warn($"UpdateLineHistory Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the version controller for querying line history/blame data.
        /// </summary>
        public void SetVersionController(VersionController controller, string repositoryRoot)
        {
            _versionController = controller;
            _repositoryRoot = repositoryRoot;
        }
    }
}