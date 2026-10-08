using System;
using LIKAsys.Core;
using Microsoft.Win32;

namespace LIKAsys.Monitoring
{
    /// <summary>CPU load from GetSystemTimes - zero dependencies, works on every Windows 10/11 machine.</summary>
    internal sealed class CpuMonitor
    {
        private ulong _prevIdle, _prevKernel, _prevUser;
        private bool _primed;
        private double _last;

        public string Name { get; }

        public CpuMonitor()
        {
            Name = ReadCpuName();
            Sample(); // prime
        }

        public double Sample()
        {
            try
            {
                if (!Native.GetSystemTimes(out var idleFt, out var kernelFt, out var userFt)) return _last;
                ulong idle = Native.ToU64(idleFt), kernel = Native.ToU64(kernelFt), user = Native.ToU64(userFt);

                if (!_primed)
                {
                    _prevIdle = idle; _prevKernel = kernel; _prevUser = user;
                    _primed = true;
                    return 0;
                }

                ulong dIdle = idle - _prevIdle;
                ulong dKernel = kernel - _prevKernel;
                ulong dUser = user - _prevUser;
                _prevIdle = idle; _prevKernel = kernel; _prevUser = user;

                ulong total = dKernel + dUser;           // kernel already includes idle
                if (total == 0) return _last;
                double usage = (total - dIdle) * 100.0 / total;
                _last = Math.Max(0, Math.Min(100, usage));
            }
            catch { }
            return _last;
        }

        private static string ReadCpuName()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0"))
                {
                    var n = key?.GetValue("ProcessorNameString") as string;
                    if (!string.IsNullOrWhiteSpace(n))
                        return n.Replace("(R)", "").Replace("(TM)", "").Replace("CPU", "").Replace("  ", " ").Trim();
                }
            }
            catch { }
            return "CPU";
        }
    }
}
