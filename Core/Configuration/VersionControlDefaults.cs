namespace Linage.Core.Configuration
{
    /// <summary>
    /// Default values and constants for version control operations.
    /// </summary>
    public static class VersionControlDefaults
    {
        // Remote Configuration
        public const string DefaultRemoteName = "origin";
        public const string DefaultBranchName = "main";
        public const string FallbackBranchName = "master";

        // Merge Configuration
        public const int MaxMergeConflictRetries = 3;
        public const bool AutoResolveNonConflicting = true;
    }

    /// <summary>
    /// File change type constants.
    /// </summary>
    public static class FileChangeTypes
    {
        public const string New = "NEW";
        public const string Modified = "MODIFIED";
        public const string Deleted = "DELETED";
        public const string Renamed = "RENAMED";
        public const string Copied = "COPIED";
    }

    /// <summary>
    /// View name constants for sidebar navigation.
    /// </summary>
    public static class ViewNames
    {
        public const string Explorer = "Explorer";
        public const string SourceControl = "SourceControl";
        public const string History = "History";
        public const string Debug = "Debug";
        public const string Search = "Search";
        public const string Extensions = "Extensions";
    }

    /// <summary>
    /// Tab identifier prefixes for special tabs.
    /// </summary>
    public static class TabPrefixes
    {
        public const string Conflict = "CONFLICT:";
        public const string Graph = "Graph";
        public const string Welcome = "WELCOME";
        public const string Settings = "SETTINGS";
    }

    /// <summary>
    /// Icon identifiers using Segoe MDL2 Assets Unicode values.
    /// </summary>
    public static class Icons
    {
        // Navigation
        public const string Explorer = "\uE838";        // Folder icon
        public const string SourceControl = "\uEA68";   // Git icon
        public const string History = "\uE81C";         // Clock/History icon
        public const string Debug = "\uE890";           // Bug icon
        public const string Search = "\uE721";          // Search icon
        public const string Settings = "\uE713";        // Settings/Gear icon

        // Actions
        public const string Add = "\uE710";             // Add/Plus icon
        public const string Remove = "\uE711";          // Remove/Minus icon
        public const string Refresh = "\uE72C";         // Refresh icon
        public const string Save = "\uE74E";            // Save/Disk icon
        public const string Close = "\uE711";           // Close/X icon
        public const string Commit = "\uE73E";          // Checkmark icon

        // File States
        public const string FileNew = "\uE710";         // New file
        public const string FileModified = "\uE70F";    // Modified file
        public const string FileDeleted = "\uE74D";     // Deleted file
        public const string FileRenamed = "\uE8AC";     // Renamed file

        // Git Operations
        public const string Branch = "\uE71B";          // Branch icon
        public const string Merge = "\uE8AB";           // Merge icon
        public const string Pull = "\uE896";            // Download icon
        public const string Push = "\uE898";            // Upload icon
        public const string Clone = "\uE8C8";           // Copy icon

        // UI Elements
        public const string Notification = "\uEA8F";    // Bell icon
        public const string Warning = "\uE7BA";         // Warning triangle
        public const string Error = "\uE783";           // Error X
        public const string Info = "\uE946";            // Info i
    }

    /// <summary>
    /// Keyboard shortcut definitions.
    /// </summary>
    public static class Shortcuts
    {
        public const string Save = "Ctrl+S";
        public const string SaveAll = "Ctrl+Shift+S";
        public const string Close = "Ctrl+W";
        public const string CloseAll = "Ctrl+Shift+W";
        public const string Commit = "Ctrl+Enter";
        public const string Refresh = "F5";
        public const string Find = "Ctrl+F";
        public const string FindInFiles = "Ctrl+Shift+F";
        public const string CommandPalette = "Ctrl+Shift+P";
        public const string QuickOpen = "Ctrl+P";
        public const string ToggleTerminal = "Ctrl+`";
        public const string ToggleSidebar = "Ctrl+B";
    }
}
