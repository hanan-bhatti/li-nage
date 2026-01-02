using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Linage.GUI.Theme;
using Linage.GUI.Controls;

namespace Linage.GUI
{
    /// <summary>
    /// File Explorer view showing repository files in a tree structure
    /// </summary>
    public class FileExplorerView : UserControl
    {
        private ModernTreeView _treeView;
        private MaterialTextBox _searchBox;
        private Label _lblPath;
        private string _rootPath;
        private ContextMenuStrip _contextMenu;

        public event EventHandler<FileSelectedEventArgs> FileSelected;
        public event EventHandler<string> FileCreated;
        public event EventHandler<PathChangedEventArgs> FileDeleted;
        public event EventHandler<PathChangedEventArgs> FileRenamed;

        public FileExplorerView()
        {
            InitializeComponent();
            SetupContextMenu();
        }

        private void SetupContextMenu()
        {
            _contextMenu = new ContextMenuStrip();
            _contextMenu.Renderer = new ToolStripProfessionalRenderer(new ModernMenuRenderer());
            
            var menuNewFile = new ToolStripMenuItem("New File", null, OnNewFileClick);
            var menuNewFolder = new ToolStripMenuItem("New Folder", null, OnNewFolderClick);
            var menuRename = new ToolStripMenuItem("Rename", null, OnRenameClick);
            var menuDelete = new ToolStripMenuItem("Delete", null, OnDeleteClick);
            
            _contextMenu.Items.AddRange(new ToolStripItem[] { 
                menuNewFile, 
                menuNewFolder, 
                new ToolStripSeparator(),
                menuRename,
                menuDelete 
            });

            _treeView.ContextMenuStrip = _contextMenu;
            _treeView.NodeMouseClick += (s, e) => {
                if (e.Button == MouseButtons.Right) _treeView.SelectedNode = e.Node;
            };
        }

        // --- Event Handlers for File Operations ---
        private void OnNewFileClick(object sender, EventArgs e)
        {
            // Implementation same as before, omitted for brevity but preserved in real code
            // Actually I must include it if I am replacing the whole file or class, 
            // but here I am replacing specific parts? 
            // The prompt asks to "Replace the InitializeComponent method and the search logic".
            // I should be careful. The user instruction implies modifying existing methods.
            // I will use a large replace block to cover the changes.
            CreateFileOrFolder(false);
        }

        private void OnNewFolderClick(object sender, EventArgs e) => CreateFileOrFolder(true);

        private void CreateFileOrFolder(bool isFolder)
        {
            var selectedNode = _treeView.SelectedNode;
            string targetDir = (selectedNode?.Tag as string) ?? _rootPath;
            if (targetDir != null && File.Exists(targetDir)) targetDir = Path.GetDirectoryName(targetDir);
            if (string.IsNullOrEmpty(targetDir)) return;

            string name = Microsoft.VisualBasic.Interaction.InputBox($"Enter {(isFolder ? "folder" : "file")} name:", "New", "");
            if (string.IsNullOrWhiteSpace(name)) return;

            string fullPath = Path.Combine(targetDir, name);
            try {
                if (isFolder) Directory.CreateDirectory(fullPath);
                else File.WriteAllText(fullPath, "");
                Refresh();
                if (!isFolder) FileCreated?.Invoke(this, fullPath);
            } catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void OnRenameClick(object sender, EventArgs e)
        {
            var node = _treeView.SelectedNode;
            if (node?.Tag == null) return;
            string oldPath = node.Tag.ToString();
            string newName = Microsoft.VisualBasic.Interaction.InputBox("New name:", "Rename", Path.GetFileName(oldPath));
            if (string.IsNullOrWhiteSpace(newName)) return;
            
            string newPath = Path.Combine(Path.GetDirectoryName(oldPath), newName);
            try {
                if (File.Exists(oldPath)) File.Move(oldPath, newPath);
                else Directory.Move(oldPath, newPath);
                Refresh();
                FileRenamed?.Invoke(this, new PathChangedEventArgs { OldPath = oldPath, NewPath = newPath });
            } catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void OnDeleteClick(object sender, EventArgs e)
        {
            var node = _treeView.SelectedNode;
            if (node?.Tag == null) return;
            string path = node.Tag.ToString();
            if (MessageBox.Show($"Delete {Path.GetFileName(path)}?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try {
                    if (File.Exists(path)) File.Delete(path);
                    else Directory.Delete(path, true);
                    Refresh();
                    FileDeleted?.Invoke(this, new PathChangedEventArgs { OldPath = path });
                } catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
        }

        private void InitializeComponent()
        {
            this.BackColor = ModernTheme.SurfaceColor;
            this.Padding = new Padding(0); // Full bleed

            // Header Container (Compact & Clean)
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(10, 5, 10, 5),
                BackColor = ModernTheme.SurfaceColor
            };

            // Search Box (Material Style)
            _searchBox = new MaterialTextBox
            {
                Dock = DockStyle.Fill, // Fill the header
                Height = 30 
            };
            
            // Set Placeholder text logic via direct TextBox access if possible, or just tooltip
            // We'll rely on user knowing it's a search box
            
            // Hook up search event
            // Note: MaterialTextBox.InnerTextBox helper property is needed if I didn't add it in ModernControls yet?
            // I checked ModernControls, I didn't add InnerTextBox property publicly in the LAST replace (I only replaced TreeView).
            // But checking the file content from earlier read, MaterialTextBox has `_textBox` private.
            // Wait, looking at `GUI\Controls\ModernControls.cs` read output:
            // public override string Text { get => _textBox.Text; set => _textBox.Text = value; }
            // So I can just bind to TextChanged of the MaterialTextBox if it exposed it, but it inherits Panel.
            // I need to access controls[0].
            
            if (_searchBox.Controls.Count > 0 && _searchBox.Controls[0] is TextBox tb)
            {
                tb.TextChanged += OnSearchTextChanged;
                tb.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) tb.Text = ""; };
                // tb.PlaceholderText = "Search..."; // Not available in .NET 4.8
            }

            // Collapse Button (Optional, simple label for now)
            var btnCollapse = new Label
            {
                Text = "-",
                AutoSize = true,
                Dock = DockStyle.Right,
                ForeColor = ModernTheme.TextSecondary,
                Padding = new Padding(5),
                Cursor = Cursors.Hand
            };
            btnCollapse.Click += (s, e) => _treeView.CollapseAll();

            header.Controls.Add(_searchBox);
            // header.Controls.Add(btnCollapse); // Maybe later

            // Tree View
            _treeView = new ModernTreeView
            {
                Dock = DockStyle.Fill
            };
            _treeView.NodeMouseDoubleClick += OnNodeDoubleClick;

            this.Controls.Add(_treeView);
            this.Controls.Add(header);
        }

