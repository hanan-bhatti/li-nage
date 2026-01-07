using System;
using System.Drawing;
using System.Windows.Forms;
using Linage.GUI.Theme;
using Linage.GUI.Controls;

namespace Linage.GUI.Dialogs
{
    public class ModernConfigDialog : Form
    {
        public string UserName { get; private set; }
        public string UserEmail { get; private set; }
        public DialogResult CustomResult { get; private set; }

        private TextBox _txtName;
        private TextBox _txtEmail;
        private MaterialButton _btnSave;
        private MaterialButton _btnCancel;
        private Label _btnClose;

        public ModernConfigDialog(string currentName, string currentEmail)
        {
            UserName = currentName;
            UserEmail = currentEmail;
            InitializeComponent();
            ApplyTheme();
        }

        private void InitializeComponent()
        {
            this.Text = "Li'nage Configuration";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Padding = new Padding(1);

            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.SurfaceColor,
                Padding = new Padding(20)
            };

            // Title
            var lblTitle = new Label
            {
                Text = "Configuration",
                Font = ModernTheme.FontH1,
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(20, 20)
            };

            // Close Button
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
            _btnClose.Click += (s, e) => { CustomResult = DialogResult.Cancel; this.Close(); };


            // Name Field
            var lblName = new Label
            {
                Text = "User Name",
                Font = ModernTheme.FontBody,
                ForeColor = ModernTheme.TextSecondary,
                Location = new Point(20, 70),
                AutoSize = true
            };
            _txtName = new TextBox
            {
                Text = UserName,
                Location = new Point(20, 95),
                Width = 360,
                Font = ModernTheme.FontBody,
                BackColor = ModernTheme.InputBack,
                ForeColor = ModernTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Email Field
            var lblEmail = new Label
            {
                Text = "User Email",
                Font = ModernTheme.FontBody,
                ForeColor = ModernTheme.TextSecondary,
                Location = new Point(20, 140),
                AutoSize = true
            };
            _txtEmail = new TextBox
            {
                Text = UserEmail,
                Location = new Point(20, 165),
                Width = 360,
                Font = ModernTheme.FontBody,
                BackColor = ModernTheme.InputBack,
                ForeColor = ModernTheme.TextPrimary,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Buttons
            _btnSave = new MaterialButton
            {
                Text = "Save",
                Location = new Point(230, 230),
                Size = new Size(150, 36),
                BackColor = ModernTheme.PrimaryColor,
                ForeColor = Color.White
            };
            _btnCancel = new MaterialButton
            {
                Text = "Cancel",
                Location = new Point(70, 230),
                Size = new Size(150, 36),
                BackColor = ModernTheme.SurfaceLight,
                ForeColor = ModernTheme.TextPrimary
            };

            _btnSave.Click += (s, e) =>
            {
                UserName = _txtName.Text.Trim();
                UserEmail = _txtEmail.Text.Trim();
                CustomResult = DialogResult.OK;
                this.Close();
            };
            _btnCancel.Click += (s, e) => { CustomResult = DialogResult.Cancel; this.Close(); };

            this.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, this.ClientRectangle, ModernTheme.PrimaryColor, ButtonBorderStyle.Solid);

            mainPanel.Controls.AddRange(new Control[] { lblTitle, _btnClose, lblName, _txtName, lblEmail, _txtEmail, _btnSave, _btnCancel });
            this.Controls.Add(mainPanel);
        }

        private void ApplyTheme()
        {
            this.BackColor = ModernTheme.PrimaryColor;
        }
    }
}
