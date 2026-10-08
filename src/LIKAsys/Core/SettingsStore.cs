using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LIKAsys.Core
{
    public static class SettingsStore
    {
        private static readonly object Gate = new object();
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(AppInfo.SettingsFile))
                {
                    var json = File.ReadAllText(AppInfo.SettingsFile);
                    var s = JsonSerializer.Deserialize<AppSettings>(json, Options);
                    if (s != null) return s;
                }
            }
            catch (Exception ex) { AppInfo.Log("Settings load failed: " + ex.Message); }
            return new AppSettings();
        }

        public static void Save(AppSettings settings)
        {
            if (settings == null) return;
            try
            {
                lock (Gate)
                {
                    var json = JsonSerializer.Serialize(settings, Options);
                    var tmp = AppInfo.SettingsFile + ".tmp";
                    File.WriteAllText(tmp, json);
                    if (File.Exists(AppInfo.SettingsFile)) File.Delete(AppInfo.SettingsFile);
                    File.Move(tmp, AppInfo.SettingsFile);
                }
            }
            catch (Exception ex) { AppInfo.Log("Settings save failed: " + ex.Message); }
        }
    }
}
