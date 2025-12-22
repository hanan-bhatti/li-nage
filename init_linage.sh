#!/bin/bash

# ==========================================
# Li'nage Project Initializer (.NET 4.8 WinForms Designer Edition)
# Generates a .NET Framework 4.8 SDK-style project
# Optimized for JetBrains Rider / VS WinForms Designer
# ==========================================

NAMESPACE="Linage"

echo "Initializing .NET 4.8 WinForms Project..."

# 1. Create Directory Structure
mkdir -p "GUI"
mkdir -p "Controllers"
mkdir -p "Core"
mkdir -p "Infrastructure"

# 2. Create Project File (.csproj) - Targeted for .NET 4.8
# We use the SDK style project format which supports .NET Framework 4.8
cat <<EOF > "${NAMESPACE}.csproj"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net48</TargetFramework>
    <UseWindowsForms>true</UseWindowsForms>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <LangVersion>8.0</LangVersion>
  </PropertyGroup>
</Project>
EOF

# 3. Create Entry Point (Program.cs) - Legacy WinForms Startup
cat <<EOF > "Program.cs"
using System;
using System.Windows.Forms;
using ${NAMESPACE}.GUI;

namespace ${NAMESPACE}
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainWindow());
        }
    }
}
EOF

# ==========================================
# PACKAGE: INFRASTRUCTURE
# ==========================================

cat <<EOF > "Infrastructure/FileMetadata.cs"
using System;

namespace ${NAMESPACE}.Infrastructure
{
    public class FileMetadata
    {
        public string Path { get; set; } = string.Empty;
        public string Inode { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime ModifiedTime { get; set; }
        public string Hash { get; set; } = string.Empty;
    }
}
EOF

cat <<EOF > "Infrastructure/FileChangeEvent.cs"
using System;

namespace ${NAMESPACE}.Infrastructure
{
    public class FileChangeEvent : EventArgs
    {
        public string Path { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
EOF

cat <<EOF > "Infrastructure/FileWatcher.cs"
using System;

namespace ${NAMESPACE}.Infrastructure
{
    public class FileWatcher
    {
        public string WatchPath { get; set; } = string.Empty;
        public bool Recursive { get; set; }

        public event EventHandler<FileChangeEvent> OnFileChanged;

        public void TriggerChange(FileChangeEvent e)
        {
            OnFileChanged?.Invoke(this, e);
        }
    }
}
EOF

cat <<EOF > "Infrastructure/FileService.cs"
using System;

namespace ${NAMESPACE}.Infrastructure
{
    public class FileService
    {
        public int BufferSize { get; set; }

        public FileMetadata GetMetadata(string path) 
        { 
            return new FileMetadata { Path = path }; 
        }
    }
}
EOF

cat <<EOF > "Infrastructure/MetadataStore.cs"
using System;
using System.Collections.Generic;

namespace ${NAMESPACE}.Infrastructure
{
    public class MetadataStore
    {
        public string StorageType { get; set; } = "SQLite";
        public List<FileMetadata> StoredMetadata { get; set; } = new List<FileMetadata>();
    }
}
EOF

# ==========================================
# PACKAGE: CORE
# ==========================================

cat <<EOF > "Core/LineChange.cs"
namespace ${NAMESPACE}.Core
{
    public class LineChange
    {
        public int LineNumber { get; set; }
        public string OldHash { get; set; } = string.Empty;
        public string NewHash { get; set; } = string.Empty;
    }
}
EOF

cat <<EOF > "Core/Snapshot.cs"
using System;
using System.Collections.Generic;
using ${NAMESPACE}.Infrastructure;

namespace ${NAMESPACE}.Core
{
    public class Snapshot
    {
        public DateTime Timestamp { get; set; }
        public string Hash { get; set; } = string.Empty;
        public List<FileMetadata> Files { get; set; } = new List<FileMetadata>();
    }
}
EOF

cat <<EOF > "Core/Commit.cs"
using System;
using System.Collections.Generic;

namespace ${NAMESPACE}.Core
{
    public class Commit
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        
        public Snapshot State { get; set; }
        public List<Commit> Parents { get; set; } = new List<Commit>();
    }
}
EOF

cat <<EOF > "Core/Branch.cs"
namespace ${NAMESPACE}.Core
{
    public class Branch
    {
        public string Name { get; set; } = string.Empty;
        public Commit Head { get; set; }
    }
}
EOF

cat <<EOF > "Core/ErrorTrace.cs"
using System;

namespace ${NAMESPACE}.Core
{
    public class ErrorTrace
    {
        public string Message { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public DateTime Timestamp { get; set; }

        public LineChange LinkedChange { get; set; }
        public Commit LinkedCommit { get; set; }
    }
}
EOF

cat <<EOF > "Core/ExternalResource.cs"
using System;

namespace ${NAMESPACE}.Core
{
    public class ExternalResource
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string[] Tags { get; set; } = new string[0];
    }
}
EOF

cat <<EOF > "Core/SolutionIndex.cs"
using System.Collections.Generic;

namespace ${NAMESPACE}.Core
{
    public class SolutionIndex
    {
        public string ErrorSignature { get; set; } = string.Empty;
        public List<ExternalResource> Resources { get; set; } = new List<ExternalResource>();
    }
}
EOF

cat <<EOF > "Core/AITool.cs"
namespace ${NAMESPACE}.Core
{
    public class AITool
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
    }
}
EOF

cat <<EOF > "Core/AIAccessLog.cs"
using System;

namespace ${NAMESPACE}.Core
{
    public class AIAccessLog
    {
        public string AccessType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        
        public AITool Tool { get; set; }
        public LineChange ContextChange { get; set; }
    }
}
EOF

cat <<EOF > "Core/FileScanner.cs"
using ${NAMESPACE}.Infrastructure;

namespace ${NAMESPACE}.Core
{
    public class FileScanner
    {
        public string RootPath { get; set; } = string.Empty;
        public int ScanDepth { get; set; }

