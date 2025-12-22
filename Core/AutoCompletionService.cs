using Linage.Infrastructure;

namespace Linage.Core
{
    public class AutoCompletionService
    {
        public string EngineType { get; set; } = "Transformer";
        
        public FileMetadata ContextFile { get; set; }
        public Snapshot ContextSnapshot { get; set; }
    }
}
