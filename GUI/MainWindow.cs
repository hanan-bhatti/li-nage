using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Linage.Controllers;
using Linage.Core;
using Linage.Core.Services;
using Linage.GUI.Configuration;
using Linage.GUI.Controls;
using Linage.GUI.Helpers;
using Linage.GUI.Services;
using Linage.GUI.Theme;
using Linage.Infrastructure;
using Microsoft.VisualBasic;

namespace Linage.GUI
{
    public partial class MainWindow : Form
    {
        // Controllers
        private readonly VersionController _versionController;
        private readonly DebugController _debugController;
        private readonly IndexController _indexController;
        private readonly RemoteController _remoteController;
        private readonly AuthController _authController;

        // Services
        private readonly IDialogService _dialogService;
        private readonly AsyncOperationHelper _asyncHelper;
        private readonly RemoteOperationsService _remoteOperationsService;
        private readonly UILayoutConfiguration _layoutConfig;

        // UI Components
        private MenuStrip _menuStrip;
        private Panel _activityBar;
        private Panel _sideBarContainer;
        private SplitContainer _mainSplit; // Separates SideBar+ActivityBar from Editor Area
        private SplitContainer _editorSplit; // Separates Editor from Terminal (Bottom)
        private ModernTabControl _editorTabs;
        private ModernTabControl _terminalTabs;
        private ModernStatusBar _statusBar;
        private ImprovedStatusBar _improvedStatusBar;

        // Status Labels
        private Label _lblStatus;
        private Label _lblBranch;
        private Label _lblRepo;
        private Label _lblFileStats;
        private ProgressBar _progressBar;

        // Activity Bar Buttons
        private ActivityBarButton _btnExplorer;
        private ActivityBarButton _btnSourceControl;
        private ActivityBarButton _btnHistory;
        private ActivityBarButton _btnDebug;

        // Views
        private FileExplorerView _fileExplorer;
        private StagingView _stagingView;
        private GitGraphView _gitGraphView;
        private DebugView _debugView;
        private TerminalView _terminalView;

        // State
        private string _currentRepository;
        private Dictionary<string, TabPage> _openFiles = new Dictionary<string, TabPage>();
        private Dictionary<string, TabPageData> _tabEventHandlers = new Dictionary<string, TabPageData>();

        // Helper class to store event handlers for cleanup
        private class TabPageData
        {
            public EditorView Editor { get; set; }
            public EventHandler ContentHandler { get; set; }
            public EventHandler SavedHandler { get; set; }
        }

        public MainWindow()
        {
            // Initialize Logger (File-based logging)
            Logger.Initialize(Path.Combine(Application.StartupPath, "logs"));

            // Initialize Controllers
            _debugController = new DebugController();
            _indexController = new IndexController();

            try
            {
                _versionController = new VersionController();

                // Initialize Auth and Remote controllers using VersionController's services
                _authController = new AuthController(_versionController.AuthService);

                var httpTransport = new HttpTransport(_versionController.AuthService, ".");
                var sshTransport = new SshTransport(_versionController.AuthService, ".");
                _remoteController = new RemoteController(httpTransport, sshTransport, _authController);

                // Initialize Services
                _dialogService = new DialogService();
                _asyncHelper = new AsyncOperationHelper(ToggleProgress, UpdateStatus, ShowError, this);
                _remoteOperationsService = new RemoteOperationsService(_remoteController, _versionController.GraphService);
                _layoutConfig = UILayoutConfiguration.LoadFromSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to initialize: {ex.Message}\n\nCheck SQL Server connection.",
                    "Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _versionController = null;
            }

            InitializeComponent();
            SetupViews();
            ApplyTheme();

            // Removed: InitializeRefreshTimer() - Using FileWatcher events instead

            // Default to Explorer
            SwitchSideBar("Explorer");
        }

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
            
            // Initialize Containers
            _mainSplit = new SplitContainer { Dock = DockStyle.Fill, SplitterWidth = 1 };
            _editorSplit = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterWidth = 1 };
            _sideBarContainer = new Panel { Dock = DockStyle.Fill };
            
            // Initialize Tab Controls
            _editorTabs = new ModernTabControl { Dock = DockStyle.Fill };
            _terminalTabs = new ModernTabControl { Dock = DockStyle.Fill };
            
