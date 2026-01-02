using System;
using System.Threading.Tasks;

namespace Linage.Core.Authentication
{
    public abstract class Credential
    {
        public Guid CredentialId { get; set; } = Guid.NewGuid();
        public string RemoteUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUsed { get; set; }

        public Credential()
        {
            CreatedAt = DateTime.Now;
            LastUsed = DateTime.Now;
        }

        public abstract bool Validate();
        public abstract Task<bool> AuthenticateAsync();
        public abstract Task Refresh();
        public abstract bool IsExpired();
    }
}
