using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Linage.GUI.Theme;
using Linage.Controllers;
using Linage.Core;
using Linage.Infrastructure;

namespace Linage.GUI
{
    public class TerminalView : UserControl, IThemable
    {
        // ...

        public void ApplyTheme()
        {
            this.BackColor = ModernTheme.BackColor;
            
            // Layout (TableLayoutPanel)
            if (this.Controls.Count > 0 && this.Controls[0] is TableLayoutPanel layout)
            {
                layout.BackColor = ModernTheme.BackColor;
            }

            if (_output != null)
            {
                _output.BackColor = ModernTheme.BackColor;
                _output.ForeColor = ModernTheme.TextPrimary;
                _output.Font = ModernTheme.FontCode;
            }

            if (_input != null)
            {
                _input.BackColor = ModernTheme.InputBack;
                _input.ForeColor = ModernTheme.TextPrimary;
                _input.Font = ModernTheme.FontCode;
            }
        }

        private TextBox _input;
        private RichTextBox _output;
        private List<string> _commandHistory;
        private int _historyIndex = -1;

        // Shell process for real command execution
        private Process _shellProcess;
        private string _currentDirectory;
        private bool _isExecutingCommand;
        private CancellationTokenSource _cancellationTokenSource;

        // Controllers (will be injected)
        public VersionController VersionController { get; set; }
        public ScanController ScanController { get; set; }
        public IndexController IndexController { get; set; }
        public AuthController AuthController { get; set; }

        public TerminalView()
        {
            _commandHistory = new List<string>();
            _currentDirectory = Environment.CurrentDirectory;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.BackColor = ModernTheme.BackColor;
            this.Padding = new Padding(5);

            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.RowCount = 2;
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));
            layout.BackColor = ModernTheme.BackColor;

            _output = new RichTextBox();
            _output.Dock = DockStyle.Fill;
            _output.BackColor = ModernTheme.BackColor;
            _output.ForeColor = ModernTheme.TextPrimary;
            _output.Font = ModernTheme.FontCode;
            _output.BorderStyle = BorderStyle.None;
            _output.ReadOnly = true;

            _input = new TextBox();
            _input.Dock = DockStyle.Fill;
            _input.BackColor = ModernTheme.InputBack;
            _input.ForeColor = ModernTheme.TextPrimary;
            _input.Font = ModernTheme.FontCode;
            _input.BorderStyle = BorderStyle.FixedSingle;
            _input.KeyDown += OnInputKeyDown;

            layout.Controls.Add(_output, 0, 0);
            layout.Controls.Add(_input, 0, 1);

            this.Controls.Add(layout);

