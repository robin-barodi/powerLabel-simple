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

        public static void WriteSettings(Settings settings)
        {
            string jsonString = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("settings.json", jsonString);
        }

        public static Settings ReadSettings()
        {
            string fileName = "settings.json";
            if (!File.Exists(fileName))
            {
                var defaults = new Settings();
                WriteSettings(defaults);
                return defaults;
            }
            try
            {
                string jsonString = File.ReadAllText(fileName);
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