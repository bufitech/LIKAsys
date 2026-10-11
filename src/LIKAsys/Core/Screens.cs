using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Forms = System.Windows.Forms;

namespace LIKAsys.Core
{
    /// <summary>One monitor, as the rest of the app wants to see it.</summary>
    public sealed class ScreenInfo
    {
        public int Index;
        /// <summary>The adapter path, like \\.\DISPLAY2. Stable while the cable stays in.</summary>
        public string Id = "";
        /// <summary>What the monitor calls itself, like DELL U2719D. Falls back to the size.</summary>
        public string Name = "";
        public int Left, Top, Width, Height;
        public int WorkLeft, WorkTop, WorkWidth, WorkHeight;
        public bool Primary;
        public double Dpi = 1.0;

        public int Right => Left + Width;
        public int Bottom => Top + Height;
        public int CenterX => Left + Width / 2;
        public int CenterY => Top + Height / 2;

        /// <summary>What a person sees in the picker: "2  DELL U2719D  2560x1440".</summary>
        public string Caption =>
            (Index + 1) + "  " + (string.IsNullOrEmpty(Name) ? "Monitor" : Name) + "  " + Width + "x" + Height;

        public bool Contains(int x, int y) => x >= Left && x < Right && y >= Top && y < Bottom;
    }

    /// <summary>
    /// Finds the monitors and decides which one the widget belongs on.
    /// Everything here is physical pixels, the same units SetWindowPos wants.
    /// </summary>
    public static class Screens
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum,
            ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
            public int StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;
        }

        /// <summary>Raised after Windows reports a monitor change, once the list is already fresh.</summary>
        public static event EventHandler Changed;

        private static List<ScreenInfo> _cache;
        private static readonly object Gate = new object();
        private static bool _hooked;

        /// <summary>Starts listening for monitors being plugged, unplugged or resized.</summary>
        public static void Watch()
        {
            if (_hooked) return;
            _hooked = true;
            try
            {
                Microsoft.Win32.SystemEvents.DisplaySettingsChanged += (s, e) =>
                {
                    Invalidate();
                    try { Changed?.Invoke(null, EventArgs.Empty); } catch { }
                };
            }
            catch { }
        }

        public static void Invalidate()
        {
            lock (Gate) _cache = null;
        }

        public static List<ScreenInfo> All()
        {
            lock (Gate)
            {
                if (_cache != null) return _cache;
                _cache = Read();
                return _cache;
            }
        }

        private static List<ScreenInfo> Read()
        {
            var list = new List<ScreenInfo>();
            try
            {
                var raw = Forms.Screen.AllScreens;
                for (int i = 0; i < raw.Length; i++)
                {
                    var b = raw[i].Bounds;
                    var w = raw[i].WorkingArea;
                    var si = new ScreenInfo
                    {
                        Index = i,
                        Id = raw[i].DeviceName ?? "",
                        Left = b.Left,
                        Top = b.Top,
                        Width = b.Width,
                        Height = b.Height,
                        WorkLeft = w.Left,
                        WorkTop = w.Top,
                        WorkWidth = w.Width,
                        WorkHeight = w.Height,
                        Primary = raw[i].Primary
                    };
                    si.Dpi = Native.GetScaleForPoint(si.CenterX, si.CenterY);
                    si.Name = FriendlyName(si.Id, b.Width, b.Height);
                    list.Add(si);
                }
            }
            catch { }

            if (list.Count == 0)
            {
                // Never hand back an empty list. Something is better than a crash.
                var v = Forms.SystemInformation.VirtualScreen;
                list.Add(new ScreenInfo
                {
                    Index = 0,
                    Id = "",
                    Name = "Monitor",
                    Left = v.Left,
                    Top = v.Top,
                    Width = Math.Max(640, v.Width),
                    Height = Math.Max(480, v.Height),
                    WorkLeft = v.Left,
                    WorkTop = v.Top,
                    WorkWidth = Math.Max(640, v.Width),
                    WorkHeight = Math.Max(480, v.Height),
                    Primary = true
                });
            }
            return list;
        }

        /// <summary>Asks Windows what the panel is actually called, so the picker is readable.</summary>
        private static string FriendlyName(string device, int w, int h)
        {
            try
            {
                if (!string.IsNullOrEmpty(device))
                {
                    var dd = new DISPLAY_DEVICE();
                    dd.cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE));
                    if (EnumDisplayDevices(device, 0, ref dd, 0))
                    {
                        var n = (dd.DeviceString ?? "").Trim();
                        // the generic driver name tells a person nothing
                        if (n.Length > 0 &&
                            !n.StartsWith("Generic", StringComparison.OrdinalIgnoreCase) &&
                            !n.Equals("Default Monitor", StringComparison.OrdinalIgnoreCase))
                            return n;
                    }
                }
            }
            catch { }
            return w + "x" + h;
        }

        // ------------------------------------------------------------ choosing

        public static ScreenInfo Primary()
        {
            var all = All();
            return all.FirstOrDefault(s => s.Primary) ?? all[0];
        }

        public static ScreenInfo FromPoint(int x, int y)
        {
            var all = All();
            foreach (var s in all) if (s.Contains(x, y)) return s;

            // outside every monitor, so take the closest one by centre distance
            ScreenInfo best = all[0];
            double bd = double.MaxValue;
            foreach (var s in all)
            {
                double dx = s.CenterX - x, dy = s.CenterY - y;
                double d = dx * dx + dy * dy;
                if (d < bd) { bd = d; best = s; }
            }
            return best;
        }

        /// <summary>The monitor the window in front is on. Used by the follow the game mode.</summary>
        public static ScreenInfo Foreground()
        {
            try
            {
                var h = Native.GetForegroundWindow();
                if (h != IntPtr.Zero && Native.GetWindowRect(h, out var r))
                {
                    int cx = r.Left + (r.Right - r.Left) / 2;
                    int cy = r.Top + (r.Bottom - r.Top) / 2;
                    if (r.Right > r.Left && r.Bottom > r.Top) return FromPoint(cx, cy);
                }
            }
            catch { }
            return Primary();
        }

        /// <summary>
        /// The monitor the widget should be on right now, following the saved rule.
        /// A monitor that was unplugged falls back to the primary rather than leaving
        /// the card stranded on coordinates nobody can see.
        /// </summary>
        public static ScreenInfo Pick(AppSettings s)
        {
            var all = All();
            try
            {
                switch (s.MonitorMode)
                {
                    case MonitorMode.Game:
                        return Foreground();

                    case MonitorMode.Fixed:
                        if (!string.IsNullOrEmpty(s.MonitorId))
                        {
                            var byId = all.FirstOrDefault(x =>
                                string.Equals(x.Id, s.MonitorId, StringComparison.OrdinalIgnoreCase));
                            if (byId != null) return byId;
                        }
                        if (s.MonitorIndex >= 0 && s.MonitorIndex < all.Count) return all[s.MonitorIndex];
                        return Primary();

                    default:
                        return Primary();
                }
            }
            catch { }
            return all[0];
        }

        /// <summary>True when the point is not on any monitor, so the card has to be rescued.</summary>
        public static bool OffScreen(int x, int y)
        {
            foreach (var s in All()) if (s.Contains(x, y)) return false;
            return true;
        }
    }
}
