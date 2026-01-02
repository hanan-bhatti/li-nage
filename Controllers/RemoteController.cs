using System.Collections.Generic;
using System.Threading.Tasks;
using Linage.Core;
using Linage.Infrastructure;

namespace Linage.Controllers
{
    public class RemoteController
    {
        private readonly ITransport _httpTransport;
        private readonly ITransport _sshTransport;
        private readonly AuthController _authController;

        public RemoteController(ITransport httpTransport, ITransport sshTransport, AuthController authController)
        {
            _httpTransport = httpTransport;
            _sshTransport = sshTransport;
            _authController = authController;
        }

        public async Task Push(Remote remote, string branchName)
        {
            if (await _authController.Authenticate(remote))
            {
                var transport = GetTransport(remote);
                await transport.PushAsync(remote.RemoteUrl, branchName);
            }
        }

        public async Task Pull(Remote remote, string branchName)
        {
            if (await _authController.Authenticate(remote))
            {
                var transport = GetTransport(remote);
                await transport.PullAsync(remote.RemoteUrl, branchName);
            }
        }

        public async Task<string> Clone(string remoteUrl, string destinationPath)
        {
             return await Task.Run(() => 
             {
                 // Ensure we have credentials if needed (prompt or store)
                 // For now, relying on AuthService to provide if cached/available 
                 var options = new LibGit2Sharp.CloneOptions();
                 options.FetchOptions.CredentialsProvider = (url, user, types) => _authController.GetCredentials(url, user, types);
                 
                 return LibGit2Sharp.Repository.Clone(remoteUrl, destinationPath, options);
             });
        }

        private ITransport GetTransport(Remote remote)
        {
            switch (remote.Protocol)
            {
                case RemoteProtocol.SSH:
                    return _sshTransport;
                case RemoteProtocol.HTTPS:
                default:
                    return _httpTransport;
            }
        }
    }
}
