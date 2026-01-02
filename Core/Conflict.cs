using System;

namespace Linage.Core
{
    public class Conflict
    {
        public Guid ConflictId { get; set; } = Guid.NewGuid();
        public string FilePath { get; set; }
        
        // The content from the common ancestor
        public string BaseContent { get; set; }
        
        // The content from the current branch (target)
        public string LocalContent { get; set; }
        
        // The content from the incoming branch (source)
        public string RemoteContent { get; set; }

        public bool IsResolved { get; set; }
        public string ResolvedContent { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        public Conflict() { }

        public Conflict(string filePath, string baseContent, string localContent, string remoteContent)
        {
            FilePath = filePath;
            BaseContent = baseContent;
            LocalContent = localContent;
            RemoteContent = remoteContent;
            IsResolved = false;
        }
        
        public void Resolve(string mergedContent)
        {
            if (string.IsNullOrEmpty(mergedContent))
                throw new ArgumentException("Merged content cannot be empty.", nameof(mergedContent));
                
            ResolvedContent = mergedContent;
            IsResolved = true;
        }
    }
}