            // Welcome message
            WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            WriteOutput("Li'nage Terminal v1.0");
            WriteOutput("Type 'help' for Li'nage commands, or use shell commands");
            WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            WritePrompt();
        }

        private void WritePrompt()
        {
            _output.AppendText($"\n{_currentDirectory}> ");
            _output.ScrollToCaret();
        }

        /// <summary>
        /// Sets the working directory for the terminal
        /// </summary>
        public void SetWorkingDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                _currentDirectory = path;
            }
        }

        private void OnInputKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (_isExecutingCommand)
                {
                    e.SuppressKeyPress = true;
                    return;
                }

                var command = _input.Text.Trim();
                if (!string.IsNullOrEmpty(command))
                {
                    _input.Text = "";
                    _output.AppendText($"{command}\n");
                    _commandHistory.Add(command);
                    _historyIndex = -1;
                    ProcessCommandAsync(command);
                }
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Up && _historyIndex < _commandHistory.Count - 1)
            {
                _historyIndex++;
                _input.Text = _commandHistory[_commandHistory.Count - 1 - _historyIndex];
                _input.SelectionStart = _input.Text.Length;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Down && _historyIndex > 0)
            {
                _historyIndex--;
                _input.Text = _historyIndex >= 0 ? _commandHistory[_commandHistory.Count - 1 - _historyIndex] : "";
                _input.SelectionStart = _input.Text.Length;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.C && e.Control)
            {
                // Ctrl+C to cancel running command
                if (_isExecutingCommand && _cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancel();
                    WriteOutput("\n^C");
                }
                e.SuppressKeyPress = true;
            }
        }

        private async void ProcessCommandAsync(string input)
        {
            var parts = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
            {
                WritePrompt();
                return;
            }

            var command = parts[0].ToLower();
            var args = parts.Skip(1).ToArray();

            try
            {
                // Check if it's a Li'nage built-in command
                if (IsLinageCommand(command))
                {
                    ProcessLinageCommand(command, args);
                    WritePrompt();
                }
                else
                {
                    // Execute as shell command
                    await ExecuteShellCommandAsync(input);
                }
            }
            catch (Exception ex)
            {
                WriteOutput($"Error: {ex.Message}");
                WritePrompt();
            }
        }

        private bool IsLinageCommand(string command)
        {
            var linageCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "help", "ln-init", "ln-status", "ln-add", "ln-commit", "ln-log",
                "ln-branch", "ln-switch", "ln-merge", "ln-rebase", "ln-diff",
                "ln-sync", "ln-push", "ln-pull", "ln-remote", "ln-blame",
                "ln-stash", "ln-reset", "ln-config", "clear", "cls", "exit", "quit"
            };
            return linageCommands.Contains(command);
        }

        private void ProcessLinageCommand(string command, string[] args)
        {
            switch (command.ToLower())
            {
                case "help":
                    ShowHelp();
                    break;
                case "ln-init":
                    HandleInit(args);
                    break;
                case "ln-status":
                    HandleStatus(args);
                    break;
                case "ln-add":
                    HandleAdd(args);
                    break;
                case "ln-commit":
                    HandleCommit(args);
                    break;
                case "ln-log":
                    HandleLog(args);
                    break;
                case "ln-branch":
                    HandleBranch(args);
                    break;
                case "ln-switch":
                    HandleSwitch(args);
                    break;
                case "ln-merge":
                    HandleMerge(args);
                    break;
                case "ln-rebase":
                    HandleRebase(args);
                    break;
                case "ln-diff":
                    HandleDiff(args);
                    break;
                case "ln-sync":
                    HandleSync(args);
                    break;
                case "ln-push":
                    HandlePush(args);
                    break;
                case "ln-pull":
                    HandlePull(args);
                    break;
                case "ln-remote":
                    HandleRemote(args);
                    break;
                case "ln-blame":
                    HandleBlame(args);
                    break;
                case "ln-stash":
                    HandleStash(args);
                    break;
                case "ln-reset":
                    HandleReset(args);
                    break;
                case "ln-config":
                    HandleConfig(args);
                    break;
                case "clear":
                case "cls":
                    _output.Clear();
                    break;
                case "exit":
                case "quit":
                    WriteOutput("Goodbye!");
                    break;
                default:
                    WriteOutput($"Unknown Li'nage command: '{command}'");
                    break;
            }
        }

        private async Task ExecuteShellCommandAsync(string command)
        {
            _isExecutingCommand = true;
            _input.Enabled = false;
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                // Handle cd command specially to change working directory
                if (command.Trim().StartsWith("cd ", StringComparison.OrdinalIgnoreCase) ||
                    command.Trim().Equals("cd", StringComparison.OrdinalIgnoreCase))
                {
                    HandleCdCommand(command);
                    return;
                }

                var processInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = _currentDirectory,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                using (_shellProcess = new Process())
                {
                    _shellProcess.StartInfo = processInfo;
                    _shellProcess.EnableRaisingEvents = true;

                    var outputBuilder = new StringBuilder();
                    var errorBuilder = new StringBuilder();

                    _shellProcess.OutputDataReceived += (s, e) =>
                    {
                        if (e.Data != null)
                        {
                            WriteOutputThreadSafe(e.Data);
                        }
                    };

                    _shellProcess.ErrorDataReceived += (s, e) =>
                    {
                        if (e.Data != null)
                        {
                            WriteOutputThreadSafe(e.Data, true);
                        }
                    };

                    _shellProcess.Start();
                    _shellProcess.BeginOutputReadLine();
                    _shellProcess.BeginErrorReadLine();

                    // Wait for process with cancellation support
                    await Task.Run(() =>
                    {
                        while (!_shellProcess.HasExited)
                        {
                            if (_cancellationTokenSource.Token.IsCancellationRequested)
                            {
                                try
                                {
                                    _shellProcess.Kill();
                                }
                                catch { }
                                break;
                            }
                            Thread.Sleep(50);
                        }
                    });

                    _shellProcess.WaitForExit();
                }
            }
            catch (OperationCanceledException)
            {
                WriteOutput("Command cancelled.");
            }
            catch (Exception ex)
            {
                WriteOutput($"Error executing command: {ex.Message}");
            }
            finally
            {
                _shellProcess = null;
                _isExecutingCommand = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                if (_input.InvokeRequired)
                {
                    _input.Invoke(new Action(() => {
                        _input.Enabled = true;
                        _input.Focus();
                    }));
                }
                else
                {
                    _input.Enabled = true;
                    _input.Focus();
                }

                WritePromptThreadSafe();
            }
        }

        private void HandleCdCommand(string command)
        {
            try
            {
                var parts = command.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 1)
                {
                    // Just "cd" - show current directory
                    WriteOutput(_currentDirectory);
                }
                else
                {
                    var targetPath = parts[1].Trim().Trim('"');
                    string newPath;

                    if (Path.IsPathRooted(targetPath))
                    {
                        newPath = targetPath;
                    }
                    else if (targetPath == "..")
                    {
                        newPath = Directory.GetParent(_currentDirectory)?.FullName ?? _currentDirectory;
                    }
                    else if (targetPath == "~" || targetPath == "%USERPROFILE%")
                    {
                        newPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    }
                    else
                    {
                        newPath = Path.Combine(_currentDirectory, targetPath);
                    }

                    if (Directory.Exists(newPath))
                    {
                        _currentDirectory = Path.GetFullPath(newPath);
                    }
                    else
                    {
                        WriteOutput($"The system cannot find the path specified: {targetPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                WriteOutput($"Error: {ex.Message}");
            }
            finally
            {
                _isExecutingCommand = false;
                _input.Enabled = true;
                _input.Focus();
                WritePrompt();
            }
        }

        private void WriteOutputThreadSafe(string text, bool isError = false)
        {
            if (_output.InvokeRequired)
            {
                _output.Invoke(new Action<string, bool>(WriteOutputThreadSafe), text, isError);
            }
            else
            {
                if (isError)
                {
                    // Store original color
                    var originalColor = _output.SelectionColor;
                    _output.SelectionStart = _output.TextLength;
                    _output.SelectionLength = 0;
                    _output.SelectionColor = Color.FromArgb(255, 100, 100); // Red for errors
                    _output.AppendText(text + "\n");
                    _output.SelectionColor = originalColor;
                }
                else
                {
                    _output.AppendText(text + "\n");
                }
                _output.ScrollToCaret();
            }
        }

        private void WritePromptThreadSafe()
        {
            if (_output.InvokeRequired)
            {
                _output.Invoke(new Action(WritePromptThreadSafe));
            }
            else
            {
                WritePrompt();
            }
        }

        private void ShowHelp()
        {
            WriteOutput("\nLi'nage Terminal Help\n");
            WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            WriteOutput("This terminal supports both shell commands and Li'nage commands.");
            WriteOutput("");
            WriteOutput("SHELL COMMANDS:");
            WriteOutput("  Any standard Windows command works: dir, type, mkdir, etc.");
            WriteOutput("  Use 'cd <path>' to change directories.");
            WriteOutput("  Press Ctrl+C to cancel a running command.");
            WriteOutput("");
            WriteOutput("LI'NAGE COMMANDS (prefix with 'ln-'):");
            WriteOutput("  ln-init            Initialize a new Li'nage project");
            WriteOutput("  ln-status          Show repository status");
            WriteOutput("  ln-add [file]      Stage files for commit");
            WriteOutput("  ln-commit <msg>    Create a new commit");
            WriteOutput("  ln-log             Show commit history");
            WriteOutput("  ln-branch [name]   List/create branches");
            WriteOutput("  ln-switch <branch> Switch to a branch");
            WriteOutput("  ln-merge <branch>  Merge a branch");
            WriteOutput("  ln-rebase <branch> Rebase onto a branch");
            WriteOutput("  ln-diff            Show differences");
            WriteOutput("  ln-sync            Sync with remote");
            WriteOutput("  ln-push            Push to remote");
            WriteOutput("  ln-pull            Pull from remote");
            WriteOutput("  ln-remote          Manage remotes");
            WriteOutput("  ln-blame <file>    Track line authorship");
            WriteOutput("  ln-stash           Stash changes");
            WriteOutput("  ln-reset <target>  Reset to a commit");
            WriteOutput("  ln-config          User settings");
            WriteOutput("");
            WriteOutput("UTILITY:");
            WriteOutput("  clear / cls        Clear terminal");
            WriteOutput("  help               Show this message");
            WriteOutput("  exit               Close terminal");
            WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
        }

        private void HandleInit(string[] args)
        {
            if (VersionController == null)
            {
                WriteOutput("Error: VersionController not initialized");
                return;
            }

            WriteOutput("Initializing Li'nage project...");

            try
            {
                // Check if we have a working directory
                if (string.IsNullOrEmpty(_currentDirectory))
                {
                    WriteOutput("Error: No working directory set. Open a folder first.");
                    return;
                }

                // The project loads via LoadProjectAsync, called from MainWindow
                WriteOutput($"Project directory: {_currentDirectory}");
                WriteOutput("Use File > Open Folder to initialize a project.");
                WriteOutput("Run 'ln-config' to set up your user settings.");
            }
            catch (Exception ex)
            {
                WriteOutput($"Error: {ex.Message}");
            }
        }

        private void HandleStatus(string[] args)
        {
            if (VersionController == null)
            {
                WriteOutput("Error: No project loaded. Use File > Open Folder first.");
                return;
            }

            try
            {
                var graphService = VersionController.GraphService;
                var changeDetector = VersionController.ChangeDetector;

                WriteOutput("\nRepository Status:");
                WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                // Current branch
                var currentBranch = graphService?.GetCurrentBranch();
                if (currentBranch != null)
                {
                    WriteOutput($"Branch: {currentBranch.BranchName}");

                    var headCommit = currentBranch.HeadCommit;
                    if (headCommit != null)
                    {
                        var shortHash = headCommit.CommitHash?.Length >= 7
                            ? headCommit.CommitHash.Substring(0, 7)
                            : headCommit.CommitHash ?? "unknown";
                        WriteOutput($"HEAD: {shortHash} - {headCommit.Message}");
                    }
                    else
                    {
                        WriteOutput("HEAD: No commits yet");
                    }
                }
                else
                {
                    WriteOutput("Branch: (no branch)");
                }

                // Changes
                if (changeDetector != null)
                {
                    var changes = changeDetector.GetChanges();
                    if (changes.Count > 0)
                    {
                        WriteOutput("");
                        WriteOutput($"Changes ({changes.Count} files):");

                        var newFiles = changes.Where(c => c.Value == "NEW").ToList();
                        var modifiedFiles = changes.Where(c => c.Value == "MODIFIED").ToList();
                        var deletedFiles = changes.Where(c => c.Value == "DELETED").ToList();

                        if (newFiles.Count > 0)
                        {
                            WriteOutput($"  New files: {newFiles.Count}");
                            foreach (var f in newFiles.Take(10))
                                WriteOutput($"    + {f.Key}");
                            if (newFiles.Count > 10)
                                WriteOutput($"    ... and {newFiles.Count - 10} more");
                        }

                        if (modifiedFiles.Count > 0)
                        {
                            WriteOutput($"  Modified: {modifiedFiles.Count}");
                            foreach (var f in modifiedFiles.Take(10))
                                WriteOutput($"    M {f.Key}");
                            if (modifiedFiles.Count > 10)
                                WriteOutput($"    ... and {modifiedFiles.Count - 10} more");
                        }

                        if (deletedFiles.Count > 0)
                        {
                            WriteOutput($"  Deleted: {deletedFiles.Count}");
                            foreach (var f in deletedFiles.Take(10))
                                WriteOutput($"    - {f.Key}");
                            if (deletedFiles.Count > 10)
                                WriteOutput($"    ... and {deletedFiles.Count - 10} more");
                        }
                    }
                    else
                    {
                        WriteOutput("\nNo changes detected (working tree clean)");
                    }
                }

                WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            }
            catch (Exception ex)
            {
                WriteOutput($"Error: {ex.Message}");
            }
        }

        private void HandleAdd(string[] args)
        {
            if (VersionController == null)
            {
                WriteOutput("Error: No project loaded. Use File > Open Folder first.");
                return;
            }

            try
            {
                var changeDetector = VersionController.ChangeDetector;
                if (changeDetector == null)
                {
                    WriteOutput("Error: Change detector not initialized");
                    return;
                }

                var changes = changeDetector.GetChanges();

                if (args.Length == 0)
                {
                    // Stage all changes
                    if (changes.Count == 0)
                    {
                        WriteOutput("No changes to stage.");
                        return;
                    }
                    WriteOutput($"Staged {changes.Count} file(s) for commit.");
                    WriteOutput("Use 'ln-commit <message>' to commit these changes.");
                }
                else
                {
                    // Stage specific files
                    int staged = 0;
                    foreach (var arg in args)
                    {
                        var normalizedArg = arg.Replace('\\', '/');
                        var match = changes.Keys.FirstOrDefault(k =>
                            k.Equals(normalizedArg, StringComparison.OrdinalIgnoreCase) ||
                            k.EndsWith("/" + normalizedArg, StringComparison.OrdinalIgnoreCase) ||
                            k.EndsWith(normalizedArg, StringComparison.OrdinalIgnoreCase));

                        if (match != null)
                        {
                            WriteOutput($"  Staged: {match}");
                            staged++;
                        }
                        else
                        {
                            WriteOutput($"  Not found: {arg}");
                        }
                    }
                    WriteOutput($"\n{staged} file(s) staged for commit.");
                }
            }
            catch (Exception ex)
            {
                WriteOutput($"Error: {ex.Message}");
            }
        }

        private async void HandleCommit(string[] args)
        {
            if (VersionController == null)
            {
                WriteOutput("Error: No project loaded. Use File > Open Folder first.");
                return;
            }

            if (args.Length == 0)
            {
                WriteOutput("Error: Please provide a commit message");
                WriteOutput("Usage: ln-commit <message>");
                return;
            }

            try
            {
                var changeDetector = VersionController.ChangeDetector;
                if (changeDetector == null)
                {
                    WriteOutput("Error: Change detector not initialized");
                    return;
                }

                var changes = changeDetector.GetChanges();
                if (changes.Count == 0)
                {
                    WriteOutput("Nothing to commit (working tree clean)");
                    return;
                }

                var message = string.Join(" ", args);
                var filesToCommit = changes.Keys.ToList();

                WriteOutput($"Committing {filesToCommit.Count} file(s)...");

                await VersionController.CreateCommitAsync(message, filesToCommit);

                var branch = VersionController.GraphService?.GetCurrentBranch();
                var headCommit = branch?.HeadCommit;

                if (headCommit != null)
                {
                    var shortHash = headCommit.CommitHash?.Length >= 7
                        ? headCommit.CommitHash.Substring(0, 7)
                        : headCommit.CommitHash ?? "unknown";
                    WriteOutput($"[{branch?.BranchName}] {shortHash} - {message}");
                    WriteOutput($" {filesToCommit.Count} file(s) changed");
                }

                WriteOutput("Commit complete.");
            }
            catch (Exception ex)
            {
                WriteOutput($"Error: {ex.Message}");
            }
        }

        private void HandleLog(string[] args)
        {
            if (VersionController == null)
            {
                WriteOutput("Error: No project loaded. Use File > Open Folder first.");
                return;
            }

            try
            {
                var graphService = VersionController.GraphService;
                var history = graphService?.GetCommitHistory();

                if (history == null || history.Count == 0)
                {
                    WriteOutput("No commits yet.");
                    return;
                }

                int limit = 20;
                if (args.Length > 0 && args[0] == "-n" && args.Length > 1)
                {
                    int.TryParse(args[1], out limit);
                }

                WriteOutput("\nCommit History:");
                WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                var currentBranch = graphService.GetCurrentBranch();
                var headCommitId = currentBranch?.HeadCommit?.CommitId;

                foreach (var commit in history.Take(limit))
                {
                    var shortHash = commit.CommitHash?.Length >= 7
                        ? commit.CommitHash.Substring(0, 7)
                        : commit.CommitHash ?? "unknown";

                    var headMarker = commit.CommitId == headCommitId
                        ? $" (HEAD -> {currentBranch.BranchName})"
                        : "";

                    WriteOutput($"* {shortHash}{headMarker}");
                    WriteOutput($"  {commit.Message}");
                    WriteOutput($"  {commit.AuthorName} - {commit.Timestamp:yyyy-MM-dd HH:mm}");
                    WriteOutput("");
                }

                if (history.Count > limit)
                {
                    WriteOutput($"... and {history.Count - limit} more commits");
                }

                WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            }
            catch (Exception ex)
            {
                WriteOutput($"Error: {ex.Message}");
            }
        }

        private async void HandleBranch(string[] args)
        {
            if (VersionController == null)
            {
                WriteOutput("Error: No project loaded. Use File > Open Folder first.");
                return;
            }

            try
            {
                var graphService = VersionController.GraphService;

                if (args.Length == 0)
                {
                    // List branches
                    var branches = await graphService.GetAllBranchesAsync();
                    var currentBranch = graphService.GetCurrentBranch();

                    WriteOutput("\nBranches:");
                    WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                    if (branches == null || branches.Count == 0)
                    {
                        WriteOutput("No branches yet. Create a commit first.");
                    }
                    else
                    {
                        foreach (var branch in branches)
                        {
                            var marker = branch.BranchName == currentBranch?.BranchName ? "* " : "  ";
                            WriteOutput($"{marker}{branch.BranchName}");
                        }
                    }

                    WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                }
                else if (args[0] == "-d" && args.Length > 1)
                {
                    // Delete branch
                    var branchName = args[1];
                    await graphService.DeleteBranchAsync(branchName);
                    WriteOutput($"Deleted branch: {branchName}");
                }
                else
                {
                    // Create branch
                    var branchName = args[0];
                    await graphService.CreateBranchAsync(branchName);
                    WriteOutput($"Created branch: {branchName}");
                    WriteOutput($"Use 'ln-switch {branchName}' to switch to it.");
                }
            }
            catch (Exception ex)
            {
                WriteOutput($"Error: {ex.Message}");
            }
        }

        private async void HandleSwitch(string[] args)
        {
            if (VersionController == null)
            {
                WriteOutput("Error: No project loaded. Use File > Open Folder first.");
                return;
            }

            if (args.Length == 0)
            {
                WriteOutput("Error: Specify a branch to switch to");
                WriteOutput("Usage: ln-switch <branch-name>");
                return;
            }

            try
            {
                var graphService = VersionController.GraphService;
                var branchName = args[0];

                await graphService.SwitchBranchAsync(branchName);
                WriteOutput($"Switched to branch '{branchName}'");
            }
            catch (Exception ex)
            {
                WriteOutput($"Error: {ex.Message}");
            }
        }

        private void HandleMerge(string[] args)
        {
            if (VersionController == null)
            {
                WriteOutput("Error: No project loaded. Use File > Open Folder first.");
                return;
            }

            if (args.Length == 0)
            {
                WriteOutput("Error: Specify a branch to merge");
                WriteOutput("Usage: ln-merge <branch-name>");
                return;
            }

            try
            {
                var graphService = VersionController.GraphService;
                var branchName = args[0];

                // Get the branch to merge
                var branchTask = graphService.GetBranchAsync(branchName);
                branchTask.Wait();
                var sourceBranch = branchTask.Result;

                if (sourceBranch == null)
                {
                    WriteOutput($"Error: Branch '{branchName}' not found.");
                    return;
                }

                var currentBranch = graphService.GetCurrentBranch();
                WriteOutput($"Merging '{branchName}' into '{currentBranch?.BranchName}'...");

                var conflicts = graphService.Merge(sourceBranch);

                if (conflicts == null || conflicts.Count == 0)
                {
                    WriteOutput("Merge completed successfully!");
                }
                else
                {
                    WriteOutput($"Merge completed with {conflicts.Count} conflict(s):");
                    foreach (var conflict in conflicts.Take(10))
                    {
                        WriteOutput($"  CONFLICT: {conflict.FilePath}");
                    }
                    if (conflicts.Count > 10)
                    {
                        WriteOutput($"  ... and {conflicts.Count - 10} more conflicts");
                    }
                    WriteOutput("\nResolve conflicts and commit the result.");
                }
            }
            catch (Exception ex)
            {
                WriteOutput($"Error: {ex.Message}");
            }
        }

        private async void HandleRebase(string[] args)
        {
            if (VersionController == null)
            {
                WriteOutput("Error: No project loaded. Use File > Open Folder first.");
                return;
            }

            if (args.Length == 0)
            {
                WriteOutput("Error: Specify a base branch");
                WriteOutput("Usage: ln-rebase <branch-name>");
                return;
            }

            try
            {
                var graphService = VersionController.GraphService;
                var branchName = args[0];

                var targetBranch = await graphService.GetBranchAsync(branchName);
                if (targetBranch == null)
                {
                    WriteOutput($"Error: Branch '{branchName}' not found.");
                    return;
                }

                if (targetBranch.HeadCommit == null)
                {
                    WriteOutput($"Error: Branch '{branchName}' has no commits.");
                    return;
                }

                WriteOutput($"Rebasing onto '{branchName}'...");
                await graphService.RebaseAsync(targetBranch.HeadCommit);
                WriteOutput("Rebase completed!");
            }
            catch (Exception ex)
            {
                WriteOutput($"Error: {ex.Message}");
            }
        }

        private void HandleDiff(string[] args)
        {
            if (VersionController == null)
            {
                WriteOutput("Error: No project loaded. Use File > Open Folder first.");
                return;
            }

            try
            {
                var changeDetector = VersionController.ChangeDetector;
                if (changeDetector == null)
                {
                    WriteOutput("Error: Change detector not initialized");
                    return;
                }

                var changes = changeDetector.GetChanges();

                if (changes.Count == 0)
                {
                    WriteOutput("No changes to show.");
                    return;
                }

                WriteOutput("\nChanges:");
                WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                foreach (var change in changes)
                {
                    string status;
                    switch (change.Value)
                    {
                        case "NEW":
                            status = "+";
                            break;
                        case "MODIFIED":
                            status = "M";
                            break;
                        case "DELETED":
                            status = "-";
                            break;
                        default:
                            status = "?";
                            break;
                    }
                    WriteOutput($"  {status} {change.Key}");
                }

                WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                WriteOutput($"\nTotal: {changes.Count} file(s) changed");
            }
            catch (Exception ex)
            {
                WriteOutput($"Error: {ex.Message}");
            }
        }

        private async void HandleSync(string[] args)
        {
            if (VersionController == null)
            {
                WriteOutput("Error: No project loaded. Use File > Open Folder first.");
                return;
            }

            try
            {
                var remoteName = args.Length > 0 ? args[0] : "origin";

                WriteOutput($"Syncing with '{remoteName}'...");

                WriteOutput("Pulling latest changes...");
                await VersionController.Pull(remoteName);

                WriteOutput("Pushing local commits...");
                await VersionController.Push(remoteName);

                WriteOutput("Sync completed!");
            }
            catch (Exception ex)
            {
                WriteOutput($"Error: {ex.Message}");
            }
        }

        private async void HandlePush(string[] args)
        {
            if (VersionController == null)
            {
                WriteOutput("Error: No project loaded. Use File > Open Folder first.");
                return;
            }

            try
            {
                var remoteName = args.Length > 0 ? args[0] : "origin";

                WriteOutput($"Pushing to '{remoteName}'...");
                await VersionController.Push(remoteName);
                WriteOutput("Push completed!");
            }
            catch (Exception ex)
            {
                WriteOutput($"Error: {ex.Message}");
            }
        }

        private async void HandlePull(string[] args)
        {
            if (VersionController == null)
            {
                WriteOutput("Error: No project loaded. Use File > Open Folder first.");
                return;
            }

            try
            {
                var remoteName = args.Length > 0 ? args[0] : "origin";

                WriteOutput($"Pulling from '{remoteName}'...");
                await VersionController.Pull(remoteName);
                WriteOutput("Pull completed!");
            }
            catch (Exception ex)
            {
                WriteOutput($"Error: {ex.Message}");
            }
        }

        private async void HandleRemote(string[] args)
        {
            if (VersionController == null)
            {
                WriteOutput("Error: No project loaded. Use File > Open Folder first.");
                return;
            }

            try
            {
                var remoteService = VersionController.RemoteService;

                if (args.Length == 0)
                {
                    // List remotes
                    var remotes = await remoteService.GetAllRemotesAsync();

                    WriteOutput("\nRemote Repositories:");
                    WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                    if (remotes == null || remotes.Count == 0)
                    {
                        WriteOutput("No remotes configured.");
                        WriteOutput("Use: ln-remote add <name> <url>");
                    }
                    else
                    {
                        foreach (var remote in remotes)
                        {
                            WriteOutput($"  {remote.RemoteName}  {remote.RemoteUrl}");
                        }
                    }

                    WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                }
                else if (args[0] == "add" && args.Length >= 3)
                {
                    var name = args[1];
                    var url = args[2];
                    await remoteService.AddRemoteAsync(name, url);
                    WriteOutput($"Added remote '{name}' -> {url}");
                }
                else if (args[0] == "remove" && args.Length >= 2)
                {
                    var name = args[1];
                    await remoteService.RemoveRemoteAsync(name);
                    WriteOutput($"Removed remote '{name}'");
                }
                else
                {
                    WriteOutput("Usage:");
                    WriteOutput("  ln-remote              List all remotes");
                    WriteOutput("  ln-remote add <name> <url>   Add a remote");
                    WriteOutput("  ln-remote remove <name>      Remove a remote");
                }
            }
            catch (Exception ex)
            {
                WriteOutput($"Error: {ex.Message}");
            }
        }

        private async void HandleBlame(string[] args)
        {
            if (VersionController == null)
            {
                WriteOutput("Error: No project loaded. Use File > Open Folder first.");
                return;
            }

            if (args.Length == 0)
            {
                WriteOutput("Error: Specify a file to blame");
                WriteOutput("Usage: ln-blame <file-path>");
                return;
            }

            try
            {
                var graphService = VersionController.GraphService;
                var filePath = args[0].Replace('\\', '/');

                var blameInfo = await graphService.GetFileBlameAsync(filePath);

                if (blameInfo == null || blameInfo.Count == 0)
                {
                    WriteOutput($"No blame information available for: {filePath}");
                    return;
                }

                WriteOutput($"\nLine Authorship - {filePath}:");
                WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

                foreach (var lineChange in blameInfo.Take(50))
                {
                    if (lineChange.CommitId.HasValue)
                    {
                        var commit = graphService.GetCommitById(lineChange.CommitId.Value);
                        var shortHash = commit?.CommitHash?.Substring(0, 7) ?? "unknown";
                        var author = commit?.AuthorName ?? "unknown";
                        WriteOutput($"{shortHash} | {author,-12} | Line {lineChange.LineNumber}");
                    }
                    else
                    {
                        WriteOutput($"unknown | unknown      | Line {lineChange.LineNumber}");
                    }
                }

                if (blameInfo.Count > 50)
                {
                    WriteOutput($"... and {blameInfo.Count - 50} more lines");
                }

                WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            }
            catch (Exception ex)
            {
                WriteOutput($"Error: {ex.Message}");
            }
        }

        private void HandleStash(string[] args)
        {
            WriteOutput("Stash functionality is not yet implemented.");
            WriteOutput("Use 'ln-commit' to save your changes instead.");
        }

        private void HandleReset(string[] args)
        {
            if (args.Length == 0)
            {
                WriteOutput("Error: Specify what to reset");
                WriteOutput("Usage: ln-reset <commit-hash>");
                return;
            }

            WriteOutput("Reset functionality is not yet fully implemented.");
            WriteOutput("This will be available in a future version.");
        }

        private void HandleConfig(string[] args)
        {
            WriteOutput("Li'nage Configuration:");
            WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            WriteOutput($"user.name: {Environment.UserName}");
            WriteOutput($"user.machine: {Environment.MachineName}");

            if (VersionController != null)
            {
                WriteOutput($"status: {VersionController.GetStatus()}");
            }

            WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        }

        public void WriteOutput(string text)
        {
            if (_output.InvokeRequired)
            {
                _output.Invoke(new Action<string>(WriteOutput), text);
            }
            else
            {
                _output.AppendText(text + "\n");
                _output.ScrollToCaret();
            }
        }
    }
}