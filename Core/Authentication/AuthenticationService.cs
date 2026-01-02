using System;
using System.Linq;
using System.Threading.Tasks;
using Linage.Core;
using LibGit2Sharp;

namespace Linage.Core.Authentication
{
    public class AuthenticationService
    {
        private readonly ICredentialStore _credentialStore;

        public AuthenticationService(ICredentialStore credentialStore)
        {
            _credentialStore = credentialStore ?? throw new ArgumentNullException(nameof(credentialStore));
        }

        public Credential GetCredentialForRemote(Remote remote)
        {
            if (remote == null) throw new ArgumentNullException(nameof(remote));
            return _credentialStore.GetCredential(remote.RemoteUrl);
        }
        
        
        // This is the core integration point for LibGit2Sharp
        public Credentials GetLibGit2Credentials(string url, string usernameFromUrl, SupportedCredentialTypes types)
        {
            var cred = _credentialStore.GetCredential(url);
            if (cred == null)
            {
                // Fallback: try finding by host if exact URL match fails?
                // For now, strict match.
                // If no credential found, LibGit2Sharp might try anonymous or fail.
                // We return null to let it fail or use default behavior.
                return new DefaultCredentials();
            }

            if (cred is HttpCredential http)
            {
                return new UsernamePasswordCredentials
                {
                    Username = http.Username ?? "token", // "token" is common for PATs as user
                    Password = http.Token
                };
            }
            else if (cred is SshCredential ssh)
            {
                return new UsernamePasswordCredentials
                {
                    Username = usernameFromUrl ?? "git",
                    Password = ssh.Passphrase ?? ""
                };
            }

            return new DefaultCredentials();
        }

        /// <summary>
        /// Get credential for a specific URL
        /// </summary>
        public Credential GetCredentialForUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            // Try to find a credential that matches this URL
            // For now, return the first HTTP credential if URL is HTTP/HTTPS
            if (url.StartsWith("http://") || url.StartsWith("https://"))
            {
                var allCreds = _credentialStore.ListCredentials();
                return allCreds.FirstOrDefault(c => c is HttpCredential);
            }

            // SSH URLs
            if (url.StartsWith("ssh://") || url.StartsWith("git@"))
            {
                var allCreds = _credentialStore.ListCredentials();
                return allCreds.FirstOrDefault(c => c is SshCredential);
            }

            return null;
        }

        public async Task<bool> ValidateCredential(Credential cred, Remote remote)
        {
            if (cred == null) return false;
            return await cred.AuthenticateAsync();
        }

        public Credential PromptForCredential(Remote remote)
        {
            // Logic to trigger UI prompt (handled via events or UI service interaction usually)
            return null;
        }

        public void SaveCredential(string remoteUrl, Credential cred)
        {
            _credentialStore.SaveCredential(remoteUrl, cred);
        }

        public void DeleteCredential(string remoteUrl)
        {
            _credentialStore.RemoveCredential(remoteUrl);
        }

        public Credential GetCredential(string remoteUrl)
        {
            return _credentialStore.GetCredential(remoteUrl);
        }
        
        /// <summary>
        /// Get the credential store instance
        /// </summary>
        public ICredentialStore GetCredentialStore()
        {
            return _credentialStore;
        }
    }
}
