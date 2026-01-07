using System;
using System.IO;
using Newtonsoft.Json;

namespace Linage.Infrastructure
{
    public class ConfigService
    {
        private static string ConfigPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".linageconfig");

        public class UserConfig
        {
            public string UserName { get; set; }
            public string UserEmail { get; set; }
        }

        public static UserConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    return JsonConvert.DeserializeObject<UserConfig>(json);
                }
            }
            catch { }
            return new UserConfig { UserName = Environment.UserName, UserEmail = "" };
        }

        public static void Save(string name, string email)
        {
            try
            {
                var config = new UserConfig { UserName = name, UserEmail = email };
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(ConfigPath, json);
                
                // Also set env vars for current session
                Environment.SetEnvironmentVariable("LINAGE_USER", name);
                Environment.SetEnvironmentVariable("LINAGE_EMAIL", email);
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to save config: {ex.Message}");
            }
        }
    }
}
