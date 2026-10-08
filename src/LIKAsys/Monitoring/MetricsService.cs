using System;
using System.Threading;
using System.Threading.Tasks;
using LIKAsys.Core;

namespace LIKAsys.Monitoring
{
    /// <summary>Single background sampling loop that feeds the widget.</summary>
    public sealed class MetricsService : IDisposable
    {
        private readonly AppSettings _settings;
        private readonly CpuMonitor _cpu = new CpuMonitor();
        private readonly MemoryMonitor _mem = new MemoryMonitor();
        private readonly GpuMonitor _gpu = new GpuMonitor();
        private readonly HardwareMonitor _hw = new HardwareMonitor();
        private readonly FpsMonitor _fps = new FpsMonitor();
        private readonly NetMonitor _net;
        private readonly DiskMonitor _disk;

        private CancellationTokenSource _cts;
        private int _sensorDivider;

        public event Action<MetricsSnapshot> Updated;
        public MetricsSnapshot Latest { get; private set; } = new MetricsSnapshot();
        public bool FpsAvailable => _fps.Supported;
        public string FpsStatus => _fps.Status;
        public bool SensorsAvailable => _hw.Available;

        public MetricsService(AppSettings settings)
        {
            _settings = settings;
            _net = new NetMonitor(settings);
            _disk = new DiskMonitor();
        }

        public void Start()
        {
            // nothing here may block or throw on the UI thread
            if (_settings.AdvancedSensors)
                Task.Run(() => { try { _hw.Start(); } catch (Exception ex) { AppInfo.Log("hw start: " + ex.Message); } });
            if (_settings.FpsEnabled)
                Task.Run(() => { try { _fps.Start(); } catch (Exception ex) { AppInfo.Log("fps start: " + ex.Message); } });

            _cts = new CancellationTokenSource();
            Task.Run(() => LoopAsync(_cts.Token));
        }

        private async Task LoopAsync(CancellationToken token)
        {
            var snap = new MetricsSnapshot { CpuTemp = -1, GpuTemp = -1, CpuClockMhz = -1, Fps = -1 };
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var s = new MetricsSnapshot
                    {
                        CpuTemp = -1,
                        GpuTemp = -1,
                        CpuClockMhz = -1,
                        CpuName = _cpu.Name,
                        GpuName = _gpu.Name,
                        VramTotalMb = _gpu.VramTotalMb
                    };

                    s.CpuLoad = _cpu.Sample();
                    _mem.Sample(s);
                    _gpu.Sample(s);

                    // deep sensors are slower - refresh them every other tick
                    if (_settings.AdvancedSensors && (_sensorDivider++ % 2 == 0)) _hw.Sample(s);
                    else if (_settings.AdvancedSensors) { s.CpuTemp = snap.CpuTemp; s.GpuTemp = snap.GpuTemp; s.CpuClockMhz = snap.CpuClockMhz; if (s.VramTotalMb <= 0) s.VramTotalMb = snap.VramTotalMb; }

                    if (_settings.ShowNet || _settings.ShowPing) _net.Sample(s);
                    if (_settings.ShowDisk || _settings.ShowDiskIo || _settings.ShowUptime) _disk.Sample(s);

                    if (_settings.FpsEnabled)
                    {
                        _fps.Tick();
                        _fps.Fill(s);
                    }

                    snap = s;
                    Latest = s;
                    Updated?.Invoke(s);
                }
                catch (Exception ex) { AppInfo.Log("Metrics loop: " + ex.Message); }

                try { await Task.Delay(Math.Max(250, _settings.RefreshMs), token); }
                catch (TaskCanceledException) { break; }
            }
        }

        public void RestartFps()
        {
            try
            {
                _fps.Dispose();
                if (_settings.FpsEnabled) _fps.Start();
            }
            catch { }
        }

        public void Dispose()
        {
            try { _cts?.Cancel(); } catch { }
            _fps.Dispose();
            _hw.Dispose();
            _gpu.Dispose();
            _net.Dispose();
            _disk.Dispose();
        }
    }
}
