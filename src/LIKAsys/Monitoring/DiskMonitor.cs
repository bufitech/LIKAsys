using System;
using System.IO;

namespace LIKAsys.Monitoring
{
    /// <summary>
    /// Storage for the IT profile: how full the system drive is, and how hard every
    /// physical disk is being read and written right now.
    ///
    /// Space comes from <see cref="DriveInfo"/> and is only re-read every few seconds -
    /// it never changes fast enough to be worth asking the filesystem every tick.
    /// Throughput comes from the same PDH plumbing the GPU already uses, with the
    /// English counter names, so it works on a Windows installed in any language.
    /// </summary>
    internal sealed class DiskMonitor : IDisposable
    {
        private PdhWildcardCounter _read;
        private PdhWildcardCounter _write;

        private string _root = "C:\\";
        private double _usedPct, _freeGb, _totalGb;
        private DateTime _spaceStamp = DateTime.MinValue;

        public DiskMonitor()
        {
            try
            {
                var sys = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
                if (!string.IsNullOrEmpty(sys)) _root = sys;
            }
            catch { }

            try
            {
                _read = new PdhWildcardCounter(@"\PhysicalDisk(_Total)\Disk Read Bytes/sec");
                _write = new PdhWildcardCounter(@"\PhysicalDisk(_Total)\Disk Write Bytes/sec");
            }
            catch { }
        }

        public void Sample(MetricsSnapshot s)
        {
            ReadSpace();

            s.DiskName = _root.TrimEnd('\\');
            s.DiskUsedPct = _usedPct;
            s.DiskFreeGb = _freeGb;
            s.DiskTotalGb = _totalGb;

            try
            {
                if (_read != null && _read.Ok)
                {
                    double b = _read.SumSample();
                    if (b >= 0) s.DiskReadMbs = b / (1024.0 * 1024.0);
                }
                if (_write != null && _write.Ok)
                {
                    double b = _write.SumSample();
                    if (b >= 0) s.DiskWriteMbs = b / (1024.0 * 1024.0);
                }
            }
            catch { }

            try { s.UptimeSec = Environment.TickCount64 / 1000.0; } catch { }
        }

        private void ReadSpace()
        {
            if ((DateTime.UtcNow - _spaceStamp).TotalSeconds < 5) return;
            _spaceStamp = DateTime.UtcNow;

            try
            {
                var d = new DriveInfo(_root);
                if (!d.IsReady || d.TotalSize <= 0) return;

                const double gb = 1024.0 * 1024.0 * 1024.0;
                _totalGb = d.TotalSize / gb;
                _freeGb = d.AvailableFreeSpace / gb;
                _usedPct = Math.Max(0, Math.Min(100, (d.TotalSize - d.TotalFreeSpace) * 100.0 / d.TotalSize));
            }
            catch { }
        }

        public void Dispose()
        {
            try { _read?.Dispose(); } catch { }
            try { _write?.Dispose(); } catch { }
            _read = null;
            _write = null;
        }
    }
}
