using System;
using System.Threading.Tasks;
using Linage.Controllers;
using Linage.Core;

namespace Linage.Core.Services
{
    /// <summary>
    /// Result of a remote operation (push, pull, clone).
    /// </summary>
    public class RemoteOperationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }

        public static RemoteOperationResult Success(string message)
        {
            return new RemoteOperationResult { IsSuccess = true, Message = message };
        }

        public static RemoteOperationResult Failure(string message)
        {
            return new RemoteOperationResult { IsSuccess = false, Message = message };
        }
    }

    /// <summary>
    /// Service for handling remote repository operations with consistent error handling
    /// and protocol detection.
    /// </summary>
    public class RemoteOperationsService
    {
        private readonly RemoteController _remoteController;
        private readonly VersionGraphService _graphService;

        public RemoteOperationsService(RemoteController remoteController, VersionGraphService graphService)
        {
            _remoteController = remoteController ?? throw new ArgumentNullException(nameof(remoteController));
            _graphService = graphService ?? throw new ArgumentNullException(nameof(graphService));
        }

        /// <summary>
        /// Pushes local commits to a remote repository.
        /// </summary>
        /// <param name="remoteUrl">Remote repository URL</param>
        /// <param name="branchName">Branch to push (null for current branch)</param>
        /// <returns>Operation result</returns>
        public async Task<RemoteOperationResult> PushAsync(string remoteUrl, string branchName = null)
        {
            if (string.IsNullOrEmpty(remoteUrl))
                return RemoteOperationResult.Failure("Remote URL is required");

            try
            {
                // Use current branch if not specified
                if (string.IsNullOrEmpty(branchName))
                {
                    var currentBranch = _graphService.GetCurrentBranch();
                    if (currentBranch == null)
                        return RemoteOperationResult.Failure("No active branch to push");

                    branchName = currentBranch.BranchName;
                }

                var remote = CreateRemoteFromUrl(remoteUrl);
                await _remoteController.Push(remote, branchName).ConfigureAwait(false);

                return RemoteOperationResult.Success($"Successfully pushed to {remoteUrl}");
            }
            catch (Exception ex)
            {
                return RemoteOperationResult.Failure($"Push failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Pulls commits from a remote repository.
        /// </summary>
        /// <param name="remoteUrl">Remote repository URL</param>
        /// <param name="branchName">Branch to pull (null for current branch)</param>
        /// <returns>Operation result</returns>
        public async Task<RemoteOperationResult> PullAsync(string remoteUrl, string branchName = null)
        {
            if (string.IsNullOrEmpty(remoteUrl))
                return RemoteOperationResult.Failure("Remote URL is required");

            try
            {
                // Use current branch if not specified
                if (string.IsNullOrEmpty(branchName))
                {
                    var currentBranch = _graphService.GetCurrentBranch();
                    if (currentBranch == null)
                        return RemoteOperationResult.Failure("No active branch to pull into");

                    branchName = currentBranch.BranchName;
                }

                var remote = CreateRemoteFromUrl(remoteUrl);
                await _remoteController.Pull(remote, branchName).ConfigureAwait(false);

                return RemoteOperationResult.Success($"Successfully pulled from {remoteUrl}");
            }
            catch (Exception ex)
            {
                return RemoteOperationResult.Failure($"Pull failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Clones a remote repository to a local path.
        /// </summary>
        /// <param name="remoteUrl">Remote repository URL</param>
        /// <param name="destinationPath">Local destination path</param>
        /// <returns>Operation result</returns>
        public async Task<RemoteOperationResult> CloneAsync(string remoteUrl, string destinationPath)
        {
            if (string.IsNullOrEmpty(remoteUrl))
                return RemoteOperationResult.Failure("Remote URL is required");

            if (string.IsNullOrEmpty(destinationPath))
                return RemoteOperationResult.Failure("Destination path is required");

            try
            {
                await _remoteController.Clone(remoteUrl, destinationPath).ConfigureAwait(false);
                return RemoteOperationResult.Success($"Successfully cloned to {destinationPath}");
            }
            catch (Exception ex)
            {
                return RemoteOperationResult.Failure($"Clone failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a Remote object from a URL with automatic protocol detection.
        /// </summary>
        /// <param name="url">Remote repository URL</param>
        /// <returns>Remote object configured with appropriate protocol</returns>
        private Remote CreateRemoteFromUrl(string url)
        {
            var protocol = DetermineProtocol(url);
            return new Remote
            {
                RemoteUrl = url,
                RemoteName = "origin",
                Protocol = protocol
            };
        }

        /// <summary>
        /// Determines the remote protocol based on the URL format.
        /// </summary>
        /// <param name="url">Remote repository URL</param>
        /// <returns>Detected protocol</returns>
        private RemoteProtocol DetermineProtocol(string url)
        {
            if (string.IsNullOrEmpty(url))
                return RemoteProtocol.HTTPS;

            var lowerUrl = url.ToLower();

            // SSH: ssh://user@host/path or git@host:path
            if (lowerUrl.StartsWith("ssh://") || lowerUrl.StartsWith("git@"))
                return RemoteProtocol.SSH;

            // Default to HTTPS for http://, https://, or plain URLs
            return RemoteProtocol.HTTPS;
        }

        /// <summary>
        /// Validates if a URL appears to be a valid remote repository URL.
        /// </summary>
        /// <param name="url">URL to validate</param>
        /// <returns>True if URL appears valid</returns>
        public bool IsValidRemoteUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            var lowerUrl = url.ToLower();

            // Check for common patterns
            return lowerUrl.StartsWith("http://") ||
                   lowerUrl.StartsWith("https://") ||
                   lowerUrl.StartsWith("ssh://") ||
                   lowerUrl.StartsWith("git@") ||
                   lowerUrl.Contains("github.com") ||
                   lowerUrl.Contains("gitlab.com") ||
                   lowerUrl.Contains("bitbucket.org");
        }
    }
}
