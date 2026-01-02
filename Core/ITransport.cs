using System.Threading.Tasks;

namespace Linage.Core
{
    public interface ITransport
    {
        Task PushAsync(string remoteUrl, string branchName);
        Task PullAsync(string remoteUrl, string branchName);
        Task FetchAsync(string remoteUrl);
        bool ValidateConnection(string remoteUrl);
    }
}
