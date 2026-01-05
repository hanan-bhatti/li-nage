using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using Linage.Core;
using Linage.GUI.Theme;

namespace Linage.GUI
{
    public class GitGraphView : UserControl, IThemable
    {
        private List<Commit> _commits;

        public void ApplyTheme()
        {
            this.BackColor = ModernTheme.BackColor;
            this.Invalidate(); // Trigger repaint with new colors
        }

        private HashSet<Guid> _expandedCommits = new HashSet<Guid>();
        private Dictionary<int, int> _rowHeights = new Dictionary<int, int>();
        private int _totalContentHeight = 0;

        // Constants
        private const int DEFAULT_HEIGHT = 60; // Compact height
        private const int NODE_SIZE = 12;
        private const int X_CENTER = 60;
        private const int TEXT_X = 85; 
        private const int TEXT_PADDING = 10;

        public GitGraphView()
        {
            InitializeComponent();
            _commits = new List<Commit>();
            this.AutoScroll = true; 
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;
            Linage.GUI.Helpers.WatermarkHelper.AddWatermarkLabel(this, "GitGraphView.cs");
        }

        private void InitializeComponent()
        {
            this.BackColor = ModernTheme.BackColor;
            this.Paint += OnPaintGraph;
            this.MouseClick += OnMouseClick;
        }

        public void SetCommits(List<Commit> commits)
        {
            _commits = commits ?? new List<Commit>();
            _expandedCommits.Clear();
            RecalculateLayout();
        }

        private void RecalculateLayout()
        {
            using (var g = this.CreateGraphics())
            {
                _rowHeights.Clear();
                _totalContentHeight = 40; // Initial padding

                for (int i = 0; i < _commits.Count; i++)
                {
                    int height = DEFAULT_HEIGHT;
                    if (_expandedCommits.Contains(_commits[i].CommitId))
                    {
                        // Measure full text
                        var size = g.MeasureString(_commits[i].Message ?? "", ModernTheme.MainFont, this.Width - TEXT_X - 20);
                        height = (int)size.Height + 40; // Text + Padding
                        height = Math.Max(height, DEFAULT_HEIGHT);
                    }
                    
                    _rowHeights[i] = height;
                    _totalContentHeight += height;
                }
            }
            
            this.AutoScrollMinSize = new Size(0, _totalContentHeight);
            this.Invalidate();
        }

        private void OnMouseClick(object sender, MouseEventArgs e)
        {
            int scrollY = this.VerticalScroll.Value;
            int clickY = e.Y + scrollY;
            
            int currentY = 40;
            for (int i = 0; i < _commits.Count; i++)
            {
                int h = _rowHeights.ContainsKey(i) ? _rowHeights[i] : DEFAULT_HEIGHT;
                
                if (clickY >= currentY && clickY < currentY + h)
                {
                    // Toggle expansion
                    if (_expandedCommits.Contains(_commits[i].CommitId))
                        _expandedCommits.Remove(_commits[i].CommitId);
                    else
                        _expandedCommits.Add(_commits[i].CommitId);

                    RecalculateLayout();
                    return;
                }
                currentY += h;
            }
        }

        private void OnPaintGraph(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            g.Clear(ModernTheme.BackColor);

            if (_commits.Count == 0) return;

            int scrollY = this.VerticalScroll.Value;
            int currentY = 40;

            // Pens & Brushes
            using (var penLine = new Pen(ModernTheme.SelectionBack, 2))
            using (var brushNodeCurrent = new SolidBrush(ModernTheme.AccentColor)) 
            using (var brushNodeOther = new SolidBrush(ModernTheme.SelectionBack))
            using (var brushText = new SolidBrush(ModernTheme.TextColor))
            using (var brushHash = new SolidBrush(ModernTheme.MutedText))
            {
                var fontMessage = ModernTheme.MainFont;
                var fontHash = new Font(ModernTheme.CodeFont.FontFamily, 8f, FontStyle.Regular);

                // Iterate all commits to track Y position, but only draw visible ones
                for (int i = 0; i < _commits.Count; i++)
                {
                    var commit = _commits[i];
                    int height = _rowHeights.ContainsKey(i) ? _rowHeights[i] : DEFAULT_HEIGHT;
                    bool isVisible = (currentY + height >= scrollY) && (currentY <= scrollY + this.Height);

                    if (isVisible)
                    {
                        int drawY = currentY - scrollY;
                        int centerY = drawY + (DEFAULT_HEIGHT / 2); // Anchor node to top-left area essentially, or center of collapsed state? 
                                                                    // Better visuals: Anchor node to consistent top offset relative to row start.
                        int nodeCenterY = drawY + 20; // Fixed top offset for node

                        // Draw connecting line to previous
                        if (i > 0)
                        {
                            // Line connects from previous node's bottom to this node's top
                            // Ideally we need previous node's position. 
                            // Simplified: Draw line UP to top of row (which connects to bottom of prev row)
                            g.DrawLine(penLine, X_CENTER, drawY - 20, X_CENTER, nodeCenterY); 
                            // Note: This simple vertical line assumes linear flow. Git graph usually needs parent tracking.
                            // For linear list view this is fine.
                        }
                        
                        // Draw line DOWN to next
                        if (i < _commits.Count - 1)
                        {
                            g.DrawLine(penLine, X_CENTER, nodeCenterY, X_CENTER, drawY + height);
                        }

                        // Draw Node
                        bool isHead = (i == 0); // Warning: i=0 is just top of list, not necessarily HEAD if filtered. But for now ok.
                        var brush = isHead ? brushNodeCurrent : brushNodeOther;
                        g.FillEllipse(brush, X_CENTER - NODE_SIZE / 2, nodeCenterY - NODE_SIZE / 2, NODE_SIZE, NODE_SIZE);

                        // Draw Text
                        var rectText = new RectangleF(TEXT_X, drawY + TEXT_PADDING, this.Width - TEXT_X - 10, height - TEXT_PADDING);
                        
                        var format = new StringFormat();
                        if (_expandedCommits.Contains(commit.CommitId))
                        {
                            format.FormatFlags = 0; // Wrap
                            format.Trimming = StringTrimming.None;
                        }
                        else
                        {
                            format.FormatFlags = StringFormatFlags.NoWrap;
                            format.Trimming = StringTrimming.EllipsisCharacter;
                        }
                        
                        g.DrawString(commit.Message ?? "No message", fontMessage, brushText, rectText, format);

                        // Draw Hash (small, below text or to the right?)
                        // If expanded, bottom. If collapsed, right aligned? 
                        // Let's keep it simple: always below text if space, or right side.
                        // For minimal look: Right side of summary? 
                        // Screenshot showed it below.
                        
                        // Hash below text
                        if (_expandedCommits.Contains(commit.CommitId))
                        {
                             // Bottom of cell
                             g.DrawString(commit.CommitHash.Substring(0, 7), fontHash, brushHash, TEXT_X, drawY + height - 20);
                        }
                        else
                        {
                             // Next to summary for compact
                             // Or just below summary line (y + 20)
                             g.DrawString(commit.CommitHash.Substring(0, 7), fontHash, brushHash, TEXT_X, drawY + 30);
                        }
                    }

                    currentY += height;
                    
                    // Optimization: Break if we are way past viewport
                    if (currentY > scrollY + this.Height + 500) break;
                }
                
                fontHash.Dispose();
            }
        }
    }
}