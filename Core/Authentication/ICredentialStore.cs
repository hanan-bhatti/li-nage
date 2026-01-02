using System.Collections.Generic;

namespace Linage.Core.Authentication
{
    public interface ICredentialStore
    {
        void SaveCredential(string remoteUrl, Credential cred);
        Credential GetCredential(string remoteUrl);
        void RemoveCredential(string remoteUrl);
        List<Credential> ListCredentials();
        void ClearExpiredCredentials();
    }
}
