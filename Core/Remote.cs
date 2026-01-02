using System;

namespace Linage.Core
{
    public enum RemoteProtocol
    {
        HTTPS,
        SSH,
        FILE
    }

    public class Remote
    {
        public Guid RemoteId { get; set; }
        public Guid ProjectId { get; set; }
        public string RemoteName { get; set; }
        public string RemoteUrl { get; set; }
        public RemoteProtocol Protocol { get; set; }
        public string FetchRefspec { get; set; }
        public string PushRefspec { get; set; }
        public bool IsDefault { get; set; }

        public Remote()
        {
            RemoteId = Guid.NewGuid();
        }
    }
}
