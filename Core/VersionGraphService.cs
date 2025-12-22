using System.Collections.Generic;

namespace Linage.Core
{
    public class VersionGraphService
    {
        public int GraphSize { get; set; }
        public List<Commit> Commits { get; set; } = new List<Commit>();
        public List<Branch> Branches { get; set; } = new List<Branch>();
    }
}
