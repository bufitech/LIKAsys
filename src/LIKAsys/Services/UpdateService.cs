using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LIKAsys.Services
{
    public class UpdateInfo
    {
        public Version Version;
        public string Tag;
        public string Title;
        public string Notes;
        public string DownloadUrl;
        public long Size;
        public DateTime Published;
        public bool IsNewer;
    }

    /// <summary>One-click updates: asks the LIKAsys update feed for the newest build.</summary>
    public static class UpdateService
    {
        private static readonly HttpClient Http = CreateClient();

        private static HttpClient CreateClient()
        {
            var c = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            c.DefaultRequestHeaders.UserAgent.ParseAdd($"LIKAsys/{AppInfo.CurrentVersion} (+{AppInfo.Website})");
            c.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            return c;
        }

        public static async Task<UpdateInfo> CheckAsync()
        {
            try
            {
                var json = await Http.GetStringAsync(AppInfo.UpdateFeed).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("draft", out var d) && d.GetBoolean()) return null;

                string tag = root.TryGetProperty("tag_name", out var t) ? t.GetString() : null;
                if (string.IsNullOrWhiteSpace(tag)) return null;

                var parsed = ParseVersion(tag);
                if (parsed == null) return null;

                string url = null; long size = 0;
                if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
                {
                    foreach (var a in assets.EnumerateArray())
                    {
                        var name = a.TryGetProperty("name", out var n) ? n.GetString() : "";
                        if (name != null && name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
                            name.IndexOf("setup", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            url = a.TryGetProperty("browser_download_url", out var u) ? u.GetString() : null;
                            size = a.TryGetProperty("size", out var s) ? s.GetInt64() : 0;
                            break;
                        }
                    }
                    if (url == null)
                    {
                        var first = assets.EnumerateArray().FirstOrDefault(a =>
                            (a.TryGetProperty("name", out var n) ? n.GetString() : "").EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
                        if (first.ValueKind == JsonValueKind.Object)
                        {
                            url = first.TryGetProperty("browser_download_url", out var u) ? u.GetString() : null;
                            size = first.TryGetProperty("size", out var s) ? s.GetInt64() : 0;
                        }
                    }
                }

                var cur = AppInfo.CurrentVersion;
                var info = new UpdateInfo
                {
                    Version = parsed,
                    Tag = tag,
                    Title = root.TryGetProperty("name", out var nm) ? nm.GetString() : tag,
                    Notes = root.TryGetProperty("body", out var b) ? b.GetString() : "",
                    DownloadUrl = url,
                    Size = size,
                    Published = root.TryGetProperty("published_at", out var p) && p.TryGetDateTime(out var dt) ? dt : DateTime.MinValue,
                };
                info.IsNewer = parsed > new Version(cur.Major, cur.Minor, cur.Build == -1 ? 0 : cur.Build);
                return info;
            }
            catch (Exception ex)
            {
                AppInfo.Log("Update check failed: " + ex.Message);
                return null;
            }
        }

        private static Version ParseVersion(string tag)
        {
            try
            {
                var clean = new string(tag.Where(c => char.IsDigit(c) || c == '.').ToArray()).Trim('.');
                var parts = clean.Split('.');
                int maj = parts.Length > 0 && int.TryParse(parts[0], out var a) ? a : 0;
                int min = parts.Length > 1 && int.TryParse(parts[1], out var b) ? b : 0;
                int pat = parts.Length > 2 && int.TryParse(parts[2], out var c) ? c : 0;
                return new Version(maj, min, pat);
            }
            catch { return null; }
        }

        /// <summary>Downloads the installer, reporting 0..100 progress.</summary>
        public static async Task<string> DownloadAsync(UpdateInfo info, IProgress<double> progress, CancellationToken token)
        {
            if (info?.DownloadUrl == null) return null;
            var target = Path.Combine(Path.GetTempPath(), $"LIKAsys-Setup-{info.Version}.exe");
            try
            {
                using var resp = await Http.GetAsync(info.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
                resp.EnsureSuccessStatusCode();
                long total = resp.Content.Headers.ContentLength ?? info.Size;
                using var src = await resp.Content.ReadAsStreamAsync(token).ConfigureAwait(false);
                using var dst = new FileStream(target, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16, true);
                var buffer = new byte[1 << 16];
                long read = 0; int n;
                while ((n = await src.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false)) > 0)
                {
                    await dst.WriteAsync(buffer, 0, n, token).ConfigureAwait(false);
                    read += n;
                    if (total > 0) progress?.Report(read * 100.0 / total);
                }
                return target;
            }
            catch (Exception ex)
            {
                AppInfo.Log("Download failed: " + ex.Message);
                try { if (File.Exists(target)) File.Delete(target); } catch { }
                return null;
            }
        }

        /// <summary>Runs the downloaded installer silently and quits so files can be replaced.</summary>
        public static bool RunInstaller(string setupPath, bool silent = true)
        {
            string args = silent ? "/S /UPDATE" : "";
            try
            {
                if (!File.Exists(setupPath))
                {
                    AppInfo.Log("Installer missing: " + setupPath);
                    return false;
                }
                AppInfo.Log($"update: starting installer '{setupPath}' {args}");

                var psi = new ProcessStartInfo(setupPath)
                {
                    UseShellExecute = true,
                    Verb = "runas",
                    Arguments = args
                };
                var p = Process.Start(psi);
                AppInfo.Log("update: installer pid=" + (p?.Id.ToString() ?? "?"));
                return true;
            }
            catch (Exception ex)
            {
                AppInfo.Log("Installer launch failed: " + ex.Message + " - retrying without elevation verb");
                try
                {
                    var psi = new ProcessStartInfo(setupPath) { UseShellExecute = true, Arguments = args };
                    Process.Start(psi);
                    return true;
                }
                catch (Exception ex2)
                {
                    AppInfo.Log("Installer launch failed again: " + ex2.Message);
                    return false;
                }
            }
        }
    }
}
