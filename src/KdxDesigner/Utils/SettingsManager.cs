using System.IO;
using System.Text.Json;

using KdxDesigner.Models.Define;

namespace KdxDesigner.Utils
{
    public static class SettingsManager
    {
        public static AppSettings Settings { get; private set; } = new AppSettings();
        private static string ConfigPath => "project_settings.json";

        public static void Load()
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }

        public static void Save()
        {
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
    }
}