        public FileMetadata CreateMetadata(string path) 
        { 
            return new FileMetadata { Path = path }; 
        }
    }
}
EOF

cat <<EOF > "Core/ChangeDetector.cs"
using ${NAMESPACE}.Infrastructure;

namespace ${NAMESPACE}.Core
{
    public class ChangeDetector
    {
        public string Mode { get; set; } = "RealTime";
        public string LastScanHash { get; set; } = string.Empty;
        public RecoveryManager Recovery { get; set; }

        public void ReceiveEvent(FileChangeEvent evt) 
        {
            // Logic
        }
    }
}
EOF

cat <<EOF > "Core/RecoveryManager.cs"
namespace ${NAMESPACE}.Core
{
    public class RecoveryManager
    {
        public int RetryCount { get; set; }
    }
}
EOF

cat <<EOF > "Core/HashService.cs"
namespace ${NAMESPACE}.Core
{
    public class HashService
    {
        public string Algorithm { get; set; } = "SHA256";
    }
}
EOF

cat <<EOF > "Core/VersionGraphService.cs"
using System.Collections.Generic;

namespace ${NAMESPACE}.Core
{
    public class VersionGraphService
    {
        public int GraphSize { get; set; }
        public List<Commit> Commits { get; set; } = new List<Commit>();
        public List<Branch> Branches { get; set; } = new List<Branch>();
    }
}
EOF

cat <<EOF > "Core/LineTracker.cs"
namespace ${NAMESPACE}.Core
{
    public class LineTracker
    {
        public string Strategy { get; set; } = "DiffMatchPatch";

        public LineChange ProduceChange() 
        { 
            return new LineChange(); 
        }
    }
}
EOF

cat <<EOF > "Core/AutoCompletionService.cs"
using ${NAMESPACE}.Infrastructure;

namespace ${NAMESPACE}.Core
{
    public class AutoCompletionService
    {
        public string EngineType { get; set; } = "Transformer";
        
        public FileMetadata ContextFile { get; set; }
        public Snapshot ContextSnapshot { get; set; }
    }
}
EOF

# ==========================================
# PACKAGE: CONTROLLERS
# ==========================================

cat <<EOF > "Controllers/ScanController.cs"
using System;
using ${NAMESPACE}.Core;

namespace ${NAMESPACE}.Controllers
{
    public class ScanController
    {
        public string Status { get; set; } = "Idle";
        public DateTime LastRunTime { get; set; }

