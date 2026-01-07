using System;
using System.IO;
using System.Xml.Linq;

namespace Linage.Core.Configuration
{
    /// <summary>
    /// Enterprise-grade configuration management with validation and caching
    /// </summary>
    public class ConfigurationManager
    {
        private static readonly Lazy<ConfigurationManager> _instance = 
            new Lazy<ConfigurationManager>(() => new ConfigurationManager());

        public static ConfigurationManager Instance => _instance.Value;

        private XDocument _configDoc;
        private readonly string _configPath;

        private ConfigurationManager()
        {
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "linage.config");
            LoadConfiguration();
        }

        /// <summary>
        /// Loads configuration from file or creates default
        /// </summary>
        private void LoadConfiguration()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    _configDoc = XDocument.Load(_configPath);
                }
                else
                {
                    _configDoc = CreateDefaultConfiguration();
                    SaveConfiguration();
                }
            }
            catch (Exception ex)
            {
                Infrastructure.DebugLogger.Warn($"Failed to load configuration: {ex.Message}");
                _configDoc = CreateDefaultConfiguration();
            }
        }

        /// <summary>
        /// Gets a configuration value
        /// </summary>
        public string GetValue(string section, string key, string defaultValue = null)
        {
            try
            {
                var element = _configDoc?.Root?.Element(section)?.Element(key);
                return element?.Value ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Gets an integer configuration value
        /// </summary>
        public int GetInt(string section, string key, int defaultValue = 0)
        {
            var value = GetValue(section, key);
            return int.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// Gets a boolean configuration value
        /// </summary>
        public bool GetBool(string section, string key, bool defaultValue = false)
        {
            var value = GetValue(section, key);
            return bool.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// Sets a configuration value
        /// </summary>
        public void SetValue(string section, string key, string value)
        {
            try
            {
                var sectionElement = _configDoc.Root.Element(section);
                if (sectionElement == null)
                {
                    sectionElement = new XElement(section);
                    _configDoc.Root.Add(sectionElement);
                }

                var keyElement = sectionElement.Element(key);
                if (keyElement == null)
                {
                    sectionElement.Add(new XElement(key, value));
                }
                else
                {
                    keyElement.Value = value;
                }

                SaveConfiguration();
            }
            catch (Exception ex)
            {
                Infrastructure.DebugLogger.Error($"Failed to set configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves configuration to file
        /// </summary>
        private void SaveConfiguration()
        {
            try
            {
                _configDoc.Save(_configPath);
            }
            catch (Exception ex)
            {
                Infrastructure.DebugLogger.Error($"Failed to save configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates default configuration document
        /// </summary>
        private XDocument CreateDefaultConfiguration()
        {
            return new XDocument(
                new XElement("Configuration",
                    new XElement("General",
                        new XElement("AutoSave", "true"),
                        new XElement("ConfirmDelete", "true"),
                        new XElement("MaxRecentFiles", "10")
                    ),
                    new XElement("Editor",
                        new XElement("FontSize", "10"),
                        new XElement("WordWrap", "true"),
                        new XElement("LineNumbers", "true")
                    ),
                    new XElement("Performance",
                        new XElement("EnableSyntaxHighlighting", "true"),
                        new XElement("EnableAutocomplete", "true"),
                        new XElement("CacheDuration", "300")
                    ),
                    new XElement("Network",
                        new XElement("Timeout", "30000"),
                        new XElement("RetryCount", "3"),
                        new XElement("EnableProxy", "false")
                    )
                )
            );
        }

        /// <summary>
        /// Resets configuration to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            _configDoc = CreateDefaultConfiguration();
            SaveConfiguration();
        }
    }

    /// <summary>
    /// Predefined configuration keys for type-safe access
    /// </summary>
    public static class ConfigKeys
    {
        public static class General
        {
            public const string AutoSave = "General:AutoSave";
            public const string ConfirmDelete = "General:ConfirmDelete";
            public const string MaxRecentFiles = "General:MaxRecentFiles";
        }

        public static class Editor
        {
            public const string FontSize = "Editor:FontSize";
            public const string WordWrap = "Editor:WordWrap";
            public const string LineNumbers = "Editor:LineNumbers";
        }

        public static class Performance
        {
            public const string EnableSyntaxHighlighting = "Performance:EnableSyntaxHighlighting";
            public const string EnableAutocomplete = "Performance:EnableAutocomplete";
            public const string CacheDuration = "Performance:CacheDuration";
        }

        public static class Network
        {
            public const string Timeout = "Network:Timeout";
            public const string RetryCount = "Network:RetryCount";
            public const string EnableProxy = "Network:EnableProxy";
        }
    }
}
