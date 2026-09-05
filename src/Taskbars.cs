using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace TaskbarPick
{
    internal static class Taskbars
    {
        // Explorer's own taskbar windows: one primary, one per extra display.
        private static readonly string[] Classes = { "Shell_TrayWnd", "Shell_SecondaryTrayWnd" };

        public static List<IntPtr> Windows()
        {
            var found = new List<IntPtr>();
            var buf = new StringBuilder(64);
            Native.EnumWindows(delegate(IntPtr hwnd, IntPtr lp)
            {
                buf.Length = 0;
                Native.GetClassName(hwnd, buf, buf.Capacity);
                string cls = buf.ToString();
                if (cls == Classes[0] || cls == Classes[1]) found.Add(hwnd);
                return true;
            }, IntPtr.Zero);
            return found;
        }

        public static void Apply(List<MonitorInfo> monitors, Predicate<MonitorInfo> wanted)
        {
            foreach (IntPtr hwnd in Windows())
            {
                IntPtr hmon = Native.MonitorFromWindow(hwnd, Native.MONITOR_DEFAULTTONEAREST);
                MonitorInfo m = Monitors.FromHandle(monitors, hmon);
                if (m == null) continue;

                bool show = wanted(m);
                if (Native.IsWindowVisible(hwnd) != show)
                    Native.ShowWindow(hwnd, show ? Native.SW_SHOW : Native.SW_HIDE);

                // Without this the hidden bar keeps reserving its strip and maximized
                // windows leave a gap where it used to be.
                SetWorkArea(m, show ? WorkAreaWith(m, hwnd) : m.Bounds);
            }
        }

        public static void RestoreAll()
        {
            var monitors = Monitors.All();
            Apply(monitors, delegate(MonitorInfo m) { return true; });
        }

        private static Rectangle WorkAreaWith(MonitorInfo m, IntPtr taskbar)
        {
            RECT r;
            if (!Native.GetWindowRect(taskbar, out r)) return m.WorkArea;

            Rectangle bar = Rectangle.Intersect(m.Bounds, r.ToRectangle());
            if (bar.Width <= 0 || bar.Height <= 0) return m.Bounds;

            Rectangle wa = m.Bounds;
            if (bar.Width >= bar.Height)
            {
                if (bar.Top - m.Bounds.Top <= m.Bounds.Bottom - bar.Bottom)
                    wa = Rectangle.FromLTRB(wa.Left, bar.Bottom, wa.Right, wa.Bottom);
                else
                    wa = Rectangle.FromLTRB(wa.Left, wa.Top, wa.Right, bar.Top);
            }
            else
            {
                if (bar.Left - m.Bounds.Left <= m.Bounds.Right - bar.Right)
                    wa = Rectangle.FromLTRB(bar.Right, wa.Top, wa.Right, wa.Bottom);
                else
                    wa = Rectangle.FromLTRB(wa.Left, wa.Top, bar.Left, wa.Bottom);
            }
            return wa;
        }

        private static void SetWorkArea(MonitorInfo m, Rectangle want)
        {
            if (want == m.WorkArea || want.Width <= 0 || want.Height <= 0) return;
            RECT r = RECT.From(want);
            Native.SystemParametersInfo(Native.SPI_SETWORKAREA, 0, ref r, Native.SPIF_SENDCHANGE);
            m.WorkArea = want;
        }
    }
}
