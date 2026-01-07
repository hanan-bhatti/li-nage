using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Linage.Infrastructure
{
    public class WorkspaceService
    {
        public class WorkspaceState
        {
            public List<string> OpenFiles { get; set; } = new List<string>();
            public string ActiveFile { get; set; }
        }

        private string _repoPath;

        public WorkspaceService(string repoPath)
        {
            _repoPath = repoPath;
        }

        private string GetStateFilePath()
        {
            return Path.Combine(_repoPath, ".linage", "workspace.json");
        }

        public void SaveState(List<string> openFiles, string activeFile)
        {
            try
            {
                var linageDir = Path.Combine(_repoPath, ".linage");
                if (!Directory.Exists(linageDir)) Directory.CreateDirectory(linageDir);

                var state = new WorkspaceState
                {
                    OpenFiles = openFiles,
                    ActiveFile = activeFile
                };

                string json = JsonConvert.SerializeObject(state, Formatting.Indented);
                File.WriteAllText(GetStateFilePath(), json);
            }
            catch { /* Ignore persistence errors */ }
        }

        public WorkspaceState LoadState()
        {
            try
            {
                var path = GetStateFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonConvert.DeserializeObject<WorkspaceState>(json);
                }
            }
            catch { }
            return new WorkspaceState();
        }
    }
}
