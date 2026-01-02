using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using Linage.Core;
using Linage.GUI.Theme;

namespace Linage.GUI
{
    public class GitGraphView : UserControl
    {
        private List<Commit> _commits;

        public GitGraphView()
        {
            InitializeComponent();
            _commits = new List<Commit>();
            this.AutoScroll = true; // Enable virtual scrolling
            this.DoubleBuffered = true; // Prevent flicker
            this.ResizeRedraw = true;
        }

        private void InitializeComponent()
        {
            // We draw directly on the control now for better virtual scrolling support 
            // instead of using a child PictureBox which acts weird with virtual AutoScroll
            this.BackColor = ModernTheme.BackColor;
            this.Paint += OnPaintGraph;
        }

        public void SetCommits(List<Commit> commits)
        {
            _commits = commits ?? new List<Commit>();
            
            // Calculate virtual height
            int stepY = 50;
            int totalHeight = (_commits.Count * stepY) + 100;
            this.AutoScrollMinSize = new Size(0, totalHeight);
            
            this.Invalidate(); // Trigger redraw
        }

        private void OnPaintGraph(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Background
            g.Clear(ModernTheme.BackColor);

            if (_commits.Count == 0) return;

            int stepY = 50;
            int nodeSize = 12;
            int xCenter = 60;
            
            // Viewport calculations for Virtual Rendering
            int scrollY = this.VerticalScroll.Value;
            int startIndex = Math.Max(0, (scrollY / stepY) - 1);
            int visibleCount = (this.Height / stepY) + 2;
            int endIndex = Math.Min(_commits.Count - 1, startIndex + visibleCount);

            // Metro Style Pens/Brushes
            using (var penLine = new Pen(ModernTheme.SelectionBack, 2))
            using (var brushNodeCurrent = new SolidBrush(ModernTheme.AccentColor)) 
            using (var brushNodeOther = new SolidBrush(ModernTheme.SelectionBack))
            using (var brushText = new SolidBrush(ModernTheme.TextColor))
            using (var brushHash = new SolidBrush(ModernTheme.MutedText))
            {
                var fontMessage = ModernTheme.MainFont;
                var fontHash = new Font(ModernTheme.CodeFont.FontFamily, 8f, FontStyle.Regular);

                // Only draw visible range
                for (int i = startIndex; i <= endIndex; i++)
                {
                    var commit = _commits[i];
                    bool isHead = (i == 0);
                    
                    // Calculate Y pos relative to scroll
                    // absoluteY = 40 + (i * stepY)
                    // drawY = absoluteY - scrollY
                    int absoluteY = 40 + (i * stepY);
                    int y = absoluteY - scrollY;

                    // Draw connecting line to previous (if visible or just above)
                    // We draw line UP from current node
                    if (i > 0)
                    {
                        g.DrawLine(penLine, xCenter, y - stepY + nodeSize / 2, xCenter, y - nodeSize / 2);
                    }

                    // Draw Node
                    var brush = isHead ? brushNodeCurrent : brushNodeOther;
                    g.FillEllipse(brush, xCenter - nodeSize / 2, y - nodeSize / 2, nodeSize, nodeSize);

                    // Draw Text
                    int textX = xCenter + 25;
                    g.DrawString(commit.Message ?? "No message", fontMessage, brushText, textX, y - 10);
                    
                    var hashText = commit.CommitHash != null && commit.CommitHash.Length >= 7 
                        ? commit.CommitHash.Substring(0, 7) 
                        : (commit.CommitHash ?? "N/A");
                    g.DrawString(hashText, fontHash, brushHash, textX, y + 10);
                }

                fontHash.Dispose();
            }
        }
    }
}