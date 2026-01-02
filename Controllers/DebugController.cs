using System;
using System.Collections.Generic;
using System.Text;
using Linage.Core;
using Linage.Core.Diff;
using Linage.Infrastructure;

namespace Linage.Controllers
{
    public class DebugController
    {
        public string Status { get; set; } = "Idle";
        public DateTime LastRunTime { get; set; }

        public LineTracker Tracker { get; set; }
        public ErrorTrace Trace { get; set; }
        public SolutionIndex Solutions { get; set; }
        
        private readonly List<string> _debugLog = new List<string>();
        
        public DebugController()
        {
            Tracker = new LineTracker(new MyersDiffStrategy());
            Trace = new ErrorTrace();
            Solutions = new SolutionIndex();
        }
        
        /// <summary>
        /// Log a debug message
        /// </summary>
        public void Log(string message, string level = "INFO")
        {
            var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
            _debugLog.Add(entry);
            
            // Persist to file
            LogLevel logLevel = LogLevel.Info;
            if (level.ToUpper() == "ERROR") logLevel = LogLevel.Error;
            else if (level.ToUpper() == "WARN" || level.ToUpper() == "WARNING") logLevel = LogLevel.Warning;
            else if (level.ToUpper() == "DEBUG") logLevel = LogLevel.Debug;
            
            Logger.Log(message, logLevel);
            
            LastRunTime = DateTime.Now;
            Status = $"Last log: {level}";
        }
        
        /// <summary>
        /// Get all debug logs
        /// </summary>
        public List<string> GetLogs()
        {
            return new List<string>(_debugLog);
        }
        
        /// <summary>
        /// Clear debug logs
        /// </summary>
        public void ClearLogs()
        {
            _debugLog.Clear();
            Status = "Logs cleared";
        }
        
        /// <summary>
        /// Perform a diff comparison for debugging
        /// </summary>
        public string PerformDiff(string oldContent, string newContent, string strategy = "Myers")
        {
            IDiffStrategy diffStrategy;
            switch (strategy.ToUpper())
            {
                case "PATIENT":
                    diffStrategy = new PatientDiffStrategy();
                    break;
                case "MINIMAL":
                    diffStrategy = new MinimalDiffStrategy();
                    break;
                default:
                    diffStrategy = new MyersDiffStrategy();
                    break;
            }
            
            Tracker.SetDiffStrategy(diffStrategy);
            var changes = Tracker.GenerateLineChanges(oldContent, newContent);
            
            var sb = new StringBuilder();
            sb.AppendLine($"Diff Strategy: {strategy}");
            sb.AppendLine($"Total Changes: {changes.Count}");
            sb.AppendLine("---");
            
            foreach (var change in changes)
            {
                sb.AppendLine($"Line {change.LineNumber}: {change.ChangeType}");
            }
            
            return sb.ToString();
        }
    }
}
