using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace Linage.GUI.Theme
{
    /// <summary>
    /// Manages application themes and provides theme switching capabilities
    /// </summary>
    public class ThemeManager
    {
        private static ThemeManager _instance;
        private static readonly object _lock = new object();
        private ThemeConfig _currentTheme;
        private readonly string _themeConfigPath;

        public static ThemeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ThemeManager();
                        }
                    }
                }
                return _instance;
            }
        }

        private ThemeManager()
        {
            _themeConfigPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Linage",
                "theme.json"
            );
            LoadTheme();
        }

        public ThemeConfig CurrentTheme => _currentTheme;

        public event EventHandler ThemeChanged;

        /// <summary>
        /// Load theme from configuration or use default
        /// </summary>
        private void LoadTheme()
        {
            try
            {
                if (File.Exists(_themeConfigPath))
                {
                    var json = File.ReadAllText(_themeConfigPath);
                    _currentTheme = JsonConvert.DeserializeObject<ThemeConfig>(json);
                }
            }
            catch
            {
                // If loading fails, use default
            }

            // Use default if no theme loaded
            if (_currentTheme == null)
            {
                _currentTheme = GetDefaultDarkTheme();
            }

            ApplyTheme();
        }

        /// <summary>
        /// Save current theme to configuration
        /// </summary>
        public void SaveTheme()
        {
            try
            {
                var directory = Path.GetDirectoryName(_themeConfigPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonConvert.SerializeObject(_currentTheme, Formatting.Indented);
                File.WriteAllText(_themeConfigPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save theme: {ex.Message}", 
                    "Theme Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Switch to a new theme
        /// </summary>
        public void SwitchTheme(ThemeConfig newTheme)
        {
            _currentTheme = newTheme;
            ApplyTheme();
            SaveTheme();
            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Apply current theme to ModernTheme static class
        /// </summary>
        private void ApplyTheme()
        {
            ModernTheme.BackColor = ColorFromHex(_currentTheme.BackColor);
            ModernTheme.SurfaceColor = ColorFromHex(_currentTheme.SurfaceColor);
            ModernTheme.SurfaceLight = ColorFromHex(_currentTheme.SurfaceLight);
            ModernTheme.ActivityBarColor = ColorFromHex(_currentTheme.ActivityBarColor);

            ModernTheme.TextPrimary = ColorFromHex(_currentTheme.TextPrimary);
            ModernTheme.TextSecondary = ColorFromHex(_currentTheme.TextSecondary);
            ModernTheme.TextDisabled = ColorFromHex(_currentTheme.TextDisabled);

            ModernTheme.PrimaryColor = ColorFromHex(_currentTheme.PrimaryColor);
            ModernTheme.PrimaryDark = ColorFromHex(_currentTheme.PrimaryDark);
            ModernTheme.StatusBarColor = ColorFromHex(_currentTheme.StatusBarColor);

            ModernTheme.ErrorColor = ColorFromHex(_currentTheme.ErrorColor);
            ModernTheme.SuccessColor = ColorFromHex(_currentTheme.SuccessColor);
            ModernTheme.WarningColor = ColorFromHex(_currentTheme.WarningColor);

            ModernTheme.BorderColor = ColorFromHex(_currentTheme.BorderColor);
            ModernTheme.SplitterColor = ColorFromHex(_currentTheme.SplitterColor);

            ModernTheme.TabActive = ColorFromHex(_currentTheme.TabActive);
            ModernTheme.TabInactive = ColorFromHex(_currentTheme.TabInactive);
            ModernTheme.TabHover = ColorFromHex(_currentTheme.TabHover);

            ModernTheme.ScrollBarBack = ColorFromHex(_currentTheme.ScrollBarBack);
            ModernTheme.ScrollBarThumb = ColorFromHex(_currentTheme.ScrollBarThumb);
            ModernTheme.ScrollBarThumbHover = ColorFromHex(_currentTheme.ScrollBarThumbHover);
            ModernTheme.ScrollBarThumbActive = ColorFromHex(_currentTheme.ScrollBarThumbActive);
        }

        /// <summary>
        /// Get default VS Code Dark theme
        /// </summary>
        public static ThemeConfig GetDefaultDarkTheme()
        {
            return new ThemeConfig
            {
                Name = "VS Code Dark",
                BackColor = "#1E1E1E",
                SurfaceColor = "#252526",
                SurfaceLight = "#333333",
                ActivityBarColor = "#333333",
                TextPrimary = "#CCCCCC",
                TextSecondary = "#969696",
                TextDisabled = "#646464",
                PrimaryColor = "#007ACC",
                PrimaryDark = "#005A9E",
                StatusBarColor = "#007ACC",
                ErrorColor = "#F48771",
                SuccessColor = "#89D185",
                WarningColor = "#CCA700",
                BorderColor = "#404040",
                SplitterColor = "#2D2D2D",
                TabActive = "#1E1E1E",
                TabInactive = "#2D2D2D",
                TabHover = "#282828",
                ScrollBarBack = "#1E1E1E",
                ScrollBarThumb = "#424242",
                ScrollBarThumbHover = "#4F4F4F",
                ScrollBarThumbActive = "#646464"
            };
        }

        /// <summary>
        /// Get VS Code Light theme
        /// </summary>
        public static ThemeConfig GetLightTheme()
        {
            return new ThemeConfig
            {
                Name = "VS Code Light",
                BackColor = "#FFFFFF",
                SurfaceColor = "#F3F3F3",
                SurfaceLight = "#E8E8E8",
                ActivityBarColor = "#2C2C2C",
                TextPrimary = "#3B3B3B",
                TextSecondary = "#6A6A6A",
                TextDisabled = "#A6A6A6",
                PrimaryColor = "#007ACC",
                PrimaryDark = "#005A9E",
                StatusBarColor = "#007ACC",
                ErrorColor = "#E51400",
                SuccessColor = "#388A34",
                WarningColor = "#BF8803",
                BorderColor = "#E5E5E5",
                SplitterColor = "#E5E5E5",
                TabActive = "#FFFFFF",
                TabInactive = "#ECECEC",
                TabHover = "#F0F0F0",
                ScrollBarBack = "#F3F3F3",
                ScrollBarThumb = "#C4C4C4",
                ScrollBarThumbHover = "#A0A0A0",
                ScrollBarThumbActive = "#7C7C7C"
            };
        }

        /// <summary>
        /// Convert hex color string to Color
        /// </summary>
        private Color ColorFromHex(string hex)
        {
            hex = hex.TrimStart('#');
            return Color.FromArgb(
                Convert.ToInt32(hex.Substring(0, 2), 16),
                Convert.ToInt32(hex.Substring(2, 2), 16),
                Convert.ToInt32(hex.Substring(4, 2), 16)
            );
        }

        /// <summary>
        /// Convert Color to hex string
        /// </summary>
        public static string ColorToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }

    /// <summary>
    /// Theme configuration model
    /// </summary>
    public class ThemeConfig
    {
        public string Name { get; set; }
        public string BackColor { get; set; }
        public string SurfaceColor { get; set; }
        public string SurfaceLight { get; set; }
        public string ActivityBarColor { get; set; }
        public string TextPrimary { get; set; }
        public string TextSecondary { get; set; }
        public string TextDisabled { get; set; }
        public string PrimaryColor { get; set; }
        public string PrimaryDark { get; set; }
        public string StatusBarColor { get; set; }
        public string ErrorColor { get; set; }
        public string SuccessColor { get; set; }
        public string WarningColor { get; set; }
        public string BorderColor { get; set; }
        public string SplitterColor { get; set; }
        public string TabActive { get; set; }
        public string TabInactive { get; set; }
        public string TabHover { get; set; }
        public string ScrollBarBack { get; set; }
        public string ScrollBarThumb { get; set; }
        public string ScrollBarThumbHover { get; set; }
        public string ScrollBarThumbActive { get; set; }
    }
}