            CreateStatusBar(); // Calling this helper to init _statusBar
            CreateMenuStrip(); // Init _menuStrip
            CreateActivityBar(); // Init _activityBar and buttons
            
            this.SuspendLayout();
            
            // 
            // Layout Setup
            // 
            
            // Main Split - Use configuration values
            _mainSplit.Panel1.Controls.Add(_sideBarContainer);
            _mainSplit.Panel2.Controls.Add(_editorSplit);
            _mainSplit.SplitterDistance = _layoutConfig?.SidebarWidth ?? Spacing.Layout.SidebarWidth;

            // Editor Split - Use configuration values
            _editorSplit.Panel1.Controls.Add(_editorTabs);
            _editorSplit.Panel2.Controls.Add(_terminalTabs);
            _editorSplit.SplitterDistance = _layoutConfig?.EditorPanelHeight ?? 600;

            // Form - Use configuration values
            this.ClientSize = new System.Drawing.Size(
                _layoutConfig?.DefaultWindowWidth ?? 1200,
                _layoutConfig?.DefaultWindowHeight ?? 800);
            this.MinimumSize = new System.Drawing.Size(
                _layoutConfig?.MinimumWindowWidth ?? 800,
                _layoutConfig?.MinimumWindowHeight ?? 600);
            this.Controls.Add(_mainSplit);     // Fill
            this.Controls.Add(_activityBar);   // Left
            this.Controls.Add(_statusBar);     // Bottom
            this.Controls.Add(_menuStrip);     // Top
            this.MainMenuStrip = _menuStrip;

