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
using Linage.GUI.Dialogs;

namespace Linage.GUI
{
    public class TerminalView : UserControl, IThemable
    {
        // Event to request main window to load a project
        public event Action<string> OnProjectLoadRequested;

        public void ApplyTheme()
        {
            this.BackColor = ModernTheme.BackColor;
            if (_output != null)
            {
                _output.BackColor = ModernTheme.BackColor;
                _output.ForeColor = ModernTheme.TextPrimary;
                _output.Font = ModernTheme.FontCode;
            }
        }

        private RichTextBox _output;
        private List<string> _commandHistory;
        private int _historyIndex = -1;
        private int _promptStart = 0;

        // Shell process
        private Process _shellProcess;
        private string _currentDirectory;
        private bool _isExecutingCommand;
        private CancellationTokenSource _cancellationTokenSource;

        // Controllers
        public VersionController VersionController { get; set; }
        public ScanController ScanController { get; set; }
        public IndexController IndexController { get; set; }
        public AuthController AuthController { get; set; }

        public TerminalView()
        {
            _commandHistory = new List<string>();
            _currentDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.BackColor = ModernTheme.BackColor;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(5);

            _output = new RichTextBox();
            _output.Dock = DockStyle.Fill;
            _output.BackColor = ModernTheme.BackColor;
            _output.ForeColor = ModernTheme.TextPrimary;
            _output.Font = ModernTheme.FontCode;
            _output.BorderStyle = BorderStyle.None;
            _output.ReadOnly = false; // Allow input
            _output.DetectUrls = false; // Prevent auto-linking for cleaner look
            _output.ShortcutsEnabled = true;

            _output.KeyDown += OnOutputKeyDown;
            _output.KeyPress += OnOutputKeyPress;

            this.Controls.Add(_output);

            // Welcome message
            WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", false);
            WriteOutput("Li'nage Terminal v1.1", false);
            WriteOutput("Type 'help' for commands.", false);
            WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", false);
            WritePrompt();
        }

        private void WritePrompt()
        {
            if (_output.InvokeRequired)
            {
                _output.Invoke(new Action(WritePrompt));
                return;
            }

            _output.AppendText($"\n{_currentDirectory}> ");
            _output.SelectionStart = _output.TextLength;
            _output.ScrollToCaret();
            _promptStart = _output.TextLength;
            _output.ReadOnly = false;
        }

