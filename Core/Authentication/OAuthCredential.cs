using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace Linage.Core.Authentication
{
    public class OAuthCredential : Credential
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string TokenEndpoint { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; } // Storing secret in client app is risky, typically use PKCE, but needed for non-interactive flows sometimes.
        public DateTime ExpiresAt { get; set; }
        public List<string> Scopes { get; set; }

        private static readonly HttpClient _httpClient = new HttpClient();

        public override bool Validate()
        {
            return !string.IsNullOrEmpty(AccessToken) && !string.IsNullOrEmpty(RefreshToken);
        }

        public override Task<bool> AuthenticateAsync()
        {
            // OAuth doesn't just "authenticate", it validates the token.
            // If expired, try refresh.
            if (IsExpired())
            {
                return Refresh().ContinueWith(t => !IsExpired());
            }
            return Task.FromResult(true);
        }

        public override async Task Refresh()
        {
            if (string.IsNullOrEmpty(TokenEndpoint) || string.IsNullOrEmpty(RefreshToken))
                return;

            try
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", RefreshToken),
                    new KeyValuePair<string, string>("client_id", ClientId ?? ""),
                    new KeyValuePair<string, string>("client_secret", ClientSecret ?? "")
                });

                var response = await _httpClient.PostAsync(TokenEndpoint, content);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var obj = JObject.Parse(json);
                    
                    if (obj["access_token"] != null)
                    {
                        AccessToken = obj["access_token"].ToString();
                    }
                    if (obj["refresh_token"] != null)
                    {
                        RefreshToken = obj["refresh_token"].ToString();
                    }
                    if (obj["expires_in"] != null)
                    {
                        ExpiresAt = DateTime.Now.AddSeconds((int)obj["expires_in"]);
                    }
                }
            }
            catch (Exception)
            {
                // Log failure
            }
        }

        public override bool IsExpired()
        {
            return DateTime.Now >= ExpiresAt;
        }

        public bool IsAccessTokenValid()
        {
            return !IsExpired();
        }
    }
}
