using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading.Tasks;
using Linage.GUI.Theme;
using Linage.GUI.Controls;

namespace Linage.GUI
{
    /// <summary>
    /// File Explorer view showing repository files in a tree structure
    /// </summary>
    public class FileExplorerView : UserControl, IThemable
    {
        private TreeView _treeView;
        private MaterialTextBox _searchBox;
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

        public void ApplyTheme()
        {
            this.BackColor = ModernTheme.SurfaceColor;
            
            // Header and Toolbar
            if (this.Controls.Count > 1 && this.Controls[1] is Panel header)
            {
                header.BackColor = ModernTheme.SurfaceColor;
                foreach(Control c in header.Controls)
                {
                    if (c is Button b)
                    {
                        b.ForeColor = ModernTheme.TextSecondary;
                        b.BackColor = Color.Transparent;
                    }
                }
            }

            // TreeView
            if (_treeView != null)
            {
                _treeView.BackColor = ModernTheme.SurfaceColor;
                _treeView.ForeColor = ModernTheme.TextPrimary;
                _treeView.LineColor = ModernTheme.BorderColor;
            }
            
            // SearchBox
            if (_searchBox != null)
            {
                _searchBox.BackColor = ModernTheme.SurfaceColor;
                if (_searchBox.InnerTextBox != null)
                {
                    _searchBox.InnerTextBox.BackColor = ModernTheme.SurfaceColor;
                    _searchBox.InnerTextBox.ForeColor = ModernTheme.TextPrimary;
                }
            }
            
            // Context Menu
            if (_contextMenu != null)
            {
                _contextMenu.Renderer = new ToolStripProfessionalRenderer(new ModernMenuRenderer());
            }
        }

        private void SetupContextMenu()
        {
            _contextMenu = new ContextMenuStrip();
            _contextMenu.Renderer = new ToolStripProfessionalRenderer(new ModernMenuRenderer());
            
            var menuOpen = new ToolStripMenuItem("Open", null, (s, e) => OpenSelectedNode());
            var menuReveal = new ToolStripMenuItem("Reveal in Explorer", null, OnRevealInExplorerClick);
            var menuCopyPath = new ToolStripMenuItem("Copy Path", null, OnCopyPathClick);
            var menuNewFile = new ToolStripMenuItem("New File", null, OnNewFileClick);
            var menuNewFolder = new ToolStripMenuItem("New Folder", null, OnNewFolderClick);
            var menuRename = new ToolStripMenuItem("Rename", null, OnRenameClick);
            var menuDelete = new ToolStripMenuItem("Delete", null, OnDeleteClick);
            
            _contextMenu.Items.AddRange(new ToolStripItem[] { 
                menuOpen,
                new ToolStripSeparator(),
                menuNewFile, 
                menuNewFolder, 
                new ToolStripSeparator(),
                menuReveal,
                menuCopyPath,
                new ToolStripSeparator(),
                menuRename,
                menuDelete 
            });

            _treeView.ContextMenuStrip = _contextMenu;
        }

        private List<Control> _toolbarButtons = new List<Control>();

        private void InitializeComponent()
        {
            this.BackColor = ModernTheme.SurfaceColor;
            this.Padding = new Padding(0); // Full bleed

            // Header Container
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(5),
                BackColor = ModernTheme.SurfaceColor
            };

            // Toolbar Buttons
            // Icons: Collapse (\uE738), Refresh (\uE72C), New Folder (\uE8F4), New File (\uE710)
            var btnCollapse = CreateToolbarButton("\uE738", "Collapse All", (s, e) => _treeView.CollapseAll());
            var btnRefresh = CreateToolbarButton("\uE72C", "Refresh", (s, e) => Refresh());
            var btnNewFolder = CreateToolbarButton("\uE8F4", "New Folder", OnNewFolderClick);
            var btnNewFile = CreateToolbarButton("\uE710", "New File", OnNewFileClick);

            // Search Box
            _searchBox = new MaterialTextBox
            {
                Dock = DockStyle.Fill,
                Height = 30
            };
            
            if (_searchBox.Controls.Count > 0 && _searchBox.Controls[0] is TextBox tb)
            {
                tb.TextChanged += OnSearchTextChanged;
                tb.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) tb.Text = ""; };
                
