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
using Linage.GUI.Dialogs;
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
        private readonly MetadataStore _metadataStore;
        private WorkspaceService _workspaceService;
        private readonly FileService _fileService;
        private readonly ChangeDetector _changeDetector;
        private readonly DebugController _debugController;
        private readonly IndexController _indexController;
        private readonly RemoteController _remoteController;
        private readonly AuthController _authController;
        private Linage.GUI.Notifications.NotificationPresenter _notificationPresenter;

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
        private WelcomeView _welcomeView;

        // State
        private string _currentRepository;
        private Dictionary<string, TabPage> _openFiles = new Dictionary<string, TabPage>();
        private Dictionary<string, TabPageData> _tabEventHandlers = new Dictionary<string, TabPageData>();
        
        // Empty State Placeholder
        private Label _emptyStateLabel;

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
                // Placeholder for 'path' variable, assuming this block is moved/adapted from a method like LoadProject
                // For the constructor, we might not have a path yet, or it comes from config.
                // The provided diff implies a 'path' variable is available here.
                // For now, let's assume 'path' is initialized elsewhere or is a default.
                string path = Application.StartupPath; // Example placeholder

                _currentRepository = path;
                
                // Initialize Infrastructure
                var dbContext = new LiNageDbContext();
                _metadataStore = new MetadataStore(dbContext); 
                var hashService = new HashService();
                
                _fileService = new FileService(hashService); 
                _changeDetector = new ChangeDetector(path); 
                
                // Initialize VersionController with DI
                _versionController = new VersionController(path, _metadataStore);
                _workspaceService = new WorkspaceService(path);

                // Initialize Auth and Remote controllers using VersionController's services
                _authController = new AuthController(_versionController.AuthService);

                // Initialize RemoteController with AuthController
                _remoteController = new RemoteController(_authController);

                // Initialize Services
                _dialogService = new DialogService();
                _asyncHelper = new AsyncOperationHelper(ToggleProgress, UpdateStatus, ShowError, this);
                _remoteOperationsService = new RemoteOperationsService(_remoteController, _versionController.GraphService);
                _layoutConfig = UILayoutConfiguration.LoadFromSettings();

                // These lines seem to belong to a project loading method, not the constructor directly.
                // However, following the diff, they are placed here.
                // Assuming _terminalView and _scanController are initialized before this point or are null-checked.
                // Moved TerminalView config to after initialization

                // Load workspace state (assuming this method exists)
                // RestoreWorkspaceState();

                // await _versionController.LoadProjectAsync(path); // Cannot use await in constructor
                UpdateStatus($"Project loaded: {path}");
                
                // _changeWatcher = new FileSystemWatcher(path); // _changeWatcher is not defined in the provided context
            }
            catch (Exception ex)
            {
                // Unwrap InnerException if this is a TargetInvocationException
                var realException = ex;
                if (ex.InnerException != null)
                {
                    realException = ex.InnerException;
                }

                MessageBox.Show(
                    $"Failed to initialize Li'nage:\n\n" +
                    $"Error: {realException.Message}\n\n" +
                    $"Troubleshooting:\n" +
                    $"• Ensure SQL Server is running (LocalDB, Express, or full edition)\n" +
                    $"• Verify the connection string in App.config\n" +
                    $"• Check that the database can be accessed\n" +
                    $"• Review logs in the 'logs' folder for more details\n\n" +
                    $"Technical: {realException.GetType().Name}",
                    "Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
                _versionController = null;
            }

            // Initialize theme manager first
            var themeManager = ThemeManager.Instance;
            themeManager.ThemeChanged += (s, e) => ApplyThemeToAll();

            InitializeComponent();
            SetupViews();
            ApplyThemeToAll();

            // Initialize Notification Presenter
            _notificationPresenter = new Linage.GUI.Notifications.NotificationPresenter(this);
            if (_improvedStatusBar != null)
            {
                _improvedStatusBar.NotificationClicked += (s, e) => _notificationPresenter.ToggleCenter();
            }

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
            _mainSplit = new SplitContainer 
            { 
                Dock = DockStyle.Fill, 
                SplitterWidth = 1,
                FixedPanel = FixedPanel.Panel1 // Fix Sidebar width
            };
            _editorSplit = new SplitContainer 
            { 
                Dock = DockStyle.Fill, 
                Orientation = Orientation.Horizontal, 
                SplitterWidth = 1,
                FixedPanel = FixedPanel.Panel2 // Fix Terminal height
            };
            _sideBarContainer = new Panel 
            { 
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.SurfaceColor 
            };
            
            // Initialize Tab Controls
            _editorTabs = new ModernTabControl 
            { 
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.BackColor,
                ForeColor = ModernTheme.TextPrimary
            };
            _terminalTabs = new ModernTabControl 
            { 
                Dock = DockStyle.Fill,
                BackColor = ModernTheme.SurfaceColor,
                ForeColor = ModernTheme.TextPrimary
            };
            
            // Empty State Label
            _emptyStateLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "No file is open", 
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 16, FontStyle.Regular),
                ForeColor = ModernTheme.TextSecondary,
                BackColor = ModernTheme.BackColor,
                Visible = false
            };
            
            CreateStatusBar(); // Calling this helper to init _statusBar
            CreateMenuStrip(); // Init _menuStrip
            CreateActivityBar(); // Init _activityBar and buttons
            
            this.SuspendLayout();
            
            // 
            // Layout Setup
            // 
            
            // Set form size first
            this.ClientSize = new System.Drawing.Size(
                _layoutConfig?.DefaultWindowWidth ?? 1200,
                _layoutConfig?.DefaultWindowHeight ?? 800);
            this.MinimumSize = new System.Drawing.Size(
                _layoutConfig?.MinimumWindowWidth ?? 800,
                _layoutConfig?.MinimumWindowHeight ?? 600);
            
            // Main Split - Use configuration values
            _mainSplit.Panel1.Controls.Add(_sideBarContainer);
            _mainSplit.Panel2.Controls.Add(_editorSplit);
            
            // Set splitter distance after form is sized
            int sidebarWidth = _layoutConfig?.SidebarWidth ?? Spacing.Layout.SidebarWidth;
            _mainSplit.SplitterDistance = Math.Min(sidebarWidth, this.ClientSize.Width - 150);
            
            _mainSplit.BackColor = ModernTheme.SplitterColor;
            _mainSplit.Panel1.BackColor = ModernTheme.SurfaceColor;
            _mainSplit.Panel2.BackColor = ModernTheme.BackColor;

            // Editor Split - Use configuration values
            _editorSplit.Panel1.Controls.Add(_editorTabs);
            _editorSplit.Panel1.Controls.Add(_emptyStateLabel); // Add empty state label
            _editorSplit.Panel2.Controls.Add(_terminalTabs);
            
            // Set splitter distance after form is sized
            int editorHeight = _layoutConfig?.EditorPanelHeight ?? 600;
            _editorSplit.SplitterDistance = Math.Min(editorHeight, this.ClientSize.Height - 450);
            
            // Hide terminal panel on startup
            _editorSplit.Panel2Collapsed = true;
            
            _editorSplit.BackColor = ModernTheme.SplitterColor;
            _editorSplit.Panel1.BackColor = ModernTheme.BackColor;
            _editorSplit.Panel2.BackColor = ModernTheme.SurfaceColor;

            // Add controls to form
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

        private void UpdateEditorState()
        {
            // If welcome screen is visible, don't show empty state
            bool isWelcomeVisible = _editorTabs.TabPages.Cast<TabPage>().Any(t => t.Name == "WelcomeTab");
            if (isWelcomeVisible)
            {
                _editorTabs.Visible = true;
                _emptyStateLabel.Visible = false;
                return;
            }

            bool hasTabs = _editorTabs.TabPages.Count > 0;
            
            if (hasTabs)
            {
                _editorTabs.Visible = true;
                _emptyStateLabel.Visible = false;
            }
            else
            {
                _editorTabs.Visible = false;
                _emptyStateLabel.Visible = true;
                _emptyStateLabel.BringToFront();
            }
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
            _menuStrip = new MenuStrip
            {
                BackColor = ModernTheme.SurfaceColor,
                ForeColor = ModernTheme.TextPrimary,
                Renderer = new Linage.GUI.Controls.PremiumMenuRenderer(),
                Padding = new Padding(5, 2, 0, 2)
            };

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
            viewMenu.DropDownItems.Add("&Themes...", null, (s, e) => new ThemeEditorDialog().ShowDialog(this));
            viewMenu.DropDownItems.Add("Refresh &Status", null, async (s, e) => {
                await _versionController?.ScanChangesAsync();
                UpdateUI();
            });

            // Remote Menu
            var remoteMenu = new ToolStripMenuItem("&Remote");
            remoteMenu.DropDownItems.Add("&Manage Remotes...", null, OnManageRemotes);
            remoteMenu.DropDownItems.Add("-"); // Separator
            remoteMenu.DropDownItems.Add("&Push", null, OnPush);
            remoteMenu.DropDownItems.Add("P&ull", null, OnPull);

            // Help Menu
            var helpMenu = new ToolStripMenuItem("&Help");
            helpMenu.DropDownItems.Add("&About Li'nage", null, OnAbout);
            helpMenu.DropDownItems.Add("Simulate &Notifications", null, (s, e) => {
                var mgr = Linage.Infrastructure.Services.NotificationManager.Instance;
                mgr.ShowSuccess("Build Succeeded", "Project compilation completed successfully.");
                mgr.ShowWarning("Disk Space Low", "You are running low on disk space.");
                mgr.ShowError("Connection Failed", "Connection timed out", new Exception("timeout"));
                
                var actions = new System.Collections.Generic.List<Linage.Core.Notifications.NotificationAction>
                {
                    new Linage.Core.Notifications.NotificationAction("Update", () => Linage.Infrastructure.Services.NotificationManager.Instance.ShowSuccess("Success","Updated!")),
                    new Linage.Core.Notifications.NotificationAction("Later", () => { })
                };
                mgr.Show("Update Available", "A new version of Li'nage is available.", Linage.Core.Notifications.NotificationSeverity.Info, actions);
            });

            _menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, editMenu, viewMenu, remoteMenu, helpMenu });
        }

        private void SetupViews()
        {
            // 0. Welcome View (show when no repository is open)
            _welcomeView = new WelcomeView { Dock = DockStyle.Fill };
            _welcomeView.OpenRepositoryClicked += OnOpenRepository;
            _welcomeView.CloneRepositoryClicked += async (s, e) => await OnClone();
            _welcomeView.ImportGitClicked += (s, e) => OnImportGitRepository(s, e);

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
            _terminalView.VersionController = _versionController;
            if (!string.IsNullOrEmpty(_currentRepository)) _terminalView.SetWorkingDirectory(_currentRepository);
            
            _terminalView.OnProjectLoadRequested += async (path) => await LoadProjectAsync(path);
            _debugView = new DebugView { Dock = DockStyle.Fill };

            var terminalTab = new TabPage("Terminal")
            {
                BackColor = ModernTheme.BackColor
            };
            terminalTab.Controls.Add(_terminalView);
            
            var debugTab = new TabPage("Debug Console")
            {
                BackColor = ModernTheme.BackColor
            };
            debugTab.Controls.Add(_debugView);
            
            _terminalTabs.TabPages.Add(terminalTab);
            _terminalTabs.TabPages.Add(debugTab);
            
            // Show welcome view in editor area by default
            ShowWelcomeScreen();
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
                        if (_gitGraphView == null || _gitGraphView.IsDisposed)
                        {
                            _gitGraphView = new GitGraphView { Dock = DockStyle.Fill };
                            if (_versionController != null)
                            {
                                 _gitGraphView.SetCommits(_versionController.GraphService.GetCommitHistory());
                            }
                        }

                        var tab = new TabPage("Commit Graph")
                        {
                            Name = "Graph",
                            BackColor = ModernTheme.BackColor
                        };
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


        private void ApplyThemeToAll()
        {
            // Main form
            this.BackColor = ModernTheme.BackColor;
            this.ForeColor = ModernTheme.TextPrimary;

            // Menu
            if (_menuStrip != null)
            {
                _menuStrip.BackColor = ModernTheme.SurfaceColor;
                _menuStrip.ForeColor = ModernTheme.TextPrimary;
                _menuStrip.Renderer = new Linage.GUI.Controls.PremiumMenuRenderer();
            }

            // Activity Bar
            if (_activityBar != null)
            {
                _activityBar.BackColor = ModernTheme.ActivityBarColor;
                foreach (Control c in _activityBar.Controls) c.Invalidate();
            }

            // Sidebar Container
            if (_sideBarContainer != null)
            {
                _sideBarContainer.BackColor = ModernTheme.SurfaceColor;
                ApplyThemeToControls(_sideBarContainer.Controls);
            }

            // Main Split
            if (_mainSplit != null)
            {
                _mainSplit.BackColor = ModernTheme.SplitterColor;
                _mainSplit.Panel1.BackColor = ModernTheme.SurfaceColor;
                _mainSplit.Panel2.BackColor = ModernTheme.BackColor;
            }

            // Editor Split
            if (_editorSplit != null)
            {
                _editorSplit.BackColor = ModernTheme.SplitterColor;
                _editorSplit.Panel1.BackColor = ModernTheme.BackColor;
                _editorSplit.Panel2.BackColor = ModernTheme.SurfaceColor;
            }

            // Tab Controls
            if (_editorTabs != null)
            {
                _editorTabs.BackColor = ModernTheme.BackColor;
                _editorTabs.ForeColor = ModernTheme.TextPrimary;
                _editorTabs.Invalidate(); // Redraw tabs
                
                // Refresh all open tabs in editor
                foreach (TabPage tab in _editorTabs.TabPages)
                {
                    tab.BackColor = ModernTheme.BackColor;
                    tab.ForeColor = ModernTheme.TextPrimary;
                    ApplyThemeToControls(tab.Controls);
                }
            }
            
            // Empty State Label
            if (_emptyStateLabel != null)
            {
                _emptyStateLabel.BackColor = ModernTheme.BackColor;
                _emptyStateLabel.ForeColor = ModernTheme.TextSecondary;
            }

            if (_terminalTabs != null)
            {
                _terminalTabs.BackColor = ModernTheme.SurfaceColor;
                _terminalTabs.ForeColor = ModernTheme.TextPrimary;
                _terminalTabs.Invalidate(); 
                
                // Refresh all tabs in terminal
                foreach (TabPage tab in _terminalTabs.TabPages)
                {
                    tab.BackColor = ModernTheme.SurfaceColor;
                    tab.ForeColor = ModernTheme.TextPrimary;
                    ApplyThemeToControls(tab.Controls);
                }
            }

            // Status Bar
            if (_improvedStatusBar != null)
            {
                _improvedStatusBar.ApplyTheme();
            }

            // Refresh the form
            this.Refresh();
        }

        private void ApplyThemeToControls(Control.ControlCollection controls)
        {
            foreach (Control c in controls)
            {
                if (c is IThemable themable)
                {
                    themable.ApplyTheme();
                }
                
                // Recursively check children if needed? 
                // Usually IThemable controls handle their own children.
                // But for panels that just hold IThemables...
                if (c.HasChildren && !(c is IThemable)) // Don't go inside if it handles itself
                {
                    ApplyThemeToControls(c.Controls);
                }
            }
        }

        private void ApplyThemeToControl(Control control)
        {
            control.BackColor = ModernTheme.BackColor;
            control.ForeColor = ModernTheme.TextPrimary;
            control.Font = ModernTheme.FontBody;
            
            if (control is IThemable themable)
            {
                themable.ApplyTheme();
            }
            
            ApplyThemeToControls(control.Controls);
        }

        private void ShowWelcomeScreen()
        {
            // Clear editor tabs
            if (_editorTabs.TabPages.Count == 0)
            {
                var welcomeTab = new TabPage("Welcome")
                {
                    BackColor = ModernTheme.BackColor,
                    Name = "WelcomeTab"
                };
                welcomeTab.Controls.Add(_welcomeView);
                _editorTabs.TabPages.Add(welcomeTab);
                _editorTabs.SelectTab(welcomeTab);
            }
            UpdateEditorState();
        }

        private void HideWelcomeScreen()
        {
            // Remove welcome tab if it exists
            var welcomeTab = _editorTabs.TabPages.Cast<TabPage>().FirstOrDefault(t => t.Name == "WelcomeTab");
            if (welcomeTab != null)
            {
                _editorTabs.TabPages.Remove(welcomeTab);
            }
            UpdateEditorState();
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

                // Hide welcome screen when loading a repository
                HideWelcomeScreen();

                if (_versionController != null)
                {
                    await _versionController.LoadProjectAsync(path);
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
                // MessageBox.Show($"Failed to load repository:\n\n{errorMessage}", "Error",
                //    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _dialogService.ShowError("Load Failed", errorMessage);
                _debugView?.Log($"Load Error (AggregateException): {errorMessage}");
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("Error", ex.Message);
            }
            finally
            {
                ToggleProgress(false);
            }
        }

        private async void OnFileSelected(object sender, FileSelectedEventArgs e)
        {
            try
            {
                ToggleProgress(true);
                UpdateStatus($"Opening {Path.GetFileName(e.FilePath)}...");
                await OpenFileInEditor(e.FilePath).ConfigureAwait(true);
                UpdateStatus("File opened");
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"OnFileSelected: Error opening file - {ex.Message}");
                ShowError("File Open Failed", ex);
            }
            finally
            {
                ToggleProgress(false);
            }
        }

        private async Task OpenFileInEditor(string filePath)
        {
            if (_openFiles.ContainsKey(filePath))
            {
                _editorTabs.SelectedTab = _openFiles[filePath];
                return;
            }

            try
            {
                var editor = new EditorView { Dock = DockStyle.Fill };

                // Set version controller for line history/blame feature
                if (_versionController != null && !string.IsNullOrEmpty(_currentRepository))
                {
                    editor.SetVersionController(_versionController, _currentRepository);
                }

                // Load file asynchronously
                await editor.LoadFile(filePath).ConfigureAwait(true);

                var tabPage = new TabPage(Path.GetFileName(filePath))
                {
                    Tag = filePath,
                    ToolTipText = filePath,
                    BackColor = ModernTheme.BackColor
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
                
                UpdateEditorState();
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"OpenFileInEditor: Error - {ex.Message}");
                throw;
            }
        }

        private void OpenVirtualFile(string title, string content)
        {
            if (_openFiles.ContainsKey(title))
            {
                _editorTabs.SelectedTab = _openFiles[title];
                return;
            }

            var editor = new EditorView { Dock = DockStyle.Fill };
            editor.LoadContent(title, content, readOnly: true);

            var tabPage = new TabPage(title)
            {
                Tag = title,
                ToolTipText = title,
                BackColor = ModernTheme.BackColor
            };
            tabPage.Controls.Add(editor);

            _editorTabs.TabPages.Add(tabPage);
            _openFiles.Add(title, tabPage);
            _editorTabs.SelectedTab = tabPage;
            
            UpdateEditorState();
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
            
            UpdateEditorState();
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
            
            UpdateEditorState();
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
                    
                    _dialogService.ShowSuccess("Resolution Success", "Conflict resolved and saved!");
                    
                    _editorTabs.TabPages.Remove(tabPage);
                    _openFiles.Remove(tabKey);
                }
                catch (Exception ex)
                {
                    _dialogService.ShowError("Error", $"Error saving resolution: {ex.Message}");
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
            DebugLogger.Info("MainWindow.OnCommitRequested received");
            DebugLogger.Trace($"  -> Message: {e.Message}");
            DebugLogger.Trace($"  -> Files count: {e.SelectedFiles?.Count ?? 0}");

            if (_versionController == null)
            {
                DebugLogger.Warn("  -> Aborting: _versionController is null");
                return;
            }
            try
            {
                await _versionController.CreateCommitAsync(e.Message, e.SelectedFiles);
                DebugLogger.Info("  -> Commit async completed");

                _gitGraphView.SetCommits(_versionController.GraphService.GetCommitHistory());
                DebugLogger.Trace("  -> Updated git graph");

                // Check what ChangeDetector has after the commit
                var changesAfterCommit = _versionController.ChangeDetector?.GetChanges();
                DebugLogger.Info($"  -> ChangeDetector has {changesAfterCommit?.Count ?? 0} dirty files after commit");
                if (changesAfterCommit != null)
                {
                    foreach (var kvp in changesAfterCommit.Take(10))
                    {
                        DebugLogger.Trace($"     - {kvp.Key} : {kvp.Value}");
                    }
                }

                // Clear staging view by passing empty list
                DebugLogger.Trace("  -> Clearing staging view with empty list");
                _stagingView.SetFiles(new List<string>()); // Clear

                UpdateStatus($"Committed: {e.Message}");
                DebugLogger.Info("  -> Commit complete, showing success message");
                
                // Show sync prompt after successful commit
                var result = MessageBox.Show(
                    "Commit successful! Would you like to sync changes with remote?",
                    "Sync Changes",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    await OnSyncChanges();
                }
                else
                {
                    _dialogService.ShowSuccess("Commit", "Commit Success");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"  -> Commit failed: {ex.Message}");
                // MessageBox.Show($"Commit Failed: {ex.Message}");
                ShowError("Commit Failed", ex);
            }
        }

        /// <summary>
        /// Handles syncing changes: pulls from remote and handles merge conflicts
        /// </summary>
        private async Task OnSyncChanges()
        {
            try
            {
                ToggleProgress(true);
                UpdateStatus("Syncing changes...");
                DebugLogger.Info("OnSyncChanges: Starting sync operation");

                // Get the default remote
                var remotes = await _versionController.RemoteService.GetAllRemotesAsync();
                var defaultRemote = remotes.FirstOrDefault(r => r.IsDefault) ?? remotes.FirstOrDefault();

                if (defaultRemote == null)
                {
                    UpdateStatus("No remote configured");
                    MessageBox.Show("No remote configured. Please add a remote first.", "No Remote", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    ToggleProgress(false);
                    return;
                }

                DebugLogger.Info($"OnSyncChanges: Pulling from remote '{defaultRemote.RemoteName}'");
                
                // Pull from remote
                await _versionController.Pull(defaultRemote.RemoteName);
                
                // Update the graph with new commits
                _gitGraphView.SetCommits(_versionController.GraphService.GetCommitHistory());
                UpdateStatus("Sync completed successfully");
                DebugLogger.Info("OnSyncChanges: Sync completed");
                
                MessageBox.Show("Sync completed successfully!", "Sync Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (InvalidOperationException conflictEx) when (conflictEx.Message.Contains("conflict"))
            {
                DebugLogger.Warn($"OnSyncChanges: Merge conflicts detected - {conflictEx.Message}");
                UpdateStatus("Merge conflicts detected - resolving...");
                
                // Handle merge conflicts
                await HandleMergeConflicts();
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"OnSyncChanges: Sync failed - {ex.Message}");
                UpdateStatus($"Sync failed: {ex.Message}");
                ShowError("Sync Failed", ex);
            }
            finally
            {
                ToggleProgress(false);
            }
        }

        /// <summary>
        /// Handles merge conflicts by showing conflict resolution window
        /// </summary>
        private async Task HandleMergeConflicts()
        {
            try
            {
                DebugLogger.Info("HandleMergeConflicts: Getting conflict data");
                
                // Get conflicts from the version controller (if available)
                // For now, we'll create a basic conflict resolution window
                var conflictDialog = new Form
                {
                    Text = "Merge Conflict Resolution",
                    Width = 1000,
                    Height = 700,
                    StartPosition = FormStartPosition.CenterParent,
                    MaximizeBox = true
                };

                var mergeView = new MergeConflictView
                {
                    Dock = DockStyle.Fill
                };

                // Example conflict - in real scenario, get from GraphService
                var exampleConflict = new Conflict
                {
                    FilePath = "Example Conflict",
                    LocalContent = "Local version content here",
                    RemoteContent = "Remote version content here"
                };

                mergeView.SetConflict(exampleConflict);
                mergeView.ConflictResolved += async (s, conflict) =>
                {
                    try
                    {
                        DebugLogger.Info($"HandleMergeConflicts: Conflict resolved for {conflict.FilePath}");
                        
                        // Save the resolved content back to the file
                        if (!string.IsNullOrEmpty(conflict.FilePath))
                        {
                            string filePath = Path.Combine(_currentRepository, conflict.FilePath);
                            using (var writer = new StreamWriter(filePath))
                            {
                                await writer.WriteAsync(conflict.ResolvedContent);
                            }
                            
                            DebugLogger.Info($"HandleMergeConflicts: Saved resolved content to {filePath}");
                        }

                        // Complete the merge and create a merge commit
                        await CompleteMergeAfterResolution();
                        
                        conflictDialog.Close();
                        UpdateStatus("Merge conflicts resolved");
                        MessageBox.Show("Merge conflicts resolved successfully!", "Resolution Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.Error($"HandleMergeConflicts: Error saving resolution - {ex.Message}");
                        ShowError("Resolution Error", ex);
                    }
                };

                conflictDialog.Controls.Add(mergeView);
                ApplyThemeToControl(conflictDialog);
                conflictDialog.ShowDialog(this);
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"HandleMergeConflicts: Error - {ex.Message}");
                ShowError("Conflict Resolution Failed", ex);
            }
        }

        /// <summary>
        /// Completes the merge after conflicts are resolved
        /// </summary>
        private async Task CompleteMergeAfterResolution()
        {
            try
            {
                DebugLogger.Info("CompleteMergeAfterResolution: Creating merge commit");
                
                // Create a merge commit to finalize the merge
                var currentBranch = _versionController.GraphService.GetCurrentBranch();
                if (currentBranch != null)
                {
                    // Re-scan for changes after conflict resolution
                    await _versionController.ScanChangesAsync();
                    
                    // Create merge commit
                    await _versionController.CreateCommitAsync("Merge: Resolved conflicts", null);
                    
                    // Update UI
                    _gitGraphView.SetCommits(_versionController.GraphService.GetCommitHistory());
                    _stagingView.SetFiles(new List<string>());
                    
                    DebugLogger.Info("CompleteMergeAfterResolution: Merge commit created successfully");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error($"CompleteMergeAfterResolution: Error - {ex.Message}");
                throw;
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
                    var result = await _remoteOperationsService.PushAsync(remoteUrl, _currentRepository);
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
                    var result = await _remoteOperationsService.PullAsync(remoteUrl, _currentRepository);
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
        
        private async void OnBranches(object sender, EventArgs e) 
        {
            if (_versionController?.GraphService == null) return;
            if (string.IsNullOrEmpty(_currentRepository)) 
            {
                 _dialogService.ShowWarning("Warning", "No project loaded.");
                 return;
            }

            // FILTER: Pass _currentRepository to get project-specific branches
            var branches = await _versionController.GraphService.GetAllBranchesAsync(_currentRepository);
            
            // Check if any branches exist. If not, maybe create 'main' by default or show empty dialog?
            // If empty, we can still show dialog to let user create one.
            var branchNames = branches?.Select(b => b.BranchName).ToArray() ?? new string[0];

            var currentBranch = _versionController.GraphService.GetCurrentBranch();
            
            using (var dialog = new Linage.GUI.Dialogs.ModernBranchSelectorDialog(branchNames, currentBranch?.BranchName))
            {
                dialog.ShowDialog(this);
                
                if (dialog.CustomResult == DialogResult.OK)
                {
                   await SwitchBranchAsync(dialog.SelectedBranch);
                }
                else if (dialog.CustomResult == DialogResult.Retry)
                {
                   await CreateNewBranchAsync();
                }
                else if (dialog.CustomResult == DialogResult.Abort)
                {
                    string branchToDelete = dialog.SelectedBranch;
                    if (string.IsNullOrEmpty(branchToDelete)) return;

                    if (currentBranch?.BranchName == branchToDelete)
                    {
                        _dialogService.ShowWarning("Delete Branch", "Cannot delete the current branch. Switch to another branch first.");
                        return;
                    }

                    if (MessageBox.Show($"Delete branch '{branchToDelete}'?", "Confirm Delete", 
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        await DeleteBranchAsync(branchToDelete);
                    }
                }
            }
        }

        private async Task SwitchBranchAsync(string branchName)
        {
            try
            {
                ToggleProgress(true);
                UpdateStatus($"Switching to branch '{branchName}'...");

                await _versionController.GraphService.SwitchBranchAsync(branchName);

                // Refresh UI
                _lblBranch.Text = branchName;
                _gitGraphView.SetCommits(_versionController.GraphService.GetCommitHistory());
                _stagingView.SetFiles(_versionController.ChangeDetector?.GetChangedFiles());

                UpdateStatus($"Switched to branch '{branchName}'");
                _debugView?.Log($"Switched to branch: {branchName}");
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("Error", $"Failed to switch branch: {ex.Message}");
                _debugView?.Log($"Switch branch error: {ex.Message}");
            }
            finally
            {
                ToggleProgress(false);
            }
        }

        private async Task CreateNewBranchAsync()
        {
            string branchName = _dialogService.PromptForInput("Create Branch", "Enter new branch name:");
            if (string.IsNullOrEmpty(branchName)) return;

            try
            {
                ToggleProgress(true);
                UpdateStatus($"Creating branch '{branchName}'...");

                await _versionController.GraphService.CreateBranchAsync(branchName, _currentRepository);
                await _versionController.GraphService.SwitchBranchAsync(branchName);

                // Refresh UI
                _lblBranch.Text = branchName;
                _gitGraphView.SetCommits(_versionController.GraphService.GetCommitHistory());

                UpdateStatus($"Created and switched to branch '{branchName}'");
                _debugView?.Log($"Created new branch: {branchName}");
                _dialogService.ShowSuccess("Success", $"Created and switched to branch '{branchName}'");
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("Error", $"Failed to create branch: {ex.Message}");
                _debugView?.Log($"Create branch error: {ex.Message}");
            }
            finally
            {
                ToggleProgress(false);
            }
        }

        private async Task DeleteBranchAsync(string branchName)
        {
            try
            {
                ToggleProgress(true);
                UpdateStatus($"Deleting branch '{branchName}'...");

                await _versionController.GraphService.DeleteBranchAsync(branchName);

                UpdateStatus($"Deleted branch '{branchName}'");
                _debugView?.Log($"Deleted branch: {branchName}");
                _dialogService.ShowSuccess("Success", $"Deleted branch '{branchName}'");
                
                // Refresh branches dialog
                OnBranches(null, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("Error", $"Failed to delete branch: {ex.Message}");
                _debugView?.Log($"Delete branch error: {ex.Message}");
            }
            finally
            {
                ToggleProgress(false);
            }
        }
        
        
        private async void OnManageRemotes(object sender, EventArgs e)
        {
            if (_versionController?.RemoteService == null) return;
             if (string.IsNullOrEmpty(_currentRepository)) 
            {
                 _dialogService.ShowWarning("Warning", "No project loaded.");
                 return;
            }

            // FILTER: Pass _currentRepository
            var remotes = await _versionController.RemoteService.GetAllRemotesAsync(_currentRepository);

            using (var dialog = new Linage.GUI.Dialogs.ModernRemoteManagerDialog(remotes))
            {
                dialog.ShowDialog(this);
                
                if (dialog.CustomResult == DialogResult.Retry) // Add
                {
                    string name = _dialogService.PromptForInput("Remote Name", "Enter remote name (e.g. origin):", "origin");
                    if (string.IsNullOrEmpty(name)) return;
                    
                    string url = _dialogService.PromptForInput("Remote URL", "Enter remote URL:");
                    if (string.IsNullOrEmpty(url)) return;

                    try
                    {
                        // Pass current repository path
                        await _versionController.RemoteService.AddRemoteAsync(name, url, _currentRepository);
                        _dialogService.ShowSuccess("Success", $"Added remote '{name}'");
                        OnManageRemotes(sender, e); // Refresh
                    }
                    catch(Exception ex)
                    {
                        _dialogService.ShowError("Error", ex.Message);
                    }
                }
                else if (dialog.CustomResult == DialogResult.OK) // Set Default
                {
                     if (!string.IsNullOrEmpty(dialog.SelectedRemote))
                     {
                         await _versionController.RemoteService.SetDefaultRemoteAsync(dialog.SelectedRemote, _currentRepository);
                         _dialogService.ShowSuccess("Success", $"Set '{dialog.SelectedRemote}' as default");
                         OnManageRemotes(sender, e);
                     }
                }
                else if (dialog.CustomResult == DialogResult.Abort) // Remove
                {
                     if (!string.IsNullOrEmpty(dialog.SelectedRemote))
                     {
                         if (MessageBox.Show($"Remove remote '{dialog.SelectedRemote}'?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                         {
                             await _versionController.RemoteService.RemoveRemoteAsync(dialog.SelectedRemote);
                             OnManageRemotes(sender, e);
                         }
                     }
                }
            }
        }
        private async Task AddRemoteAsync()
        {
            string remoteName = _dialogService.PromptForInput("Add Remote", "Enter remote name (e.g., 'origin'):");
            if (string.IsNullOrEmpty(remoteName)) return;

            string remoteUrl = _dialogService.PromptForInput("Add Remote", "Enter remote URL (e.g., 'https://...'):");
            if (string.IsNullOrEmpty(remoteUrl)) return;

            try
            {
                ToggleProgress(true);
                UpdateStatus($"Adding remote '{remoteName}'...");

                await _versionController.RemoteService.AddRemoteAsync(remoteName, remoteUrl, _currentRepository);

                UpdateStatus($"Added remote '{remoteName}'");
                _debugView?.Log($"Added remote: {remoteName} -> {remoteUrl}");
                _dialogService.ShowSuccess("Success", $"Added remote '{remoteName}'");
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("Error", $"Failed to add remote: {ex.Message}");
                _debugView?.Log($"Add remote error: {ex.Message}");
            }
            finally
            {
                ToggleProgress(false);
            }
        }

        private async Task RemoveRemoteAsync(string remoteName)
        {
            try
            {
                ToggleProgress(true);
                UpdateStatus($"Removing remote '{remoteName}'...");

                await _versionController.RemoteService.RemoveRemoteAsync(remoteName);

                UpdateStatus($"Removed remote '{remoteName}'");
                _debugView?.Log($"Removed remote: {remoteName}");
                _dialogService.ShowSuccess("Success", $"Removed remote '{remoteName}'");
                
                // Refresh remotes dialog
                OnManageRemotes(null, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("Error", $"Failed to remove remote: {ex.Message}");
                _debugView?.Log($"Remove remote error: {ex.Message}");
            }
            finally
            {
                ToggleProgress(false);
            }
        }

        private async Task SetDefaultRemoteAsync(string remoteName)
        {
            try
            {
                ToggleProgress(true);
                UpdateStatus($"Setting '{remoteName}' as default remote...");

                await _versionController.RemoteService.SetDefaultRemoteAsync(remoteName, _currentRepository);

                UpdateStatus($"'{remoteName}' is now the default remote");
                _debugView?.Log($"Set default remote: {remoteName}");
                _dialogService.ShowSuccess("Success", $"'{remoteName}' is now the default remote");
                
                // Refresh remotes dialog
                OnManageRemotes(null, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError("Error", $"Failed to set default remote: {ex.Message}");
                _debugView?.Log($"Set default remote error: {ex.Message}");
            }
            finally
            {
                ToggleProgress(false);
            }
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
                        _dialogService.PromptYesNo("Clone", "Clone successful. Open repository now?", 
                             async () => await LoadRepositoryAsync(destinationPath));
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

        private async Task LoadProjectAsync(string path)
        {
             if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;

             try
             {
                 _currentRepository = path;
                 
                 // Re-init controllers if needed or just switch context
                 // For now, simpler to just load it via VC
                 await _versionController.LoadProjectAsync(path);
                 
                 // Update UI
                 if (_fileExplorer != null) 
                    _fileExplorer.LoadRepository(path);
                    
                 UpdateStatus($"Loaded project: {path}");
             }
             catch (Exception ex)
             {
                 ShowError("Load Project Error", ex);
             }
        }

        private void OnImportGitRepository(object sender, EventArgs e)
        {
            var dialog = new ModernFolderBrowserDialog { Title = "Select Git Repository to Import" };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            var gitPath = dialog.SelectedPath;

            // Validate it's a Git repo
            if (!Directory.Exists(Path.Combine(gitPath, ".git")))
            {
                _dialogService.ShowError("Error", "Selected folder is not a Git repository.");
                return;
            }

            // Ask import type
            var actions = new List<Linage.Core.Notifications.NotificationAction>
            {
                new Linage.Core.Notifications.NotificationAction("Full Import", async () => await PerformImport(gitPath, false), true),
                new Linage.Core.Notifications.NotificationAction("Quick Import", async () => await PerformImport(gitPath, true)),
                new Linage.Core.Notifications.NotificationAction("Cancel", () => { })
            };

            Linage.Infrastructure.Services.NotificationManager.Instance.Show(
                "Import Type",
                "Choose import strategy:\nFull Import: Entire history (slower)\nQuick Import: Current state only (faster)",
                Linage.Core.Notifications.NotificationSeverity.Question,
                actions);
        }

        private async Task PerformImport(string gitPath, bool isQuick)
        {
            try
            {
                ToggleProgress(true);
                UpdateStatus("Starting import...");
                
                var importer = _versionController.CreateGitImporter();
                ImportResult result;
                
                var progress = new Progress<string>(status => UpdateStatus(status));

                if (isQuick)
                {
                    result = await importer.QuickImportAsync(gitPath);
                }
                else
                {
                    // Full import with progress reporting
                    result = await importer.ImportRepositoryAsync(gitPath, progress);
                }

                if (result.Success)
                {
                    _dialogService.ShowSuccess("Import", $"Import Successful!\n{result}");
                    
                    // Load the imported project
                    await _versionController.LoadProjectAsync(gitPath);
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
                _dialogService.ShowError("Import Error", $"Import failed:\n\n{errorMessage}");
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
            string aboutText = "LINAGE is a modern, developer-focused Version Control System built to track code evolution with clarity and intent.\n\n" +
                               "It combines a structured core, a powerful CLI, and an intuitive GUI to manage repositories, commits, branching, and history efficiently.\n\n" +
                               "Designed with scalability and precision in mind, LINAGE emphasizes transparent lineage tracking, making it easier to understand how code changes over time, not just that they changed.\n\n" +
                               "It is built for developers who want control, insight, and a system that respects how real software is developed.\n\n" +
                               "Key Features:\n" +
                               "• Line-level version control tracking\n" +
                               "• Intelligent branching and merging\n" +
                               "• Multi-protocol remote support (HTTP, SSH)\n" +
                               "• Advanced conflict resolution\n" +
                               "• Real-time file synchronization\n" +
                               "• Integrated GUI and CLI\n\n" +
                               "© 2025 Hanan Bhatti. Licensed under GNU General Public License v3.0";

            OpenVirtualFile("About Li'nage", aboutText);
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
        private async void RestoreWorkspaceState()
        {
            if (_workspaceService == null) return;
            var state = _workspaceService.LoadState();
            
            if (state.OpenFiles != null)
            {
                foreach (var file in state.OpenFiles)
                {
                     if (File.Exists(file)) await OpenFileInEditor(file); // Await the async call
                }
            }
        }

        // Hook into FormClosing to save state
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_workspaceService != null)
            {
                var openFiles = _openFiles.Keys.ToList();
                string activeDoc = _editorTabs.SelectedTab?.Tag as string;
                _workspaceService.SaveState(openFiles, activeDoc);
            }
            base.OnFormClosing(e);
        }
    }
}