        public FileScanner Scanner { get; set; } = new FileScanner();
        public ChangeDetector Detector { get; set; } = new ChangeDetector();
    }
}
EOF

cat <<EOF > "Controllers/IndexController.cs"
using System;
using ${NAMESPACE}.Core;
using ${NAMESPACE}.Infrastructure;

namespace ${NAMESPACE}.Controllers
{
    public class IndexController
    {
        public string Status { get; set; } = "Idle";
        public DateTime LastRunTime { get; set; }

        public MetadataStore Store { get; set; } = new MetadataStore();
        public HashService Hasher { get; set; } = new HashService();
    }
}
EOF

cat <<EOF > "Controllers/VersionController.cs"
using System;
using ${NAMESPACE}.Core;

namespace ${NAMESPACE}.Controllers
{
    public class VersionController
    {
        public string Status { get; set; } = "Idle";
        public DateTime LastRunTime { get; set; }

        public VersionGraphService GraphService { get; set; } = new VersionGraphService();
    }
}
EOF

cat <<EOF > "Controllers/DebugController.cs"
using System;
using ${NAMESPACE}.Core;

namespace ${NAMESPACE}.Controllers
{
    public class DebugController
    {
        public string Status { get; set; } = "Idle";
        public DateTime LastRunTime { get; set; }

        public LineTracker Tracker { get; set; } = new LineTracker();
        public ErrorTrace Trace { get; set; }
        public SolutionIndex Solutions { get; set; }
    }
}
EOF

cat <<EOF > "Controllers/AIActivityController.cs"
using System;
using ${NAMESPACE}.Core;

namespace ${NAMESPACE}.Controllers
{
    public class AIActivityController
    {
        public string Status { get; set; } = "Idle";
        public DateTime LastRunTime { get; set; }

        public AIAccessLog LogService { get; set; }
    }
}
EOF

# ==========================================
# PACKAGE: GUI (Views)
# ==========================================

cat <<EOF > "GUI/CommandDispatcher.cs"
namespace ${NAMESPACE}.GUI
{
    public class CommandDispatcher
    {
        public string Id { get; set; } = string.Empty;
        public string State { get; set; } = "Ready";
    }
}
EOF

cat <<EOF > "GUI/EditorView.cs"
using System.Windows.Forms;
using ${NAMESPACE}.Core;

namespace ${NAMESPACE}.GUI
{
    public class EditorView : UserControl
    {
        public string Id { get; set; } = string.Empty;
        public string State { get; set; } = "Visible";
        
        public AutoCompletionService AutoCompleter { get; set; } = new AutoCompletionService();

        public EditorView()
        {
            this.BorderStyle = BorderStyle.FixedSingle;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
        }
    }
}
EOF

cat <<EOF > "GUI/GitGraphView.cs"
using System.Collections.Generic;
using System.Windows.Forms;
using ${NAMESPACE}.Core;

namespace ${NAMESPACE}.GUI
{
    public class GitGraphView : UserControl
    {
        public string Id { get; set; } = string.Empty;
        public string State { get; set; } = "Visible";
        
        public List<Commit> Commits { get; set; } = new List<Commit>();
        public List<Branch> Branches { get; set; } = new List<Branch>();

        public GitGraphView()
        {
            this.BackColor = System.Drawing.Color.LightGray;
        }
    }
}
EOF

cat <<EOF > "GUI/TerminalView.cs"
using System.Windows.Forms;

namespace ${NAMESPACE}.GUI
{
    public class TerminalView : UserControl
    {
        public string Id { get; set; } = string.Empty;
        public string State { get; set; } = "Visible";
        
        public CommandDispatcher Dispatcher { get; set; } = new CommandDispatcher();

