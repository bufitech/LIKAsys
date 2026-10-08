using System;
using Microsoft.Win32;

namespace LIKAsys.Monitoring
{
    /// <summary>
    /// GPU usage + VRAM from the Windows 10/11 WDDM performance counters.
    /// Works for NVIDIA / AMD / Intel without any vendor SDK and without admin rights.
    /// Temperatures come from <see cref="HardwareMonitor"/> when available.
    /// </summary>
    internal sealed class GpuMonitor : IDisposable
    {
        private PdhWildcardCounter _utilization;   // \GPU Engine(*engtype_3D)\Utilization Percentage
        private PdhWildcardCounter _dedicated;     // \GPU Adapter Memory(*)\Dedicated Usage
        private double _lastLoad;
        private double _vramTotalMb;

        public string Name { get; private set; } = "GPU";
        public double VramTotalMb => _vramTotalMb;

        public GpuMonitor()
        {
            try
            {
                _utilization = new PdhWildcardCounter(@"\GPU Engine(*engtype_3D)\Utilization Percentage");
                _dedicated = new PdhWildcardCounter(@"\GPU Adapter Memory(*)\Dedicated Usage");
            }
            catch { }
            ReadAdapterFromRegistry();
        }

        public void Sample(MetricsSnapshot s)
        {
            try
            {
                if (_utilization != null && _utilization.Ok)
                {
                    double v = _utilization.SumSample();
                    if (v > 0 || _lastLoad > 0) _lastLoad = Math.Max(0, Math.Min(100, v));
                }
                s.GpuLoad = _lastLoad;

                if (_dedicated != null && _dedicated.Ok)
                {
                    double bytes = _dedicated.MaxSample();
                    if (bytes > 0) s.VramUsedMb = bytes / (1024.0 * 1024.0);
                }

                if (s.VramTotalMb <= 0 && _vramTotalMb > 0) s.VramTotalMb = _vramTotalMb;
                if (string.IsNullOrEmpty(s.GpuName) || s.GpuName == "GPU") s.GpuName = Name;
            }
            catch { }
        }

        /// <summary>Best-effort adapter name + dedicated memory size straight from the display class registry key.</summary>
        private void ReadAdapterFromRegistry()
        {
            try
            {
                const string root = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";
                using (var cls = Registry.LocalMachine.OpenSubKey(root))
                {
                    if (cls == null) return;
                    double bestMem = 0;
                    string bestName = null;
                    foreach (var sub in cls.GetSubKeyNames())
                    {
                        if (sub.Length != 4) continue; // 0000, 0001 ...
                        using (var k = cls.OpenSubKey(sub))
                        {
                            if (k == null) continue;
                            var name = k.GetValue("DriverDesc") as string;
                            double mem = 0;
                            var qw = k.GetValue("HardwareInformation.qwMemorySize");
                            if (qw is long l) mem = l / (1024.0 * 1024.0);
                            else if (qw is int i) mem = (uint)i / (1024.0 * 1024.0);
                            if (mem <= 0)
                            {
                                var dw = k.GetValue("HardwareInformation.MemorySize");
                                if (dw is byte[] bytes && bytes.Length >= 4) mem = BitConverter.ToUInt32(bytes, 0) / (1024.0 * 1024.0);
                                else if (dw is int di) mem = (uint)di / (1024.0 * 1024.0);
                            }
                            if (!string.IsNullOrWhiteSpace(name) && (mem > bestMem || bestName == null))
                            {
                                bestMem = Math.Max(bestMem, mem);
                                bestName = name;
                            }
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(bestName)) Name = CleanName(bestName);
                    if (bestMem > 0) _vramTotalMb = bestMem;
                }
            }
            catch { }
        }

        internal static string CleanName(string n)
        {
            if (string.IsNullOrWhiteSpace(n)) return "GPU";
            return n.Replace("(R)", "").Replace("(TM)", "").Replace("NVIDIA ", "").Replace("AMD ", "")
                    .Replace("Intel ", "").Replace("Corporation", "").Replace("  ", " ").Trim();
        }

        public void Dispose()
        {
            try { _utilization?.Dispose(); } catch { }
            try { _dedicated?.Dispose(); } catch { }
        }
    }
}
