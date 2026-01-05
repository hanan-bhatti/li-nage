using System;
using System.Drawing;
using System.Windows.Forms;
using Linage.Infrastructure;
using Linage.GUI.Theme;

namespace Linage.GUI
{
    public class DebugView : UserControl, IThemable
    {
        private RichTextBox _logBox;

        public void ApplyTheme()
        {
            // Debug view might want to keep its specific look or adapt
            // For now, let's make it adapt but keep green text?
            // Actually let's make it follow theme but maybe Monospace
            
            if (_logBox != null)
            {
                _logBox.BackColor = ModernTheme.BackColor;
                _logBox.ForeColor = ModernTheme.SuccessColor; // Keep it matrix-y or use text primary?
                _logBox.Font = ModernTheme.FontCode;
            }
        }

        private Timer _refreshTimer;

        public DebugView()
        {
            InitializeComponent();
            SetupTimer();
            SubscribeToDebugLogger();
            Linage.GUI.Helpers.WatermarkHelper.AddWatermarkLabel(this, "DebugView.cs");
        }

        private void SubscribeToDebugLogger()
        {
            DebugLogger.OnMessage += (message, level) =>
            {
                string prefix = GetLevelPrefix(level);
                Color color = GetLevelColor(level);
                AppendColoredLog(prefix, message, color);
            };
        }

        private string GetLevelPrefix(DebugLevel level)
        {
            switch (level)
            {
                case DebugLevel.Trace: return "[TRACE]";
                case DebugLevel.Info: return "[INFO]";
                case DebugLevel.Warning: return "[WARN]";
                case DebugLevel.Error: return "[ERROR]";
                default: return "[LOG]";
            }
        }

        private Color GetLevelColor(DebugLevel level)
        {
            switch (level)
            {
                case DebugLevel.Trace: return Color.Gray;
                case DebugLevel.Info: return Color.LimeGreen;
                case DebugLevel.Warning: return Color.Yellow;
                case DebugLevel.Error: return Color.Red;
                default: return Color.LimeGreen;
            }
        }

        private void AppendColoredLog(string prefix, string message, Color color)
        {
            if (_logBox.InvokeRequired)
            {
                _logBox.Invoke(new Action<string, string, Color>(AppendColoredLog), prefix, message, color);
            }
            else
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");

                // Add timestamp in default color
                _logBox.SelectionStart = _logBox.TextLength;
                _logBox.SelectionColor = Color.Gray;
                _logBox.AppendText($"[{timestamp}] ");

                // Add level prefix in level color
                _logBox.SelectionStart = _logBox.TextLength;
                _logBox.SelectionColor = color;
                _logBox.AppendText($"{prefix} ");

                // Add message in default color
                _logBox.SelectionStart = _logBox.TextLength;
                _logBox.SelectionColor = Color.LimeGreen;
                _logBox.AppendText($"{message}\n");

                _logBox.ScrollToCaret();
            }
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