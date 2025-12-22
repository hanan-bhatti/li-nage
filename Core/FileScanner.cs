using Linage.Infrastructure;

namespace Linage.Core
{
    public class FileScanner
    {
        public string RootPath { get; set; } = string.Empty;
        public int ScanDepth { get; set; }

        public FileMetadata CreateMetadata(string path) 
        { 
            return new FileMetadata { Path = path }; 
        }
    }
}