                // Toggle toolbar visibility on focus
                tb.Enter += (s, e) => ToggleToolbar(false);
                tb.Leave += (s, e) => ToggleToolbar(true);
            }

            // Layout: Search | NewFile | NewFolder | Refresh | Collapse
            // We use DockStyle.Right for buttons (added in reverse order)
            header.Controls.Add(_searchBox);
            header.Controls.Add(btnNewFile);
            header.Controls.Add(btnNewFolder);
            header.Controls.Add(btnRefresh);
            header.Controls.Add(btnCollapse);

            // Tree View
            _treeView = new ModernTreeView
            {
                Dock = DockStyle.Fill
            };
            
            // Single click handling for Open/Expand
            _treeView.NodeMouseClick += OnNodeMouseClick;

            this.Controls.Add(_treeView);
            this.Controls.Add(header);
        }

        private void ToggleToolbar(bool visible)
        {
            foreach (var btn in _toolbarButtons)
            {
                btn.Visible = visible;
            }
        }

        private Button CreateToolbarButton(string iconHex, string tooltip, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = iconHex,
                Dock = DockStyle.Right,
                Width = 30,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0, MouseOverBackColor = ModernTheme.SurfaceLight },
                ForeColor = ModernTheme.TextSecondary,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe MDL2 Assets", 10f), // Use Icon Font
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.Click += onClick;
            new ToolTip().SetToolTip(btn, tooltip);
            _toolbarButtons.Add(btn);
            return btn;
        }

        private void OnNodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            // Handle Right Click for Selection
            if (e.Button == MouseButtons.Right)
            {
                _treeView.SelectedNode = e.Node;
                return;
            }

            // Handle Left Click
            if (e.Button == MouseButtons.Left)
            {
                var node = e.Node;
                bool isFolder = node.ImageIndex == 0; // Using ImageIndex convention: 0=Folder, 1=File

                if (isFolder)
                {
                    // Toggle Expand/Collapse on single click for folder
                    if (node.IsExpanded) node.Collapse();
                    else node.Expand();
                }
                else
                {
                    // Open File on single click
                    OpenSelectedNode();
                }
            }
        }

        private bool _isOpening = false;

        private async void OpenSelectedNode()
        {
            if (_isOpening) return;

            var node = _treeView.SelectedNode;
            if (node?.Tag is string path && File.Exists(path))
            {
                try
                {
                    _isOpening = true;
                    // Invoke event - subscriber handles async loading
                    FileSelected?.Invoke(this, new FileSelectedEventArgs { FilePath = path });
                    
                    // Simple debounce to prevent double-click / rapid-fire issues
                    await Task.Delay(500);
                }
                finally
                {
                    _isOpening = false;
                }
            }
        }

        private void OnRevealInExplorerClick(object sender, EventArgs e)
        {
            var path = _treeView.SelectedNode?.Tag as string;
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    if (File.Exists(path) || Directory.Exists(path))
                        Process.Start("explorer.exe", "/select,\"" + path + "\"");
                    else if (Directory.Exists(Path.GetDirectoryName(path)))
                        Process.Start("explorer.exe", Path.GetDirectoryName(path));
                }
                catch (Exception ex) { Linage.Infrastructure.Services.NotificationManager.Instance.ShowError("Error", ex.Message); }
            }
        }

        private void OnCopyPathClick(object sender, EventArgs e)
        {
            var path = _treeView.SelectedNode?.Tag as string;
            if (!string.IsNullOrEmpty(path)) Clipboard.SetText(path);
        }

        // --- Event Handlers for File Operations ---
        private void OnNewFileClick(object sender, EventArgs e) => CreateFileOrFolder(false);
        private void OnNewFolderClick(object sender, EventArgs e) => CreateFileOrFolder(true);

        private void CreateFileOrFolder(bool isFolder)
        {
            var selectedNode = _treeView.SelectedNode;
            string targetDir = (selectedNode?.Tag as string) ?? _rootPath;
            
            // If selected is file, use its parent dir
            if (targetDir != null && File.Exists(targetDir)) targetDir = Path.GetDirectoryName(targetDir);
            
            // If nothing selected, use root
            if (string.IsNullOrEmpty(targetDir)) targetDir = _rootPath;
            if (string.IsNullOrEmpty(targetDir)) return;

            string name = Microsoft.VisualBasic.Interaction.InputBox($"Enter {(isFolder ? "folder" : "file")} name:", "New", "");
            if (string.IsNullOrWhiteSpace(name)) return;

            string fullPath = Path.Combine(targetDir, name);
            try {
                if (isFolder) Directory.CreateDirectory(fullPath);
                else File.WriteAllText(fullPath, "");
                Refresh();
                
                // Try to find and select the new node
                // (Simplistic approach: Refresh reloads all, so we'd need to find it again. skipping for now)
                
                if (!isFolder) FileCreated?.Invoke(this, fullPath);
            } catch (Exception ex) { Linage.Infrastructure.Services.NotificationManager.Instance.ShowError("Error", ex.Message); }
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
            } catch (Exception ex) { Linage.Infrastructure.Services.NotificationManager.Instance.ShowError("Error", ex.Message); }
        }

        private void OnDeleteClick(object sender, EventArgs e)
        {
            var node = _treeView.SelectedNode;
            if (node?.Tag == null) return;
            string path = node.Tag.ToString();
            Linage.Infrastructure.Services.NotificationManager.Instance.ShowConfirmation("Confirm", $"Delete {Path.GetFileName(path)}?", () => 
            {
                try {
                    if (File.Exists(path)) File.Delete(path);
                    else Directory.Delete(path, true);
                    Refresh();
                    FileDeleted?.Invoke(this, new PathChangedEventArgs { OldPath = path });
                } catch (Exception ex) { Linage.Infrastructure.Services.NotificationManager.Instance.ShowError("Error", ex.Message); }
            });
        }

        public void LoadRepository(string rootPath)
        {
            if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath)) return;
            _rootPath = rootPath;
            LoadTree(null); 
        }

        private void LoadTree(string filter)
        {
            _treeView.BeginUpdate();
            _treeView.Nodes.Clear();
            if (string.IsNullOrEmpty(_rootPath)) { _treeView.EndUpdate(); return; }

            try
            {
                var rootNode = new TreeNode(Path.GetFileName(_rootPath)) { Tag = _rootPath, ImageIndex = 0 };
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
                    bool childMatch = LoadDirectory(dirNode, dir, filter); 
                    
                    bool nameMatch = string.IsNullOrEmpty(filter) || dirName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

                    if (nameMatch || childMatch)
                    {
                        parentNode.Nodes.Add(dirNode);
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
