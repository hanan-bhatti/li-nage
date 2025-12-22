using System;

namespace Linage.Infrastructure
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
