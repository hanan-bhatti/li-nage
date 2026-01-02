using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Linage.Core;
using Linage.Core.Authentication;
using LibGit2Sharp;

namespace Linage.Infrastructure
{
    public class HttpTransport : ITransport
    {
        private readonly AuthenticationService _authService;
        private readonly string _localRepoPath;

        // In a real application, the local repository path would be injected or determined from context.
        // For this "Transport" service, we assume we operate on the current working directory's repo
        // or a path provided via configuration...
        public HttpTransport(AuthenticationService authService, string localRepoPath = ".")
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _localRepoPath = Path.GetFullPath(localRepoPath);
        }

        public Task PushAsync(string remoteUrl, string branchName)
        {
            return Task.Run(() =>
            {
                if (!Repository.IsValid(_localRepoPath))
                {
                    throw new DirectoryNotFoundException($"No valid git repository found at {_localRepoPath}");
                }

                using (var repo = new Repository(_localRepoPath))
                {
                    var remote = repo.Network.Remotes["origin"]; 
                    // Or find by URL match if explicit remoteUrl differs from configured remotes
                    if (remote == null || remote.Url != remoteUrl)
                    {
                        // Temporary remote for this operation if not configured
                        // LibGit2Sharp allows pushing to a URL directly usually via options or by creating a temp remote
                        // We will try to find the remote that matches the URL
                        foreach(var r in repo.Network.Remotes)
                        {
                            if (r.Url == remoteUrl)
                            {
                                remote = r;
                                break;
                            }
                        }
                    }

                    // If still null, create a temporary one? 
                    // For safety, we only push to configured remotes in this implementation or throw.
                    if (remote == null) throw new ArgumentException($"Remote '{remoteUrl}' not configured in local repository.");

                    var options = new PushOptions
                    {
                        CredentialsProvider = (url, user, types) => _authService.GetLibGit2Credentials(url, user, types)
                    };

                    // Assuming branchName is "master" or "main"
                    // We push the local branch to the remote branch of the same name.
                    // branchName might be "refs/heads/main" or just "main"
                    string refSpec = branchName.StartsWith("refs/") ? branchName : $"refs/heads/{branchName}";
                    
                    repo.Network.Push(remote, refSpec, options);
                }
            });
        }

        public Task PullAsync(string remoteUrl, string branchName)
        {
             return Task.Run(() =>
            {
                if (!Repository.IsValid(_localRepoPath)) return;

                using (var repo = new Repository(_localRepoPath))
                {
                    var options = new PullOptions
                    {
                        FetchOptions = new FetchOptions
                        {
                            CredentialsProvider = (url, user, types) => _authService.GetLibGit2Credentials(url, user, types)
                        }
                    };

                    // Pull requires identity for merge commit if needed
                    var signature = repo.Config.BuildSignature(DateTimeOffset.Now);
                    
                    // We need to know which local branch to pull into. Assuming current CheckedOut branch
                    // if it tracks the remote.
                    // "Pull" in git is Fetch + Merge.
                    
                    // Complex logic: Check if remoteUrl matches a configured remote.
                    // Then fetch that remote.
                    // Then merge the target branch.
                    
                    Commands.Pull(repo, signature, options);
                }
            });
        }

        public Task FetchAsync(string remoteUrl)
        {
            return Task.Run(() =>
            {
                 if (!Repository.IsValid(_localRepoPath)) return;

                using (var repo = new Repository(_localRepoPath))
                {
                    var remote = repo.Network.Remotes["origin"]; // Default assumption
                     foreach(var r in repo.Network.Remotes)
                    {
                        if (r.Url == remoteUrl)
                        {
                            remote = r;
                            break;
                        }
                    }
                    
                    if (remote == null) 
                    {
                         // Fetch from URL directly if supported or create temp
                         // LibGit2Sharp.Commands.Fetch can take a remote name/remote object
                         // If remote is not configured, we can't easily fetch without defining refspecs.
                         throw new ArgumentException($"Remote {remoteUrl} must be configured.");
                    }

                    var options = new FetchOptions
                    {
                        CredentialsProvider = (url, user, types) => _authService.GetLibGit2Credentials(url, user, types)
                    };

                    Commands.Fetch(repo, remote.Name, repo.Network.Remotes[remote.Name].FetchRefSpecs.Select(x => x.Specification), options, null);
                }
            });
        }

        public bool ValidateConnection(string remoteUrl)
        {
             // LibGit2Sharp doesn't have a simple "Ping".
             // We can try ls-remote equivalent.
             try 
             {
                 var references = Repository.ListRemoteReferences(remoteUrl, (url, user, types) => 
                    _authService.GetLibGit2Credentials(url, user, types));
                 return references != null;
             }
             catch
             {
                 return false;
             }
        }
    }
}
