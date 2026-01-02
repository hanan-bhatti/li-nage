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
    public class EditorView : UserControl
    {
        private SplitContainer _mainContainer;
        private Panel _editorContainer;
        private Panel _gutter; // Changed to Panel for custom painting
        private EnhancedRichTextBox _codeEditor;
        private DataGridView _lineHistoryGrid;
        private Label _lblCurrentFile;
        private Panel _searchPanel;
        private TextBox _txtSearch;
        
        private string _currentFilePath;
        private bool _isDirty;
        private int _lastLineCount = 0;
        private SyntaxHighlighter _highlighter;
        private Timer _typingTimer; // Debounce for heavier highlighting if needed
        private VersionController _versionController;
        private string _repositoryRoot;

        public event EventHandler ContentChanged;
        public event EventHandler FileSaved;

        public bool IsDirty
        {
            get => _isDirty;
            private set
            {
                if (_isDirty != value)
                {
                    _isDirty = value;
                    ContentChanged?.Invoke(this, EventArgs.Empty);
                    _lblCurrentFile.Text = (_isDirty ? "● " : "") + 
                                         (_currentFilePath != null ? Path.GetFileName(_currentFilePath) : "Untitled");
                }
            }
        }

        public EditorView()
        {
            InitializeComponent();
            SetupSearchPanel();
            
            // Enable Double Buffering on Gutter Panel to remove flicker
            typeof(Panel).InvokeMember("DoubleBuffered", 
                System.Reflection.BindingFlags.SetProperty | 
                System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.NonPublic, 
                null, _gutter, new object[] { true });

            _highlighter = new SyntaxHighlighter(_codeEditor);
            
            _typingTimer = new Timer { Interval = 500 };
            _typingTimer.Tick += async (s, e) =>
            {
                _typingTimer.Stop();
                if (_isHighlighting) return;

                try
                {
                    // Calculate visible range
                    int firstChar = _codeEditor.GetCharIndexFromPosition(new Point(0, 0));
                    int lastChar = _codeEditor.GetCharIndexFromPosition(new Point(_codeEditor.Width, _codeEditor.Height));
                    
                    int start = Math.Max(0, firstChar - 1000); // Reduced buffer for speed
                    int end = Math.Min(_codeEditor.TextLength, lastChar + 1000);
                    int length = end - start;
                    
                    if (length > 0)
                    {
                        string visibleText = _codeEditor.Text.Substring(start, length);
                        
                        // Parse on background thread
                        var tokens = await _highlighter.ParseAsync(visibleText, start);
                        
                        // Apply on UI thread with Guard
                        if (!_codeEditor.IsDisposed)
                        {
                            _isHighlighting = true;
                            try { _highlighter.ApplyTokens(tokens, start, length); }
                            finally { _isHighlighting = false; }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Highlight error: " + ex.Message); 
                }
            };
        }

        private void InitializeComponent()
        {
            this.BackColor = ModernTheme.BackColor;

            // 1. Top Bar (File Info)
            _lblCurrentFile = new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = ModernTheme.SurfaceColor,
                ForeColor = ModernTheme.TextPrimary,
                Text = "No file open",
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Font = new Font(ModernTheme.MainFont.FontFamily, 9f, FontStyle.Bold)
            };

            // 2. Main Split Container
            _mainContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 800,
                BackColor = ModernTheme.SplitterColor,
                SplitterWidth = 2
            };

            // 3. Editor Container
            _editorContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.BackColor
            };

            // Gutter (Line Numbers)
            _gutter = new Panel
            {
                Dock = DockStyle.Left,
                Width = 50,
                BackColor = ModernTheme.SurfaceColor,
            };
            _gutter.Paint += OnGutterPaint;

            // Code Editor
            _codeEditor = new EnhancedRichTextBox
            {
                Dock = DockStyle.Fill,
                WordWrap = false,
                BackColor = ModernTheme.BackColor,
                ForeColor = ModernTheme.TextPrimary,
                BorderStyle = BorderStyle.None,
                AcceptsTab = true,
                ScrollBars = RichTextBoxScrollBars.Vertical // Force vertical only usually helps sync
            };
            
            try { _codeEditor.Font = ModernTheme.CodeFont; } catch { /* Fallback handled by control default */ }
            
            _codeEditor.TextChanged += OnTextChanged;
            
            // Sync Logic: Immediate Update
            _codeEditor.VScrollHappened += (s, e) => _gutter.Invalidate(); 
            _codeEditor.PaintHappened += (s, e) => _gutter.Invalidate();
            
            _codeEditor.Resize += (s, e) => _gutter.Invalidate();
            _codeEditor.SelectionChanged += (s, e) => {
                _gutter.Invalidate(); 
                UpdateLineHistory();
            };
            
            _editorContainer.Controls.Add(_codeEditor);
            _editorContainer.Controls.Add(_gutter);

            // 4. Line History Grid
            InitializeHistoryGrid();

            _mainContainer.Panel1.Controls.Add(_editorContainer);
            _mainContainer.Panel2.Controls.Add(_lineHistoryGrid);

            this.Controls.Add(_mainContainer);
            this.Controls.Add(_lblCurrentFile);
        }

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

        // --- Gutter Logic ---

        private void OnGutterPaint(object sender, PaintEventArgs e)
        {
            // Fast Line Number Rendering
            e.Graphics.Clear(ModernTheme.SurfaceColor);
            
            // Get visible range
            // Optimisation: GetCharIndexFromPosition(0,0) gives first visible char
            int firstCharIndex = _codeEditor.GetCharIndexFromPosition(new Point(0, 0));
            int firstLine = _codeEditor.GetLineFromCharIndex(firstCharIndex);
            
            // Get last visible line roughly
            int lastCharIndex = _codeEditor.GetCharIndexFromPosition(new Point(0, _codeEditor.Height));
            int lastLine = _codeEditor.GetLineFromCharIndex(lastCharIndex);
            if (lastLine < 0) lastLine = _codeEditor.Lines.Length - 1;

            // We need precise Y position of the first line
            Point firstPos = _codeEditor.GetPositionFromCharIndex(firstCharIndex);
            
            using (var brush = new SolidBrush(ModernTheme.TextSecondary))
            using (var currentLineBrush = new SolidBrush(ModernTheme.TextPrimary))
            {
                var font = _codeEditor.Font; // Do NOT dispose this! It belongs to the control.
                
                int currentLine = _codeEditor.GetLineFromCharIndex(_codeEditor.SelectionStart);
                int y = firstPos.Y;
                
                // We assume constant line height for speed, but fallback to GetPosition if needed
                // Using GetPosition inside loop is slow. 
                // Let's measure line height once.
                int lineHeight = TextRenderer.MeasureText("W", font).Height; 
                // Note: RichTextBox line height might vary slightly due to RTF, but usually constant for code.
                // Better approach: GetPosition for first, then add offset? 
                // No, safest is GetPosition for each line in visible range. It's fast enough for ~50 lines.
                
                for (int i = firstLine; i <= lastLine + 1; i++) // +1 just in case
                {
                    if (i >= _codeEditor.Lines.Length) break;

                    // This call is the most expensive part, but essential for correct sync
                    Point linePos = _codeEditor.GetPositionFromCharIndex(_codeEditor.GetFirstCharIndexFromLine(i));
                    
                    if (linePos.Y > _codeEditor.Height) break;
                    if (linePos.Y < -lineHeight) continue;

                    var b = (i == currentLine) ? currentLineBrush : brush;
                    e.Graphics.DrawString((i + 1).ToString(), font, b, new PointF(5, linePos.Y));
                }
            }
            
            using (var pen = new Pen(ModernTheme.BorderColor))
            {
                e.Graphics.DrawLine(pen, _gutter.Width - 1, 0, _gutter.Width - 1, _gutter.Height);
            }
        }

        // --- File Operations ---

        public async Task LoadFile(string filePath)
        {
            if (!File.Exists(filePath)) return;

            try
            {
                _currentFilePath = filePath;
                _lblCurrentFile.Text = $"📄 {Path.GetFileName(filePath)}";
                
                string content = File.ReadAllText(filePath);
                
                _codeEditor.TextChanged -= OnTextChanged;
                _codeEditor.Text = content;
                
                // Full Highlight on Load (Async)
                _isHighlighting = true;
                try { await _highlighter.HighlightAllAsync(); }
                finally { _isHighlighting = false; }
                
                _codeEditor.TextChanged += OnTextChanged;
                
                IsDirty = false;
                _lastLineCount = _codeEditor.Lines.Length;
                _gutter.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading file: {ex.Message}");
            }
        }

        public void SaveFile()
        {
            if (string.IsNullOrEmpty(_currentFilePath)) return;

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
                MessageBox.Show($"Error saving: {ex.Message}");
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
                _gutter.Invalidate();
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

        private void UpdateLineHistory()
        {
            _lineHistoryGrid.Rows.Clear();

            if (_versionController == null || string.IsNullOrEmpty(_currentFilePath))
                return;

            try
            {
                int currentLineIndex = _codeEditor.GetLineFromCharIndex(_codeEditor.SelectionStart);
                int lineNumber = currentLineIndex + 1; // 1-based

                // Get the relative path for the file
                string relativePath = _currentFilePath;
                if (!string.IsNullOrEmpty(_repositoryRoot) && _currentFilePath.StartsWith(_repositoryRoot))
                {
                    relativePath = _currentFilePath.Substring(_repositoryRoot.Length).TrimStart('\\', '/');
                }

                // Get commit history and find which commits touched this file/line
                var commits = _versionController.GraphService.GetCommitHistory();

                foreach (var commit in commits.Take(10)) // Show last 10 relevant commits
                {
                    // Check if this commit contains this file
                    if (commit.Snapshot?.Files == null) continue;

                    var fileInCommit = commit.Snapshot.Files
                        .FirstOrDefault(f => f.FilePath.Equals(relativePath, StringComparison.OrdinalIgnoreCase) ||
                                            f.FilePath.EndsWith(Path.GetFileName(_currentFilePath), StringComparison.OrdinalIgnoreCase));

                    if (fileInCommit != null)
                    {
                        _lineHistoryGrid.Rows.Add(
                            commit.CommitHash?.Substring(0, 7) ?? "N/A",
                            commit.AuthorName ?? "Unknown",
                            commit.Timestamp.ToString("yyyy-MM-dd HH:mm")
                        );
                    }
                }

                // If no history found, show placeholder
                if (_lineHistoryGrid.Rows.Count == 0)
                {
                    _lineHistoryGrid.Rows.Add("---", "Not tracked", "---");
                }
            }
            catch (Exception ex)
            {
                _lineHistoryGrid.Rows.Clear();
                _lineHistoryGrid.Rows.Add("Error", ex.Message, "---");
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