using System.Collections.Generic;

namespace Linage.Core
{
    public class SolutionIndex
    {
        public string ErrorSignature { get; set; } = string.Empty;
        public List<ExternalResource> Resources { get; set; } = new List<ExternalResource>();
    }
}
