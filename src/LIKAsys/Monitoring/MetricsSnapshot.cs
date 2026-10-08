namespace LIKAsys.Monitoring
{
    /// <summary>Immutable-ish bag of everything the widget can display.</summary>
    public class MetricsSnapshot
    {
        public double CpuLoad;          // %
        public double CpuTemp;          // C   (-1 = n/a)
        public double CpuClockMhz;      // MHz (-1 = n/a)
        public string CpuName = "CPU";

        public double GpuLoad;          // %
        public double GpuTemp;          // C   (-1 = n/a)
        public double VramUsedMb;       // MB
        public double VramTotalMb;      // MB  (0 = unknown)
        public string GpuName = "GPU";

        public double RamUsedGb;
        public double RamTotalGb;
        public double RamLoad;          // %

        public double Fps;              // -1 = n/a
        public double FrameTimeMs;      // -1 = n/a
        public double DiskUsedPct = -1;
        public double DiskFreeGb = -1;
        public double DiskTotalGb = -1;
        public double DiskReadMbs = -1;
        public double DiskWriteMbs = -1;
        public string DiskName = "";
        public double UptimeSec = -1;

        public double FpsLow1 = -1;     // 1% low
        public double FpsLow01 = -1;    // 0.1% low
        public string FpsSource = "";   // process name of the measured app

        public double NetDownMbps;      // Mb/s
        public double NetUpMbps;        // Mb/s
        public string NetAdapter = "";
        public int PingMs = -1;         // -1 = n/a

        public MetricsSnapshot Clone() => (MetricsSnapshot)MemberwiseClone();
    }
}