        public TerminalView()
        {
            this.BackColor = System.Drawing.Color.Black;
            this.ForeColor = System.Drawing.Color.White;
        }
    }
}
EOF

cat <<EOF > "GUI/DebugView.cs"
using System.Collections.Generic;
using System.Windows.Forms;
using ${NAMESPACE}.Core;

namespace ${NAMESPACE}.GUI
{
    public class DebugView : UserControl
    {
        public string Id { get; set; } = string.Empty;
        public string State { get; set; } = "Visible";
        
        public ErrorTrace CurrentTrace { get; set; }
        public List<ExternalResource> Resources { get; set; } = new List<ExternalResource>();

        public DebugView()
        {
            this.BorderStyle = BorderStyle.Fixed3D;
        }
    }
}
EOF

cat <<EOF > "GUI/AIHistoryView.cs"
using System.Collections.Generic;
using System.Windows.Forms;
using ${NAMESPACE}.Core;

namespace ${NAMESPACE}.GUI
{
    public class AIHistoryView : UserControl
    {
        public string Id { get; set; } = string.Empty;
        public string State { get; set; } = "Visible";
        
        public List<AIAccessLog> Logs { get; set; } = new List<AIAccessLog>();
        
        public AIHistoryView()
        {
            this.BackColor = System.Drawing.Color.AliceBlue;
        }
    }
}
EOF

# ==========================================
# PACKAGE: GUI (MainWindow - Split for Designer)
# ==========================================

# 1. MainWindow.Designer.cs (Auto-generated code style)
cat <<EOF > "GUI/MainWindow.Designer.cs"
namespace ${NAMESPACE}.GUI
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
            this.Editor = new ${NAMESPACE}.GUI.EditorView();
            this.GraphView = new ${NAMESPACE}.GUI.GitGraphView();
            this.Terminal = new ${NAMESPACE}.GUI.TerminalView();
            this.Debugger = new ${NAMESPACE}.GUI.DebugView();
            this.AIHistory = new ${NAMESPACE}.GUI.AIHistoryView();
            this.SuspendLayout();
            
            // 
            // Editor
            // 
            this.Editor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Editor.Location = new System.Drawing.Point(200, 50);
            this.Editor.Name = "Editor";
            this.Editor.Size = new System.Drawing.Size(574, 568);
            this.Editor.TabIndex = 0;
            
            // 
            // GraphView
            // 
            this.GraphView.Dock = System.Windows.Forms.DockStyle.Left;
            this.GraphView.Location = new System.Drawing.Point(0, 50);
            this.GraphView.Name = "GraphView";
            this.GraphView.Size = new System.Drawing.Size(200, 568);
            this.GraphView.TabIndex = 1;

            // 
            // Terminal
            // 
            this.Terminal.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.Terminal.Location = new System.Drawing.Point(0, 618);
            this.Terminal.Name = "Terminal";
            this.Terminal.Size = new System.Drawing.Size(1024, 150);
            this.Terminal.TabIndex = 2;

            // 
            // Debugger
            // 
            this.Debugger.Dock = System.Windows.Forms.DockStyle.Right;
            this.Debugger.Location = new System.Drawing.Point(774, 50);
            this.Debugger.Name = "Debugger";
            this.Debugger.Size = new System.Drawing.Size(250, 568);
            this.Debugger.TabIndex = 3;

            // 
            // AIHistory
            // 
            this.AIHistory.Dock = System.Windows.Forms.DockStyle.Top;
            this.AIHistory.Location = new System.Drawing.Point(0, 0);
            this.AIHistory.Name = "AIHistory";
            this.AIHistory.Size = new System.Drawing.Size(1024, 50);
            this.AIHistory.TabIndex = 4;

            // 
            // MainWindow
            // 
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
EOF

# 2. MainWindow.cs (Logic)
cat <<EOF > "GUI/MainWindow.cs"
using System.Windows.Forms;

namespace ${NAMESPACE}.GUI
{
    public partial class MainWindow : Form
    {
        public string Id { get; set; } = "MAIN_001";
        public string State { get; set; } = "Normal";

        public MainWindow()
        {
            InitializeComponent();
        }
    }
}
EOF

echo "WinForms (.NET 4.8) Project initialization complete."
echo "Open ${NAMESPACE}.csproj in Rider or Visual Studio."