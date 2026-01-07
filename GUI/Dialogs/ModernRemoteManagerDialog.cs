using System;
using System.Drawing;
using System.Windows.Forms;
using Linage.Core;
using Linage.GUI.Theme;
using Linage.GUI.Controls;

namespace Linage.GUI.Dialogs
{
    public class ModernRemoteManagerDialog : Form
    {
        public string SelectedRemote { get; private set; }
        public DialogResult CustomResult { get; private set; }

        private ModernListBox _listBox;
        private MaterialButton _btnAdd;
        private MaterialButton _btnRemove;
        private MaterialButton _btnSetDefault;
        private Label _btnClose;

        public ModernRemoteManagerDialog(System.Collections.Generic.List<Remote> remotes)
        {
            InitializeComponent(remotes);
            ApplyTheme();
        }

        private void InitializeComponent(System.Collections.Generic.List<Remote> remotes)
        {
            this.Text = "Manage Remotes";
            this.Size = new Size(500, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Padding = new Padding(1);

            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.SurfaceColor,
                Padding = new Padding(20)
            };

            var lblTitle = new Label
            {
                Text = "Manage Remotes",
                Font = new Font("Segoe UI", 14, FontStyle.Regular),
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(20, 20)
            };

            _listBox = new ModernListBox
            {
                Location = new Point(20, 60),
                Size = new Size(460, 320),
                BackColor = ModernTheme.BackColor,
                ForeColor = ModernTheme.TextPrimary,
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.None,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 40
            };

            _listBox.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;
                
                var remote = (Remote)_listBox.Items[e.Index];
                bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                
                e.Graphics.FillRectangle(new SolidBrush(isSelected ? ModernTheme.SelectionColor : ModernTheme.BackColor), e.Bounds);

                var titleColor = isSelected ? Color.White : ModernTheme.TextPrimary;
                var urlColor = isSelected ? Color.LightGray : ModernTheme.TextSecondary;
                
                // Name
                TextRenderer.DrawText(e.Graphics, remote.RemoteName, new Font(e.Font, FontStyle.Bold), new Point(e.Bounds.X + 5, e.Bounds.Y + 4), titleColor);
                
                // URL
                TextRenderer.DrawText(e.Graphics, remote.RemoteUrl, new Font(e.Font.FontFamily, 9), new Point(e.Bounds.X + 5, e.Bounds.Y + 22), urlColor);

                // Default Badge
                if (remote.IsDefault)
                {
                    var badgeRect = new Rectangle(e.Bounds.Right - 70, e.Bounds.Y + 10, 60, 20);
                    e.Graphics.FillRectangle(new SolidBrush(ModernTheme.PrimaryColor), badgeRect);
                    TextRenderer.DrawText(e.Graphics, "DEFAULT", new Font("Segoe UI", 7, FontStyle.Bold), badgeRect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            };
            
            _listBox.DisplayMember = "RemoteName";
            foreach(var r in remotes) _listBox.Items.Add(r);

            // Buttons
            _btnAdd = new MaterialButton { Text = "Add Remote", Location = new Point(20, 400), Size = new Size(110, 36) };
            _btnSetDefault = new MaterialButton { Text = "Set Default", Location = new Point(140, 400), Size = new Size(110, 36), BackColor = ModernTheme.SurfaceColor, ForeColor = ModernTheme.TextPrimary };
            _btnRemove = new MaterialButton { Text = "Remove", Location = new Point(260, 400), Size = new Size(110, 36), BackColor = Color.FromArgb(200, 60, 60), ForeColor = Color.White };
            
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

            _btnAdd.Click += (s, e) => { CustomResult = DialogResult.Retry; this.Close(); };
            _btnSetDefault.Click += (s, e) => { 
                if(_listBox.SelectedItem != null) { 
                    SelectedRemote = ((Remote)_listBox.SelectedItem).RemoteName; 
                    CustomResult = DialogResult.OK; 
                    this.Close(); 
                } 
            };
            _btnRemove.Click += (s, e) => { 
                if(_listBox.SelectedItem != null) { 
                    SelectedRemote = ((Remote)_listBox.SelectedItem).RemoteName; 
                    CustomResult = DialogResult.Abort; 
                    this.Close(); 
                } 
            };
            _btnClose.Click += (s, e) => { CustomResult = DialogResult.Cancel; this.Close(); };

            this.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, this.ClientRectangle, ModernTheme.PrimaryColor, ButtonBorderStyle.Solid);

            mainPanel.Controls.Add(lblTitle);
            mainPanel.Controls.Add(_btnClose);
            mainPanel.Controls.Add(_listBox);
            mainPanel.Controls.Add(_btnAdd);
            mainPanel.Controls.Add(_btnSetDefault);
            mainPanel.Controls.Add(_btnRemove);
            
            this.Controls.Add(mainPanel);
        }

        private void ApplyTheme()
        {
            this.BackColor = ModernTheme.PrimaryColor;
        }

        private class ModernListBox : ListBox 
        { 
            public ModernListBox() 
            {
                this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            }
        }
    }
}
