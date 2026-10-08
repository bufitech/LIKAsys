using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using LIKAsys.Core;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;

namespace LIKAsys.Monitoring
{
    /// <summary>
    /// Real FPS for any game (DX9/DX11/DX12/Vulkan/OpenGL that presents through DXGI) measured the same
    /// way PresentMon does it: by counting ETW "PresentStart" events per process.
    /// Requires administrator rights; degrades gracefully to "--" when not elevated.
    /// </summary>
    internal sealed class FpsMonitor : IDisposable
    {
        private static readonly Guid DxgiProvider = new Guid("CA11C036-0102-4A2D-A6AD-F03CFED5D3C9");
        private static readonly Guid D3d9Provider = new Guid("783ACA0A-790E-4D7F-8451-AA850511C6B9");
        private const int DxgiPresentStart = 42;
        private const int D3d9PresentStart = 1;
        private const string SessionName = "LIKAsys-PresentSession";

        /// <summary>How many recent frame intervals we keep per process for the percentiles.</summary>
        private const int FrameCap = 4096;

        private sealed class Counter
        {
            public long Total;
            public long LastTotal;
            public long LastTicks;
            public double Fps;
            public DateTime LastSeen;
            public string ProcessName;

            // --- real frame pacing, straight from the ETW timestamps
            public readonly object Gate = new object();
            public readonly float[] Frames = new float[FrameCap];
            public int FrameCount;
            public int FrameIndex;
            public double LastEventMs = -1;
        }

        private readonly float[] _sortBuf = new float[FrameCap];

        private readonly ConcurrentDictionary<int, Counter> _counters = new ConcurrentDictionary<int, Counter>();
        private TraceEventSession _session;
        private Thread _worker;
        private volatile bool _running;

        public bool Supported { get; private set; }
        public string Status { get; private set; } = "off";

        public void Start()
        {
            if (_running) return;
            if (!Native.IsElevated())
            {
                Status = "admin";
                AppInfo.Log("FPS: not elevated, present monitoring disabled.");
                return;
            }

            _running = true;
            _worker = new Thread(Run) { IsBackground = true, Name = "LIKAsys-ETW", Priority = ThreadPriority.AboveNormal };
            _worker.Start();
        }

        private void Run()
        {
            try
            {
                // clean up a session left behind by a crash
                try { TraceEventSession.GetActiveSession(SessionName)?.Dispose(); } catch { }

                _session = new TraceEventSession(SessionName)
                {
                    StopOnDispose = true,
                    BufferSizeMB = 16,
                    CpuSampleIntervalMSec = 10
                };

                _session.EnableProvider(DxgiProvider, TraceEventLevel.Informational, 0x1);
                _session.EnableProvider(D3d9Provider, TraceEventLevel.Informational, 0x1);

                _session.Source.AllEvents += OnEvent;
                Supported = true;
                Status = "on";
                _session.Source.Process();   // blocks until the session is disposed
            }
            catch (Exception ex)
            {
                Supported = false;
                Status = "error";
                AppInfo.Log("FPS monitor failed: " + ex.Message);
            }
            finally
            {
                _running = false;
            }
        }

        private void OnEvent(TraceEvent data)
        {
            try
            {
                int id = (int)data.ID;
                if (data.ProviderGuid == DxgiProvider)
                {
                    if (id != DxgiPresentStart) return;
                }
                else if (data.ProviderGuid == D3d9Provider)
                {
                    if (id != D3d9PresentStart) return;
                }
                else return;

                int pid = data.ProcessID;
                if (pid <= 4) return;

                var c = _counters.GetOrAdd(pid, _ => new Counter { LastTicks = Stopwatch.GetTimestamp(), LastSeen = DateTime.UtcNow });
                Interlocked.Increment(ref c.Total);
                c.LastSeen = DateTime.UtcNow;

                // the event's own timestamp is far steadier than the moment the callback runs
                double ts = data.TimeStampRelativeMSec;
                double prev = c.LastEventMs;
                c.LastEventMs = ts;
                if (prev < 0) return;

                double ms = ts - prev;
                if (ms <= 0.05 || ms > 2000) return;        // ignore duplicates and long pauses

                lock (c.Gate)
                {
                    c.Frames[c.FrameIndex] = (float)ms;
                    c.FrameIndex = (c.FrameIndex + 1) % FrameCap;
                    if (c.FrameCount < FrameCap) c.FrameCount++;
                }
            }
            catch { }
        }

