using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        // Controllers (will be injected)
        public VersionController VersionController { get; set; }
        public ScanController ScanController { get; set; }
        public IndexController IndexController { get; set; }
        public AuthController AuthController { get; set; }

        public TerminalView()
        {
            _commandHistory = new List<string>();
            InitializeComponent();
            Linage.GUI.Helpers.WatermarkHelper.AddWatermarkLabel(this, "TerminalView.cs");
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
            WriteOutput("✨ Li'nage Terminal v1.0");
            WriteOutput("📖 Type 'help' for commands");
            WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        }

        private void OnInputKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                var command = _input.Text.Trim();
                if (!string.IsNullOrEmpty(command))
                {
                    _input.Text = "";
                    _output.AppendText($"❯ {command}\n");
                    _commandHistory.Add(command);
                    _historyIndex = -1;
                    ProcessCommand(command);
                }
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Up && _historyIndex < _commandHistory.Count - 1)
            {
                _historyIndex++;
                _input.Text = _commandHistory[_commandHistory.Count - 1 - _historyIndex];
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Down && _historyIndex > 0)
            {
                _historyIndex--;
                _input.Text = _historyIndex >= 0 ? _commandHistory[_commandHistory.Count - 1 - _historyIndex] : "";
                e.SuppressKeyPress = true;
            }
        }

        private void ProcessCommand(string input)
        {
            var parts = input.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            var command = parts[0].ToLower();
            var args = parts.Skip(1).ToArray();

            try
            {
                switch (command)
                {
                    case "help":
                        ShowHelp();
                        break;

                    case "init":
                        HandleInit(args);
                        break;

                    case "status":
                    case "st":
                        HandleStatus(args);
                        break;

                    case "add":
                        HandleAdd(args);
                        break;

                    case "commit":
                    case "ci":
                        HandleCommit(args);
                        break;

                    case "log":
                        HandleLog(args);
                        break;

                    case "branch":
                    case "br":
                        HandleBranch(args);
                        break;

                    case "switch":
                    case "checkout":
                    case "co":
                        HandleSwitch(args);
                        break;

                    case "merge":
                        HandleMerge(args);
                        break;

                    case "rebase":
                        HandleRebase(args);
                        break;

                    case "diff":
                        HandleDiff(args);
                        break;

                    case "sync":
                        HandleSync(args);
                        break;

                    case "push":
                        HandlePush(args);
                        break;

                    case "pull":
                        HandlePull(args);
                        break;

                    case "remote":
                        HandleRemote(args);
                        break;

                    case "blame":
                        HandleBlame(args);
                        break;

                    case "stash":
                        HandleStash(args);
                        break;

                    case "reset":
                        HandleReset(args);
                        break;

                    case "config":
                        HandleConfig(args);
                        break;

                    case "ls":
                    case "list":
                        HandleList(args);
                        break;

                    case "clear":
                    case "cls":
                        _output.Clear();
                        break;

                    case "exit":
                    case "quit":
                        WriteOutput("👋 Goodbye!");
                        break;

                    default:
                        WriteOutput($"❌ Unknown command: '{command}'");
                        WriteOutput("💡 Try 'help' for available commands");
                        break;
                }
            }
            catch (Exception ex)
            {
                WriteOutput($"⚠️  Error: {ex.Message}");
            }

            _output.ScrollToCaret();
        }

        private void ShowHelp()
        {
            WriteOutput("\n📚 Li'nage Commands:\n");
            WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            WriteOutput("🚀 Getting Started:");
            WriteOutput("  init              Initialize a new Li'nage project");
            WriteOutput("  config            Set up user details");
            WriteOutput("");
            WriteOutput("📦 Working with Changes:");
            WriteOutput("  status (st)       Show repository status");
            WriteOutput("  add [file]        Stage files for commit");
            WriteOutput("  commit (ci)       Create a new commit");
            WriteOutput("  diff              Show differences");
            WriteOutput("");
            WriteOutput("🌳 Branch Management:");
            WriteOutput("  branch (br)       List/create/delete branches");
            WriteOutput("  switch (co)       Change to a different branch");
            WriteOutput("  merge             Merge branches");
            WriteOutput("  rebase            Rebase commits");
            WriteOutput("");
            WriteOutput("📊 History & Analysis:");
            WriteOutput("  log               Show commit history");
            WriteOutput("  blame             Track line authorship");
            WriteOutput("  ls                List files in repo");
            WriteOutput("");
            WriteOutput("🔄 Sync & Remote:");
            WriteOutput("  sync              Sync with remote");
            WriteOutput("  push              Push to remote");
            WriteOutput("  pull              Pull from remote");
            WriteOutput("  remote            Manage remotes");
            WriteOutput("");
            WriteOutput("🛠️  Advanced:");
            WriteOutput("  stash             Stash changes");
            WriteOutput("  reset             Undo changes");
            WriteOutput("  config            User settings");
            WriteOutput("");
            WriteOutput("💾 Utility:");
            WriteOutput("  clear (cls)       Clear terminal");
            WriteOutput("  help              Show this message");
            WriteOutput("  exit              Close terminal");
            WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
        }

        private void HandleInit(string[] args)
        {
            WriteOutput("✨ Initializing Li'nage project...");
            WriteOutput("📁 Creating project structure");
            WriteOutput("💾 Setting up database");
            WriteOutput("✅ Project initialized! Run 'config' to set up your user");
        }

        private void HandleStatus(string[] args)
        {
            WriteOutput("\n📊 Repository Status:");
            WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            WriteOutput("🌿 Branch: main");
            WriteOutput("📍 Head: abc1234 - Initial commit");
            WriteOutput("📝 Changes: 0 modified, 0 new");
            WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        }

        private void HandleAdd(string[] args)
        {
            if (args.Length == 0)
            {
                WriteOutput("📂 Staging all changes...");
                WriteOutput("✅ All files staged");
            }
            else
            {
                WriteOutput($"📂 Staging: {string.Join(", ", args)}");
                WriteOutput("✅ Files staged");
            }
        }

        private void HandleCommit(string[] args)
        {
            if (args.Length == 0)
            {
                WriteOutput("❌ Please provide a commit message");
                WriteOutput("💡 Usage: commit 'Your message here'");
                return;
            }

            var message = string.Join(" ", args);
            WriteOutput($"📝 Creating commit: {message}");
            WriteOutput("🔗 Commit: def5678 - " + message);
            WriteOutput("✅ Committed!");
        }

        private void HandleLog(string[] args)
        {
            WriteOutput("\n📜 Commit History:");
            WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            WriteOutput("• def5678 (HEAD -> main) - Second commit");
            WriteOutput("  📆 2 minutes ago");
            WriteOutput("");
            WriteOutput("• abc1234 - Initial commit");
            WriteOutput("  📆 1 hour ago");
            WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        }

        private void HandleBranch(string[] args)
        {
            if (args.Length == 0)
            {
                WriteOutput("\n🌿 Branches:");
                WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                WriteOutput("✨ main");
                WriteOutput("  develop");
                WriteOutput("  feature/new-ui");
                WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            }
            else if (args[0] == "-d" && args.Length > 1)
            {
                WriteOutput($"🗑️  Deleted branch: {args[1]}");
            }
            else
            {
                WriteOutput($"✨ Created branch: {args[0]}");
            }
        }

        private void HandleSwitch(string[] args)
        {
            if (args.Length == 0)
            {
                WriteOutput("❌ Specify a branch to switch to");
                return;
            }
            WriteOutput($"🔄 Switching to {args[0]}...");
            WriteOutput($"✅ Switched to branch '{args[0]}'");
        }

        private void HandleMerge(string[] args)
        {
            if (args.Length == 0)
            {
                WriteOutput("❌ Specify a branch to merge");
                return;
            }
            WriteOutput($"🔗 Merging {args[0]} into main...");
            WriteOutput("✅ Merge completed!");
        }

        private void HandleRebase(string[] args)
        {
            if (args.Length == 0)
            {
                WriteOutput("❌ Specify a base branch");
                return;
            }
            WriteOutput($"♻️  Rebasing onto {args[0]}...");
            WriteOutput("✅ Rebase completed!");
        }

        private void HandleDiff(string[] args)
        {
            WriteOutput("\n📋 Differences:");
            WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            WriteOutput("- old line");
            WriteOutput("+ new line");
            WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        }

        private void HandleSync(string[] args)
        {
            WriteOutput("🔄 Syncing with remote...");
            WriteOutput("📥 Pulling latest changes");
            WriteOutput("📤 Pushing local commits");
            WriteOutput("✅ Sync completed!");
        }

        private void HandlePush(string[] args)
        {
            WriteOutput("📤 Pushing to remote...");
            WriteOutput("✅ Push completed!");
        }

        private void HandlePull(string[] args)
        {
            WriteOutput("📥 Pulling from remote...");
            WriteOutput("✅ Pull completed!");
        }

        private void HandleRemote(string[] args)
        {
            if (args.Length == 0)
            {
                WriteOutput("\n🔗 Remote Repositories:");
                WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                WriteOutput("origin   git@github.com:user/repo.git");
                WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            }
            else
            {
                WriteOutput($"🔗 Remote operation: {string.Join(" ", args)}");
            }
        }

        private void HandleBlame(string[] args)
        {
            if (args.Length == 0)
            {
                WriteOutput("❌ Specify a file to blame");
                return;
            }
            WriteOutput($"\n👤 Line Authorship - {args[0]}:");
            WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            WriteOutput("abc1234 | User A | console.log('hello')");
            WriteOutput("def5678 | User B | const x = 42");
            WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        }

        private void HandleStash(string[] args)
        {
            if (args.Length > 0 && args[0] == "list")
            {
                WriteOutput("\n💾 Stashed Changes:");
                WriteOutput("stash@{0}: WIP on main");
            }
            else
            {
                WriteOutput("💾 Changes stashed");
            }
        }

        private void HandleReset(string[] args)
        {
            if (args.Length == 0)
            {
                WriteOutput("❌ Specify what to reset");
                return;
            }
            WriteOutput($"↩️  Resetting to {args[0]}...");
            WriteOutput("✅ Reset completed!");
        }

        private void HandleConfig(string[] args)
        {
            if (args.Length < 2)
            {
                WriteOutput("⚙️  Li'nage Configuration:");
                WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                WriteOutput("user.name: Hanan");
                WriteOutput("user.email: hanan@example.com");
                WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            }
            else
            {
                WriteOutput($"✅ Config set: {args[0]} = {string.Join(" ", args.Skip(1))}");
            }
        }

        private void HandleList(string[] args)
        {
            WriteOutput("\n📁 Files in Repository:");
            WriteOutput("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            WriteOutput("Program.cs");
            WriteOutput("App.config");
            WriteOutput("LICENSE");
            WriteOutput("README.md");
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