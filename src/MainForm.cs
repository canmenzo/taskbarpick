using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace TaskbarPick
{
    internal class MainForm : Form
    {
        public const string AppTitle = "Taskbar Picker";
        public static readonly string Version =
            Assembly.GetExecutingAssembly().GetName().Version.ToString(2);

        private readonly bool _silent;
        private bool _firstShow = true;
        private bool _loading;

        private List<MonitorInfo> _monitors;
        private List<string> _hidden;

        private Panel _rowsPanel;
        private readonly List<CheckBox> _rows = new List<CheckBox>();
        private CheckBox _autostart;
        private CheckBox _fill;
        private Label _hint;
        private NotifyIcon _tray;
        private Timer _watchdog;
        private Timer _delayed;
        private readonly int _taskbarCreated = Native.RegisterWindowMessage("TaskbarCreated");

        public MainForm(bool silent)
        {
            _silent = silent;
            _monitors = Monitors.All();
            _hidden = Config.LoadHidden();

            BuildUi();
            BuildTray();
            RebuildRows(true);

            _watchdog = new Timer();
            _watchdog.Interval = 500;
            _watchdog.Tick += delegate { Reapply(); };
            _watchdog.Start();

            _delayed = new Timer();
            _delayed.Interval = 1500;
            _delayed.Tick += delegate { _delayed.Stop(); ReloadDisplays(); };

            SystemEvents.DisplaySettingsChanged += OnDisplaysChanged;
            Reapply();
        }

        private void BuildUi()
        {
            Text = AppTitle + " " + Version;
            Icon = Glyph.Window();
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9f);
            ClientSize = new Size(462, 358);

            var heading = new Label();
            heading.Text = AppTitle;
            heading.Font = new Font("Segoe UI Semibold", 13.5f, FontStyle.Regular);
            heading.ForeColor = Color.FromArgb(23, 23, 23);
            heading.SetBounds(20, 18, 300, 26);

            var caption = new Label();
            caption.Text = "Show the Windows taskbar on these displays";
            caption.ForeColor = Color.FromArgb(96, 96, 96);
            caption.SetBounds(21, 46, 380, 18);

            _rowsPanel = new Panel();
            _rowsPanel.SetBounds(20, 74, 422, 116);
            _rowsPanel.BackColor = Color.FromArgb(250, 250, 250);
            _rowsPanel.BorderStyle = BorderStyle.FixedSingle;

            var identify = new Button();
            identify.Text = "Identify Displays";
            identify.SetBounds(20, 200, 130, 28);
            identify.FlatStyle = FlatStyle.System;
            identify.Click += delegate { Identify(); };

            _autostart = new CheckBox();
            _autostart.Text = "Start with Windows";
            _autostart.AutoSize = true;
            _autostart.Location = new Point(166, 205);
            _autostart.CheckedChanged += delegate
            {
                if (!_loading) Config.Autostart = _autostart.Checked;
            };

            _fill = new CheckBox();
            _fill.Text = "Stretch maximized windows over the leftover strip";
            _fill.AutoSize = true;
            _fill.Location = new Point(21, 240);
            _fill.CheckedChanged += delegate
            {
                if (_loading) return;
                Config.FillBareDisplays = _fill.Checked;
                if (!_fill.Checked) Fill.RestoreAll();
                UpdateHint();
                Reapply();
            };

            _hint = new Label();
            _hint.SetBounds(21, 268, 421, 46);
            _hint.ForeColor = Color.FromArgb(128, 128, 128);

            var apply = new Button();
            apply.Text = "Apply";
            apply.SetBounds(276, 320, 80, 28);
            apply.FlatStyle = FlatStyle.System;
            apply.Click += delegate { Save(); };

            var close = new Button();
            close.Text = "Close";
            close.SetBounds(362, 320, 80, 28);
            close.FlatStyle = FlatStyle.System;
            close.Click += delegate { Hide(); };

            AcceptButton = apply;
            CancelButton = close;
            Controls.AddRange(new Control[]
            {
                heading, caption, _rowsPanel, identify, _autostart, _fill, _hint, apply, close
            });
        }

        private void BuildTray()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Settings", null, delegate { ShowSettings(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit (Restore All Taskbars)", null, delegate { ExitApp(); });

            _tray = new NotifyIcon();
            _tray.Icon = Glyph.Tray();
            _tray.Text = AppTitle + " " + Version;
            _tray.ContextMenuStrip = menu;
            _tray.DoubleClick += delegate { ShowSettings(); };
            _tray.Visible = true;
        }

        // fromSaved: reset the ticks to what is on disk. Otherwise keep whatever the user has
        // set but not applied yet, so a background refresh cannot silently undo their edit.
        private void RebuildRows(bool fromSaved)
        {
            var pending = new Dictionary<string, bool>();
            if (!fromSaved)
                foreach (CheckBox old in _rows)
                    pending[((MonitorInfo)old.Tag).Key] = old.Checked;

            _loading = true;
            _rowsPanel.Controls.Clear();
            _rows.Clear();

            int y = 12;
            foreach (MonitorInfo m in _monitors)
            {
                var cb = new CheckBox();
                cb.AutoSize = true;
                cb.Tag = m;
                cb.Text = m.Describe();
                cb.Location = new Point(14, y);
                cb.BackColor = Color.Transparent;

                bool state;
                cb.Checked = pending.TryGetValue(m.Key, out state) ? state : !IsHidden(m);
                cb.CheckedChanged += delegate { if (!_loading) UpdateHint(); };

                _rowsPanel.Controls.Add(cb);
                _rows.Add(cb);
                y += 30;
            }

            _autostart.Checked = Config.Autostart;
            _fill.Checked = Config.FillBareDisplays;
            _loading = false;
            UpdateHint();
        }

        private bool WantsNonPrimary()
        {
            foreach (CheckBox cb in _rows)
                if (cb.Checked && !((MonitorInfo)cb.Tag).IsPrimary) return true;
            return false;
        }

        private bool HidesNonPrimary()
        {
            foreach (CheckBox cb in _rows)
                if (!cb.Checked && !((MonitorInfo)cb.Tag).IsPrimary) return true;
            return false;
        }

        private void UpdateHint()
        {
            if (WantsNonPrimary() != Config.MultiMonTaskbars)
                _hint.Text = "Applying this changes a Windows setting, so Explorer has to restart once.";
            else if (WantsNonPrimary() && HidesNonPrimary())
                _hint.Text = Config.FillBareDisplays
                    ? "Windows keeps a hidden taskbar's strip reserved, so windows maximized there are stretched over it instead. They stop counting as maximized."
                    : "A hidden taskbar keeps its strip reserved, so maximized windows on that display stop just short of the edge. Windows will not release it.";
            else
                _hint.Text = "Runs in the tray and reapplies itself whenever Explorer restarts.";
        }

        private bool IsHidden(MonitorInfo m)
        {
            foreach (string key in _hidden)
                if (m.Matches(key)) return true;
            return false;
        }

        private void Save()
        {
            if (_rows.Count == 0) return;   // saving no displays would throw the picks away

            bool nonPrimaryWanted = WantsNonPrimary();

            _hidden.Clear();
            foreach (CheckBox cb in _rows)
                if (!cb.Checked) _hidden.Add(((MonitorInfo)cb.Tag).Key);
            Config.SaveHidden(_hidden);

            // With no secondary taskbar wanted at all, Windows can do it properly: turning the
            // setting off destroys those bars instead of leaving hidden ones reserving space.
            if (nonPrimaryWanted != Config.MultiMonTaskbars)
            {
                Config.MultiMonTaskbars = nonPrimaryWanted;
                string what = nonPrimaryWanted
                    ? "Windows only creates taskbars on the other displays after Explorer restarts."
                    : "Windows only removes the taskbars from the other displays after Explorer restarts.";
                DialogResult r = MessageBox.Show(this,
                    what + "\r\n\r\nAny File Explorer windows you have open will close."
                         + "\r\n\r\nRestart Explorer now?",
                    AppTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r == DialogResult.Yes)
                {
                    Config.RestartExplorer();
                    _delayed.Stop();
                    _delayed.Start();
                }
            }
            Reapply();
            UpdateHint();
        }

        private void Reapply()
        {
            _monitors = Monitors.All();
            Taskbars.Apply(_monitors, IsWanted);
            if (Config.FillBareDisplays) Fill.Apply(_monitors, IsHidden);
        }

        private bool IsWanted(MonitorInfo m)
        {
            return !IsHidden(m);
        }

        private void ReloadDisplays()
        {
            // Explorer restarts and display changes both leave a window where the monitors
            // enumerate as none at all. Drawing that would empty the list and leave it empty,
            // so wait it out instead.
            List<MonitorInfo> found = Monitors.All();
            if (found.Count == 0)
            {
                _delayed.Start();
                return;
            }

            _monitors = found;
            RebuildRows(false);
            Reapply();
        }

        private void OnDisplaysChanged(object sender, EventArgs e)
        {
            _delayed.Stop();
            _delayed.Start();
        }

        private void Identify()
        {
            var overlays = new List<Form>();
            foreach (MonitorInfo m in _monitors)
            {
                var f = new Form();
                f.FormBorderStyle = FormBorderStyle.None;
                f.StartPosition = FormStartPosition.Manual;
                f.ShowInTaskbar = false;
                f.TopMost = true;
                f.BackColor = Color.FromArgb(20, 20, 20);
                f.Size = new Size(280, 200);
                f.Location = new Point(
                    m.Bounds.Left + (m.Bounds.Width - f.Width) / 2,
                    m.Bounds.Top + (m.Bounds.Height - f.Height) / 2);

                var lbl = new Label();
                lbl.Dock = DockStyle.Fill;
                lbl.TextAlign = ContentAlignment.MiddleCenter;
                lbl.ForeColor = Color.White;
                lbl.Font = new Font("Segoe UI", 90f, FontStyle.Bold);
                lbl.Text = m.Number.ToString();
                f.Controls.Add(lbl);
                f.Show();
                overlays.Add(f);
            }

            var t = new Timer();
            t.Interval = 2500;
            t.Tick += delegate
            {
                t.Stop();
                foreach (Form f in overlays) f.Close();
            };
            t.Start();
        }

        private void ShowSettings()
        {
            _monitors = Monitors.All();
            _hidden = Config.LoadHidden();
            RebuildRows(true);
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void ExitApp()
        {
            _watchdog.Stop();
            Fill.RestoreAll();
            Taskbars.RestoreAll();
            _tray.Visible = false;
            Application.Exit();
        }

        protected override void SetVisibleCore(bool value)
        {
            if (_firstShow && _silent)
            {
                _firstShow = false;
                base.SetVisibleCore(false);
                return;
            }
            _firstShow = false;
            base.SetVisibleCore(value);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }
            SystemEvents.DisplaySettingsChanged -= OnDisplaysChanged;
            _tray.Visible = false;
            base.OnFormClosing(e);
        }

        protected override void WndProc(ref Message m)
        {
            // Explorer broadcasts this after it restarts and rebuilds its taskbars.
            if (m.Msg == _taskbarCreated)
            {
                _delayed.Stop();
                _delayed.Start();
            }
            base.WndProc(ref m);
        }
    }

    // The same .ico the build stamps onto the exe, so the window, the tray and the file in
    // Explorer are one piece of artwork instead of three that drift apart.
    internal static class Glyph
    {
        public static Icon Window()
        {
            using (Stream s = Open()) return new Icon(s);
        }

        public static Icon Tray()
        {
            using (Stream s = Open()) return new Icon(s, 16, 16);
        }

        private static Stream Open()
        {
            return Assembly.GetExecutingAssembly().GetManifestResourceStream("taskbarpick.ico");
        }
    }
}
