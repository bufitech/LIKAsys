using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace LIKAsys
{
    /// <summary>Central place for branding / update endpoints.</summary>
    public static class AppInfo
    {
        public const string Name = "LIKAsys";
        public const string Tagline = "Made in Kosovo with \u2764\ufe0f";
        public const string Website = "https://Likaapps.com";
        public const string WebsiteShort = "Likaapps.com";

        // Where the update system looks for the newest build. Internal plumbing only -
        // nothing in the interface ever names it; the user only ever sees Likaapps.com.
        private const string FeedOwner = "bufitech";
        private const string FeedName = "LIKAsys";
        public static string UpdateFeed => $"https://api.github.com/repos/{FeedOwner}/{FeedName}/releases/latest";
        public static string DownloadPage => Website;

        public static Version CurrentVersion
        {
            get
            {
                var v = Assembly.GetExecutingAssembly().GetName().Version;
                return v ?? new Version(1, 0, 0, 0);
            }
        }

        public static string VersionText
        {
            get
            {
                var v = CurrentVersion;
                int build = v.Build < 0 ? 0 : v.Build;
                return build == 0 ? $"v{v.Major}.{v.Minor}" : $"v{v.Major}.{v.Minor}.{build}";
            }
        }

        public static string ExePath
        {
            get
            {
                try { return Process.GetCurrentProcess().MainModule?.FileName ?? Assembly.GetExecutingAssembly().Location; }
                catch { return Assembly.GetExecutingAssembly().Location; }
            }
        }

        public static string InstallDir => Path.GetDirectoryName(ExePath) ?? AppContext.BaseDirectory;

        public static string DataDir
        {
            get
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Name);
                try { Directory.CreateDirectory(dir); } catch { }
                return dir;
            }
        }

        public static string SettingsFile => Path.Combine(DataDir, "settings.json");
        public static string LogFile => Path.Combine(DataDir, "likasys.log");

        public static void Log(string message)
        {
            try
            {
                var f = LogFile;
                try
                {
                    var fi = new FileInfo(f);
                    if (fi.Exists && fi.Length > 512 * 1024) fi.Delete();   // keep the log tiny
                }
                catch { }
                File.AppendAllText(f, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
            }
            catch { }
        }

        public static void OpenUrl(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch (Exception ex) { Log("OpenUrl failed: " + ex.Message); }
        }
    }
}
