using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Linage.Core.Authentication
{
    public class HttpCredential : Credential
    {
        public string Username { get; set; }
        public string Token { get; set; }
        public DateTime ExpiresAt { get; set; }

        public override bool Validate()
        {
            return !string.IsNullOrEmpty(Token);
        }

        public override async Task<bool> AuthenticateAsync()
        {
            if (string.IsNullOrEmpty(RemoteUrl)) return false;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("LinageVCS/1.0");
                
                if (!string.IsNullOrEmpty(Username))
                {
                    var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{Token}"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
                }
                else
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
                }

                try
                {
                    // Attempt to connect to the remote URL. 
                    // For Git remotes, appending /info/refs?service=git-upload-pack is a standard way to check.
                    string checkUrl = RemoteUrl.TrimEnd('/') + "/info/refs?service=git-upload-pack";
                    var response = await client.GetAsync(checkUrl);
                    
                    return response.IsSuccessStatusCode;
                }
                catch
                {
                    return false;
                }
            }
        }

        public override Task Refresh()
        {
            // Refresh token logic would go here if using OAuth-based HttpCredential
            return Task.CompletedTask;
        }

        public override bool IsExpired()
        {
            return ExpiresAt != default && DateTime.Now > ExpiresAt;
        }

        public bool IsTokenValid()
        {
            return !IsExpired();
        }
    }
}
