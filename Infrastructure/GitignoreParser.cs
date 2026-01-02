using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Linage.Infrastructure
{
    /// <summary>
    /// Parser for .gitignore files with full pattern matching support......
    /// Supports glob patterns, negation, directory-specific rules, and comments...
    /// </summary>
    public class GitignoreParser
    {
        private readonly List<IgnoreRule> _rules = new List<IgnoreRule>();
        private readonly string _basePath;

        public GitignoreParser(string basePath)
        {
            _basePath = basePath ?? throw new ArgumentNullException(nameof(basePath));
        }

        /// <summary>
        /// Load patterns from a .gitignore file.
        /// </summary>
        public void LoadFromFile(string gitignorePath)
        {
            if (!File.Exists(gitignorePath))
                return;

            var lines = File.ReadAllLines(gitignorePath);
            foreach (var line in lines)
            {
                AddPattern(line);
            }
        }

        /// <summary>
        /// Add a single pattern.
        /// </summary>
        public void AddPattern(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return;

            var trimmed = pattern.Trim();

            // Skip comments
            if (trimmed.StartsWith("#"))
                return;

            // Check for negation
            bool isNegation = trimmed.StartsWith("!");
            if (isNegation)
                trimmed = trimmed.Substring(1);

            // Skip empty patterns
            if (string.IsNullOrWhiteSpace(trimmed))
                return;

            // Check if directory-only pattern
            bool directoryOnly = trimmed.EndsWith("/");
            if (directoryOnly)
                trimmed = trimmed.TrimEnd('/');

            // Convert glob pattern to regex
            var regex = ConvertGlobToRegex(trimmed);

            _rules.Add(new IgnoreRule
            {
                Pattern = trimmed,
                Regex = regex,
                IsNegation = isNegation,
                DirectoryOnly = directoryOnly
            });
        }

        /// <summary>
        /// Check if a path should be ignored.
        /// </summary>
        public bool IsIgnored(string path, bool isDirectory = false)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            // Make path relative to base path
            var relativePath = MakeRelativePath(path);
            
            // Normalize path separators
            relativePath = relativePath.Replace('\\', '/');

            bool ignored = false;

            // Process rules in order (later rules override earlier ones)
            foreach (var rule in _rules)
            {
                // Skip directory-only rules for files
                if (rule.DirectoryOnly && !isDirectory)
                    continue;

                // Check if pattern matches
                if (rule.Regex.IsMatch(relativePath) || rule.Regex.IsMatch(Path.GetFileName(relativePath)))
                {
                    ignored = !rule.IsNegation;
                }
            }

            return ignored;
        }

        /// <summary>
        /// Convert glob pattern to regex.
        /// </summary>
        private Regex ConvertGlobToRegex(string pattern)
        {
            // Escape special regex characters except glob wildcards
            var regexPattern = Regex.Escape(pattern);

            // Replace escaped glob wildcards with regex equivalents
            regexPattern = regexPattern.Replace(@"\*\*", ".*");  // ** matches any path
            regexPattern = regexPattern.Replace(@"\*", "[^/]*"); // * matches anything except /
            regexPattern = regexPattern.Replace(@"\?", ".");     // ? matches single char

            // Handle leading slash (anchored to root)
            if (pattern.StartsWith("/"))
            {
                regexPattern = "^" + regexPattern.Substring(1);
            }
            else
            {
                // Pattern can match at any level
                regexPattern = "(^|/)" + regexPattern;
            }

            // Anchor to end
            regexPattern += "$";

            return new Regex(regexPattern, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Make path relative to base path.
        /// </summary>
        private string MakeRelativePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            var fullPath = Path.GetFullPath(path);
            var fullBase = Path.GetFullPath(_basePath);

            if (fullPath.StartsWith(fullBase, StringComparison.OrdinalIgnoreCase))
            {
                var relative = fullPath.Substring(fullBase.Length);
                return relative.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            return path;
        }

        /// <summary>
        /// Get default ignore patterns (common patterns like .git, bin, obj).
        /// </summary>
        public static List<string> GetDefaultPatterns()
        {
            return new List<string>
            {
                ".git/",
                ".linage/",
                "bin/",
                "obj/",
                "*.exe",
                "*.dll",
                "*.pdb",
                "*.user",
                "*.suo",
                ".vs/",
                "node_modules/",
                "*.log",
                "*.tmp",
                "*.temp",
                "Thumbs.db",
                ".DS_Store"
            };
        }

        /// <summary>
        /// Load default patterns.
        /// </summary>
        public void LoadDefaultPatterns()
        {
            foreach (var pattern in GetDefaultPatterns())
            {
                AddPattern(pattern);
            }
        }

        private class IgnoreRule
        {
            public string Pattern { get; set; }
            public Regex Regex { get; set; }
            public bool IsNegation { get; set; }
            public bool DirectoryOnly { get; set; }
        }
    }
}
