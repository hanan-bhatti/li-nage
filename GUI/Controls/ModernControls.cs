using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Linage.GUI.Theme;

namespace Linage.GUI.Controls
{
    // --- Custom Scrollbar ---
    public class ModernScrollBar : Control
    {
#pragma warning disable CS0067
        public event EventHandler<ScrollEventArgs> Scroll;
#pragma warning restore CS0067
        
        private int _value = 0;
        private int _maximum = 100;
        private int _largeChange = 10;
        
        // Minimalist Dimensions
        private const int ThinWidth = 4;
        private const int ThickWidth = 10;
        
        public ModernScrollBar()
        {
            this.Width = ThinWidth; 
            this.DoubleBuffered = true;
            this.BackColor = ModernTheme.BackColor; // Match Editor Background instead of Transparent
            this.Dock = DockStyle.Right;
        }

        public int Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = Math.Max(0, Math.Min(value, _maximum));
                    Invalidate();
                }
            }
        }

        public int Maximum
        {
            get => _maximum;
            set
            {
                if (_maximum != value)
                {
                    _maximum = Math.Max(0, value);
                    if (_value > _maximum) _value = _maximum;
                    Invalidate();
                }
            }
        }

        public int LargeChange
        {
            get => _largeChange;
            set
            {
                if (_largeChange != value)
                {
                    _largeChange = Math.Max(1, value);
                    Invalidate();
                }
            }
        }
        
        // ... (properties)

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.None;

            // Fill background manually to match theme if needed
            using (var brush = new SolidBrush(this.BackColor))
                g.FillRectangle(brush, ClientRectangle);

            if (_maximum <= 0) return;
            
            // ... (rest of drawing logic)
        }
        
        // ... (event handlers)
        
        // Removed unused OnMouseEnter/OnMouseLeave logic
        
        // Removed CreateParams transparency hack which caused crash
        // protected override CreateParams CreateParams ...
        // protected override void OnPaintBackground ...
    }

    // --- Modern Tab Control (VS Code Style) ---
    public class ModernTabControl : TabControl
    {
        private const int CloseButtonWidth = 16;
        private const int IconWidth = 16;
        private const int MinTabWidth = 80;
        private const int MaxTabWidth = 250;

        public ModernTabControl()
        {
            this.DrawMode = TabDrawMode.OwnerDrawFixed;
            this.Padding = new Point(0, 0); // Minimal padding for clean look
            this.SizeMode = TabSizeMode.Fixed;
            this.ItemSize = new Size(150, 35);
            this.DoubleBuffered = true;
            this.Multiline = false;
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index >= this.TabPages.Count) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            var tabRect = this.GetTabRect(e.Index);
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            var tabPage = this.TabPages[e.Index];

            // Background with subtle gradient
            Color bg = isSelected ? ModernTheme.TabActive : ModernTheme.TabInactive;
            using (var brush = new SolidBrush(bg))
                g.FillRectangle(brush, tabRect);

            // Right border separator (subtle)
            if (e.Index < this.TabPages.Count - 1)
            {
                using (var pen = new Pen(Color.FromArgb(30, ModernTheme.BorderColor), 1))
                {
                    g.DrawLine(pen, tabRect.Right - 1, tabRect.Top + 8, tabRect.Right - 1, tabRect.Bottom - 8);
                }
            }

            string title = tabPage.Text;
            Color textColor = isSelected ? ModernTheme.TextPrimary : ModernTheme.TextSecondary;

            // Top accent bar for active tab (JetBrains style)
            if (isSelected)
            {
                using (var brush = new SolidBrush(ModernTheme.PrimaryColor))
                {
                    var accentRect = new Rectangle(tabRect.Left, tabRect.Top, tabRect.Width, 3);
                    g.FillRectangle(brush, accentRect);
                }
            }

            int currentX = tabRect.Left + 8; // 8px left padding (Spacing.XSmall)

            // Draw icon if TabPage has an Icon tag (single Unicode character)
            if (tabPage.Tag is string iconHex && !string.IsNullOrEmpty(iconHex) && iconHex.Length <= 2)
            {
                using (var iconFont = new Font("Segoe MDL2 Assets", 10f))
                using (var iconBrush = new SolidBrush(textColor))
                {
                    var iconSize = g.MeasureString(iconHex, iconFont);
                    float iconY = tabRect.Top + (tabRect.Height - iconSize.Height) / 2;
                    g.DrawString(iconHex, iconFont, iconBrush, currentX, iconY);
                }
                currentX += IconWidth + 4; // Icon + 4px spacing
            }

            // Calculate available text area
            int availableWidth = tabRect.Width - (currentX - tabRect.Left) - CloseButtonWidth - 8;
            var textRect = new Rectangle(currentX, tabRect.Top, availableWidth, tabRect.Height);

            // Draw text with ellipsis if needed
            TextRenderer.DrawText(g, title, ModernTheme.FontBody, textRect, textColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

            // Close 'x' button (only show on hover or active)
            var closeRect = new Rectangle(tabRect.Right - 20, tabRect.Top + (tabRect.Height - 12) / 2, 12, 12);
            if (isSelected || (e.State & DrawItemState.HotLight) != 0)
            {
                using (var pen = new Pen(textColor, 1.5f))
                {
                    g.DrawLine(pen, closeRect.Left, closeRect.Top, closeRect.Right, closeRect.Bottom);
                    g.DrawLine(pen, closeRect.Left, closeRect.Bottom, closeRect.Right, closeRect.Top);
                }
            }
        }

        /// <summary>
        /// Calculates the optimal width for a tab based on its content.
        /// </summary>
        public int CalculateTabWidth(TabPage tabPage)
        {
            using (var g = this.CreateGraphics())
            {
                int width = 16; // Left padding (8px) + Right padding (8px)

                // Add icon width if present (single Unicode character)
                if (tabPage.Tag is string iconHex && !string.IsNullOrEmpty(iconHex) && iconHex.Length <= 2)
                {
                    width += IconWidth + 4; // Icon + spacing
                }

                // Measure text width
                var textSize = g.MeasureString(tabPage.Text, ModernTheme.FontBody);
                width += (int)textSize.Width + 4; // Text + small buffer

                // Add close button width
                width += CloseButtonWidth + 8;

                // Clamp to min/max
                return Math.Max(MinTabWidth, Math.Min(width, MaxTabWidth));
            }
        }

        /// <summary>
        /// Enables auto-sizing for tabs based on their content.
        /// </summary>
        public void EnableAutoSizing()
        {
            this.SizeMode = TabSizeMode.Normal;
            this.DrawMode = TabDrawMode.OwnerDrawFixed;

            // Recalculate tab sizes when tabs change
            this.ControlAdded += (s, e) => UpdateTabSizes();
            this.ControlRemoved += (s, e) => UpdateTabSizes();
        }

        private void UpdateTabSizes()
        {
            if (this.TabPages.Count == 0) return;

            // Calculate average or use fixed size
            // For Fixed mode, we set ItemSize to a reasonable default
            // For truly dynamic sizing per tab, we'd need Variable mode, but that doesn't support owner-draw well in WinForms
            // As a compromise, we can set ItemSize to fit the longest tab
            int maxWidth = 150;
            foreach (TabPage tab in this.TabPages)
            {
                int width = CalculateTabWidth(tab);
                if (width > maxWidth) maxWidth = width;
            }

            this.ItemSize = new Size(Math.Min(maxWidth, MaxTabWidth), 35);
            this.Invalidate();
        }

        /// <summary>
        /// Override OnPaint to remove white border around the tab control area (VS Code minimal style)
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // Suppress the default border by not drawing it
            // The background color should already be set correctly
        }

        /// <summary>
        /// Override to customize the tab page content area background
        /// </summary>
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Fill the entire control with dark background to avoid white showing
            using (var brush = new SolidBrush(ModernTheme.BackColor))
            {
                e.Graphics.FillRectangle(brush, this.ClientRectangle);
            }
        }
    }

    // --- Modern Tree View ---
    public class ModernTreeView : TreeView
    {
        [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string pszSubIdList);

        public ModernTreeView()
        {
            this.DrawMode = TreeViewDrawMode.OwnerDrawAll; // Full control
            this.ShowLines = false;
            this.ShowPlusMinus = false; // We draw our own chevrons
            this.FullRowSelect = true;
            this.HotTracking = true;
            this.BackColor = ModernTheme.SurfaceColor;
            this.ForeColor = ModernTheme.TextPrimary;
            this.LineColor = ModernTheme.BorderColor;
            this.ItemHeight = 28; // Modern, taller rows (VS Code style is usually 22, but 28 feels better for touch/modern)
            this.BorderStyle = BorderStyle.None;
            this.DoubleBuffered = true; // Prevent flicker
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            SetWindowTheme(this.Handle, "explorer", null);
        }

        protected override void OnDrawNode(DrawTreeNodeEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var node = e.Node;
            var bounds = e.Bounds;
            
            // 1. Background
            if ((e.State & TreeNodeStates.Selected) != 0)
            {
                using (var brush = new SolidBrush(ModernTheme.SelectionBack)) 
                    g.FillRectangle(brush, bounds);
            }
            else if ((e.State & TreeNodeStates.Hot) != 0)
            {
                using (var brush = new SolidBrush(ModernTheme.TabHover)) 
                    g.FillRectangle(brush, bounds);
            }
            else
            {
                using (var brush = new SolidBrush(this.BackColor))
                    g.FillRectangle(brush, bounds);
            }

            // 2. Indentation
            int indent = 20; // Slightly wider indentation
            int left = bounds.Left + (node.Level * indent) + 8;

            // 3. Chevron (Expand/Collapse)
            // Draw only if it has children
            if (node.Nodes.Count > 0)
            {
                using (var pen = new Pen(ModernTheme.TextSecondary, 1.5f))
                {
                    // Center Y for icon
                    float cy = bounds.Top + (bounds.Height / 2f);
                    float cx = left + 4;

                    if (node.IsExpanded)
                    {
                        // Down Arrow (v)
                        g.DrawLine(pen, cx - 4, cy - 2, cx, cy + 2);
                        g.DrawLine(pen, cx, cy + 2, cx + 4, cy - 2);
                    }
                    else
                    {
                        // Right Arrow (>)
                        g.DrawLine(pen, cx - 2, cy - 4, cx + 2, cy);
                        g.DrawLine(pen, cx + 2, cy, cx - 2, cy + 4);
                    }
                }
            }
            
            left += 16; // Space after chevron

            // 4. Icon (Folder/File) - Programmatic Drawing
            DrawIcon(g, node, left, bounds.Top + (bounds.Height - 16) / 2); // Center 16px icon
            left += 22; // Space after icon

            // 5. Text
            Color textColor = (e.State & TreeNodeStates.Selected) != 0 ? Color.White : 
                              (node.NodeFont != null ? node.ForeColor : this.ForeColor);
            
            if (node.Text.StartsWith(".")) textColor = ModernTheme.TextSecondary;

            // Vertically center text
            TextRenderer.DrawText(g, node.Text, this.Font, 
                new Rectangle(left, bounds.Top, bounds.Width - left, bounds.Height), 
                textColor, 
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            // Custom Expand/Collapse logic for Chevron click
            var info = this.HitTest(e.Location);
            if (info.Node != null && e.Button == MouseButtons.Left)
            {
                int indent = 20;
                int left = info.Node.Bounds.Left + (info.Node.Level * indent) + 8;
                
                // Chevron click zone (generous)
                if (e.X >= left - 5 && e.X <= left + 20)
                {
                    if (info.Node.IsExpanded) info.Node.Collapse();
                    else info.Node.Expand();
                    return; // Handled
                }
            }

            base.OnMouseDown(e);
        }

        private void DrawIcon(Graphics g, TreeNode node, int x, int y)
        {
            // Vector Icons - Size 16x16
            int size = 16;
            bool isFolder = node.ImageIndex == 0;

            if (isFolder)
            {
                // Folder Icon (Yellowish)
                Color folderColor = Color.FromArgb(209, 154, 102); 
                using (var brush = new SolidBrush(folderColor))
                {
                    // Main body
                    g.FillRectangle(brush, x, y + 2, size, size - 3);
                    // Tab
                    g.FillRectangle(brush, x, y, size / 2, 3);
                }
            }
            else
            {
                // File Icon
                string ext = System.IO.Path.GetExtension(node.Text).ToLower();
                Color fileColor = ModernTheme.TextPrimary;
                string letter = "";

                switch (ext)
                {
                    case ".cs": fileColor = Color.FromArgb(23, 134, 0); letter = "#"; break;
                    case ".json": fileColor = Color.Yellow; letter = "{}"; break;
                    case ".xml": fileColor = Color.Orange; letter = "<>"; break;
                    case ".txt": fileColor = ModernTheme.TextSecondary; letter = "≡"; break;
                    case ".md": fileColor = Color.LightBlue; letter = "M↓"; break;
                    case ".csproj": fileColor = Color.Purple; letter = "C"; break;
                    case ".sln": fileColor = Color.Purple; letter = "S"; break;
                    case ".gitignore": fileColor = Color.OrangeRed; letter = "git"; break;
                    default: fileColor = ModernTheme.TextSecondary; break; 
                }

                // Draw Document Shape
                using (var pen = new Pen(fileColor))
                {
                    g.DrawRectangle(pen, x + 2, y, size - 4, size);
                    // Fold
                    g.DrawLine(pen, x + size - 2, y, x + size - 2, y + 4);
                    g.DrawLine(pen, x + size - 6, y + 4, x + size - 2, y + 4);
                }
                
                // Draw Letter
                if (letter.Length > 0)
                {
                    using (var b = new SolidBrush(fileColor))
                    {
                        var f = new Font("Arial", 6f, FontStyle.Bold); 
                        // Center letter
                        var ts = g.MeasureString(letter, f);
                        g.DrawString(letter, f, b, x + (size - ts.Width)/2 + 1, y + (size - ts.Height)/2 + 1);
                    }
                }
            }
        }
    }

    // --- Previous Controls ---

    public class MaterialButton : Button
    {
        private Timer _animationTimer;
        private int _alpha = 0;
        private bool _isHovering = false;

        public MaterialButton()
        {
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.BackColor = ModernTheme.PrimaryColor;
            this.ForeColor = Color.White;
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            this.Cursor = Cursors.Hand;
            this.Size = new Size(100, 32);
            this.Padding = new Padding(0);
            
            _animationTimer = new Timer { Interval = 15 };
            _animationTimer.Tick += (s, e) => 
            {
                if (_isHovering && _alpha < 50) _alpha += 5;
                else if (!_isHovering && _alpha > 0) _alpha -= 5;
                else _animationTimer.Stop();
                this.Invalidate();
            };
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _isHovering = true; _animationTimer.Start(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _isHovering = false; _animationTimer.Start(); }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            var g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var brush = new SolidBrush(this.BackColor)) g.FillRectangle(brush, this.ClientRectangle);
            if (_alpha > 0) using (var brush = new SolidBrush(Color.FromArgb(_alpha, 255, 255, 255))) g.FillRectangle(brush, this.ClientRectangle);
            TextRenderer.DrawText(g, this.Text, this.Font, this.ClientRectangle, this.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    public class ActivityBarButton : Control
    {
        private bool _active = false;
        private bool _hover = false;
        private ToolTip _toolTip;
        private string _toolTipText;

        public string IconHex { get; set; } = "\uE700"; 
        
        public string ToolTipText
        {
            get => _toolTipText;
            set
            {
                _toolTipText = value;
                if (_toolTip == null) _toolTip = new ToolTip();
                _toolTip.SetToolTip(this, _toolTipText);
            }
        }

        public bool IsActive { get => _active; set { _active = value; Invalidate(); } }
        public event EventHandler Clicked;

        public ActivityBarButton()
        {
            this.DoubleBuffered = true;
            this.Cursor = Cursors.Hand;
            this.Size = new Size(50, 50);
            this.ForeColor = ModernTheme.TextSecondary;
            this.Dock = DockStyle.Top;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); }
        protected override void OnClick(EventArgs e) { Clicked?.Invoke(this, e); }
        protected override void Dispose(bool disposing) { if (disposing && _toolTip != null) _toolTip.Dispose(); base.Dispose(disposing); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            Color iconColor = _active ? Color.White : (_hover ? ModernTheme.TextPrimary : ModernTheme.TextSecondary);
            
            // Active Indicator (Thicker and Primary Color)
            if (_active) 
            {
                using (var brush = new SolidBrush(ModernTheme.PrimaryColor)) 
                    g.FillRectangle(brush, 0, 8, 3, Height - 16); // 3px wide, vertically centered padding
            }
            
            using (var brush = new SolidBrush(iconColor))
            using (var font = new Font("Segoe MDL2 Assets", 16f))
            {
                SizeF textSize = g.MeasureString(IconHex, font);
                g.DrawString(IconHex, font, brush, (Width - textSize.Width)/2, (Height - textSize.Height)/2);
            }
        }
    }

    public class ModernStatusBar : Panel
    {
        public ModernStatusBar()
        {
            this.Height = 22;
            this.Dock = DockStyle.Bottom;
            this.BackColor = ModernTheme.StatusBarColor;
            this.ForeColor = Color.White;
            this.Padding = new Padding(10, 0, 10, 0);
        }
    }

    public class MaterialTextBox : Panel
    {
        private TextBox _textBox;
        private bool _isFocused = false;
        public override string Text { get => _textBox.Text; set => _textBox.Text = value; }
        public ControlCollection InnerControls => _textBox.Controls; // Expose if needed? No, just add direct access properties if needed.
        public TextBox InnerTextBox => _textBox;

        public MaterialTextBox()
        {
            this.Height = 35;
            this.BackColor = ModernTheme.SurfaceColor;
            this.Padding = new Padding(10, 7, 10, 5);
            _textBox = new TextBox { BorderStyle = BorderStyle.None, BackColor = ModernTheme.SurfaceColor, ForeColor = ModernTheme.TextPrimary, Font = ModernTheme.FontBody, Dock = DockStyle.Fill };
            _textBox.Enter += (s, e) => { _isFocused = true; this.Invalidate(); };
            _textBox.Leave += (s, e) => { _isFocused = false; this.Invalidate(); };
            this.Controls.Add(_textBox);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            int lineY = this.Height - 1;
            using (var pen = new Pen(_isFocused ? ModernTheme.PrimaryColor : ModernTheme.BorderColor, _isFocused ? 2 : 1))
                e.Graphics.DrawLine(pen, 0, lineY, this.Width, lineY);
        }
    }

    // --- Premium Menu Renderer (Custom Dropdown) ---
    public class PremiumMenuRenderer : ToolStripProfessionalRenderer
    {
        public PremiumMenuRenderer() : base(new PremiumColorTable())
        {
            this.RoundedEdges = false; // Square, crisp edges
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected) return;

            var rect = new Rectangle(Point.Empty, e.Item.Size);
            
            // 1. Flat Hover Background
            using (var brush = new SolidBrush(ModernTheme.SurfaceLight))
            {
                e.Graphics.FillRectangle(brush, rect);
            }
            
            // 2. Active Indicator (Left Bar) - Minimal 3px
            using (var brush = new SolidBrush(ModernTheme.PrimaryColor))
            {
                e.Graphics.FillRectangle(brush, 0, 0, 3, rect.Height); 
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Selected ? ModernTheme.TextPrimary : ModernTheme.TextSecondary;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            // Minimal single pixel border
            using (var pen = new Pen(ModernTheme.BorderColor))
            {
                e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1));
            }
        }
        
        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            // Do NOT draw the default gradient margin. 
            // Just fill with background color to look like a flat list.
            using (var brush = new SolidBrush(ModernTheme.SurfaceColor))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds); 
            }
        }
    }

    public class PremiumColorTable : ProfessionalColorTable
    {
        // 1. Backgrounds - All Flat
        public override Color ToolStripDropDownBackground => ModernTheme.SurfaceColor;
        public override Color MenuStripGradientBegin => ModernTheme.SurfaceColor;
        public override Color MenuStripGradientEnd => ModernTheme.SurfaceColor;
        
        // 2. Image Margin (The Left Strip) - All Flat (Matches Background)
        public override Color ImageMarginGradientBegin => ModernTheme.SurfaceColor;
        public override Color ImageMarginGradientMiddle => ModernTheme.SurfaceColor;
        public override Color ImageMarginGradientEnd => ModernTheme.SurfaceColor;
        
        // 3. Borders - None/Transparent within the menu
        public override Color MenuBorder => ModernTheme.BorderColor;
        public override Color MenuItemBorder => Color.Transparent;
        
        // 4. Hover / Selection - handled in Renderer for custom look, but defaults here to be safe
        public override Color MenuItemSelected => ModernTheme.SurfaceLight;
        public override Color MenuItemSelectedGradientBegin => ModernTheme.SurfaceLight;
        public override Color MenuItemSelectedGradientEnd => ModernTheme.SurfaceLight;
        public override Color MenuItemPressedGradientBegin => ModernTheme.SurfaceLight;
        public override Color MenuItemPressedGradientEnd => ModernTheme.SurfaceLight;
        
        // 5. Separators
        public override Color SeparatorDark => ModernTheme.BorderColor;
        public override Color SeparatorLight => ModernTheme.BorderColor;
        
        // 6. Checkboxes
        public override Color CheckBackground => ModernTheme.SurfaceLight;
        public override Color CheckSelectedBackground => ModernTheme.SurfaceLight;
        public override Color CheckPressedBackground => ModernTheme.SurfaceLight;
        public override Color ButtonSelectedHighlight => Color.Transparent; // No highlight borders
    }
}