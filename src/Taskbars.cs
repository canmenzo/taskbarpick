using System;
using System.Collections.Generic;
using System.Text;

namespace TaskbarPick
{
    internal static class Taskbars
    {
        // Hiding the window is the whole trick. The strip a hidden bar reserves belongs to
        // Explorer: SPI_SETWORKAREA is a no-op on secondary displays and on the primary it
        // only starts a tug of war that walks windows up the screen, so leave work areas alone.
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
            }
        }

        public static void RestoreAll()
        {
            Apply(Monitors.All(), delegate { return true; });
        }

        private static List<IntPtr> Windows()
        {
            var found = new List<IntPtr>();
            var buf = new StringBuilder(64);
            Native.EnumWindows(delegate(IntPtr hwnd, IntPtr lp)
            {
                buf.Length = 0;
                Native.GetClassName(hwnd, buf, buf.Capacity);
                string cls = buf.ToString();
                if (cls == "Shell_TrayWnd" || cls == "Shell_SecondaryTrayWnd") found.Add(hwnd);
                return true;
            }, IntPtr.Zero);
            return found;
        }
    }
}