        public void LoadRepository(string rootPath)
        {
            if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath)) return;
            _rootPath = rootPath;
            // _lblPath.Text = Path.GetFileName(rootPath).ToUpper();
            LoadTree(null); // Load all
        }

        private void LoadTree(string filter)
        {
            _treeView.BeginUpdate();
            _treeView.Nodes.Clear();
            if (string.IsNullOrEmpty(_rootPath)) { _treeView.EndUpdate(); return; }

            try
            {
                var rootNode = new TreeNode(Path.GetFileName(_rootPath)) { Tag = _rootPath, ImageIndex = 0 };
                // If filter is active, we might skip the root node if it doesn't match? 
                // No, we usually show root and filter children.
                
                bool hasMatches = LoadDirectory(rootNode, _rootPath, filter);
                
                if (string.IsNullOrEmpty(filter) || hasMatches)
                {
                    _treeView.Nodes.Add(rootNode);
                    rootNode.Expand();
                }
            }
            catch {}
            _treeView.EndUpdate();
        }

        private bool LoadDirectory(TreeNode parentNode, string path, string filter)
        {
            bool anyMatch = false;
            try
            {
                // Dirs
                foreach (var dir in Directory.GetDirectories(path))
                {
                    var dirName = Path.GetFileName(dir);
                    if (ShouldIgnore(dirName)) continue;

                    var dirNode = new TreeNode(dirName) { Tag = dir, ImageIndex = 0 };
                    bool childMatch = LoadDirectory(dirNode, dir, filter); // Recursion
                    
                    bool nameMatch = string.IsNullOrEmpty(filter) || dirName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

                    if (nameMatch || childMatch)
                    {
                        parentNode.Nodes.Add(dirNode);
                        // Only expand if the user is searching (filtering)
                        if (!string.IsNullOrEmpty(filter) && childMatch) dirNode.Expand(); 
                        anyMatch = true;
                    }
                }

                // Files
                foreach (var file in Directory.GetFiles(path))
                {
                    var fileName = Path.GetFileName(file);
                    bool nameMatch = string.IsNullOrEmpty(filter) || fileName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
                    
                    if (nameMatch)
                    {
                        var fileNode = new TreeNode(fileName) { Tag = file, ImageIndex = 1 };
                        parentNode.Nodes.Add(fileNode);
                        anyMatch = true;
                    }
                }
            }
            catch {}
            return anyMatch;
        }

        private bool ShouldIgnore(string name)
        {
            var ignoredDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".git", ".linage", "bin", "obj", "node_modules", ".vs", 
                "packages", "Debug", "Release", ".vscode"
            };
            return ignoredDirs.Contains(name);
        }

        private void OnNodeDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node?.Tag is string path && File.Exists(path))
                FileSelected?.Invoke(this, new FileSelectedEventArgs { FilePath = path });
        }

        private void OnSearchTextChanged(object sender, EventArgs e)
        {
            var box = sender as TextBox;
            LoadTree(box.Text.Trim());
        }

        public new void Refresh() => LoadRepository(_rootPath);
        private int GetFileIcon(string f) => 1;

        // Helper for Context Menu Styling
        private class ModernMenuRenderer : ProfessionalColorTable
        {
            public override Color MenuItemSelected => ModernTheme.SurfaceLight;
            public override Color MenuBorder => ModernTheme.BorderColor;
            public override Color ToolStripDropDownBackground => ModernTheme.SurfaceColor;
            public override Color ImageMarginGradientBegin => ModernTheme.SurfaceColor;
            public override Color ImageMarginGradientEnd => ModernTheme.SurfaceColor;
        }
    }

    public class FileSelectedEventArgs : EventArgs { public string FilePath { get; set; } }
    public class PathChangedEventArgs : EventArgs { public string OldPath { get; set; } public string NewPath { get; set; } }
}
