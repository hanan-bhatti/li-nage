using System;
using Linage.Core;
using Linage.Infrastructure;

namespace Linage.Controllers
{
    public class IndexController
    {
        public string Status { get; set; } = "Idle";
        public DateTime LastRunTime { get; set; }

        public MetadataStore Store { get; set; }
        public HashService Hasher { get; set; } = new HashService();

        public IndexController()
        {
            Store = new MetadataStore(new LiNageDbContext());
        }
    }
}
