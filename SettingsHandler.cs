using System;
using System.IO;
using System.Text.Json;

namespace powerLabel
{
    public class SettingsHandler
    {
        public class Settings
        {
            public string printerShareName { get; set; }
        }

        private static string SettingsPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        public static void WriteSettings(Settings settings)
        {
            string jsonString = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, jsonString);
        }

        public static Settings ReadSettings()
        {
            if (!File.Exists(SettingsPath))
            {
                var defaults = new Settings();
                WriteSettings(defaults);
                return defaults;
            }
            try
            {
                string jsonString = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<Settings>(jsonString) ?? new Settings();
            }
            catch (Exception)
            {
                var defaults = new Settings();
                WriteSettings(defaults);
                return defaults;
            }
        }
    }
}