            // Basic Properties
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Li\'nage - Advanced Version Control System";
            this.ResumeLayout(false);
        }

        private void CreateActivityBar()
        {
            _activityBar = new FlowLayoutPanel 
            { 
                Dock = DockStyle.Left, 
                Width = 50, 
                BackColor = ModernTheme.ActivityBarColor,
                Padding = new Padding(0, 10, 0, 0)
            };

            // Helper to create buttons
            ActivityBarButton CreateButton(string icon, string name)
            {
                var btn = new ActivityBarButton 
                { 
                    IconHex = icon, 
                    Width = 50, 
                    Height = 50,
                    Tag = name 
                };
                btn.Clicked += (s, e) => SwitchSideBar(name);
                _activityBar.Controls.Add(btn);
                return btn;
            }

            // \uE838 = Explorer (Folder)
            // \uEA68 = Source Control (Git)
            // \uE81C = History (Clock/Graph)
            // \uE890 = Debug (Bug)
            
            _btnExplorer = CreateButton("\uE838", "Explorer");
            _btnSourceControl = CreateButton("\uEA68", "SourceControl");
            _btnHistory = CreateButton("\uE81C", "History");
            _btnDebug = CreateButton("\uE890", "Debug"); // "Debug" view not yet fully implemented in SwitchSideBar case? 
            
            // Set initial active
            _btnExplorer.IsActive = true;
        }

        private void CreateStatusBar()
        {
            _improvedStatusBar = new ImprovedStatusBar(_dialogService, _layoutConfig, OnBranches);

            // Assign references to the exposed controls for backward compatibility
            _statusBar = _improvedStatusBar.StatusBar;
            _lblBranch = _improvedStatusBar.BranchLabel;
            _lblRepo = _improvedStatusBar.RepositoryLabel;
            _lblStatus = _improvedStatusBar.StatusLabel;
            _lblFileStats = _improvedStatusBar.FileStatsLabel;
            _progressBar = _improvedStatusBar.ProgressBar;
        }

        private void CreateMenuStrip()
        {
            _menuStrip = new MenuStrip();

            // File Menu
            var fileMenu = new ToolStripMenuItem("&File");
            fileMenu.DropDownItems.Add("&Open Repository...", null, OnOpenRepository);
            fileMenu.DropDownItems.Add("Clone Repository...", null, async (s, e) => await OnClone());
            fileMenu.DropDownItems.Add("&Import Git Repository...", null, OnImportGitRepository);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("&Save", null, (s, e) => SaveCurrentFile());
            fileMenu.DropDownItems.Add("Save &All", null, OnSaveAll);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("E&xit", null, (s, e) => Application.Exit());

            // Edit Menu
            var editMenu = new ToolStripMenuItem("&Edit");
            editMenu.DropDownItems.Add("&Undo", null, (s, e) => { });
            editMenu.DropDownItems.Add("&Redo", null, (s, e) => { });

            // View Menu
            var viewMenu = new ToolStripMenuItem("&View");
            viewMenu.DropDownItems.Add("Toggle Side Bar", null, (s, e) => _mainSplit.Panel1Collapsed = !_mainSplit.Panel1Collapsed);
            viewMenu.DropDownItems.Add("Toggle Terminal", null, (s, e) => _editorSplit.Panel2Collapsed = !_editorSplit.Panel2Collapsed);
            viewMenu.DropDownItems.Add(new ToolStripSeparator());
            viewMenu.DropDownItems.Add("&Explorer", null, (s, e) => SwitchSideBar("Explorer"));
            viewMenu.DropDownItems.Add("&Graph", null, (s, e) => SwitchSideBar("History")); 
            viewMenu.DropDownItems.Add("&Staging", null, (s, e) => SwitchSideBar("SourceControl"));
            viewMenu.DropDownItems.Add(new ToolStripSeparator());
            viewMenu.DropDownItems.Add("Refresh &Status", null, (s, e) => {
                _versionController?.ScanChanges();
                UpdateUI();
            });

            // Remote Menu
            var remoteMenu = new ToolStripMenuItem("&Remote");
            remoteMenu.DropDownItems.Add("&Push", null, OnPush);
            remoteMenu.DropDownItems.Add("P&ull", null, OnPull);

            // Help Menu
            var helpMenu = new ToolStripMenuItem("&Help");
            helpMenu.DropDownItems.Add("&About Li'nage", null, OnAbout);

            _menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, editMenu, viewMenu, remoteMenu, helpMenu });
        }

        private void SetupViews()
        {
            // 1. File Explorer
            _fileExplorer = new FileExplorerView { Dock = DockStyle.Fill };
            _fileExplorer.FileSelected += OnFileSelected;
            _fileExplorer.FileRenamed += OnFileRenamed;
            _fileExplorer.FileDeleted += OnFileDeleted;

            // 2. Staging View (Source Control)
            _stagingView = new StagingView { Dock = DockStyle.Fill };
            _stagingView.OnCommitRequested += OnCommitRequested;

            // 3. Git Graph (History)
            _gitGraphView = new GitGraphView { Dock = DockStyle.Fill };

            // 4. Debug/Terminal (Bottom Panel)
            _terminalView = new TerminalView { Dock = DockStyle.Fill };
            _debugView = new DebugView { Dock = DockStyle.Fill };

            _terminalTabs.TabPages.Add(new TabPage("Terminal") { Controls = { _terminalView } });
            _terminalTabs.TabPages.Add(new TabPage("Debug Console") { Controls = { _debugView } });
        }

        private void SwitchSideBar(string viewName)
        {
            _sideBarContainer.Controls.Clear();
            _btnExplorer.IsActive = false;
            _btnSourceControl.IsActive = false;
            _btnHistory.IsActive = false;
            _btnDebug.IsActive = false;

            if (_mainSplit.Panel1Collapsed) _mainSplit.Panel1Collapsed = false;

            switch (viewName)
            {
                case "Explorer":
                    _sideBarContainer.Controls.Add(_fileExplorer);
                    _btnExplorer.IsActive = true;
                    break;
                case "SourceControl":
                    _sideBarContainer.Controls.Add(_stagingView);
                    _btnSourceControl.IsActive = true;
                    break;
                case "History":
                    // For history, we might want to show it in the main area, but if we have a sidebar widget:
                    // Just show a placeholder or move graph to sidebar?
                    // Let's open the Graph in the main editor area and keep sidebar as Explorer for now
                    if (!IsTabOpen("Commit Graph"))
                    {
                        var tab = new TabPage("Commit Graph") { Name = "Graph" };
                        tab.Controls.Add(_gitGraphView);
                        _editorTabs.TabPages.Insert(0, tab);
                    }
                    _editorTabs.SelectTab("Graph");
                    _btnHistory.IsActive = true; 
                    // Keep previous sidebar content or show empty
                    _sideBarContainer.Controls.Add(new Label { Text = "History is shown in the main editor area.", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = ModernTheme.TextSecondary });
                    break;
                case "Debug":
                    _btnDebug.IsActive = true;
                     _sideBarContainer.Controls.Add(new Label { Text = "Debug configurations coming soon.", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = ModernTheme.TextSecondary });
                    break;
            }
        }
        
        private bool IsTabOpen(string name)
        {
            foreach(TabPage t in _editorTabs.TabPages) if (t.Text == name) return true;
            return false;
        }


        private void ApplyTheme()
        {
            this.BackColor = ModernTheme.BackColor;
            this.ForeColor = ModernTheme.TextPrimary;

            // Menus
            _menuStrip.BackColor = ModernTheme.BackColor; // Blend with Title Bar
            _menuStrip.ForeColor = ModernTheme.TextPrimary;
            _menuStrip.Renderer = new PremiumMenuRenderer();

            // Splitters
            _mainSplit.BackColor = ModernTheme.BorderColor;
            _mainSplit.Panel1.BackColor = ModernTheme.SurfaceColor;
            _mainSplit.Panel2.BackColor = ModernTheme.BackColor;

            _editorSplit.BackColor = ModernTheme.BorderColor;
            _editorSplit.Panel1.BackColor = ModernTheme.BackColor;
            _editorSplit.Panel2.BackColor = ModernTheme.SurfaceColor;
        }

        // --- Event Handlers (Preserved Logic) ---

        private async void OnOpenRepository(object sender, EventArgs e)
        {
            var dialog = new ModernFolderBrowserDialog { Title = "Select Repository Folder" };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _currentRepository = dialog.SelectedPath;
                await LoadRepositoryAsync(_currentRepository);
            }
        }

        private async Task LoadRepositoryAsync(string path)
        {
            try
            {
                ToggleProgress(true);
                UpdateStatus("Loading repository...");

                // Only run non-UI operations on background thread
                if (_versionController != null)
                {
                    await Task.Run(() =>
                    {
                        _versionController.LoadProject(path);
                    }).ConfigureAwait(true);
                }

                // UI operations must happen on UI thread
                _fileExplorer.LoadRepository(path);

                // Scan for changes asynchronously with progress reporting
                if (_versionController != null)
                {
                    var progress = new Progress<string>(status => UpdateStatus(status));
                    await _versionController.ScanChangesAsync(progress).ConfigureAwait(true);

                    // UI updates must happen on UI thread
                    _gitGraphView.SetCommits(_versionController.GraphService.GetCommitHistory());
                    _stagingView.SetFiles(_versionController.ChangeDetector?.GetChangedFiles());
                }

                _lblRepo.Text = Path.GetFileName(path);
                UpdateStatus($"Loaded: {Path.GetFileName(path)}");
                _debugView.Log($"Repository loaded: {path}");
            }
            catch (AggregateException aggEx)
            {
                // Unwrap AggregateException to get the actual error(s)
                var innerExceptions = aggEx.Flatten().InnerExceptions;
                var errorMessage = string.Join("\n\n", innerExceptions.Select(ex => ex.Message));
                MessageBox.Show($"Failed to load repository:\n\n{errorMessage}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _debugView?.Log($"Load Error (AggregateException): {errorMessage}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                ToggleProgress(false);
            }
        }

        private async void OnFileSelected(object sender, FileSelectedEventArgs e)
        {
            await OpenFileInEditor(e.FilePath);
        }

        private async Task OpenFileInEditor(string filePath)
        {
            if (_openFiles.ContainsKey(filePath))
            {
                _editorTabs.SelectedTab = _openFiles[filePath];
                return;
            }

            var editor = new EditorView { Dock = DockStyle.Fill };

            // Set version controller for line history/blame feature
            if (_versionController != null && !string.IsNullOrEmpty(_currentRepository))
            {
                editor.SetVersionController(_versionController, _currentRepository);
            }

            await editor.LoadFile(filePath);

            var tabPage = new TabPage(Path.GetFileName(filePath))
            {
                Tag = filePath,
                ToolTipText = filePath
            };
            tabPage.Controls.Add(editor);

            // Create event handlers that we can unsubscribe later
            EventHandler contentHandler = (s, e) =>
            {
                if (!tabPage.Text.EndsWith(" ●"))
                    tabPage.Text += " ●";
            };

            EventHandler savedHandler = (s, e) =>
            {
                if (tabPage.Text.EndsWith(" ●"))
                    tabPage.Text = tabPage.Text.TrimEnd(' ', '●');
            };

            // Subscribe to events
            editor.ContentChanged += contentHandler;
            editor.FileSaved += savedHandler;

            // Store handlers for later cleanup
            _tabEventHandlers[filePath] = new TabPageData
            {
                Editor = editor,
                ContentHandler = contentHandler,
                SavedHandler = savedHandler
            };

            _editorTabs.TabPages.Add(tabPage);
            _openFiles.Add(filePath, tabPage);
            _editorTabs.SelectedTab = tabPage;
        }

        private void CloseCurrentTab()
        {
            var selectedTab = _editorTabs.SelectedTab;
            if (selectedTab == null) return;

            string filePath = selectedTab.Tag as string;
            if (filePath != null)
            {
                // Unsubscribe event handlers to prevent memory leaks
                if (_tabEventHandlers.ContainsKey(filePath))
                {
                    var data = _tabEventHandlers[filePath];
                    data.Editor.ContentChanged -= data.ContentHandler;
                    data.Editor.FileSaved -= data.SavedHandler;
                    data.Editor.Dispose(); // Dispose the editor
                    _tabEventHandlers.Remove(filePath);
                }

                if (_openFiles.ContainsKey(filePath))
                    _openFiles.Remove(filePath);
            }

            _editorTabs.TabPages.Remove(selectedTab);
            selectedTab.Dispose(); // Dispose the tab page
        }

        private void CloseAllTabs()
        {
            // Properly cleanup all tabs
            while (_editorTabs.TabPages.Count > 0)
            {
                _editorTabs.SelectedTab = _editorTabs.TabPages[0];
                CloseCurrentTab();
            }

            _openFiles.Clear();
            _tabEventHandlers.Clear();
        }

        private void SaveCurrentFile()
        {
            var selectedTab = _editorTabs.SelectedTab;
            var editor = selectedTab?.Controls.OfType<EditorView>().FirstOrDefault();
            editor?.SaveFile();
        }

        private void OnSaveAll(object sender, EventArgs e)
        {
            foreach (var page in _openFiles.Values)
                page.Controls.OfType<EditorView>().FirstOrDefault()?.SaveFile();
            UpdateStatus("All files saved");
        }

        private void OnFileRenamed(object sender, PathChangedEventArgs e)
        {
            // Update tab if open
             if (_openFiles.ContainsKey(e.OldPath))
            {
                var tabPage = _openFiles[e.OldPath];
                _openFiles.Remove(e.OldPath);
                _openFiles.Add(e.NewPath, tabPage);
                tabPage.Text = Path.GetFileName(e.NewPath);
                tabPage.Tag = e.NewPath;
            }
        }

        public void OpenConflictResolution(Conflict conflict)
        {
            if (conflict == null) return;

            string tabKey = $"CONFLICT:{conflict.FilePath}";
            if (_openFiles.ContainsKey(tabKey))
            {
                _editorTabs.SelectedTab = _openFiles[tabKey];
                return;
            }

            var resolutionView = new ConflictResolutionView { Dock = DockStyle.Fill };
            resolutionView.LoadConflict(conflict, conflict.BaseContent, conflict.LocalContent, conflict.RemoteContent);
            
            var tabPage = new TabPage($"Conflict: {Path.GetFileName(conflict.FilePath)}") 
            { 
                Tag = tabKey 
            };
            tabPage.Controls.Add(resolutionView);

            // Wire up events
            resolutionView.OnConflictResolved += (s, eArgs) => 
            {
                // Here we would typically update the index/disk with resolved content
                // For now, let's just save it to disk and close
                try 
                {
                    File.WriteAllText(conflict.FilePath, eArgs.ResolvedContent);
                    // Note: Staging would happen here if IndexController had StageFile method
                    
                    MessageBox.Show("Conflict resolved and saved!", "Resolution Success");
                    
                    _editorTabs.TabPages.Remove(tabPage);
                    _openFiles.Remove(tabKey);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving resolution: {ex.Message}", "Error");
                }
            };

            resolutionView.OnConflictCancelled += (s, args) => 
            {
                _editorTabs.TabPages.Remove(tabPage);
                _openFiles.Remove(tabKey);
            };

            _editorTabs.TabPages.Add(tabPage);
            _openFiles[tabKey] = tabPage;
            _editorTabs.SelectedTab = tabPage;
        }

        private void OnFileDeleted(object sender, PathChangedEventArgs e)
        {
            if (_openFiles.ContainsKey(e.OldPath))
            {
                _editorTabs.TabPages.Remove(_openFiles[e.OldPath]);
                _openFiles.Remove(e.OldPath);
            }
        }

        private async void OnCommitRequested(object sender, CommitEventArgs e)
        {
            if (_versionController == null) return;
            try
            {
                _versionController.CreateCommit(e.Message, e.SelectedFiles);
                _gitGraphView.SetCommits(_versionController.GraphService.GetCommitHistory());
                _stagingView.SetFiles(new List<string>()); // Clear
                UpdateStatus($"Committed: {e.Message}");
                MessageBox.Show("Commit Success");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Commit Failed: {ex.Message}");
                ShowError("Commit Failed", ex);
            }
        }

        private async void OnCommit(object sender, EventArgs e) => SwitchSideBar("SourceControl");
        
        private void ToggleProgress(bool visible)
        {
            if (_progressBar != null)
            {
                _progressBar.Visible = visible;
                if (visible) 
                {
                   // Marquee needs style set (already set in init)
                }
            }
            Cursor = visible ? Cursors.WaitCursor : Cursors.Default;
        }

        private async void OnPush(object sender, EventArgs e)
        {
            if (_remoteOperationsService == null || _versionController == null) return;

            string remoteUrl = _dialogService.PromptForInput("Remote URL", "Enter git remote URL to push to:");
            if (string.IsNullOrEmpty(remoteUrl)) return;

            await _asyncHelper.ExecuteAsync(
                async () =>
                {
                    var result = await _remoteOperationsService.PushAsync(remoteUrl);
                    if (result.IsSuccess)
                    {
                        _dialogService.ShowInfo("Push", result.Message);
                    }
                    else
                    {
                        _dialogService.ShowError("Push Failed", result.Message);
                    }
                },
                "Push",
                "Pushing to remote...",
                null); // Don't show success message twice
        }

        private async void OnPull(object sender, EventArgs e)
        {
            if (_remoteOperationsService == null || _versionController == null) return;

            string remoteUrl = _dialogService.PromptForInput("Remote URL", "Enter git remote URL to pull from:");
            if (string.IsNullOrEmpty(remoteUrl)) return;

            await _asyncHelper.ExecuteAsync(
                async () =>
                {
                    var result = await _remoteOperationsService.PullAsync(remoteUrl);
                    if (result.IsSuccess)
                    {
                        // Refresh graph and file status
                        _gitGraphView.SetCommits(_versionController.GraphService.GetCommitHistory());
                        _stagingView.SetFiles(_versionController.ChangeDetector?.GetChangedFiles());

                        _dialogService.ShowInfo("Pull", result.Message);
                    }
                    else
                    {
                        _dialogService.ShowError("Pull Failed", result.Message);
                    }
                },
                "Pull",
                "Pulling from remote...",
                null); // Don't show success message twice
        }
        
        private void OnBranches(object sender, EventArgs e) 
        {
             // Simple branch listing for now
             var branches = _versionController.GraphService.GetAllBranches();
             string branchList = string.Join("\n", branches.ConvertAll(b => b.BranchName));
             MessageBox.Show($"Branches:\n{branchList}", "Branches");
        }
        
        
        private async Task OnClone()
        {
            string repoUrl = _dialogService.PromptForInput("Clone Repository", "Enter Git Repository URL:");
            if (string.IsNullOrEmpty(repoUrl)) return;

            string destinationPath = _dialogService.PromptForFolder("Select Destination Folder");
            if (string.IsNullOrEmpty(destinationPath)) return;

            await _asyncHelper.ExecuteAsync(
                async () =>
                {
                    var result = await _remoteOperationsService.CloneAsync(repoUrl, destinationPath);
                    if (result.IsSuccess)
                    {
                        if (_dialogService.PromptYesNo("Clone", "Clone successful. Open repository now?") == DialogResult.Yes)
                        {
                            await LoadRepositoryAsync(destinationPath);
                        }
                    }
                    else
                    {
                        _dialogService.ShowError("Clone Failed", result.Message);
                    }
                },
                "Clone",
                $"Cloning {repoUrl}...",
                null); // Don't show success message twice
        }

        private void UpdateUI()
        {
            if (_versionController == null) return;

            // Refresh Staging View
            var changes = _versionController.ChangeDetector?.GetChanges();
            _stagingView.SetFiles(changes?.Keys.ToList());

            // Update Status Bar
            _lblBranch.Text = _versionController.GraphService.GetCurrentBranch()?.BranchName ?? "No Branch";
            _lblStatus.Text = _versionController.GetStatus();

            if (changes != null)
            {
                int newFiles = changes.Values.Count(v => v == "NEW");
                int modFiles = changes.Values.Count(v => v == "MODIFIED");
                int delFiles = changes.Values.Count(v => v == "DELETED");

                _improvedStatusBar?.UpdateFileStats(newFiles, modFiles, delFiles);
            }
        }

        private async void OnImportGitRepository(object sender, EventArgs e)
        {
            var dialog = new ModernFolderBrowserDialog { Title = "Select Git Repository to Import" };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            var gitPath = dialog.SelectedPath;

            // Validate it's a Git repo
            if (!Directory.Exists(Path.Combine(gitPath, ".git")))
            {
                MessageBox.Show("Selected folder is not a Git repository.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Ask import type
            var resultType = MessageBox.Show(
                "Full Import: Import entire commit history (slower)\n" +
                "Quick Import: Import only current state (faster)\n\n" +
                "Click Yes for Full Import, No for Quick Import",
                "Import Type",
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

            if (resultType == DialogResult.Cancel) return;

            bool isQuick = resultType == DialogResult.No;

            try
            {
                ToggleProgress(true);
                UpdateStatus("Starting import...");
                
                var importer = _versionController.CreateGitImporter();
                ImportResult result;
                
                var progress = new Progress<string>(status => UpdateStatus(status));

                if (isQuick)
                {
                    result = await Task.Run(() => importer.QuickImport(gitPath));
                }
                else
                {
                    // Full import with progress reporting
                    result = await Task.Run(() => importer.ImportRepository(gitPath, progress));
                }

                if (result.Success)
                {
                    MessageBox.Show($"Import Successful!\n{result}", "Import", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    // Load the imported project
                    _versionController.LoadProject(gitPath);
                    _fileExplorer.LoadRepository(gitPath);
                    _gitGraphView.SetCommits(_versionController.GraphService.GetCommitHistory());
                }
                else
                {
                    ShowError("Import Failed", new Exception(result.ErrorMessage));
                }
            }
            catch (AggregateException aggEx)
            {
                // Unwrap AggregateException to get the actual error(s)
                var innerExceptions = aggEx.Flatten().InnerExceptions;
                var errorMessage = string.Join("\n\n", innerExceptions.Select(ex => ex.Message));
                MessageBox.Show($"Import failed:\n\n{errorMessage}", "Import Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _debugView?.Log($"Import Error (AggregateException): {errorMessage}");
            }
            catch (Exception ex)
            {
                ShowError("Import Error", ex);
            }
            finally
            {
                UpdateStatus("Ready");
                ToggleProgress(false);
            }
        }

        private void OnAbout(object sender, EventArgs e)
        {
             MessageBox.Show("Li'nage v1.0\nVS Code Inspired GUI", "About");
        }

        private void ShowError(string title, Exception ex)
        {
            var message = $"{ex.Message}";
            if (ex.InnerException != null)
                message += $"\n\nInner: {ex.InnerException.Message}";

            // Log to file
            Logger.LogError($"{title}: {message}", ex);

            _dialogService?.ShowError(title, message);
            _debugView?.Log($"ERROR: {title} - {message}");
        }

        private void UpdateStatus(string message)
        {
            if (_lblStatus != null) _lblStatus.Text = message;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.S)) { SaveCurrentFile(); return true; }
            if (keyData == (Keys.Control | Keys.W)) { CloseCurrentTab(); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
