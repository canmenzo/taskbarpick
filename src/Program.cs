using System;
using System.Threading;
using System.Windows.Forms;

namespace TaskbarPick
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Native.SetProcessDPIAware();

            bool created;
            using (var single = new Mutex(true, "taskbarpick-single-instance", out created))
            {
                if (!created) return;

                bool silent = false;
                foreach (string a in args)
                    if (a == "--silent" || a == "/silent") silent = true;

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm(silent));
                GC.KeepAlive(single);
            }
        }
    }
}
