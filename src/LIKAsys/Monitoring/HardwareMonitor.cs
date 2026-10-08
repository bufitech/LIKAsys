using System;
using System.Linq;
using LibreHardwareMonitor.Hardware;

namespace LIKAsys.Monitoring
{
    /// <summary>
    /// Optional deep-sensor layer (temperatures, clocks, VRAM, fan) through LibreHardwareMonitor.
    /// Everything is wrapped in try/catch: if the driver cannot load, the widget simply hides temps.
    /// </summary>
    internal sealed class HardwareMonitor : IDisposable
    {
        private sealed class UpdateVisitor : IVisitor
        {
            public void VisitComputer(IComputer computer) => computer.Traverse(this);
            public void VisitHardware(IHardware hardware)
            {
                hardware.Update();
                foreach (var sub in hardware.SubHardware) sub.Accept(this);
            }
            public void VisitSensor(ISensor sensor) { }
            public void VisitParameter(IParameter parameter) { }
        }

        private Computer _computer;
        private readonly UpdateVisitor _visitor = new UpdateVisitor();
        public bool Available { get; private set; }

        public void Start()
        {
            try
            {
                _computer = new Computer
                {
                    IsCpuEnabled = true,
                    IsGpuEnabled = true,
                    IsMemoryEnabled = false,
                    IsMotherboardEnabled = false,
                    IsStorageEnabled = false,
                    IsNetworkEnabled = false,
                    IsControllerEnabled = false,
                    IsBatteryEnabled = false,
                    IsPsuEnabled = false
                };
                _computer.Open();
                Available = true;
            }
            catch (Exception ex)
            {
                Available = false;
                AppInfo.Log("LibreHardwareMonitor unavailable: " + ex.Message);
            }
        }

        public void Sample(MetricsSnapshot s)
        {
            if (!Available || _computer == null) return;
            try
            {
                _computer.Accept(_visitor);

                foreach (var hw in _computer.Hardware)
                {
                    switch (hw.HardwareType)
                    {
                        case HardwareType.Cpu:
                            {
                                var temp = Pick(hw, SensorType.Temperature, "Package", "Core (Tctl/Tdie)", "CPU Package", "Core Max", "Core Average");
                                if (temp.HasValue) s.CpuTemp = temp.Value;
                                var clock = hw.Sensors.Where(x => x.SensorType == SensorType.Clock && x.Name.StartsWith("CPU Core"))
                                                      .Select(x => x.Value ?? 0).DefaultIfEmpty(0).Max();
                                if (clock > 0) s.CpuClockMhz = clock;
                                break;
                            }
                        case HardwareType.GpuNvidia:
                        case HardwareType.GpuAmd:
                        case HardwareType.GpuIntel:
                            {
                                var t = Pick(hw, SensorType.Temperature, "GPU Core", "GPU Hot Spot", "GPU Temperature", "Core");
                                if (t.HasValue && t.Value > 0) s.GpuTemp = t.Value;

                                var load = Pick(hw, SensorType.Load, "GPU Core", "D3D 3D", "GPU Utilization");
                                if (load.HasValue && load.Value > 0 && s.GpuLoad <= 0) s.GpuLoad = load.Value;

                                var used = Pick(hw, SensorType.SmallData, "GPU Memory Used", "D3D Dedicated Memory Used");
                                if (used.HasValue && used.Value > 0 && s.VramUsedMb <= 0) s.VramUsedMb = used.Value;

                                var total = Pick(hw, SensorType.SmallData, "GPU Memory Total");
                                if (total.HasValue && total.Value > 0) s.VramTotalMb = total.Value;

                                if (!string.IsNullOrWhiteSpace(hw.Name)) s.GpuName = GpuMonitor.CleanName(hw.Name);
                                break;
                            }
                    }
                }
            }
            catch (Exception ex) { AppInfo.Log("Sensor sample failed: " + ex.Message); }
        }

        private static float? Pick(IHardware hw, SensorType type, params string[] preferredNames)
        {
            try
            {
                foreach (var want in preferredNames)
                {
                    var hit = hw.Sensors.FirstOrDefault(x => x.SensorType == type &&
                        string.Equals(x.Name, want, StringComparison.OrdinalIgnoreCase) && x.Value.HasValue);
                    if (hit?.Value != null) return hit.Value;
                }
                var any = hw.Sensors.FirstOrDefault(x => x.SensorType == type && x.Value.HasValue);
                return any?.Value;
            }
            catch { return null; }
        }

        public void Dispose()
        {
            try { _computer?.Close(); } catch { }
            _computer = null;
            Available = false;
        }
    }
}
