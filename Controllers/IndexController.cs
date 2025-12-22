using System;
using Linage.Core;
using Linage.Infrastructure;

namespace Linage.Controllers
{
    public class IndexController
    {
        public string Status { get; set; } = "Idle";
        public DateTime LastRunTime { get; set; }

        public MetadataStore Store { get; set; } = new MetadataStore();
        public HashService Hasher { get; set; } = new HashService();
    }
}
