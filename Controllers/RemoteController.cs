using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LibGit2Sharp;
using Linage.Core;
using Linage.Infrastructure;
using LinageRemote = Linage.Core.Remote;

namespace Linage.Controllers
{
    public class RemoteController
    {
        private readonly AuthController _authController;

        public RemoteController(AuthController authController)
        {
            _authController = authController ?? throw new ArgumentNullException(nameof(authController));
        }

        public async Task Push(LinageRemote remote, string branchName, string localRepoPath)
        {
            await Task.Run(() =>
            {
                using (var repo = new Repository(localRepoPath))
                {
                    var gitRemote = repo.Network.Remotes[remote.RemoteName];
                    if (gitRemote == null)
                    {
                        // Fallback: create or update remote if missing in .git config but present in our DB
                        gitRemote = repo.Network.Remotes.Add(remote.RemoteName, remote.RemoteUrl);
                    }

                    var options = new PushOptions
                    {
                        CredentialsProvider = (url, user, types) => _authController.GetCredentials(url, user, types)
                    };

                    // Push specific branch
                    // Note: RefSpec format "refs/heads/branch:refs/heads/branch"
                    string pushRefSpec = $"refs/heads/{branchName}:refs/heads/{branchName}";
                    repo.Network.Push(gitRemote, pushRefSpec, options);
                }
            });
        }

        public async Task Pull(LinageRemote remote, string branchName, string localRepoPath)
        {
            await Task.Run(() =>
            {
                using (var repo = new Repository(localRepoPath))
                {
                    // 1. Fetch
                    var options = new FetchOptions
                    {
                        CredentialsProvider = (url, user, types) => _authController.GetCredentials(url, user, types)
                    };
                    
                    var gitRemote = repo.Network.Remotes[remote.RemoteName];
                    if (gitRemote == null)
                         gitRemote = repo.Network.Remotes.Add(remote.RemoteName, remote.RemoteUrl);

                    Commands.Fetch(repo, gitRemote.Name, new string[] { branchName }, options, null);

                    // 2. Merge (Pull = Fetch + Merge)
                    // We need to merge origin/branchName into local branchName
                    var signature = repo.Config.BuildSignature(DateTimeOffset.Now);
                    var remoteBranchName = $"{remote.RemoteName}/{branchName}";
                    
                    // Merge remote tracking branch into current (assuming current is checked out)
                    var result = repo.Merge(remoteBranchName, signature, new MergeOptions { FastForwardStrategy = FastForwardStrategy.Default });

                    if (result.Status == MergeStatus.Conflicts)
                    {
                        throw new InvalidOperationException("Pull resulted in conflicts. Please resolve them manually.");
                    }
                }
            });
        }

        public async Task<string> Clone(string remoteUrl, string destinationPath)
        {
             return await Task.Run(() => 
             {
                 var options = new CloneOptions
                 {
                     FetchOptions = {
                        CredentialsProvider = (url, user, types) => _authController.GetCredentials(url, user, types)
                     }
                 };
                 
                 return Repository.Clone(remoteUrl, destinationPath, options);
             });
        }
    }
}