        /// <summary>Recomputes per-process rates; call about once per second from the metrics loop.</summary>
        public void Tick()
        {
            long now = Stopwatch.GetTimestamp();
            foreach (var kv in _counters)
            {
                var c = kv.Value;
                long total = Interlocked.Read(ref c.Total);
                double seconds = (now - c.LastTicks) / (double)Stopwatch.Frequency;
                if (seconds >= 0.35)
                {
                    c.Fps = (total - c.LastTotal) / seconds;
                    c.LastTotal = total;
                    c.LastTicks = now;
                }
                if ((DateTime.UtcNow - c.LastSeen).TotalSeconds > 5)
                    _counters.TryRemove(kv.Key, out _);
            }
        }

        /// <summary>FPS of the focused window, or of the busiest presenting process when the desktop has focus.</summary>
        public void Fill(MetricsSnapshot s)
        {
            s.Fps = -1;
            s.FrameTimeMs = -1;
            s.FpsLow1 = -1;
            s.FpsLow01 = -1;
            s.FpsSource = Status == "admin" ? "admin" : "";

            if (!Supported) return;

            try
            {
                Counter best = null;
                int bestPid = 0;

                int fgPid = 0;
                var hwnd = Native.GetForegroundWindow();
                if (hwnd != IntPtr.Zero) { Native.GetWindowThreadProcessId(hwnd, out uint p); fgPid = (int)p; }

                if (fgPid != 0 && _counters.TryGetValue(fgPid, out var fg) && fg.Fps > 0.5)
                {
                    best = fg; bestPid = fgPid;
                }
                else
                {
                    foreach (var kv in _counters)
                        if (best == null || kv.Value.Fps > best.Fps) { best = kv.Value; bestPid = kv.Key; }
                }

                if (best == null || best.Fps < 0.5) { s.FpsSource = ""; return; }

                s.Fps = Math.Round(best.Fps, 0);
                s.FrameTimeMs = best.Fps > 0 ? Math.Round(1000.0 / best.Fps, 1) : -1;

                if (best.ProcessName == null)
                {
                    try { best.ProcessName = Process.GetProcessById(bestPid).ProcessName; }
                    catch { best.ProcessName = ""; }
                }
                s.FpsSource = best.ProcessName ?? "";
                FillLows(best, s);
            }
            catch { }
        }

        /// <summary>
        /// 1% low and 0.1% low, the numbers that actually describe stutter. Both are read
        /// off the sorted frame-time window: the 99th percentile frame time turned back
        /// into frames per second.
        /// </summary>
        private void FillLows(Counter c, MetricsSnapshot s)
        {
            int n;
            lock (c.Gate)
            {
                n = c.FrameCount;
                if (n > 0) Array.Copy(c.Frames, _sortBuf, n);
            }
            if (n < 120) return;                 // too few frames to mean anything yet

            Array.Sort(_sortBuf, 0, n);

            int i99 = (int)(n * 0.99);
            if (i99 >= n) i99 = n - 1;
            float p99 = _sortBuf[i99];
            if (p99 > 0.01f) s.FpsLow1 = Math.Round(1000.0 / p99, 0);

            if (n >= 1000)
            {
                int i999 = (int)(n * 0.999);
                if (i999 >= n) i999 = n - 1;
                float p999 = _sortBuf[i999];
                if (p999 > 0.01f) s.FpsLow01 = Math.Round(1000.0 / p999, 0);
            }
        }

        public void Dispose()
        {
            _running = false;
            try { _session?.Dispose(); } catch { }
            _session = null;
            Supported = false;
            Status = "off";
        }
    }
}
