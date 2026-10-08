using LIKAsys.Core;

namespace LIKAsys.Monitoring
{
    internal sealed class MemoryMonitor
    {
        private const double GB = 1024.0 * 1024.0 * 1024.0;

        public void Sample(MetricsSnapshot s)
        {
            try
            {
                var m = new Native.MEMORYSTATUSEX();
                if (!Native.GlobalMemoryStatusEx(m)) return;
                s.RamTotalGb = m.ullTotalPhys / GB;
                s.RamUsedGb = (m.ullTotalPhys - m.ullAvailPhys) / GB;
                s.RamLoad = m.dwMemoryLoad;
            }
            catch { }
        }
    }
}
