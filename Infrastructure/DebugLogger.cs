using System;

namespace Linage.Infrastructure
{
    /// <summary>
    /// Static debug logger that broadcasts messages to subscribers (like DebugView).
    /// Use this to add debug output that appears in the app's Debug Console.
    /// </summary>
    public static class DebugLogger
    {
        /// <summary>
        /// Event fired when a debug message is logged.
        /// </summary>
        public static event Action<string, DebugLevel> OnMessage;

        /// <summary>
        /// Log a debug message.
        /// </summary>
        public static void Log(string message, DebugLevel level = DebugLevel.Info)
        {
            OnMessage?.Invoke(message, level);
        }

        /// <summary>
        /// Log an info message.
        /// </summary>
        public static void Info(string message)
        {
            Log(message, DebugLevel.Info);
        }

        /// <summary>
        /// Log a warning message.
        /// </summary>
        public static void Warn(string message)
        {
            Log(message, DebugLevel.Warning);
        }

        /// <summary>
        /// Log an error message.
        /// </summary>
        public static void Error(string message)
        {
            Log(message, DebugLevel.Error);
        }

        /// <summary>
        /// Log a verbose/trace message for detailed debugging.
        /// </summary>
        public static void Trace(string message)
        {
            Log(message, DebugLevel.Trace);
        }
    }

    public enum DebugLevel
    {
        Trace,
        Info,
        Warning,
        Error
    }
}
