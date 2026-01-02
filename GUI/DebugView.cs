using System;
using System.Drawing;
using System.Windows.Forms;

namespace Linage.GUI
{
    public class DebugView : UserControl
    {
        private RichTextBox _logBox;
        private Timer _refreshTimer;

        public DebugView()
        {
            InitializeComponent();
            SetupTimer();
        }

        private void InitializeComponent()
        {
            _logBox = new RichTextBox();
            _logBox.Dock = DockStyle.Fill;
            _logBox.BackColor = Color.FromArgb(20, 20, 20);
            _logBox.ForeColor = Color.LimeGreen;
            _logBox.Font = new Font("Consolas", 9);
            _logBox.ReadOnly = true;
            
            this.Controls.Add(_logBox);
        }

        private void SetupTimer()
        {
            _refreshTimer = new Timer();
            _refreshTimer.Interval = 1000;
            _refreshTimer.Tick += (s, e) => UpdateDiagnostics();
            _refreshTimer.Start();
        }

        private void UpdateDiagnostics()
        {
            // In a real app, this would poll a central logging service or diagnostic provider
            // For now, we simulate a heartbeat
            // AppendLog($"[System] Memory: {GC.GetTotalMemory(false) / 1024} KB");
        }

        public void Log(string message)
        {
            if (_logBox.InvokeRequired)
            {
                _logBox.Invoke(new Action<string>(Log), message);
            }
            else
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                _logBox.AppendText($"[{timestamp}] {message}\n");
                _logBox.ScrollToCaret();
            }
        }
    }
}