using System;
using System.Drawing;
using System.Windows.Forms;
using Linage.Core;
using Linage.GUI.Theme;

namespace Linage.GUI
{
    public class MergeConflictView : UserControl, IThemable
    {
        private SplitContainer splitContainerMain;
        private RichTextBox _rtbLocal;
        private RichTextBox _rtbRemote;
        private RichTextBox _rtbResult;
        private Label _lblConflictInfo;
        private Button _btnAcceptLocal;
        private Button _btnAcceptRemote;
        private Button _btnSave;

        private Conflict _currentConflict;

        public event EventHandler<Conflict> ConflictResolved;

        public MergeConflictView()
        {
            InitializeComponent();
        }

        public void ApplyTheme()
        {
            this.BackColor = ModernTheme.BackColor;
            this.ForeColor = ModernTheme.TextPrimary;

            if (_lblConflictInfo != null) _lblConflictInfo.ForeColor = ModernTheme.TextPrimary;
            
            if (_rtbLocal != null)
            {
                _rtbLocal.BackColor = ModernTheme.BackColor;
                _rtbLocal.ForeColor = ModernTheme.TextPrimary;
            }

            if (_rtbRemote != null)
            {
                _rtbRemote.BackColor = ModernTheme.BackColor;
                _rtbRemote.ForeColor = ModernTheme.TextPrimary;
            }

            if (_rtbResult != null)
            {
                _rtbResult.BackColor = ModernTheme.BackColor;
                _rtbResult.ForeColor = ModernTheme.TextPrimary;
            }
        }

        private void InitializeComponent()
        {
            this.Size = new Size(800, 600);
            this.BackColor = ModernTheme.BackColor;
            this.ForeColor = ModernTheme.TextPrimary;

            // Layout
            var panelTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = ModernTheme.SurfaceColor,
                Padding = new Padding(10)
            };

            _lblConflictInfo = new Label
            {
                Location = new Point(10, 10),
                AutoSize = true,
                Text = "No Conflict Selected",
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.FontBody
            };

            _btnAcceptLocal = new Button
            {
                Text = "Accept Local",
                Location = new Point(10, 35),
                BackColor = ModernTheme.PrimaryColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnAcceptLocal.Click += (s, e) => ResolveWith(_currentConflict?.LocalContent);

            _btnAcceptRemote = new Button
            {
                Text = "Accept Remote",
                Location = new Point(130, 35),
                BackColor = ModernTheme.PrimaryColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnAcceptRemote.Click += (s, e) => ResolveWith(_currentConflict?.RemoteContent);

            _btnSave = new Button
            {
                Text = "Mark Resolved",
                Location = new Point(250, 35),
                BackColor = ModernTheme.SuccessColor,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(ModernTheme.FontBody, FontStyle.Bold)
            };
            _btnSave.Click += OnSave;

            panelTop.Controls.Add(_lblConflictInfo);
            panelTop.Controls.Add(_btnAcceptLocal);
            panelTop.Controls.Add(_btnAcceptRemote);
            panelTop.Controls.Add(_btnSave);

            splitContainerMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                BackColor = ModernTheme.BackColor,
                ForeColor = ModernTheme.TextPrimary
            };

            var splitTop = new SplitContainer
            {
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.BackColor,
                ForeColor = ModernTheme.TextPrimary
            };

            _rtbLocal = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(50, 100, 50),
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.FontCode
            };

            _rtbRemote = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(50, 50, 100),
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.FontCode
            };

            var localHeader = new Label
            {
                Text = "Local (Current)",
                Dock = DockStyle.Top,
                BackColor = ModernTheme.SurfaceColor,
                ForeColor = ModernTheme.TextPrimary,
                Height = 25,
                Padding = new Padding(5, 5, 0, 0),
                Font = ModernTheme.FontSmall
            };

            var remoteHeader = new Label
            {
                Text = "Remote (Incoming)",
                Dock = DockStyle.Top,
                BackColor = ModernTheme.SurfaceColor,
                ForeColor = ModernTheme.TextPrimary,
                Height = 25,
                Padding = new Padding(5, 5, 0, 0),
                Font = ModernTheme.FontSmall
            };

            splitTop.Panel1.Controls.Add(_rtbLocal);
            splitTop.Panel1.Controls.Add(localHeader);
            splitTop.Panel2.Controls.Add(_rtbRemote);
            splitTop.Panel2.Controls.Add(remoteHeader);

            _rtbResult = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.FontCode
            };

            var resultHeader = new Label
            {
                Text = "Result (Editable)",
                Dock = DockStyle.Top,
                BackColor = ModernTheme.SurfaceColor,
                ForeColor = ModernTheme.TextPrimary,
                Height = 25,
                Padding = new Padding(5, 5, 0, 0),
                Font = ModernTheme.FontSmall
            };

            splitContainerMain.Panel1.Controls.Add(splitTop);
            splitContainerMain.Panel2.Controls.Add(_rtbResult);
            splitContainerMain.Panel2.Controls.Add(resultHeader);

            this.Controls.Add(splitContainerMain);
            this.Controls.Add(panelTop);
        }

        public void SetConflict(Conflict conflict)
        {
            _currentConflict = conflict;
            if (conflict == null) return;

            _lblConflictInfo.Text = $"Conflict in: {conflict.FilePath}";
            _rtbLocal.Text = conflict.LocalContent;
            _rtbRemote.Text = conflict.RemoteContent;
            
            // Default result to Local <<<<< Remote markers (simple viz)
            _rtbResult.Text = $"<<<<<<< LOCAL\n{conflict.LocalContent}\n=======\n{conflict.RemoteContent}\n>>>>>>> REMOTE";
        }

        private void ResolveWith(string content)
        {
            if (content != null)
                _rtbResult.Text = content;
        }

        private void OnSave(object sender, EventArgs e)
        {
            if (_currentConflict != null)
            {
                _currentConflict.ResolvedContent = _rtbResult.Text;
                _currentConflict.IsResolved = true;
                ConflictResolved?.Invoke(this, _currentConflict);
            }
        }
    }
}
