using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using LIKAsys.Core;
using LIKAsys.Monitoring;
using LIKAsys.Services;
using LIKAsys.Ui;

namespace LIKAsys
{
    public partial class App : Application
    {
        private AppSettings _settings;
        private MetricsService _metrics;
        private WidgetWindow _widget;
        private SettingsWindow _settingsWindow;
        private TrayManager _tray;
        private DispatcherTimer _saveTimer;
        private DispatcherTimer _updateTimer;
        private UpdateInfo _pendingUpdate;
        private DateTime _lastTooltip = DateTime.MinValue;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            EnsureCultureIsUsable();

            DispatcherUnhandledException += (s, args) =>
            {
                AppInfo.Log("UI exception: " + args.Exception);
                args.Handled = true;
                ReportOnce("Gabim gjate punes", args.Exception);
            };
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
                AppInfo.Log("Fatal: " + args.ExceptionObject);

            bool admin = false;
            try { admin = Native.IsElevated(); } catch { }

            bool afterUpdate = false;
            try
            {
                foreach (var a in e.Args ?? new string[0])
                    if (string.Equals(a, "--updated", StringComparison.OrdinalIgnoreCase)) afterUpdate = true;
            }
            catch { }

            AppInfo.Log("==========================================================");
            AppInfo.Log($"LIKAsys {AppInfo.VersionText} start | Windows {Environment.OSVersion.Version} | admin={admin}" +
                        (afterUpdate ? " | pas update-it" : ""));
            AppInfo.Log("dir: " + AppInfo.InstallDir);

            // after an update the previous process may still be shutting down
            if (!SingleInstance.Acquire(afterUpdate ? 15 : 0))
            {
                MessageBox.Show((Lang.IsEnglish ? "LIKAsys is already running - look for the icon next to the clock." : "LIKAsys eshte tashme i hapur - shikoje ikonen afer ores."), "LIKAsys",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }
            AppInfo.Log("ok: single-instance");

            var problems = new List<string>();

            // ---------------------------------------------------- 1. settings
            try { _settings = SettingsStore.Load(); }
            catch (Exception ex)
            {
                AppInfo.Log("SETTINGS FAILED: " + ex);
                problems.Add("Cilesimet nuk u lexuan dot - u perdoren ato fillestare.");
            }
            if (_settings == null) _settings = new AppSettings();

            Lang.Set(_settings.Language);
            AppInfo.Log("ok: language " + Lang.Code);
            AppInfo.Log("ok: settings (tema=" + _settings.ThemeName + ")");

            try { ApplyAccentResource(); } catch (Exception ex) { AppInfo.Log("accent: " + ex.Message); }
            try { _settings.StartWithWindows = StartupManager.IsEnabled(); } catch { }
            _settings.PropertyChanged += OnSettingChanged;

            // ------------------------------- 2. tray FIRST: always a way back in
            try
            {
                _tray = new TrayManager(_settings);
                _tray.ToggleWidget += (s, a) => ToggleWidget();
                _tray.OpenSettings += (s, a) => ShowSettings();
                _tray.CheckUpdates += (s, a) => OnTrayUpdateClick();
                _tray.OpenLog += (s, a) => AppInfo.OpenUrl(AppInfo.LogFile);
                _tray.RescueWidget += (s, a) => RescueWidget();
                _tray.ExitApp += (s, a) => ExitApp();
                _tray.Changed += (s, a) => { _widget?.ApplySettings(); SaveSoon(); };
                AppInfo.Log("ok: tray");
            }
            catch (Exception ex)
            {
                AppInfo.Log("TRAY FAILED: " + ex);
                problems.Add("Ikona afer ores nuk u krijua: " + ex.Message);
            }

            // ---------------------------------------------------- 3. metrics
            try
            {
                _metrics = new MetricsService(_settings);
                _metrics.Updated += OnMetricsForTray;
                _metrics.Start();
                AppInfo.Log("ok: metrics");
            }
            catch (Exception ex)
            {
                AppInfo.Log("METRICS FAILED: " + ex);
                problems.Add("Matja e sistemit nuk u nis: " + ex.Message);
            }

            // ---------------------------------------------------- 4. widget
            try
            {
                _widget = new WidgetWindow(_settings, _metrics);
                _widget.SettingsRequested += (s, a) => ShowSettings();
                if (_settings.WidgetVisible) _widget.Show();
                AppInfo.Log("ok: widget");
            }
            catch (Exception ex)
            {
                AppInfo.Log("WIDGET FAILED: " + ex);
                problems.Add("Widget-i nuk u hap: " + ex.Message);
            }

            // ---------------------------------------------------- 5. timers
            try
            {
                _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
                _saveTimer.Tick += (s, a) => { _saveTimer.Stop(); SettingsStore.Save(_settings); };

                _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromHours(6) };
                _updateTimer.Tick += async (s, a) => await CheckUpdatesAsync(true);
                if (_settings.AutoCheckUpdates)
                {
                    _updateTimer.Start();
                    var first = new DispatcherTimer { Interval = TimeSpan.FromSeconds(12) };
                    first.Tick += async (s, a) => { first.Stop(); await CheckUpdatesAsync(true); };
                    first.Start();
                }
            }
            catch (Exception ex) { AppInfo.Log("timers: " + ex.Message); }

