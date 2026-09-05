using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

namespace TaskbarPick
{
    internal class MonitorInfo
    {
        public IntPtr Handle;
        public string Device;       // \.\DISPLAY1
        public string Id;           // stable device interface path, survives renumbering
        public string Name;         // friendly monitor name
        public Rectangle Bounds;
        public Rectangle WorkArea;
        public bool IsPrimary;
        public int Number;

        public string Key
        {
            get { return string.IsNullOrEmpty(Id) ? Device : Id; }
        }

        public bool Matches(string key)
        {
            return key == Id || key == Device;
        }

        public string Describe()
        {
            // "Generic PnP Monitor" is what most displays report; it adds nothing.
            bool useName = !string.IsNullOrEmpty(Name) && Name.IndexOf("Generic", StringComparison.OrdinalIgnoreCase) < 0;
            // · middle dot, × multiplication sign, written escaped so the build does
            // not depend on the compiler guessing the source encoding.
            return string.Format("Display {0}   ·   {1} × {2}{3}{4}",
                Number, Bounds.Width, Bounds.Height,
                IsPrimary ? "   ·   Primary" : "",
                useName ? "   ·   " + Name : "");
        }
    }

    internal static class Monitors
    {
        public static List<MonitorInfo> All()
        {
            var list = new List<MonitorInfo>();
            Native.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                delegate(IntPtr h, IntPtr hdc, ref RECT r, IntPtr d)
                {
                    var mi = new MONITORINFOEX();
                    mi.cbSize = Marshal.SizeOf(typeof(MONITORINFOEX));
                    if (Native.GetMonitorInfo(h, ref mi))
                    {
                        var m = new MonitorInfo();
                        m.Handle = h;
                        m.Device = mi.szDevice;
                        m.Bounds = mi.rcMonitor.ToRectangle();
                        m.WorkArea = mi.rcWork.ToRectangle();
                        m.IsPrimary = (mi.dwFlags & Native.MONITORINFOF_PRIMARY) != 0;
                        m.Number = ParseNumber(mi.szDevice);

                        var dd = new DISPLAY_DEVICE();
                        dd.cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE));
                        if (Native.EnumDisplayDevices(mi.szDevice, 0, ref dd, Native.EDD_GET_DEVICE_INTERFACE_NAME))
                        {
                            m.Name = dd.DeviceString;
                            m.Id = dd.DeviceID;
                        }
                        list.Add(m);
                    }
                    return true;
                }, IntPtr.Zero);

            list.Sort(delegate(MonitorInfo a, MonitorInfo b) { return a.Number.CompareTo(b.Number); });
            return list;
        }

        public static MonitorInfo FromHandle(List<MonitorInfo> list, IntPtr handle)
        {
            foreach (var m in list)
                if (m.Handle == handle) return m;
            return null;
        }

        // Windows Settings numbers displays the same way it names them: \.\DISPLAY3 is "3".
        private static int ParseNumber(string device)
        {
            int n = 0;
            for (int i = 0; i < device.Length; i++)
                if (device[i] >= '0' && device[i] <= '9') n = n * 10 + (device[i] - '0');
            return n;
        }
    }
}
