using System;
using System.Threading.Tasks;
using Linage.Core;
using Linage.Core.Authentication;

namespace Linage.Controllers
{
    public class AuthController
    {
        private readonly AuthenticationService _authService;

        public AuthController(AuthenticationService authService)
        {
            _authService = authService;
        }

        public async Task<bool> Authenticate(Remote remote)
        {
            var cred = _authService.GetCredentialForRemote(remote);
            if (cred == null)
            {
                cred = _authService.PromptForCredential(remote);
                if (cred != null)
                {
                    _authService.SaveCredential(remote.RemoteUrl, cred);
                }
            }
            
            return await _authService.ValidateCredential(cred, remote);
        }
        
        public void Logout(Remote remote)
        {
            if (remote == null) throw new ArgumentNullException(nameof(remote));
            
            // Remove credential from store
            var cred = _authService.GetCredentialForRemote(remote);
            if (cred != null)
            {
                _authService.GetCredentialStore().RemoveCredential(remote.RemoteUrl);
            }
        }
        
        /// <summary>
        /// Check if credentials exist for a remote....
        /// </summary>
        public bool HasCredentials(Remote remote)
        {
            if (remote == null) return false;
            return _authService.GetCredentialForRemote(remote) != null;
        }

        public LibGit2Sharp.Credentials GetCredentials(string url, string usernameFromUrl, LibGit2Sharp.SupportedCredentialTypes types)
        {
            return _authService.GetLibGit2Credentials(url, usernameFromUrl, types);
        }
    }
}
