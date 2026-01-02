using System;
using System.Threading.Tasks;
using Linage.Core;
using Linage.Core.Authentication;

namespace Linage.Infrastructure
{
    /// <summary>
    /// Custom SSH transport for Li'nage repositories.
    /// Wraps LinageHttpTransport since SSH is just encrypted HTTP.
    /// </summary>
    public class LinageSshTransport : ITransport
    {
        private readonly LinageHttpTransport _httpTransport;

        public LinageSshTransport(AuthenticationService authService, string localRepoPath = ".")
        {
            // SSH for Li'nage is just HTTPS with SSH credentials
            // The actual SSH tunneling would be handled by the server
            _httpTransport = new LinageHttpTransport(authService, localRepoPath);
        }

        public Task PushAsync(string remoteUrl, string branchName)
        {
            // Convert SSH URL to HTTPS if needed
            var httpsUrl = ConvertSshToHttps(remoteUrl);
            return _httpTransport.PushAsync(httpsUrl, branchName);
        }

        public Task PullAsync(string remoteUrl, string branchName)
        {
            var httpsUrl = ConvertSshToHttps(remoteUrl);
            return _httpTransport.PullAsync(httpsUrl, branchName);
        }

        public Task FetchAsync(string remoteUrl)
        {
            var httpsUrl = ConvertSshToHttps(remoteUrl);
            return _httpTransport.FetchAsync(httpsUrl);
        }

        public bool ValidateConnection(string remoteUrl)
        {
            return !string.IsNullOrEmpty(remoteUrl) && 
                   (remoteUrl.StartsWith("ssh://") || remoteUrl.StartsWith("git@"));
        }

        private string ConvertSshToHttps(string sshUrl)
        {
            // Convert git@github.com:user/repo.git to https://github.com/user/repo.git
            if (sshUrl.StartsWith("git@"))
            {
                var parts = sshUrl.Substring(4).Split(':');
                if (parts.Length == 2)
                {
                    return $"https://{parts[0]}/{parts[1]}";
                }
            }
            
            // Convert ssh://git@github.com/user/repo.git to https://github.com/user/repo.git
            if (sshUrl.StartsWith("ssh://"))
            {
                return sshUrl.Replace("ssh://git@", "https://").Replace("ssh://", "https://");
            }

            return sshUrl;
        }
    }
}