        public void SetWorkingDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                _currentDirectory = path;
                _output.AppendText("\n"); 
                WritePrompt();
            }
        }

        private void OnOutputKeyDown(object sender, KeyEventArgs e)
        {
            if (_isExecutingCommand && e.KeyCode == Keys.C && e.Control)
            {
                 if (_cancellationTokenSource != null)
                 {
                     _cancellationTokenSource.Cancel();
                     _output.AppendText("^C");
                 }
                 e.SuppressKeyPress = true;
                 return;
            }

            if (_isExecutingCommand)
            {
                e.SuppressKeyPress = true;
                return;
            }

            if (_output.SelectionStart < _promptStart && !IsNavigationKey(e.KeyCode))
            {
                _output.SelectionStart = _output.TextLength;
            }

            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                string command = _output.Text.Substring(_promptStart).Trim();
                _output.AppendText("\n");
                
                if (!string.IsNullOrEmpty(command))
                {
                    _commandHistory.Add(command);
                    _historyIndex = -1;
                    ProcessCommandAsync(command);
                }
                else
                {
                    WritePrompt();
                }
            }
            else if (e.KeyCode == Keys.Back)
            {
                if (_output.SelectionStart <= _promptStart)
                {
                    e.SuppressKeyPress = true;
                }
            }
            else if (e.KeyCode == Keys.Left)
            {
                if (_output.SelectionStart <= _promptStart)
                {
                    e.SuppressKeyPress = true;
                }
            }
            else if (e.KeyCode == Keys.Up)
            {
                e.SuppressKeyPress = true;
                if (_commandHistory.Count > 0)
                {
                    _historyIndex = (_historyIndex == -1) ? _commandHistory.Count - 1 : Math.Max(0, _historyIndex - 1);
                    ReplaceCurrentInput(_commandHistory[_historyIndex]);
                }
            }
            else if (e.KeyCode == Keys.Down)
            {
                e.SuppressKeyPress = true;
                if (_commandHistory.Count > 0 && _historyIndex != -1)
                {
                    _historyIndex = Math.Min(_commandHistory.Count - 1, _historyIndex + 1);
                     if (_historyIndex == _commandHistory.Count - 1)
                     {
                         ReplaceCurrentInput(_commandHistory[_historyIndex]);
                     }
                     else
                     {
                         ReplaceCurrentInput(_commandHistory[_historyIndex]);
                     }
                }
            }
            else if (e.KeyCode == Keys.Home)
            {
                 if (_output.SelectionStart != _promptStart)
                 {
                     e.SuppressKeyPress = true;
                     _output.SelectionStart = _promptStart;
                 }
            }
        }

        private void OnOutputKeyPress(object sender, KeyPressEventArgs e)
        {
             if (_output.SelectionStart < _promptStart)
             {
                 e.Handled = true; 
                 _output.SelectionStart = _output.TextLength;
             }
        }

        private bool IsNavigationKey(Keys key)
        {
            return key == Keys.Left || key == Keys.Right || key == Keys.Up || key == Keys.Down || key == Keys.Home || key == Keys.End;
        }

        private void ReplaceCurrentInput(string text)
        {
            _output.Select(_promptStart, _output.TextLength - _promptStart);
            _output.SelectedText = text;
        }

        private async void ProcessCommandAsync(string input)
        {
            _output.ReadOnly = true; 
            
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
                if (IsLinageCommand(command))
                {
                    await ProcessLinageCommand(command, args);
                    WritePrompt();
                }
                else
                {
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
                "ln-config", "clear", "cls", "exit", "quit"
            };
            return linageCommands.Contains(command);
        }

        private async Task ProcessLinageCommand(string command, string[] args)
        {
            // Run on thread pool but be careful with UI updates
            await Task.Run(async () => {
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
                         await HandleCommit(args);
                         break;
                     case "ln-log":
                         HandleLog(args);
                         break;
                     case "ln-branch":
                         await HandleBranch(args);
                         break;
                     case "ln-switch":
                         await HandleSwitch(args);
                         break;
                     case "ln-merge":
                         HandleMerge(args);
                         break;
                     case "ln-rebase":
                         await HandleRebase(args);
                         break;
                     case "ln-diff":
                         HandleDiff(args);
                         break;
                     case "ln-sync":
                         await HandleSync(args);
                         break;
                     case "ln-push":
                         await HandlePush(args);
                         break;
                     case "ln-pull":
                         await HandlePull(args);
                         break;
                     case "ln-remote":
                         await HandleRemote(args);
                         break;
                     case "ln-blame":
                         await HandleBlame(args);
                         break;
                     case "ln-config":
                         _output.Invoke(new Action(() => HandleConfig(args)));
                         break;
                     case "clear":
                     case "cls":
                         _output.Invoke(new Action(() => _output.Clear()));
                         break;
                     case "exit":
                     case "quit":
                         WriteOutput("Goodbye!");
                         break;
                     default:
                         WriteOutput($"Unknown Li'nage command: '{command}'");
                         break;
                 }
            });
        }

        private void WriteOutput(string text, bool isError = false)
        {
            if (_output.InvokeRequired)
            {
                _output.Invoke(new Action<string, bool>(WriteOutput), text, isError);
                return;
            }

            var originalColor = _output.SelectionColor;
            if (isError) _output.SelectionColor = ModernTheme.ErrorColor;
            _output.AppendText(text + "\n");
            _output.SelectionColor = originalColor;
            _output.ScrollToCaret();
        }

        private void ShowHelp()
        {
            WriteOutput("\nLi'nage Terminal Help\n");
            WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            WriteOutput("CORE COMMANDS");
            WriteOutput("  ln-init            Initialize a new Li'nage project here");
            WriteOutput("  ln-status          Show repository status");
            WriteOutput("  ln-add [file]      Stage files for commit");
            WriteOutput("  ln-commit <msg>    Create a new commit");
            WriteOutput("  ln-log             Show commit history");
            
            WriteOutput("\nBRANCHING");
            WriteOutput("  ln-branch [name]   List or create branches");
            WriteOutput("  ln-switch <branch> Switch to a branch");
            WriteOutput("  ln-merge <branch>  Merge a branch into current");

            WriteOutput("\nREMOTE");
            WriteOutput("  ln-remote          Manage remotes");
            WriteOutput("  ln-push [remote]   Push changes");
            WriteOutput("  ln-pull [remote]   Pull changes");
            WriteOutput("  ln-sync [remote]   Sync (pull + push)");

            WriteOutput("\nUTILITY");
            WriteOutput("  ln-diff            Show changes");
            WriteOutput("  ln-blame <file>    Show line authorship");
            WriteOutput("  ln-config          Configure user settings");
            WriteOutput("  clear / cls        Clear screen");
            WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
        }

        private async Task ExecuteShellCommandAsync(string command)
        {
            _isExecutingCommand = true;
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
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

                    _shellProcess.OutputDataReceived += (s, e) => { if (e.Data != null) WriteOutput(e.Data); };
                    _shellProcess.ErrorDataReceived += (s, e) => { if (e.Data != null) WriteOutput(e.Data, true); };

                    _shellProcess.Start();
                    _shellProcess.BeginOutputReadLine();
                    _shellProcess.BeginErrorReadLine();

                    await Task.Run(() => _shellProcess.WaitForExit());
                }
            }
            catch (Exception ex)
            {
                WriteOutput($"Error: {ex.Message}");
            }
            finally
            {
                _shellProcess = null;
                _isExecutingCommand = false;
                WritePrompt();
            }
        }

         private void HandleCdCommand(string command)
        {
            try
            {
                var parts = command.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1)
                {
                    var targetPath = parts[1].Trim().Trim('"');
                    string newPath = Path.IsPathRooted(targetPath) ? targetPath : Path.Combine(_currentDirectory, targetPath);
                    
                    if (targetPath == "..") 
                        newPath = Directory.GetParent(_currentDirectory)?.FullName ?? _currentDirectory;
                    else if (targetPath == "~")
                        newPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                    if (Directory.Exists(newPath))
                    {
                        _currentDirectory = Path.GetFullPath(newPath);
                    }
                    else
                    {
                        WriteOutput($"Path not found: {targetPath}", true);
                    }
                }
                else
                {
                     WriteOutput(_currentDirectory);
                }
            }
            catch (Exception ex)
            {
                WriteOutput($"Error: {ex.Message}", true);
            }
            finally
            {
                WritePrompt();
            }
        }

        // --- Li'nage Handlers ---

        private void HandleInit(string[] args)
        {
            try
            {
                WriteOutput($"Initializing Li'nage project in: {_currentDirectory}");
                
                string linageDir = Path.Combine(_currentDirectory, ".linage");
                if (!Directory.Exists(linageDir))
                {
                    Directory.CreateDirectory(linageDir);
                }

                if (OnProjectLoadRequested != null)
                {
                    OnProjectLoadRequested(_currentDirectory);
                    WriteOutput("Project initialized and loaded.");
                }
                else
                {
                    WriteOutput("Error: UI Context disconnected.");
                }
            }
            catch (Exception ex)
            {
                WriteOutput($"Error initializing: {ex.Message}");
            }
        }

        private void HandleStatus(string[] args)
        {
             if (VersionController == null) { WriteOutput("Error: No project loaded.", true); return; }
             var status = VersionController.GetStatus();
             WriteOutput(status);
        }

        private void HandleAdd(string[] args)
        {
            if (VersionController?.ChangeDetector == null) { WriteOutput("Error: Not initialized.", true); return; }
            WriteOutput("Staging changes... (Logic pending)");
        }

        private async Task HandleCommit(string[] args)
        {
            if (VersionController == null) { WriteOutput("Error: No project loaded.", true); return; }
            if (args.Length == 0) { WriteOutput("Usage: ln-commit <message>", true); return; }
            
            var message = string.Join(" ", args);
             var changes = VersionController.ChangeDetector?.GetChanges();
             if (changes == null || changes.Count == 0) { WriteOutput("Nothing to commit."); return; }
             
             await VersionController.CreateCommitAsync(message, changes.Keys.ToList());
             WriteOutput($"Committed: {message}");
        }

        private void HandleLog(string[] args)
        {
             if (VersionController?.GraphService == null) { WriteOutput("Error: No project loaded.", true); return; }
             var history = VersionController.GraphService.GetCommitHistory();
             foreach(var c in history.Take(10)) 
                WriteOutput($"{c.CommitHash.Substring(0,7)} - {c.Message}");
        }

        private async Task HandleBranch(string[] args)
        {
            if (VersionController?.GraphService == null) return;
            if (args.Length == 0)
            {
                var branches = await VersionController.GraphService.GetAllBranchesAsync();
                foreach(var b in branches) WriteOutput(b.BranchName);
            }
            else
            {
                 await VersionController.GraphService.CreateBranchAsync(args[0]);
                 WriteOutput($"Created branch {args[0]}");
            }
        }

        private async Task HandleSwitch(string[] args)
        {
             if (VersionController?.GraphService == null) { WriteOutput("Error: No project loaded.", true); return; }
             if (args.Length == 0) { WriteOutput("Usage: ln-switch <branch>", true); return; }
             
             await VersionController.GraphService.SwitchBranchAsync(args[0]);
             WriteOutput($"Switched to {args[0]}");
             
             // Refresh UI if possible
             HandleStatus(new string[0]);
        }
        
        private void HandleMerge(string[] args) { WriteOutput("Merge logic..."); }
        private async Task HandleRebase(string[] args) { WriteOutput("Rebase logic..."); }
        private void HandleDiff(string[] args) { WriteOutput("Diff logic..."); }
        
        private async Task HandleSync(string[] args)
        {
            if (VersionController == null) { WriteOutput("Error: No project context.", true); return; }
             string remote = args.Length > 0 ? args[0] : "origin";

            try
            {
                WriteOutput("Syncing...");
                await VersionController.Pull(remote);
                await VersionController.Push(remote);
                WriteOutput($"Sync with '{remote}' successful.");
            }
            catch (Exception ex) { WriteOutput($"Sync failed: {ex.Message}", true); }
        }

        private async Task HandlePush(string[] args)
        {
            if (VersionController == null) { WriteOutput("Error: No project context.", true); return; }
            string remote = args.Length > 0 ? args[0] : "origin";
            
            try 
            {
                await VersionController.Push(remote);
                WriteOutput($"Push to '{remote}' successful.");
            }
            catch (Exception ex) { WriteOutput($"Push failed: {ex.Message}", true); }
        }

        private async Task HandlePull(string[] args)
        {
            if (VersionController == null) { WriteOutput("Error: No project context.", true); return; }
            string remote = args.Length > 0 ? args[0] : "origin";

            try
            {
                await VersionController.Pull(remote);
                WriteOutput($"Pull from '{remote}' successful.");
            }
            catch (Exception ex) { WriteOutput($"Pull failed: {ex.Message}", true); }
        }
        
        private async Task HandleRemote(string[] args) 
        {
             WriteOutput("Remote management...");
        }

        private async Task HandleBlame(string[] args)
        {
             WriteOutput("Blame logic...");
        }

        private void HandleConfig(string[] args)
        {
            // Load current config
            var config = ConfigService.Load();

            using (var dialog = new ModernConfigDialog(config.UserName, config.UserEmail))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    ConfigService.Save(dialog.UserName, dialog.UserEmail);
                    WriteOutput($"Configuration saved.\nUser: {dialog.UserName}\nEmail: {dialog.UserEmail}");
                }
            }
            WritePrompt();
        }
    }
}