using System;
using System.Linq;
using System.Threading.Tasks;
using Linage.Core;

namespace Linage.Controllers
{
    public class SyncController
    {
        private readonly RemoteController _remoteController;

        public SyncController(RemoteController remoteController)
        {
            _remoteController = remoteController ?? throw new ArgumentNullException(nameof(remoteController));
        }

        public async Task Sync(Project project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            
            if (project.Remotes != null && project.Remotes.Any())
            {
                foreach(var remote in project.Remotes)
                {
                    try
                    {
                        // Pull first (Fetch + Merge)
                        await _remoteController.Pull(remote, project.DefaultBranch ?? "main");
                        
                        // Then Push
                        await _remoteController.Push(remote, project.DefaultBranch ?? "main");
                    }
                    catch (Exception ex)
                    {
                        // Log sync error for this remote but continue with others
                        Console.WriteLine($"Sync failed for remote {remote.RemoteName}: {ex.Message}");
                    }
                }
            }
        }
    }
}
