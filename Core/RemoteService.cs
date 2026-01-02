using System;
using System.Collections.Generic;
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

        public void AddRemote(string name, string url, RemoteProtocol protocol = RemoteProtocol.HTTPS)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Remote name cannot be empty.");
            if (string.IsNullOrEmpty(url)) throw new ArgumentException("Remote URL cannot be empty.");

            var remote = new Remote
            {
                RemoteName = name,
                RemoteUrl = url,
                Protocol = protocol,
                IsDefault = _metadataStore.GetAllRemotes().Count == 0
            };

            _metadataStore.SaveRemote(remote);
        }

        public Remote GetRemote(string name)
        {
            return _metadataStore.GetRemote(name);
        }

        public List<Remote> GetAllRemotes()
        {
            return _metadataStore.GetAllRemotes();
        }

        public void RemoveRemote(string name)
        {
            _metadataStore.DeleteRemote(name);
        }

        public void SetDefaultRemote(string name)
        {
            var remotes = _metadataStore.GetAllRemotes();
            foreach (var r in remotes)
            {
                r.IsDefault = (r.RemoteName == name);
                _metadataStore.SaveRemote(r);
            }
        }
    }
}
