using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Linage.Infrastructure;

namespace Linage.Core
{
    public class RemoteService
    {
        private readonly MetadataStore _metadataStore;

        public RemoteService(MetadataStore metadataStore)
        {
            _metadataStore = metadataStore ?? throw new ArgumentNullException(nameof(metadataStore));
        }

        public async Task AddRemoteAsync(string name, string url, string repositoryPath, RemoteProtocol protocol = RemoteProtocol.HTTPS)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Remote name cannot be empty.");
            if (string.IsNullOrEmpty(url)) throw new ArgumentException("Remote URL cannot be empty.");

            var allRemotes = await _metadataStore.GetAllRemotesAsync(repositoryPath);
            var remote = new Remote
            {
                RemoteName = name,
                RemoteUrl = url,
                RepositoryPath = repositoryPath,
                Protocol = protocol,
                IsDefault = allRemotes.Count == 0
            };

            await _metadataStore.SaveRemoteAsync(remote);
        }

        public async Task<Remote> GetRemoteAsync(string name, string repositoryPath = null)
        {
            return await _metadataStore.GetRemoteAsync(name, repositoryPath);
        }

        public async Task<List<Remote>> GetAllRemotesAsync(string repositoryPath = null)
        {
            return await _metadataStore.GetAllRemotesAsync(repositoryPath);
        }

        public async Task RemoveRemoteAsync(string name)
        {
            await _metadataStore.DeleteRemoteAsync(name);
        }

        public async Task SetDefaultRemoteAsync(string name, string repositoryPath)
        {
            var remotes = await _metadataStore.GetAllRemotesAsync(repositoryPath);
            foreach (var r in remotes)
            {
                r.IsDefault = (r.RemoteName == name);
                await _metadataStore.SaveRemoteAsync(r);
            }
        }
    }
}
