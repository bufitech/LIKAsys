using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LIKAsys.Core;
using LIKAsys.Services;

namespace LIKAsys.Ui
{
    public partial class UpdateWindow : Window
    {
        private readonly UpdateInfo _info;
        private CancellationTokenSource _cts;
        private bool _busy;

        public UpdateWindow(UpdateInfo info)
        {
            InitializeComponent();
            _info = info;
            Lang.Localize(this);

            HeadLine.Text = (Lang.IsEnglish ? "New version: " : "Version i ri: ") + info.Tag;
            var size = info.Size > 0 ? $"  -  {info.Size / 1024.0 / 1024.0:0.0} MB" : "";
            SubLine.Text = (Lang.IsEnglish ? "You have " : "Ti ke ") + AppInfo.VersionText + size;
            NotesText.Text = string.IsNullOrWhiteSpace(info.Notes)
                ? (Lang.IsEnglish ? "Small improvements and fixes." : "Permiresime dhe rregullime te vogla.")
                : info.Notes.Trim();

            if (string.IsNullOrEmpty(info.DownloadUrl))
            {
                InstallBtn.Content = Lang.IsEnglish ? "Open the download page" : "Hap faqen e shkarkimit";
            }
        }

        private async void Install_Click(object sender, RoutedEventArgs e)
        {
            if (_busy) return;

            if (string.IsNullOrEmpty(_info.DownloadUrl))
            {
                AppInfo.OpenUrl(AppInfo.DownloadPage);
                Close();
                return;
            }

            _busy = true;
            InstallBtn.IsEnabled = false;
            LaterBtn.IsEnabled = false;
            ProgressPanel.Visibility = Visibility.Visible;
            ProgressText.Text = "Duke shkarkuar...";

            _cts = new CancellationTokenSource();
            var progress = new Progress<double>(p =>
            {
                FillCol.Width = new GridLength(Math.Max(0.001, p), GridUnitType.Star);
                RestCol.Width = new GridLength(Math.Max(0.001, 100 - p), GridUnitType.Star);
                ProgressText.Text = Lang.T("Duke shkarkuar...") + " " + p.ToString("0", CultureInfo.InvariantCulture) + "%";
            });

            var path = await UpdateService.DownloadAsync(_info, progress, _cts.Token);

            if (string.IsNullOrEmpty(path))
            {
                ProgressText.Text = Lang.IsEnglish ? "The download failed. Try again or get it from Likaapps.com." : "Shkarkimi deshtoi. Provo perseri ose merre nga Likaapps.com.";
                InstallBtn.IsEnabled = true;
                LaterBtn.IsEnabled = true;
                _busy = false;
                return;
            }

            AppInfo.Log("update: downloaded to " + path);
            ProgressText.Text = Lang.T("Duke instaluar... LIKAsys mbyllet dhe rihapet vetvetiu.");

            if (UpdateService.RunInstaller(path))
            {
                // give the installer time to start before we release the files it must replace
                await System.Threading.Tasks.Task.Delay(2500);
                AppInfo.Log("update: closing for the installer");
                Application.Current.Shutdown();
            }
            else
            {
                ProgressText.Text = Lang.T("Nuk u nis instaluesi. Hape dosjen Downloads dhe nise manualisht.");
                InstallBtn.IsEnabled = true;
                LaterBtn.IsEnabled = true;
                _busy = false;
            }
        }

        private void Site_Click(object sender, RoutedEventArgs e) => AppInfo.OpenUrl(AppInfo.Website);

        private void Title_Drag(object sender, MouseButtonEventArgs e)
        {
            try { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); } catch { }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            try { _cts?.Cancel(); } catch { }
            Close();
        }
    }
}
