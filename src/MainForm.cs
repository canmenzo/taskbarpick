using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace TaskbarPick
{
    internal class MainForm : Form
    {
        private readonly bool _silent;
        private bool _firstShow = true;
        private bool _loading;

        private List<MonitorInfo> _monitors;
        private List<string> _hidden;

        private CheckedListBox _list;
        private CheckBox _autostart;
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
            Populate();

            _watchdog = new Timer();
            _watchdog.Interval = 2000;
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
            Text = "taskbarpick";
            Icon = Glyph.Make();
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(420, 250);
            Font = new Font("Segoe UI", 9f);

            var caption = new Label();
            caption.Text = "Show the taskbar on these displays:";
            caption.SetBounds(12, 12, 300, 18);

            _list = new CheckedListBox();
            _list.CheckOnClick = true;
            _list.IntegralHeight = false;
            _list.SetBounds(12, 34, 396, 108);
            // ItemCheck fires before the new state is stored, so refresh the hint after it settles.
            _list.ItemCheck += delegate
            {
                if (_loading || !IsHandleCreated) return;
                BeginInvoke((MethodInvoker)UpdateHint);
            };

            var identify = new Button();
            identify.Text = "Identify";
            identify.SetBounds(12, 150, 80, 26);
            identify.Click += delegate { Identify(); };

            _autostart = new CheckBox();
            _autostart.Text = "Start with Windows";
            _autostart.SetBounds(104, 152, 160, 22);
            _autostart.CheckedChanged += delegate
            {
                if (!_loading) Config.Autostart = _autostart.Checked;
            };

            _hint = new Label();
            _hint.SetBounds(12, 182, 396, 32);
            _hint.ForeColor = SystemColors.GrayText;

            var apply = new Button();
            apply.Text = "Apply";
            apply.SetBounds(246, 216, 80, 26);
            apply.Click += delegate { Save(); };

            var close = new Button();
            close.Text = "Close";
            close.SetBounds(332, 216, 76, 26);
            close.Click += delegate { Hide(); };

            AcceptButton = apply;
            CancelButton = close;
            Controls.AddRange(new Control[] { caption, _list, identify, _autostart, _hint, apply, close });
        }

        private void BuildTray()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Settings", null, delegate { ShowSettings(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit (restore all taskbars)", null, delegate { ExitApp(); });

            _tray = new NotifyIcon();
            _tray.Icon = Glyph.Make();
            _tray.Text = "taskbarpick";
            _tray.ContextMenuStrip = menu;
            _tray.DoubleClick += delegate { ShowSettings(); };
            _tray.Visible = true;
        }

        private void Populate()
        {
            _loading = true;
            _list.Items.Clear();
            foreach (MonitorInfo m in _monitors)
                _list.Items.Add(m.Describe(), !IsHidden(m));
            _autostart.Checked = Config.Autostart;
            _loading = false;
            UpdateHint();
        }

        private void UpdateHint()
        {
            bool nonPrimaryWanted = false;
            for (int i = 0; i < _monitors.Count && i < _list.Items.Count; i++)
                if (_list.GetItemChecked(i) && !_monitors[i].IsPrimary) nonPrimaryWanted = true;

            _hint.Text = nonPrimaryWanted && !Config.MultiMonTaskbars
                ? "Windows is set to keep the taskbar on the primary display only. Apply turns on multi-display taskbars, which needs Explorer restarted once."
                : "Runs in the tray and reapplies itself whenever Explorer restarts.";
        }

        private bool IsHidden(MonitorInfo m)
        {
            foreach (string key in _hidden)
                if (m.Matches(key)) return true;
            return false;
        }

        private void Save()
        {
            _hidden.Clear();
            bool nonPrimaryWanted = false;
            for (int i = 0; i < _monitors.Count; i++)
            {
                if (_list.GetItemChecked(i))
                {
                    if (!_monitors[i].IsPrimary) nonPrimaryWanted = true;
                }
                else _hidden.Add(_monitors[i].Key);
            }
            Config.SaveHidden(_hidden);

            if (nonPrimaryWanted && !Config.MultiMonTaskbars)
            {
                Config.MultiMonTaskbars = true;
                DialogResult r = MessageBox.Show(this,
                    "Windows only creates taskbars on the other displays after Explorer restarts.\r\n\r\nRestart Explorer now?",
                    "taskbarpick", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r == DialogResult.Yes)
                {
                    Config.RestartExplorer();
                    _delayed.Stop();
                    _delayed.Start();
                }
            }
            Reapply();
        }

        private void Reapply()
        {
            _monitors = Monitors.All();
            Taskbars.Apply(_monitors, IsWanted);
        }

        private bool IsWanted(MonitorInfo m)
        {
            return !IsHidden(m);
        }

        private void ReloadDisplays()
        {
            _monitors = Monitors.All();
            Populate();
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
            ReloadDisplays();
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void ExitApp()
        {
            _watchdog.Stop();
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

    internal static class Glyph
    {
        public static Icon Make()
        {
            using (var bmp = new Bitmap(32, 32))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Transparent);
                    using (var pen = new Pen(Color.FromArgb(240, 240, 240), 2f))
                    {
                        g.DrawRectangle(pen, 3, 6, 25, 18);
                        g.DrawLine(pen, 12, 27, 20, 27);
                    }
                    using (var brush = new SolidBrush(Color.FromArgb(0, 150, 255)))
                        g.FillRectangle(brush, 5, 19, 21, 4);
                }
                return Icon.FromHandle(bmp.GetHicon());
            }
        }
    }
}
