namespace Linage.GUI
{
    partial class MainWindow
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.Editor = new Linage.GUI.EditorView();
            this.GraphView = new Linage.GUI.GitGraphView();
            this.Terminal = new Linage.GUI.TerminalView();
            this.Debugger = new Linage.GUI.DebugView();
            this.AIHistory = new Linage.GUI.AIHistoryView();
            this.SuspendLayout();
            
            // Editor
            this.Editor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Editor.Location = new System.Drawing.Point(200, 50);
            this.Editor.Name = "Editor";
            this.Editor.Size = new System.Drawing.Size(574, 568);
            this.Editor.TabIndex = 0;
            
            // GraphView
            this.GraphView.Dock = System.Windows.Forms.DockStyle.Left;
            this.GraphView.Location = new System.Drawing.Point(0, 50);
            this.GraphView.Name = "GraphView";
            this.GraphView.Size = new System.Drawing.Size(200, 568);
            this.GraphView.TabIndex = 1;

            // Terminal
            this.Terminal.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.Terminal.Location = new System.Drawing.Point(0, 618);
            this.Terminal.Name = "Terminal";
            this.Terminal.Size = new System.Drawing.Size(1024, 150);
            this.Terminal.TabIndex = 2;

            // Debugger
            this.Debugger.Dock = System.Windows.Forms.DockStyle.Right;
            this.Debugger.Location = new System.Drawing.Point(774, 50);
            this.Debugger.Name = "Debugger";
            this.Debugger.Size = new System.Drawing.Size(250, 568);
            this.Debugger.TabIndex = 3;

            // AIHistory
            this.AIHistory.Dock = System.Windows.Forms.DockStyle.Top;
            this.AIHistory.Location = new System.Drawing.Point(0, 0);
            this.AIHistory.Name = "AIHistory";
            this.AIHistory.Size = new System.Drawing.Size(1024, 50);
            this.AIHistory.TabIndex = 4;

            // MainWindow
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1024, 768);
            this.Controls.Add(this.Editor);
            this.Controls.Add(this.Debugger);
            this.Controls.Add(this.GraphView);
            this.Controls.Add(this.AIHistory);
            this.Controls.Add(this.Terminal);
            this.Name = "MainWindow";
            this.Text = "Li'nage Architect (Net48)";
            this.ResumeLayout(false);
        }

        public EditorView Editor;
        public GitGraphView GraphView;
        public TerminalView Terminal;
        public DebugView Debugger;
        public AIHistoryView AIHistory;
    }
}
