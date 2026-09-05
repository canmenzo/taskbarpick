using System;
using System.Collections.Generic;
using System.Drawing;

namespace TaskbarPick
{
    // Explorer keeps reserving the strip of a hidden taskbar and there is no way to take it
    // back (SPI_SETWORKAREA, ABM_SETPOS, the per-monitor autohide flag and the taskbar size in
    // MMStuckRects3 were all measured as no-ops on a secondary display). So instead of fighting
    // for the work area, resize the windows that care: a window maximized on such a display is
    // un-maximized and placed over the full monitor, which is the only way past a maximized
    // window's size, since the shell re-applies the maximized rect over any SetWindowPos.
    internal static class Fill
    {
        private const int MaxBorder = 32;

        private static readonly Dictionary<IntPtr, RECT> Stretched = new Dictionary<IntPtr, RECT>();

        public static void Apply(List<MonitorInfo> monitors, Predicate<MonitorInfo> bare)
        {
            foreach (IntPtr hwnd in Candidates())
            {
                MonitorInfo m = Monitors.FromHandle(monitors, Native.MonitorFromWindow(hwnd, Native.MONITOR_DEFAULTTONEAREST));
                if (m == null || !bare(m)) continue;

                RECT wr;
                if (!Native.GetWindowRect(hwnd, out wr)) continue;

                // A maximized window sits over the work area, inflated by its invisible resize
                // border. Reuse that same inset against the full monitor instead of guessing it.
                int dl = wr.Left - m.WorkArea.Left;
                int dt = wr.Top - m.WorkArea.Top;
                int dr = wr.Right - m.WorkArea.Right;
                int db = wr.Bottom - m.WorkArea.Bottom;
                if (Math.Abs(dl) > MaxBorder || Math.Abs(dt) > MaxBorder ||
                    Math.Abs(dr) > MaxBorder || Math.Abs(db) > MaxBorder)
                    continue;   // not a plain maximize, leave it alone

                Rectangle target = Rectangle.FromLTRB(
                    m.Bounds.Left + dl, m.Bounds.Top + dt, m.Bounds.Right + dr, m.Bounds.Bottom + db);
                Stretch(hwnd, target);
            }

            // Give back anything on a display that has its taskbar again.
            var done = new List<IntPtr>();
            foreach (KeyValuePair<IntPtr, RECT> kv in Stretched)
            {
                MonitorInfo m = Monitors.FromHandle(monitors, Native.MonitorFromWindow(kv.Key, Native.MONITOR_DEFAULTTONEAREST));
                if (!Native.IsWindow(kv.Key) || m == null || !bare(m))
                {
                    Unstretch(kv.Key, kv.Value);
                    done.Add(kv.Key);
                }
            }
            foreach (IntPtr hwnd in done) Stretched.Remove(hwnd);
        }

        public static void RestoreAll()
        {
            foreach (KeyValuePair<IntPtr, RECT> kv in Stretched)
                Unstretch(kv.Key, kv.Value);
            Stretched.Clear();
        }

        private static List<IntPtr> Candidates()
        {
            var found = new List<IntPtr>();
            Native.EnumWindows(delegate(IntPtr hwnd, IntPtr lp)
            {
                if (Native.IsZoomed(hwnd) && Native.IsWindowVisible(hwnd) &&
                    Native.GetWindow(hwnd, Native.GW_OWNER) == IntPtr.Zero &&
                    (Native.GetWindowLong(hwnd, Native.GWL_EXSTYLE) & Native.WS_EX_TOOLWINDOW) == 0)
                    found.Add(hwnd);
                return true;
            }, IntPtr.Zero);
            return found;
        }

        private static void Stretch(IntPtr hwnd, Rectangle target)
        {
            var wp = new WINDOWPLACEMENT();
            wp.length = System.Runtime.InteropServices.Marshal.SizeOf(typeof(WINDOWPLACEMENT));
            if (!Native.GetWindowPlacement(hwnd, ref wp)) return;

            if (!Stretched.ContainsKey(hwnd))
                Stretched[hwnd] = wp.rcNormalPosition;   // what the window goes back to

            wp.showCmd = Native.SW_SHOWNORMAL;
            wp.rcNormalPosition.Left = target.Left;
            wp.rcNormalPosition.Top = target.Top;
            wp.rcNormalPosition.Right = target.Right;
            wp.rcNormalPosition.Bottom = target.Bottom;
            Native.SetWindowPlacement(hwnd, ref wp);
            Native.SetWindowPos(hwnd, IntPtr.Zero, target.Left, target.Top, target.Width, target.Height,
                Native.SWP_NOZORDER | Native.SWP_NOACTIVATE);
        }

        private static void Unstretch(IntPtr hwnd, RECT normal)
        {
            if (!Native.IsWindow(hwnd)) return;

            var wp = new WINDOWPLACEMENT();
            wp.length = System.Runtime.InteropServices.Marshal.SizeOf(typeof(WINDOWPLACEMENT));
            if (!Native.GetWindowPlacement(hwnd, ref wp)) return;

            wp.showCmd = Native.SW_SHOWMAXIMIZED;
            wp.rcNormalPosition = normal;
            Native.SetWindowPlacement(hwnd, ref wp);
        }
    }
}
