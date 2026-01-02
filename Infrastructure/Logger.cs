using System;
using System.IO;

namespace Linage.Infrastructure
{
    public static class Logger
    {
        private static string _logPath;
        private static readonly object _lock = new object();

        public static void Initialize(string logDirectory)
        {
            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);

            string fileName = $"linage_{DateTime.Now:yyyyMMdd}.log";
            _logPath = Path.Combine(logDirectory, fileName);
        }

        public static void Log(string message, LogLevel level = LogLevel.Info)
        {
            if (string.IsNullOrEmpty(_logPath)) return;

            var logEntry = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}";

            try
            {
                lock (_lock)
                {
                    File.AppendAllText(_logPath, logEntry + Environment.NewLine);
                }
            }
            catch
            {
                // Fail silently if logging fails to avoid crashing app
            }
        }

        public static void LogError(string message, Exception ex)
        {
            Log($"{message}\nException: {ex.Message}\nStack: {ex.StackTrace}", LogLevel.Error);
        }
    }

    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Debug
    }
}
