using System;
using System.Collections.Generic;
using Linage.Core;
using Linage.Infrastructure;

namespace Linage.Controllers
{
    public class ScanController
    {
        public string Status { get; set; } = "Idle";
        public DateTime LastRunTime { get; set; }
        public FileScanner Scanner { get; set; } = new FileScanner();
        public ChangeDetector Detector { get; set; }
        private readonly FileService _fileService;
        
        public ScanController()
        {
            _fileService = new FileService(new HashService());
        }
        
        /// <summary>
        /// Scan a directory for files and create metadata
        /// </summary>
        public List<FileMetadata> ScanDirectory(string directoryPath, string rootPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
                throw new ArgumentNullException(nameof(directoryPath));
                
            Status = "Scanning...";
            LastRunTime = DateTime.Now;
            
            try
            {
                Scanner.RootPath = rootPath;
                var files = _fileService.ScanDirectory(directoryPath, rootPath);
                Status = $"Scanned {files.Count} files";
                return files;
            }
            catch (Exception ex)
            {
                Status = $"Error: {ex.Message}";
                throw;
            }
        }
        
        /// <summary>
        /// Start monitoring changes in a directory
        /// </summary>
        public void StartMonitoring(string rootPath)
        {
            if (Detector != null)
            {
                Detector.StopMonitoring();
                Detector.Dispose();
            }
            
            Detector = new ChangeDetector(rootPath);
            Detector.StartMonitoring();
            Status = "Monitoring changes";
        }
        
        /// <summary>
        /// Get list of changed files
        /// </summary>
        public List<string> GetChangedFiles()
        {
            if (Detector == null)
                return new List<string>();
                
            return Detector.GetChangedFiles();
        }
    }
}
