namespace Linage.GUI.Configuration
{
    /// <summary>
    /// 4px-based spacing system for consistent layout throughout the application.
    /// Based on common design systems like Material Design and VS Code.
    /// </summary>
    public static class Spacing
    {
        // Base unit: 4px
        public const int Unit = 4;

        // Spacing scale (multiples of 4px)
        public const int XXSmall = Unit;      // 4px - Minimal spacing
        public const int XSmall = Unit * 2;   // 8px - Tight spacing
        public const int Small = Unit * 3;    // 12px - Compact spacing
        public const int Medium = Unit * 4;   // 16px - Standard spacing
        public const int Large = Unit * 6;    // 24px - Generous spacing
        public const int XLarge = Unit * 8;   // 32px - Section spacing
        public const int XXLarge = Unit * 12; // 48px - Major section spacing

        // Component-specific spacing
        public static class Control
        {
            public const int Padding = Small;           // 12px internal padding
            public const int Margin = Medium;           // 16px external margin
            public const int Gap = Medium;              // 16px gap between controls
            public const int BorderWidth = 1;           // 1px border width
        }

        public static class Panel
        {
            public const int Padding = Medium;          // 16px panel padding
            public const int HeaderHeight = XLarge;     // 32px header height
            public const int FooterHeight = XLarge;     // 32px footer height
        }

        public static class Layout
        {
            public const int SidebarWidth = 250;        // Default sidebar width
            public const int ActivityBarWidth = 50;     // Activity bar width
            public const int StatusBarHeight = 24;      // Status bar height
            public const int TabHeight = 35;            // Tab height
            public const int SplitterWidth = 1;         // Splitter width
        }
    }

    /// <summary>
    /// Typography scale for consistent text styling.
    /// </summary>
    public static class Typography
    {
        // Font families
        public const string DefaultFontFamily = "Segoe UI";
        public const string MonospaceFontFamily = "Consolas";
        public const string IconFontFamily = "Segoe MDL2 Assets";

        // Font sizes (in points)
        public const float XXSmall = 8f;
        public const float XSmall = 9f;
        public const float Small = 10f;
        public const float Medium = 11f;      // Default body text
        public const float Large = 12f;
        public const float XLarge = 14f;
        public const float XXLarge = 16f;
        public const float Huge = 20f;

        // Line heights (relative to font size)
        public const float LineHeightNormal = 1.5f;
        public const float LineHeightCompact = 1.25f;
        public const float LineHeightLoose = 1.75f;

        // Font weights (not directly supported in WinForms, documented for future)
        public static class Weight
        {
            public const string Light = "Light";
            public const string Regular = "Regular";
            public const string SemiBold = "Semibold";
            public const string Bold = "Bold";
        }
    }

    /// <summary>
    /// UI layout configuration with defaults that can be customized.
    /// </summary>
    public class UILayoutConfiguration
    {
        // Window Layout
        public int DefaultWindowWidth { get; set; } = 1200;
        public int DefaultWindowHeight { get; set; } = 800;
        public int MinimumWindowWidth { get; set; } = 800;
        public int MinimumWindowHeight { get; set; } = 600;

        // Splitter Distances
        public int SidebarWidth { get; set; } = Spacing.Layout.SidebarWidth;
        public int EditorPanelHeight { get; set; } = 600;

        // Activity Bar
        public int ActivityBarWidth { get; set; } = Spacing.Layout.ActivityBarWidth;
        public int ActivityButtonHeight { get; set; } = 50;

        // Status Bar
        public int StatusBarHeight { get; set; } = Spacing.Layout.StatusBarHeight;

        // Tabs
        public int TabHeight { get; set; } = Spacing.Layout.TabHeight;
        public int TabDefaultWidth { get; set; } = 150;
        public int TabMinWidth { get; set; } = 80;
        public int TabMaxWidth { get; set; } = 250;

        // Refresh Intervals (milliseconds)
        public int RepositoryRefreshIntervalMs { get; set; } = 2000;
        public int FileWatcherDebounceMs { get; set; } = 300;

        // Editor
        public int EditorLineNumberWidth { get; set; } = 50;
        public int EditorMarginWidth { get; set; } = 80;

        public static UILayoutConfiguration Default => new UILayoutConfiguration();

        /// <summary>
        /// Loads configuration from user settings or returns defaults.
        /// </summary>
        public static UILayoutConfiguration LoadFromSettings()
        {
            // TODO: Load from app.config, JSON file, or user preferences
            return Default;
        }

        /// <summary>
        /// Saves current configuration to user settings.
        /// </summary>
        public void SaveToSettings()
        {
            // TODO: Save to app.config, JSON file, or user preferences
        }
    }
}
