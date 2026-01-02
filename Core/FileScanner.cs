using System;
using System.IO;
using Linage.Infrastructure;

namespace Linage.Core
{
    public class FileScanner
    {
        public string RootPath { get; set; } = string.Empty;
        public int ScanDepth { get; set; }

        public FileMetadata CreateMetadata(string path) 
        { 
            var fileInfo = new FileInfo(path);
            return new FileMetadata(
                path, 
                path, 
                fileInfo.Exists ? fileInfo.Length : 0, 
                fileInfo.Exists ? fileInfo.LastWriteTime : DateTime.Now,
                false
            ); 
        }
    }
}
