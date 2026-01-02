using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Linage.Core.Authentication;
using Linage.Infrastructure.Security;

namespace Linage.Infrastructure
{
    public class CredentialStore : ICredentialStore
    {
        private Dictionary<string, Credential> _store = new Dictionary<string, Credential>();
        private readonly string _storagePath;
        private readonly object _lock = new object();

        public CredentialStore()
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            _storagePath = Path.Combine(path, "linage_creds.enc"); // Changed extension to denote encryption
            Load();
        }

        private void Load()
        {
            lock (_lock)
            {
                if (File.Exists(_storagePath))
                {
                    try
                    {
                        var encrypted = File.ReadAllText(_storagePath);
                        var json = EncryptionHelper.Decrypt(encrypted);
                        if (!string.IsNullOrEmpty(json))
                        {
                            var list = JsonConvert.DeserializeObject<List<CredentialWrapper>>(json);
                            if (list != null)
                            {
                                _store = new Dictionary<string, Credential>();
                                foreach (var wrapper in list)
                                {
                                    Credential cred = null;
                                    if (wrapper.Type == "HTTP") cred = JsonConvert.DeserializeObject<HttpCredential>(wrapper.Json);
                                    else if (wrapper.Type == "SSH") cred = JsonConvert.DeserializeObject<SshCredential>(wrapper.Json);
                                    else if (wrapper.Type == "OAUTH") cred = JsonConvert.DeserializeObject<OAuthCredential>(wrapper.Json);
                                    
                                    if (cred != null) _store[wrapper.RemoteUrl] = cred;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error in production
                        Console.WriteLine($"Failed to load credentials: {ex.Message}");
                    }
                }
            }
        }

        private void Save()
        {
            lock (_lock)
            {
                var list = new List<CredentialWrapper>();
                foreach (var kvp in _store)
                {
                    var type = kvp.Value is HttpCredential ? "HTTP" : (kvp.Value is SshCredential ? "SSH" : "OAUTH");
                    list.Add(new CredentialWrapper 
                    { 
                        RemoteUrl = kvp.Key, 
                        Type = type, 
                        Json = JsonConvert.SerializeObject(kvp.Value) 
                    });
                }
                
                var json = JsonConvert.SerializeObject(list);
                var encrypted = EncryptionHelper.Encrypt(json);
                File.WriteAllText(_storagePath, encrypted);
            }
        }

        private class CredentialWrapper
        {
            public string RemoteUrl { get; set; }
            public string Type { get; set; }
            public string Json { get; set; }
        }

        public void SaveCredential(string remoteUrl, Credential cred)
        {
            if (string.IsNullOrEmpty(remoteUrl)) throw new ArgumentNullException(nameof(remoteUrl));
            if (cred == null) throw new ArgumentNullException(nameof(cred));

            lock (_lock)
            {
                if (_store.ContainsKey(remoteUrl))
                {
                    _store[remoteUrl] = cred;
                }
                else
                {
                    _store.Add(remoteUrl, cred);
                }
                Save();
            }
        }

        public Credential GetCredential(string remoteUrl)
        {
            if (string.IsNullOrEmpty(remoteUrl)) return null;
            lock (_lock)
            {
                return _store.ContainsKey(remoteUrl) ? _store[remoteUrl] : null;
            }
        }

        public void RemoveCredential(string remoteUrl)
        {
            if (string.IsNullOrEmpty(remoteUrl)) return;
            lock (_lock)
            {
                if (_store.ContainsKey(remoteUrl))
                {
                    _store.Remove(remoteUrl);
                    Save();
                }
            }
        }

        public List<Credential> ListCredentials()
        {
            lock (_lock)
            {
                return new List<Credential>(_store.Values);
            }
        }

        public void ClearExpiredCredentials()
        {
            lock (_lock)
            {
                var keysToRemove = _store.Where(kvp => kvp.Value.IsExpired()).Select(kvp => kvp.Key).ToList();
                foreach (var key in keysToRemove)
                {
                    _store.Remove(key);
                }
                if (keysToRemove.Any()) Save();
            }
        }
    }
}
