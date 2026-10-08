using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using LIKAsys;

namespace LIKAsys.Core
{
    /// <summary>Cursor packs. IT profile only - Gaming never touches the pointer.</summary>
    public enum MouseCursorStyle
    {
        None = 0,
        Neon = 1,
        Terminal = 2,
        Carbon = 3,
        Blueprint = 4
    }

    public sealed class CursorPack
    {
        public MouseCursorStyle Style;
        public string Folder;
        public string Name;
        public string Desc;      // Albanian, Lang.T() turns it into English
    }

    /// <summary>
    /// Swaps the Windows pointer for one of ours while LIKAsys is running.
    ///
    /// Nothing is written to the registry. <c>SetSystemCursor</c> replaces the cursor for
    /// the current session only, and <c>SystemParametersInfo(SPI_SETCURSORS)</c> reloads the
    /// user's own scheme straight back from their settings. So every exit path - clean quit,
    /// profile switch, logoff, even a crash followed by a reboot - lands the user back on
    /// the pointer they had. Nothing here is allowed to throw: a missing .cur file or a
    /// locked-down machine must never stop the app from starting.
    /// </summary>
    public static class MouseCursors
    {
        public static readonly CursorPack[] All =
        {
            new CursorPack { Style = MouseCursorStyle.None, Folder = null,
                             Name = "Kursori i Windows-it",
                             Desc = "Pa ndryshim. LIKAsys nuk e prek shigjetën." },
            new CursorPack { Style = MouseCursorStyle.Neon, Folder = "neon",
                             Name = "Neon",
                             Desc = "Xham i errët me buzë ciani që ndriçon. Shkon me temat e errëta." },
            new CursorPack { Style = MouseCursorStyle.Terminal, Folder = "terminal",
                             Name = "Terminal",
                             Desc = "Piksel jeshil fosfori, me vija skanimi. Teksti merr bllokun e konsolës." },
            new CursorPack { Style = MouseCursorStyle.Carbon, Folder = "carbon",
                             Name = "Carbon",
                             Desc = "Metal i ngurtë me një fije të bardhë rreth e rrotull. I qetë dhe i pastër." },
            new CursorPack { Style = MouseCursorStyle.Blueprint, Folder = "blueprint",
                             Name = "Blueprint",
                             Desc = "Vizatim teknik: vija të holla, hijezim diagonal, shenjëstër blu." },
        };

        public static CursorPack Find(MouseCursorStyle s)
        {
            foreach (var p in All) if (p.Style == s) return p;
            return All[0];
        }

        public static string FolderPath(CursorPack p)
        {
            if (p == null || string.IsNullOrEmpty(p.Folder)) return null;
            try { return Path.Combine(AppContext.BaseDirectory, "assets", "cursors", p.Folder); }
            catch { return null; }
        }

        /// <summary>PNG strip of the four pointers, for the settings cards. Null when missing.</summary>
        public static string PreviewPath(CursorPack p)
        {
            var dir = FolderPath(p);
            if (dir == null) return null;
            try { var f = Path.Combine(dir, "preview.png"); return File.Exists(f) ? f : null; }
            catch { return null; }
        }

        public static bool Applied { get; private set; }
        public static MouseCursorStyle Current { get; private set; } = MouseCursorStyle.None;

        // ------------------------------------------------------------- win32
        private const uint OCR_NORMAL = 32512, OCR_IBEAM = 32513, OCR_CROSS = 32515, OCR_HAND = 32649;
        private const uint IMAGE_CURSOR = 2, LR_LOADFROMFILE = 0x0010;
        private const uint SPI_SETCURSORS = 0x0057, SPIF_SENDCHANGE = 0x02;
        private const int SM_CXCURSOR = 13, SM_CYCURSOR = 14;

        private static readonly KeyValuePair<string, uint>[] Slots =
        {
            new KeyValuePair<string, uint>("arrow.cur", OCR_NORMAL),
            new KeyValuePair<string, uint>("ibeam.cur", OCR_IBEAM),
            new KeyValuePair<string, uint>("link.cur",  OCR_HAND),
            new KeyValuePair<string, uint>("cross.cur", OCR_CROSS),
        };

        [DllImport("user32.dll", EntryPoint = "LoadImageW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadImageW(IntPtr hinst, string name, uint type, int cx, int cy, uint load);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetSystemCursor(IntPtr hcur, uint id);

        [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)]
        private static extern bool SystemParametersInfoW(uint action, uint param, IntPtr pv, uint winIni);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int index);

        [DllImport("user32.dll")]
        private static extern bool DestroyCursor(IntPtr h);

        // ------------------------------------------------------------- api

        /// <summary>The only entry point the app uses: cursors live in the IT profile.</summary>
        public static void Sync(AppSettings s)
        {
            if (s == null) { Restore(); return; }
            if (s.Profile != UiProfile.It || s.MouseCursor == MouseCursorStyle.None) { Restore(); return; }
            Apply(s.MouseCursor);
        }

        public static void Apply(MouseCursorStyle style)
        {
            try
            {
                if (style == MouseCursorStyle.None) { Restore(); return; }

                var dir = FolderPath(Find(style));
                if (dir == null || !Directory.Exists(dir)) { AppInfo.Log("cursors: mungon " + (dir ?? "?")); return; }

                // Windows scales the pointer with the DPI and the accessibility size slider.
                // Ask for exactly the size the machine wants and let LoadImage pick the frame.
                int cx = GetSystemMetrics(SM_CXCURSOR), cy = GetSystemMetrics(SM_CYCURSOR);
                if (cx <= 0) cx = 32;
                if (cy <= 0) cy = 32;

                bool any = false;
                foreach (var slot in Slots)
                {
                    var file = Path.Combine(dir, slot.Key);
                    if (!File.Exists(file)) continue;

                    var h = LoadImageW(IntPtr.Zero, file, IMAGE_CURSOR, cx, cy, LR_LOADFROMFILE);
                    if (h == IntPtr.Zero) continue;

                    // SetSystemCursor takes ownership of the handle and destroys it itself,
                    // so a fresh one is loaded on every apply. Only free it if the call failed.
                    if (SetSystemCursor(h, slot.Value)) any = true;
                    else DestroyCursor(h);
                }

                if (any)
                {
                    Applied = true;
                    Current = style;
                    AppInfo.Log("ok: kursoret -> " + style);
                }
            }
            catch (Exception ex) { AppInfo.Log("cursors apply: " + ex.Message); }
        }

        /// <summary>Hands the machine back the user's own pointers.</summary>
        public static void Restore()
        {
            if (!Applied) { Current = MouseCursorStyle.None; return; }
            try { SystemParametersInfoW(SPI_SETCURSORS, 0, IntPtr.Zero, SPIF_SENDCHANGE); }
            catch (Exception ex) { AppInfo.Log("cursors restore: " + ex.Message); }
            finally
            {
                Applied = false;
                Current = MouseCursorStyle.None;
            }
        }
    }
}
