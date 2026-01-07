using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Linage.GUI.Theme;
using Linage.GUI.Controls;

namespace Linage.GUI.Dialogs
{
    public class ModernInputDialog : Form
    {
        public string InputValue { get; private set; }

        private MaterialTextBox _txtInput;
        private MaterialButton _btnOk;
        private MaterialButton _btnCancel;

        public ModernInputDialog(string title, string prompt, string defaultValue = "")
        {
            InitializeComponent(title, prompt, defaultValue);
        }

        private void InitializeComponent(string title, string prompt, string defaultValue)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(400, 200);
            this.BackColor = ModernTheme.SurfaceLight;
            this.ShowInTaskbar = false;

            // 1. Border & Shadow simulation (Paint Event)
            this.Padding = new Padding(1); // For border
            this.Paint += (s, e) => 
            {
                using (var pen = new Pen(ModernTheme.PrimaryColor, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }
            };

            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.SurfaceColor,
                Padding = new Padding(20)
            };

            // 2. Title
            var lblTitle = new Label
            {
                Text = title,
                Font = new Font(ModernTheme.FontBody.FontFamily, 14f, FontStyle.Bold),
                ForeColor = ModernTheme.TextPrimary,
                AutoSize = true,
                Location = new Point(20, 20)
            };

            // 3. Prompt Message
            var lblPrompt = new Label
            {
                Text = prompt,
                Font = ModernTheme.FontBody,
                ForeColor = ModernTheme.TextSecondary,
                AutoSize = true,
                Location = new Point(20, 60)
            };

            // 4. Input Box
            _txtInput = new MaterialTextBox
            {
                Location = new Point(20, 90),
                Width = 360,
                Text = defaultValue
            };
            // Handle Enter key
            _txtInput.InnerTextBox.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { _btnOk.PerformClick(); } };

            // 5. Buttons
            _btnCancel = new MaterialButton
            {
                Text = "Cancel",
                BackColor = ModernTheme.SurfaceLight,
                ForeColor = ModernTheme.TextPrimary,
                Location = new Point(180, 145),
                Width = 90
            };
            _btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            _btnOk = new MaterialButton
            {
                Text = "OK",
                BackColor = ModernTheme.PrimaryColor,
                ForeColor = Color.White,
                Location = new Point(280, 145),
                Width = 90
            };
            _btnOk.Click += (s, e) => 
            { 
                InputValue = _txtInput.Text; 
                this.DialogResult = DialogResult.OK; 
                this.Close(); 
            };

            mainPanel.Controls.Add(lblTitle);
            mainPanel.Controls.Add(lblPrompt);
            mainPanel.Controls.Add(_txtInput);
            mainPanel.Controls.Add(_btnCancel);
            mainPanel.Controls.Add(_btnOk);

            this.Controls.Add(mainPanel);
        }
    }
}
