using System;

namespace Linage.Core
{
    public class ExternalResource
    {
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string[] Tags { get; set; } = new string[0];
    }
}
