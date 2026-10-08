using System;
using System.Diagnostics;

namespace LIKAsys.Services
{
    /// <summary>
    /// Auto-start. Because LIKAsys runs elevated, a scheduled task with "highest privileges"
    /// is used instead of the Run key - that way Windows starts it without a UAC prompt.
    /// </summary>
    public static class StartupManager
    {
        private const string TaskName = "LIKAsys";

        public static bool IsEnabled()
        {
            try
            {
                var p = Run($"/Query /TN \"{TaskName}\"");
                return p == 0;
            }
            catch { return false; }
        }

        public static bool Enable()
        {
            try
            {
                var exe = AppInfo.ExePath;
                var cmd = $"/Create /TN \"{TaskName}\" /TR \"\\\"{exe}\\\" --autostart\" /SC ONLOGON /RL HIGHEST /F";
                return Run(cmd) == 0;
            }
            catch (Exception ex) { AppInfo.Log("Enable autostart failed: " + ex.Message); return false; }
        }

        public static bool Disable()
        {
            try { return Run($"/Delete /TN \"{TaskName}\" /F") == 0; }
            catch { return false; }
        }

        public static void Apply(bool enabled)
        {
            if (enabled) Enable(); else Disable();
        }

        private static int Run(string args)
        {
            var psi = new ProcessStartInfo("schtasks.exe", args)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var p = Process.Start(psi);
            if (p == null) return -1;
            p.StandardOutput.ReadToEnd();
            p.StandardError.ReadToEnd();
            p.WaitForExit(8000);
            return p.HasExited ? p.ExitCode : -1;
        }
    }
}
