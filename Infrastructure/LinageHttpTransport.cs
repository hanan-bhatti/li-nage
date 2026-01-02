using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Linage.Core;
using Linage.Core.Authentication;
using Newtonsoft.Json;

namespace Linage.Infrastructure
{
    /// <summary>
    /// Custom HTTP transport for Li'nage repositories.
    /// Uses simple JSON-based protocol for push/pull operations.
    /// </summary>
    public class LinageHttpTransport : ITransport
    {
        private readonly AuthenticationService _authService;
        private readonly string _localRepoPath;
        private readonly HttpClient _httpClient;

        public LinageHttpTransport(AuthenticationService authService, string localRepoPath = ".")
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _localRepoPath = Path.GetFullPath(localRepoPath);
            _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        }

        public async Task PushAsync(string remoteUrl, string branchName)
        {
            // Get authentication token
            var credential = _authService.GetCredentialForUrl(remoteUrl);
            if (credential == null)
                throw new UnauthorizedAccessException("No credentials found for remote");

            // Read local commits
            var commits = ReadLocalCommits(branchName);
            if (commits.Count == 0)
                throw new InvalidOperationException("No commits to push");

            // Prepare push payload
            var payload = new PushPayload
            {
                BranchName = branchName,
                Commits = commits,
                Repository = Path.GetFileName(_localRepoPath)
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Add authentication header
            if (credential is HttpCredential httpCred)
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", httpCred.Token);
            }

            // Send push request
            var pushUrl = $"{remoteUrl.TrimEnd('/')}/push";
            var response = await _httpClient.PostAsync(pushUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Push failed: {response.StatusCode} - {error}");
            }
        }

        public async Task PullAsync(string remoteUrl, string branchName)
        {
            // Get authentication token
            var credential = _authService.GetCredentialForUrl(remoteUrl);
            if (credential == null)
                throw new UnauthorizedAccessException("No credentials found for remote");

            // Add authentication header
            if (credential is HttpCredential httpCred)
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", httpCred.Token);
            }

            // Request pull
            var pullUrl = $"{remoteUrl.TrimEnd('/')}/pull?branch={branchName}";
            var response = await _httpClient.GetAsync(pullUrl);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Pull failed: {response.StatusCode} - {error}");
            }

            // Parse response
            var json = await response.Content.ReadAsStringAsync();
            var payload = JsonConvert.DeserializeObject<PullPayload>(json);

            // Write commits to local repository
            WriteLocalCommits(branchName, payload.Commits);
        }

        public async Task FetchAsync(string remoteUrl)
        {
            // Similar to pull but doesn't merge
            var credential = _authService.GetCredentialForUrl(remoteUrl);
            if (credential == null)
                throw new UnauthorizedAccessException("No credentials found for remote");

            if (credential is HttpCredential httpCred)
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", httpCred.Token);
            }

            var fetchUrl = $"{remoteUrl.TrimEnd('/')}/fetch";
            var response = await _httpClient.GetAsync(fetchUrl);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Fetch failed: {response.StatusCode} - {error}");
            }
        }

        public bool ValidateConnection(string remoteUrl)
        {
            return !string.IsNullOrEmpty(remoteUrl) && 
                   (remoteUrl.StartsWith("http://") || remoteUrl.StartsWith("https://"));
        }

        private List<CommitData> ReadLocalCommits(string branchName)
        {
            var commits = new List<CommitData>();
            var linageDir = Path.Combine(_localRepoPath, ".linage");
            
            if (!Directory.Exists(linageDir))
                return commits;

            // Read commits from .linage/commits directory
            var commitsDir = Path.Combine(linageDir, "commits");
            if (!Directory.Exists(commitsDir))
                return commits;

            foreach (var file in Directory.GetFiles(commitsDir, "*.json"))
            {
                var json = File.ReadAllText(file);
                var commit = JsonConvert.DeserializeObject<CommitData>(json);
                commits.Add(commit);
            }

            return commits;
        }

        private void WriteLocalCommits(string branchName, List<CommitData> commits)
        {
            var linageDir = Path.Combine(_localRepoPath, ".linage");
            var commitsDir = Path.Combine(linageDir, "commits");
            
            Directory.CreateDirectory(commitsDir);

            foreach (var commit in commits)
            {
                var json = JsonConvert.SerializeObject(commit, Formatting.Indented);
                var fileName = $"{commit.Hash}.json";
                File.WriteAllText(Path.Combine(commitsDir, fileName), json);
            }
        }
    }

    // DTOs for network transfer
    public class PushPayload
    {
        public string BranchName { get; set; }
        public string Repository { get; set; }
        public List<CommitData> Commits { get; set; }
    }

    public class PullPayload
    {
        public string BranchName { get; set; }
        public List<CommitData> Commits { get; set; }
    }

    public class CommitData
    {
        public string Hash { get; set; }
        public string Author { get; set; }
        public string Email { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public List<string> Parents { get; set; }
        public List<FileData> Files { get; set; }
    }

    public class FileData
    {
        public string Path { get; set; }
        public string Hash { get; set; }
        public long Size { get; set; }
        public List<LineData> Lines { get; set; } // Line-level tracking
    }

    public class LineData
    {
        public int LineNumber { get; set; }
        public string Content { get; set; }
        public string Hash { get; set; }
        public string Author { get; set; }
        public DateTime Modified { get; set; }
    }
}
