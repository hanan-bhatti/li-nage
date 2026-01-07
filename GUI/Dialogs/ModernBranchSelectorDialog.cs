using System;
using System.Drawing;
using System.Windows.Forms;
using Linage.GUI.Theme;
using Linage.GUI.Controls;

namespace Linage.GUI.Dialogs
{
    public class ModernBranchSelectorDialog : Form
    {
        public string SelectedBranch { get; private set; }
        public DialogResult CustomResult { get; private set; }

        private ModernListBox _listBox;
        private MaterialButton _btnSwitch;
        private MaterialButton _btnNew;
        private MaterialButton _btnDelete;
        private Label _btnClose;

        public ModernBranchSelectorDialog(string[] branches, string currentBranch)
        {
            InitializeComponent(branches, currentBranch);
            ApplyTheme();
        }

        private void InitializeComponent(string[] branches, string currentBranch)
        {
            this.Text = "Switch Branch";
            this.Size = new Size(400, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Padding = new Padding(1); // Border width

            // Main Container with Border
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.SurfaceColor,
                Padding = new Padding(20)
            };

            // Title
            var lblTitle = new Label
            {
                Text = "Switch Branch",
                Font = new Font("Segoe UI", 14, FontStyle.Regular),
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(20, 20)
            };

            // ListBox
            _listBox = new ModernListBox
            {
                Location = new Point(20, 60),
                Size = new Size(360, 320),
                BackColor = ModernTheme.BackColor,
                ForeColor = ModernTheme.TextPrimary,
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.None,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 30
            };

            // Custom Draw for ListBox
            _listBox.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;
                
                bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                string text = _listBox.Items[e.Index].ToString();
                bool isCurrent = text == currentBranch;

                // Background
                e.Graphics.FillRectangle(new SolidBrush(isSelected ? ModernTheme.SelectionColor : ModernTheme.BackColor), e.Bounds);

                // Text Color
                var textColor = isSelected ? Color.White : (isCurrent ? ModernTheme.PrimaryColor : ModernTheme.TextPrimary);
                
                // Draw Text
                TextRenderer.DrawText(e.Graphics, text, e.Font, new Point(e.Bounds.X + 5, e.Bounds.Y + 6), textColor);

                // Draw "Current" indicator
                if (isCurrent)
                {
                    TextRenderer.DrawText(e.Graphics, "(current)", new Font(e.Font.FontFamily, 8), 
                        new Point(e.Bounds.Right - 60, e.Bounds.Y + 8), ModernTheme.TextSecondary);
                }
            };
            
            _listBox.Items.AddRange(branches);
            if (!string.IsNullOrEmpty(currentBranch))
            {
                int index = Array.IndexOf(branches, currentBranch);
                if (index >= 0) _listBox.SelectedIndex = index;
            }

            // Buttons
            _btnSwitch = new MaterialButton { Text = "Switch", Location = new Point(20, 400), Size = new Size(110, 36) };
            _btnNew = new MaterialButton { Text = "New Branch", Location = new Point(140, 400), Size = new Size(110, 36), BackColor = ModernTheme.SurfaceColor, ForeColor = ModernTheme.TextPrimary };
            _btnDelete = new MaterialButton { Text = "Delete", Location = new Point(260, 400), Size = new Size(110, 36), BackColor = Color.FromArgb(200, 60, 60), ForeColor = Color.White };
            
            _btnClose = new Label 
            { 
                Text = "\uE711", // X icon
                Font = new Font("Segoe MDL2 Assets", 10f),
                Size = new Size(30, 30), 
                Location = new Point(mainPanel.Width - 35, 15), 
                ForeColor = ModernTheme.TextSecondary,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            
            _btnClose.MouseEnter += (s, e) => { _btnClose.ForeColor = ModernTheme.TextPrimary; _btnClose.BackColor = Color.FromArgb(40, 40, 40); };
            _btnClose.MouseLeave += (s, e) => { _btnClose.ForeColor = ModernTheme.TextSecondary; _btnClose.BackColor = Color.Transparent; };

            // Events
            _btnSwitch.Click += (s, e) => { SelectedBranch = _listBox.SelectedItem?.ToString(); CustomResult = DialogResult.OK; this.Close(); };
            _btnNew.Click += (s, e) => { CustomResult = DialogResult.Retry; this.Close(); };
            _btnDelete.Click += (s, e) => { SelectedBranch = _listBox.SelectedItem?.ToString(); CustomResult = DialogResult.Abort; this.Close(); };
            _btnClose.Click += (s, e) => { CustomResult = DialogResult.Cancel; this.Close(); };
            
            // Double click to switch
            _listBox.DoubleClick += (s, e) => { 
                if (_listBox.SelectedItem != null) { 
                    SelectedBranch = _listBox.SelectedItem.ToString(); 
                    CustomResult = DialogResult.OK; 
                    this.Close(); 
                } 
            };
            
            // Paint Border
            this.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, this.ClientRectangle, ModernTheme.PrimaryColor, ButtonBorderStyle.Solid);

            // Add Controls
            mainPanel.Controls.Add(lblTitle);
            mainPanel.Controls.Add(_btnClose);
            mainPanel.Controls.Add(_listBox);
            mainPanel.Controls.Add(_btnSwitch);
            mainPanel.Controls.Add(_btnNew);
            mainPanel.Controls.Add(_btnDelete);
            
            this.Controls.Add(mainPanel);
        }

        private void ApplyTheme()
        {
            this.BackColor = ModernTheme.PrimaryColor; // Border color
        }
        
        // Use standard ListBox if ModernListBox doesn't exist yet, but assuming check...
        // Fallback or use standard ListBox with owner draw if ModernListBox is missing in codebase
        // Based on search "ModernListBox", I haven't seen it defined. I'll define a simple subclass here to be safe.
        private class ModernListBox : ListBox 
        { 
            public ModernListBox() 
            {
                this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            }
        }
    }
}
