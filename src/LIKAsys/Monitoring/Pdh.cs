using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LIKAsys.Monitoring
{
    /// <summary>
    /// Minimal PDH (performance data helper) wrapper for wildcard counters such as
    /// \GPU Engine(*engtype_3D)\Utilization Percentage - fast, no admin required.
    /// </summary>
    internal sealed class PdhWildcardCounter : IDisposable
    {
        private const uint PDH_FMT_DOUBLE = 0x00000200;
        private const uint PDH_FMT_NOCAP100 = 0x00008000;
        private const int PDH_MORE_DATA = unchecked((int)0x800007D2);
        private const int PDH_NO_DATA = unchecked((int)0x800007D5);
        private const int PDH_INVALID_DATA = unchecked((int)0xC0000BC6);

        [DllImport("pdh.dll")] private static extern int PdhOpenQueryW(string dataSource, IntPtr userData, out IntPtr query);
        [DllImport("pdh.dll")] private static extern int PdhAddEnglishCounterW(IntPtr query, string fullPath, IntPtr userData, out IntPtr counter);
        [DllImport("pdh.dll")] private static extern int PdhCollectQueryData(IntPtr query);
        [DllImport("pdh.dll")] private static extern int PdhCloseQuery(IntPtr query);
        [DllImport("pdh.dll")] private static extern int PdhGetFormattedCounterArrayW(IntPtr counter, uint format, ref uint bufferSize, out uint itemCount, IntPtr itemBuffer);

        [StructLayout(LayoutKind.Explicit, Size = 24)]
        private struct PdhItemDouble
        {
            [FieldOffset(0)] public IntPtr szName;
            [FieldOffset(8)] public uint CStatus;
            [FieldOffset(16)] public double Value;
        }

        private IntPtr _query = IntPtr.Zero;
        private IntPtr _counter = IntPtr.Zero;
        private uint _bufferSize;
        private IntPtr _buffer = IntPtr.Zero;
        private bool _failed;

        public bool Ok => !_failed && _counter != IntPtr.Zero;

        public PdhWildcardCounter(string path)
        {
            try
            {
                if (PdhOpenQueryW(null, IntPtr.Zero, out _query) != 0) { _failed = true; return; }
                if (PdhAddEnglishCounterW(_query, path, IntPtr.Zero, out _counter) != 0) { _failed = true; return; }
                PdhCollectQueryData(_query); // priming sample
            }
            catch { _failed = true; }
        }

        /// <summary>Collects a sample and returns the sum of all matching instances.</summary>
        public double SumSample() => Sample(false);

        /// <summary>Collects a sample and returns the largest instance value (better for multi-GPU laptops).</summary>
        public double MaxSample() => Sample(true);

        private double Sample(bool max)
        {
            double total = 0;
            if (!Ok) return 0;
            try
            {
                int rc = PdhCollectQueryData(_query);
                if (rc != 0) return 0;

                uint size = _bufferSize;
                uint count;
                rc = PdhGetFormattedCounterArrayW(_counter, PDH_FMT_DOUBLE | PDH_FMT_NOCAP100, ref size, out count, _buffer);
                if (rc == PDH_MORE_DATA)
                {
                    if (_buffer != IntPtr.Zero) Marshal.FreeHGlobal(_buffer);
                    _bufferSize = size + 1024;
                    _buffer = Marshal.AllocHGlobal((int)_bufferSize);
                    size = _bufferSize;
                    rc = PdhGetFormattedCounterArrayW(_counter, PDH_FMT_DOUBLE | PDH_FMT_NOCAP100, ref size, out count, _buffer);
                }
                if (rc != 0 || _buffer == IntPtr.Zero) return 0;

                int itemSize = Marshal.SizeOf(typeof(PdhItemDouble));
                for (int i = 0; i < count; i++)
                {
                    var item = Marshal.PtrToStructure<PdhItemDouble>(_buffer + i * itemSize);
                    if (item.CStatus == 0 && !double.IsNaN(item.Value) && !double.IsInfinity(item.Value))
                        total = max ? Math.Max(total, item.Value) : total + item.Value;
                }
            }
            catch { return 0; }
            return total;
        }

        public void Dispose()
        {
            try
            {
                if (_buffer != IntPtr.Zero) { Marshal.FreeHGlobal(_buffer); _buffer = IntPtr.Zero; }
                if (_query != IntPtr.Zero) { PdhCloseQuery(_query); _query = IntPtr.Zero; }
            }
            catch { }
        }
    }
}