            // ------------------------------------------- 6. first run / report
            if (problems.Count == 0)
            {
                if (!_settings.FirstRunDone)
                {
                    _settings.FirstRunDone = true;
                    try { SettingsStore.Save(_settings); } catch { }
                    _tray?.Notify("LIKAsys eshte gati",
                        "Widget-i u hap ne qoshe te ekranit. Kliko dy here mbi ikonen per ta fshehur ose shfaqur.");
                }
            }
            else
            {
                AppInfo.Log("startup problems: " + string.Join(" | ", problems));
                try
                {
                    MessageBox.Show(
                        "LIKAsys u nis, por disa pjese nuk punuan:\n\n   - " + string.Join("\n   - ", problems) +
                        "\n\nDetajet e plota jane ketu:\n" + AppInfo.LogFile,
                        "LIKAsys - diagnostike", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch { }
            }

            AppInfo.Log("ok: startup finished");
        }

        /// <summary>
        /// Every WPF data binding resolves its culture through FrameworkElement.Language ->
        /// XmlLanguage.GetSpecificCulture(). On a runtime without full globalization data that call
        /// throws "Cannot find non-neutral culture related to 'en-us'" and every single window dies
        /// while the process keeps running. Verify it once here and fall back to the invariant
        /// language if the machine really cannot resolve cultures.
        /// </summary>
        private static void EnsureCultureIsUsable()
        {
            try
            {
                var tag = CultureInfo.CurrentCulture.IetfLanguageTag;
                if (string.IsNullOrWhiteSpace(tag)) tag = "en-US";

                var lang = System.Windows.Markup.XmlLanguage.GetLanguage(tag);
                lang.GetSpecificCulture();      // throws when globalization data is missing

                FrameworkElement.LanguageProperty.OverrideMetadata(
                    typeof(FrameworkElement), new FrameworkPropertyMetadata(lang));
                AppInfo.Log("ok: culture " + tag);
            }
            catch (Exception ex)
            {
                AppInfo.Log("culture unusable (" + ex.Message + ") - falling back to invariant");
                try
                {
                    FrameworkElement.LanguageProperty.OverrideMetadata(
                        typeof(FrameworkElement),
                        new FrameworkPropertyMetadata(System.Windows.Markup.XmlLanguage.Empty));
                }
                catch (Exception ex2) { AppInfo.Log("culture fallback failed: " + ex2.Message); }
            }
        }

        private bool _reported;

        /// <summary>Shows the first real error instead of dying silently in the background.</summary>
        private void ReportOnce(string title, Exception ex)
        {
            if (_reported) return;
            _reported = true;
            try
            {
                MessageBox.Show(title + ":\n\n" + ex.Message + "\n\nDetajet: " + AppInfo.LogFile,
                    "LIKAsys", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch { }
        }

        /// <summary>Tray -> "Rikthe widget-in ne ekran": back to a known-good state.</summary>
        private void RescueWidget()
        {
            try
            {
                _settings.BeginBatch();
                _settings.MonitorIndex = 0;
                _settings.Position = WidgetPosition.TopRight;
                _settings.MarginX = 18;
                _settings.MarginY = 18;
                _settings.Scale = 1.0;
                _settings.ClickThrough = false;
                _settings.Locked = false;
                _settings.ShowOnlyInGame = false;
                _settings.AlwaysOnTop = true;
                _settings.WidgetVisible = true;
                _settings.EndBatch();

                if (_widget == null)
                {
                    _widget = new WidgetWindow(_settings, _metrics);
                    _widget.SettingsRequested += (s, a) => ShowSettings();
                }
                _widget.Show();
                _widget.BuildRows();
                _widget.ApplySettings();
                _tray?.Sync();
                SaveSoon();
                AppInfo.Log("ok: widget rescued");
            }
            catch (Exception ex)
            {
                AppInfo.Log("RescueWidget: " + ex);
                ReportOnce("Widget-i nuk u rikthye dot", ex);
            }
        }

        // ------------------------------------------------------------ settings plumbing

        private void ApplyAccentResource()
        {
            try
            {
                var c = (Color)ColorConverter.ConvertFromString(_settings.Accent);
                var b = new SolidColorBrush(c); b.Freeze();
                Resources["Accent"] = b;
                var soft = new SolidColorBrush(Color.FromArgb(0x33, c.R, c.G, c.B)); soft.Freeze();
                Resources["AccentSoft"] = soft;
            }
            catch { }
        }

        private void OnSettingChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                var p = e.PropertyName ?? "";

                if (p.Length == 0) ApplyAccentResource();   // batch (theme switch)

                switch (p)
                {
                    case nameof(AppSettings.Accent):
                        ApplyAccentResource();
                        break;
                    case nameof(AppSettings.StartWithWindows):
                        StartupManager.Apply(_settings.StartWithWindows);
                        break;
                    case nameof(AppSettings.FpsEnabled):
                        _metrics?.RestartFps();
                        _settingsWindow?.RefreshStatus();
                        break;
                    case nameof(AppSettings.AutoCheckUpdates):
                        if (_settings.AutoCheckUpdates) _updateTimer?.Start(); else _updateTimer?.Stop();
                        break;
                }

                if (p == nameof(AppSettings.ShowCpu) || p == nameof(AppSettings.ShowGpu) ||
                    p == nameof(AppSettings.ShowVram) || p == nameof(AppSettings.ShowRam) ||
                    p == nameof(AppSettings.ShowFps) || p == nameof(AppSettings.ShowNet) ||
                    p == nameof(AppSettings.ShowFpsLow) || p == nameof(AppSettings.ShowFrameTime) ||
                    p == nameof(AppSettings.ShowPing) || p.Length == 0)
                {
                    _widget?.BuildRows();
                }

                _widget?.ApplySettings();
                _tray?.Sync();
                SaveSoon();
            }
            catch (Exception ex) { AppInfo.Log("OnSettingChanged: " + ex.Message); }
        }

        private void SaveSoon()
        {
            try { _saveTimer?.Stop(); _saveTimer?.Start(); } catch { }
        }

        // ------------------------------------------------------------ windows

        private void ToggleWidget()
        {
            if (_widget == null) return;
            if (_widget.IsVisible)
            {
                _widget.Hide();
                _settings.WidgetVisible = false;
            }
            else
            {
                _widget.Show();
                _settings.WidgetVisible = true;
                _widget.ApplySettings();
            }
            _tray?.Sync();
            SaveSoon();
        }

        private void ShowSettings()
        {
            if (_settingsWindow != null)
            {
                _settingsWindow.Activate();
                return;
            }
            _settingsWindow = new SettingsWindow(_settings, _metrics);
            _settingsWindow.CheckUpdatesRequested += async (s, a) =>
            {
                _settingsWindow.SetCheckEnabled(false);
                _settingsWindow.SetUpdateStatus(Lang.T("Duke kontrolluar..."));
                await CheckUpdatesAsync(false);
                _settingsWindow?.SetCheckEnabled(true);
            };
            _settingsWindow.ResetRequested += (s, a) => ResetSettings();
            _settingsWindow.LanguageChanged += (s, a) =>
            {
                AppInfo.Log("language -> " + Lang.Code);
                try { _tray?.Rebuild(); } catch { }
                try { _widget?.Localize(); } catch { }
                SaveSoon();
            };
            _settingsWindow.Closed += (s, a) => { _settingsWindow = null; SettingsStore.Save(_settings); };
            _settingsWindow.Show();
            _settingsWindow.Activate();
        }

        private void ResetSettings()
        {
            try
            {
                // a reset restores the look, not the language the user reads the app in
                var fresh = new AppSettings { FirstRunDone = true, Language = _settings.Language };
                _settings.PropertyChanged -= OnSettingChanged;

                foreach (var prop in typeof(AppSettings).GetProperties().Where(x => x.CanWrite))
                    prop.SetValue(_settings, prop.GetValue(fresh));

                _settings.PropertyChanged += OnSettingChanged;
                ApplyAccentResource();
                _widget?.BuildRows();
                _widget?.ApplySettings();
                _settingsWindow?.SyncAll();
                SettingsStore.Save(_settings);

                if (_settingsWindow != null)
                {
                    _settingsWindow.Close();
                    ShowSettings();
                }
            }
            catch (Exception ex) { AppInfo.Log("Reset failed: " + ex.Message); }
        }

        // ------------------------------------------------------------ tray tooltip

        private void OnMetricsForTray(MetricsSnapshot s)
        {
            // the drawn icon follows every tick; the tooltip text is throttled below
            try
            {
                var snap = s?.Clone();
                if (snap != null)
                    Dispatcher.BeginInvoke(new Action(() => { try { _tray?.UpdateLiveIcon(snap); } catch { } }));
            }
            catch { }

            if ((DateTime.UtcNow - _lastTooltip).TotalSeconds < 2) return;
            _lastTooltip = DateTime.UtcNow;
            try
            {
                var text = string.Format(CultureInfo.InvariantCulture,
                    "LIKAsys {0}\nCPU {1:0}%  GPU {2:0}%  RAM {3:0}%", AppInfo.VersionText, s.CpuLoad, s.GpuLoad, s.RamLoad);
                Dispatcher.BeginInvoke(new Action(() => _tray?.SetTooltip(text)));
            }
            catch { }
        }

        // ------------------------------------------------------------ updates

        private void OnTrayUpdateClick()
        {
            if (_pendingUpdate != null && _pendingUpdate.IsNewer)
            {
                ShowUpdateWindow(_pendingUpdate);
                return;
            }
            _ = CheckUpdatesAsync(false);
        }

        private async System.Threading.Tasks.Task CheckUpdatesAsync(bool silent)
        {
            var info = await UpdateService.CheckAsync();

            if (info == null)
            {
                _settingsWindow?.SetUpdateStatus(Lang.IsEnglish ? "Could not reach the update server. Check your internet and try again." : "Nuk u arrit serveri i perditesimeve. Kontrollo internetin dhe provo perseri.");
                if (!silent) _tray?.Notify("LIKAsys", Lang.IsEnglish ? "The update check failed." : "Nuk u arrit te kontrollohet per update.", true);
                return;
            }

            _pendingUpdate = info;

            if (!info.IsNewer)
            {
                _settingsWindow?.SetUpdateStatus((Lang.IsEnglish ? "You are on the latest version (" : "Je ne versionin me te fundit (") + AppInfo.VersionText + ").");
                if (!silent) _tray?.Notify("LIKAsys", (Lang.IsEnglish ? "You have the latest version (" : "Ti ke versionin me te fundit (") + AppInfo.VersionText + ").");
                return;
            }

            _settingsWindow?.SetUpdateStatus((Lang.IsEnglish ? "A new version is available: " : "Version i ri i disponueshem: ") + info.Tag);

            if (silent)
                _tray?.Notify($"LIKAsys {info.Tag}" + (Lang.IsEnglish ? " is out!" : " doli!"), Lang.IsEnglish ? "Click here to install it - it only takes a few seconds." : "Kliko ketu per ta instaluar - zgjat vetem disa sekonda.");
            else
                ShowUpdateWindow(info);
        }

        private void ShowUpdateWindow(UpdateInfo info)
        {
            try
            {
                var w = new UpdateWindow(info);
                w.Show();
                w.Activate();
            }
            catch (Exception ex) { AppInfo.Log("UpdateWindow: " + ex.Message); }
        }

        // ------------------------------------------------------------ exit

        private void ExitApp()
        {
            try
            {
                SettingsStore.Save(_settings);
                _tray?.Dispose();
                _metrics?.Dispose();
                SingleInstance.Release();
            }
            catch { }
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                _tray?.Dispose();
                _metrics?.Dispose();
                SingleInstance.Release();
            }
            catch { }
            base.OnExit(e);
        }
    }
}
