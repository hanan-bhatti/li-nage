using System;
using System.Drawing;
using System.Windows.Forms;
using Linage.Core;

namespace Linage.GUI
{
    public class MergeConflictView : UserControl
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

        private void InitializeComponent()
        {
            this.Size = new Size(800, 600);

            // Layout
            var panelTop = new Panel { Dock = DockStyle.Top, Height = 60 };
            _lblConflictInfo = new Label { Location = new Point(10, 10), AutoSize = true, Text = "No Conflict Selected" };
            
            _btnAcceptLocal = new Button { Text = "Accept Local", Location = new Point(10, 30) };
            _btnAcceptLocal.Click += (s, e) => ResolveWith(_currentConflict?.LocalContent);

            _btnAcceptRemote = new Button { Text = "Accept Remote", Location = new Point(120, 30) };
            _btnAcceptRemote.Click += (s, e) => ResolveWith(_currentConflict?.RemoteContent);

            _btnSave = new Button { Text = "Mark Resolved", Location = new Point(230, 30) };
            _btnSave.Click += OnSave;

            panelTop.Controls.Add(_lblConflictInfo);
            panelTop.Controls.Add(_btnAcceptLocal);
            panelTop.Controls.Add(_btnAcceptRemote);
            panelTop.Controls.Add(_btnSave);

            splitContainerMain = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal };
            
            var splitTop = new SplitContainer { Dock = DockStyle.Fill }; // Local vs Remote
            _rtbLocal = new RichTextBox { Dock = DockStyle.Fill, BackColor = Color.LightPink };
            _rtbRemote = new RichTextBox { Dock = DockStyle.Fill, BackColor = Color.LightCyan };
            
            splitTop.Panel1.Controls.Add(new Label { Text = "Local (Current)", Dock = DockStyle.Top });
            splitTop.Panel1.Controls.Add(_rtbLocal);
            splitTop.Panel2.Controls.Add(new Label { Text = "Remote (Incoming)", Dock = DockStyle.Top });
            splitTop.Panel2.Controls.Add(_rtbRemote);

            _rtbResult = new RichTextBox { Dock = DockStyle.Fill, BackColor = Color.LightYellow };

            splitContainerMain.Panel1.Controls.Add(splitTop);
            splitContainerMain.Panel2.Controls.Add(new Label { Text = "Result (Editable)", Dock = DockStyle.Top });
            splitContainerMain.Panel2.Controls.Add(_rtbResult);

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
