using System;

namespace Linage.Infrastructure
{
    public class FileService
    {
        public int BufferSize { get; set; }

        public FileMetadata GetMetadata(string path) 
        { 
            return new FileMetadata { Path = path }; 
        }
    }
}
