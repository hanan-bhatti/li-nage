using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Linage.Core.Validation
{
    /// <summary>
    /// Centralized input validation utilities for enterprise-grade input handling
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// Validates that a string is not null or whitespace
        /// </summary>
        public static bool IsValidRequired(string value, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(value))
            {
                error = "Value cannot be empty";
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates repository path exists and is valid
        /// </summary>
        public static bool IsValidRepositoryPath(string path, out string error)
        {
            error = null;
            if (!IsValidRequired(path, out error)) return false;

            if (!System.IO.Directory.Exists(path))
            {
                error = "Directory does not exist";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates branch name format
        /// </summary>
        public static bool IsValidBranchName(string name, out string error)
        {
            error = null;
            if (!IsValidRequired(name, out error)) return false;

            // Branch name validation rules
            if (name.Length > 255)
            {
                error = "Branch name too long (max 255 characters)";
                return false;
            }

            // Invalid characters for branch names
            var invalidChars = new[] { ' ', '\t', '\n', '\\', '~', '^', ':', '?', '*', '[' };
            if (name.Any(c => invalidChars.Contains(c)))
            {
                error = "Branch name contains invalid characters";
                return false;
            }

            if (name.StartsWith("-") || name.StartsWith("."))
            {
                error = "Branch name cannot start with '-' or '.'";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates commit message is meaningful
        /// </summary>
        public static bool IsValidCommitMessage(string message, out string error)
        {
            error = null;
            if (!IsValidRequired(message, out error)) return false;

            if (message.Length < 3)
            {
                error = "Commit message too short (minimum 3 characters)";
                return false;
            }

            if (message.Length > 1000)
            {
                error = "Commit message too long (maximum 1000 characters)";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates remote URL format
        /// </summary>
        public static bool IsValidRemoteUrl(string url, out string error)
        {
            error = null;
            if (!IsValidRequired(url, out error)) return false;

            // Check if it's a valid URI
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                // Also allow SSH format: git@github.com:user/repo.git
                if (!url.Contains("@") || !url.Contains(":"))
                {
                    error = "Invalid URL format. Use HTTP(S) or SSH format";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Validates file path is safe (prevents directory traversal attacks)
        /// </summary>
        public static bool IsValidFilePath(string filePath, string baseDir, out string error)
        {
            error = null;
            if (!IsValidRequired(filePath, out error)) return false;

            try
            {
                var fullPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, filePath));
                var basePath = System.IO.Path.GetFullPath(baseDir);

                // Ensure the resolved path is within the base directory
                if (!fullPath.StartsWith(basePath))
                {
                    error = "Invalid file path - path traversal detected";
                    return false;
                }

                return true;
            }
            catch
            {
                error = "Invalid file path";
                return false;
            }
        }

        /// <summary>
        /// Validates email format
        /// </summary>
        public static bool IsValidEmail(string email, out string error)
        {
            error = null;
            if (!IsValidRequired(email, out error)) return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                error = "Invalid email format";
                return false;
            }
        }
    }

    /// <summary>
    /// Fluent validation builder for complex validation scenarios
    /// </summary>
    public class ValidatorBuilder
    {
        private List<string> _errors = new List<string>();

        public ValidatorBuilder ValidateRequired(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                _errors.Add($"{fieldName} is required");
            return this;
        }

        public ValidatorBuilder ValidateRange(int value, int min, int max, string fieldName)
        {
            if (value < min || value > max)
                _errors.Add($"{fieldName} must be between {min} and {max}");
            return this;
        }

        public ValidatorBuilder ValidatePattern(string value, string pattern, string fieldName)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(value, pattern))
                _errors.Add($"{fieldName} format is invalid");
            return this;
        }

        public bool IsValid(out List<string> errors)
        {
            errors = _errors;
            return _errors.Count == 0;
        }

        public string GetErrorMessage()
        {
            return string.Join("\n", _errors);
        }
    }
}
