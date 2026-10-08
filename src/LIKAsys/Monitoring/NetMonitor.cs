using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using LIKAsys.Core;

namespace LIKAsys.Monitoring
{
    /// <summary>
    /// Throughput of the active network adapter plus a round-trip time to a host of the
    /// user's choice. Both are cheap: the counters come from the adapter itself and the
    /// ping runs on its own slow timer, never on the sampling loop.
    /// </summary>
    public sealed class NetMonitor : IDisposable
    {
        private readonly AppSettings _settings;

        private long _lastRx, _lastTx;
        private long _lastStamp;
        private string _adapter = "";

        private volatile int _pingMs = -1;
        private int _pingBusy;
        private DateTime _lastPing = DateTime.MinValue;
        private bool _disposed;

        public NetMonitor(AppSettings settings)
        {
            _settings = settings;
            _lastStamp = Stopwatch.GetTimestamp();
        }

        public void Sample(MetricsSnapshot s)
        {
            if (s == null) return;

            try
            {
                long rx = 0, tx = 0;
                string name = "";

                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;

                    var st = ni.GetIPv4Statistics();
                    // virtual adapters report zero traffic - the busiest real one wins
                    if (st.BytesReceived + st.BytesSent <= 0) continue;

                    rx += st.BytesReceived;
                    tx += st.BytesSent;
                    if (name.Length == 0) name = ni.Name;
                }

                long now = Stopwatch.GetTimestamp();
                double seconds = (now - _lastStamp) / (double)Stopwatch.Frequency;
                _lastStamp = now;
                _adapter = name;

                if (_lastRx > 0 && seconds > 0.05 && rx >= _lastRx && tx >= _lastTx)
                {
                    // bytes -> megabits
                    s.NetDownMbps = (rx - _lastRx) * 8.0 / 1_000_000.0 / seconds;
                    s.NetUpMbps = (tx - _lastTx) * 8.0 / 1_000_000.0 / seconds;
                }
                else
                {
                    s.NetDownMbps = 0;
                    s.NetUpMbps = 0;
                }

                _lastRx = rx;
                _lastTx = tx;
                s.NetAdapter = _adapter;
            }
            catch
            {
                s.NetDownMbps = 0;
                s.NetUpMbps = 0;
            }

            s.PingMs = _pingMs;
            KickPing();
        }

        /// <summary>Fires a ping at most every 5 s and never waits for the answer.</summary>
        private void KickPing()
        {
            if (!_settings.ShowPing || _disposed) return;
            if ((DateTime.UtcNow - _lastPing).TotalSeconds < 5) return;
            if (Interlocked.Exchange(ref _pingBusy, 1) == 1) return;

            _lastPing = DateTime.UtcNow;
            var host = string.IsNullOrWhiteSpace(_settings.PingHost) ? "1.1.1.1" : _settings.PingHost.Trim();

            Task.Run(async () =>
            {
                try
                {
                    using var p = new Ping();
                    var reply = await p.SendPingAsync(host, 1500).ConfigureAwait(false);
                    _pingMs = reply != null && reply.Status == IPStatus.Success
                        ? (int)Math.Max(0, Math.Min(9999, reply.RoundtripTime))
                        : -1;
                }
                catch { _pingMs = -1; }
                finally { Interlocked.Exchange(ref _pingBusy, 0); }
            });
        }

        public void Dispose() => _disposed = true;
    }
}
