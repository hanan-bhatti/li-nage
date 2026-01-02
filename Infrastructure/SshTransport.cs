using System;
using System.Threading.Tasks;
using Linage.Core;
using Linage.Core.Authentication;

namespace Linage.Infrastructure
{
    // LibGit2Sharp handles both HTTP and SSH via the same Network API transparently.
    // The key difference is the protocol check and credential type.
    // We can reuse the logic or inherit.
    public class SshTransport : ITransport
    {
        private readonly HttpTransport _implementation;

        public SshTransport(AuthenticationService authService, string localRepoPath = ".")
        {
            // We reuse the implementation because LibGit2Sharp unifies them.
            _implementation = new HttpTransport(authService, localRepoPath);
        }

        public Task PushAsync(string remoteUrl, string branchName)
        {
            return _implementation.PushAsync(remoteUrl, branchName);
        }

        public Task PullAsync(string remoteUrl, string branchName)
        {
            return _implementation.PullAsync(remoteUrl, branchName);
        }

        public Task FetchAsync(string remoteUrl)
        {
            return _implementation.FetchAsync(remoteUrl);
        }

        public bool ValidateConnection(string remoteUrl)
        {
             return !string.IsNullOrEmpty(remoteUrl) && (remoteUrl.StartsWith("ssh://") || remoteUrl.StartsWith("git@"));
        }
    }
}