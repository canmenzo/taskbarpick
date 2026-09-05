using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Win32;

namespace TaskbarPick
{
    internal static class Config
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AdvancedKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
        private const string AppName = "taskbarpick";

        private static string Dir
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName);
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        private static string FilePath
        {
            get { return Path.Combine(Dir, "hidden.txt"); }
        }

        // Only the displays whose taskbar is hidden are stored, so a newly attached
        // display keeps its taskbar instead of silently losing it.
        public static List<string> LoadHidden()
        {
            var list = new List<string>();
            try
            {
                if (File.Exists(FilePath))
                    foreach (string line in File.ReadAllLines(FilePath))
                        if (line.Trim().Length > 0) list.Add(line.Trim());
            }
            catch { }
            return list;
        }

        public static void SaveHidden(List<string> keys)
        {
            try { File.WriteAllLines(FilePath, keys.ToArray()); }
            catch { }
        }

        public static bool Autostart
        {
            get
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey))
                    return k != null && k.GetValue(AppName) != null;
            }
            set
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (k == null) return;
                    if (value)
                        k.SetValue(AppName, "\"" + Application_ExecutablePath() + "\" --silent");
                    else if (k.GetValue(AppName) != null)
                        k.DeleteValue(AppName);
                }
            }
        }

        private static string Application_ExecutablePath()
        {
            return Process.GetCurrentProcess().MainModule.FileName;
        }

        public static bool MultiMonTaskbars
        {
            get
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(AdvancedKey))
                {
                    if (k == null) return false;
                    object v = k.GetValue("MMTaskbarEnabled");
                    return v != null && Convert.ToInt32(v) == 1;
                }
            }
            set
            {
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(AdvancedKey))
                    if (k != null) k.SetValue("MMTaskbarEnabled", value ? 1 : 0, RegistryValueKind.DWord);
            }
        }

        public static void RestartExplorer()
        {
            foreach (Process p in Process.GetProcessesByName("explorer"))
            {
                try { p.Kill(); }
                catch { }
            }
            for (int i = 0; i < 20; i++)
            {
                Thread.Sleep(250);
                if (Process.GetProcessesByName("explorer").Length > 0) return;
            }
            try { Process.Start("explorer.exe"); }
            catch { }
        }
    }
}
