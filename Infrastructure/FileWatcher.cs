using System;
using System.IO;

namespace Linage.Infrastructure
{
    /// <summary>
    /// Monitors file system for changes using Windows file system APIs.
    /// Spec: 2.2.4, 5.5 (Integration)
    /// </summary>
    public class FileWatcher : IDisposable
    {
        private FileSystemWatcher _watcher;
        private bool _disposed;
        private readonly GitignoreParser _ignoreParser;

        public event EventHandler<FileChangeEvent> OnFileChanged;

        public string WatchPath { get; private set; }
        public bool Recursive { get; private set; }

        public FileWatcher(string path, bool recursive = true)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
            if (!Directory.Exists(path)) throw new DirectoryNotFoundException($"Directory not found: {path}");

            WatchPath = path;
            Recursive = recursive;

            _ignoreParser = new GitignoreParser(path);
            _ignoreParser.LoadDefaultPatterns();
            
            string gitignorePath = Path.Combine(path, ".gitignore");
            if (File.Exists(gitignorePath))
            {
                _ignoreParser.LoadFromFile(gitignorePath);
            }

            InitializeWatcher();
        }

        private void InitializeWatcher()
        {
            _watcher = new FileSystemWatcher(WatchPath)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size,
                IncludeSubdirectories = Recursive,
                Filter = "*.*" // Watch all files
            };

            _watcher.Changed += OnFileSystemEvent;
            _watcher.Created += OnFileSystemEvent;
            _watcher.Deleted += OnFileSystemEvent;
            _watcher.Renamed += OnRenamedEvent;
        }

        public void Start()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = true;
            }
        }

        public void Stop()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
            }
        }

        private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
        {
            bool isDir = Directory.Exists(e.FullPath);
            if (ShouldIgnore(e.FullPath, isDir)) return;

            TriggerChange(new FileChangeEvent
            {
                Path = e.FullPath,
                EventType = e.ChangeType.ToString().ToUpperInvariant(), // CREATED, DELETED, CHANGED
                Timestamp = DateTime.Now
            });
        }

        private void OnRenamedEvent(object sender, RenamedEventArgs e)
        {
            bool isDir = Directory.Exists(e.FullPath);
            if (ShouldIgnore(e.FullPath, isDir)) return;

            TriggerChange(new FileChangeEvent
            {
                Path = e.FullPath,
                EventType = "RENAMED",
                Timestamp = DateTime.Now
            });
        }

        private bool ShouldIgnore(string path, bool isDirectory)
        {
            // Explicitly ignore .git and .linage directories
            if (path.IndexOf(".git", StringComparison.OrdinalIgnoreCase) >= 0 || 
                path.IndexOf(".linage", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            // Use the robust GitignoreParser
            return _ignoreParser.IsIgnored(path, isDirectory);
        }

        protected virtual void TriggerChange(FileChangeEvent e)
        {
            OnFileChanged?.Invoke(this, e);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_watcher != null)
                    {
                        _watcher.EnableRaisingEvents = false;
                        _watcher.Changed -= OnFileSystemEvent;
                        _watcher.Created -= OnFileSystemEvent;
                        _watcher.Deleted -= OnFileSystemEvent;
                        _watcher.Renamed -= OnRenamedEvent;
                        _watcher.Dispose();
                        _watcher = null;
                    }
                }
                _disposed = true;
            }
        }
    }
}
