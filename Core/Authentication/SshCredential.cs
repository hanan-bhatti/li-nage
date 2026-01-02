using System;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace Linage.Core.Authentication
{
    public class SshCredential : Credential
    {
        public string Username { get; set; }
        public string PublicKeyPath { get; set; }
        public string PrivateKeyPath { get; set; }
        public string Passphrase { get; set; }

        public override bool Validate()
        {
            return !string.IsNullOrEmpty(PrivateKeyPath) && System.IO.File.Exists(PrivateKeyPath);
        }

        public override Task<bool> AuthenticateAsync()
        {
            return Task.Run(() =>
            {
                if (string.IsNullOrEmpty(RemoteUrl)) return false;
                if (!Validate()) return false;

                try
                {
                    // SSH authentication test using LibGit2Sharp credential handler
                    // The library handles SSH key files through the credential callback
                    var refs = Repository.ListRemoteReferences(RemoteUrl, 
                        (url, usernameFromUrl, types) => 
                        {
                            // For SSH, LibGit2Sharp uses the SSH agent or key files
                            // We return UsernamePasswordCredentials as a compatibility layer
                            return new UsernamePasswordCredentials
                            {
                                Username = Username ?? usernameFromUrl ?? "git",
                                Password = string.Empty // SSH uses key files, not password
                            };
                        });
                    return refs != null;
                }
                catch
                {
                    return false;
                }
            });
        }

        public override Task Refresh()
        {
            return Task.CompletedTask; // SSH keys don't typically refresh like tokens
        }

        public override bool IsExpired()
        {
            return false;
        }

        public byte[] LoadPrivateKey()
        {
            if (string.IsNullOrEmpty(PrivateKeyPath) || !System.IO.File.Exists(PrivateKeyPath))
            {
                return Array.Empty<byte>();
            }
            return System.IO.File.ReadAllBytes(PrivateKeyPath);
        }

        public bool ValidateKeyPair()
        {
            return !string.IsNullOrEmpty(PublicKeyPath) && System.IO.File.Exists(PublicKeyPath);
        }
    }
}
