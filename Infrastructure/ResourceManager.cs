using System;
using System.Collections.Generic;

namespace Linage.Infrastructure
{
    /// <summary>
    /// Enterprise resource management utility for proper disposal and cleanup
    /// Tracks resources and ensures cleanup on application shutdown
    /// </summary>
    public static class ResourceManager
    {
        private static readonly List<IDisposable> _managedResources = new List<IDisposable>();
        private static readonly object _lock = new object();

        /// <summary>
        /// Registers a resource for cleanup on application shutdown
        /// </summary>
        public static void RegisterResource(IDisposable resource)
        {
            if (resource == null) return;

            lock (_lock)
            {
                if (!_managedResources.Contains(resource))
                {
                    _managedResources.Add(resource);
                }
            }
        }

        /// <summary>
        /// Unregisters and disposes a resource
        /// </summary>
        public static void UnregisterResource(IDisposable resource)
        {
            if (resource == null) return;

            lock (_lock)
            {
                if (_managedResources.Contains(resource))
                {
                    _managedResources.Remove(resource);
                    try
                    {
                        resource.Dispose();
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.Warn($"Error disposing resource: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Cleans up all managed resources
        /// Call this on application shutdown
        /// </summary>
        public static void CleanupAll()
        {
            lock (_lock)
            {
                foreach (var resource in _managedResources)
                {
                    try
                    {
                        resource?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.Error($"Error during cleanup: {ex.Message}");
                    }
                }

                _managedResources.Clear();
            }
        }

        /// <summary>
        /// Gets the count of currently managed resources
        /// </summary>
        public static int GetManagedResourceCount()
        {
            lock (_lock)
            {
                return _managedResources.Count;
            }
        }
    }

    /// <summary>
    /// Scope-based resource management for using statement
    /// </summary>
    public class ResourceScope : IDisposable
    {
        private List<IDisposable> _resources = new List<IDisposable>();
        private bool _disposed = false;

        public void Add(IDisposable resource)
        {
            if (resource != null && !_disposed)
            {
                _resources.Add(resource);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var resource in _resources)
            {
                try
                {
                    resource?.Dispose();
                }
                catch (Exception ex)
                {
                    DebugLogger.Warn($"Error disposing resource: {ex.Message}");
                }
            }

            _resources.Clear();
        }
    }

    /// <summary>
    /// Connection pooling utility for database connections
    /// </summary>
    public class ConnectionPool : IDisposable
    {
        private readonly Queue<System.Data.SqlClient.SqlConnection> _available = 
            new Queue<System.Data.SqlClient.SqlConnection>();
        private readonly HashSet<System.Data.SqlClient.SqlConnection> _inUse = 
            new HashSet<System.Data.SqlClient.SqlConnection>();
        private readonly string _connectionString;
        private readonly int _maxPoolSize;
        private readonly object _poolLock = new object();
        private bool _disposed = false;

        public ConnectionPool(string connectionString, int maxPoolSize = 10)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _maxPoolSize = maxPoolSize;
        }

        public System.Data.SqlClient.SqlConnection GetConnection()
        {
            lock (_poolLock)
            {
                System.Data.SqlClient.SqlConnection connection = null;

                if (_available.Count > 0)
                {
                    connection = _available.Dequeue();
                    if (connection.State == System.Data.ConnectionState.Closed)
                    {
                        connection.Open();
                    }
                }
                else if (_inUse.Count < _maxPoolSize)
                {
                    connection = new System.Data.SqlClient.SqlConnection(_connectionString);
                    connection.Open();
                }
                else
                {
                    throw new InvalidOperationException("Connection pool exhausted");
                }

                if (connection != null)
                {
                    _inUse.Add(connection);
                }

                return connection;
            }
        }

        public void ReleaseConnection(System.Data.SqlClient.SqlConnection connection)
        {
            if (connection == null) return;

            lock (_poolLock)
            {
                if (_inUse.Contains(connection))
                {
                    _inUse.Remove(connection);
                    _available.Enqueue(connection);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            lock (_poolLock)
            {
                foreach (var conn in _available)
                {
                    try
                    {
                        conn?.Close();
                        conn?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.Warn($"Error closing connection: {ex.Message}");
                    }
                }

                foreach (var conn in _inUse)
                {
                    try
                    {
                        conn?.Close();
                        conn?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.Warn($"Error closing connection: {ex.Message}");
                    }
                }

                _available.Clear();
                _inUse.Clear();
            }
        }
    }

    /// <summary>
    /// Memory usage tracking and optimization
    /// </summary>
    public static class MemoryManager
    {
        /// <summary>
        /// Gets current memory usage in MB
        /// </summary>
        public static long GetCurrentMemoryMB()
        {
            return GC.GetTotalMemory(false) / (1024 * 1024);
        }

        /// <summary>
        /// Performs garbage collection if memory exceeds threshold
        /// </summary>
        public static void OptimizeMemoryIfNeeded(long thresholdMB = 500)
        {
            var currentMemory = GetCurrentMemoryMB();
            if (currentMemory > thresholdMB)
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized);
                DebugLogger.Info($"Memory optimization triggered. Used: {currentMemory}MB");
            }
        }

        /// <summary>
        /// Forces garbage collection and reports memory
        /// </summary>
        public static void ForceCleanup()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            DebugLogger.Info($"Force cleanup completed. Current memory: {GetCurrentMemoryMB()}MB");
        }
    }
}
