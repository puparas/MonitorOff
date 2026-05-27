using System;
using System.Drawing;
using System.Windows.Forms;
using MonitorOff.Core;

namespace MonitorOff.App
{
    internal sealed class TrayIconService : IDisposable
    {
        private readonly MonitorManager _monitors;
        private readonly Action _onExit;
        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _menu;

        public TrayIconService(MonitorManager monitors, Icon icon, Action onExit)
        {
            _monitors = monitors ?? throw new ArgumentNullException(nameof(monitors));
            _onExit = onExit ?? throw new ArgumentNullException(nameof(onExit));

            _menu = new ContextMenuStrip();
            _notifyIcon = new NotifyIcon
            {
                Icon = icon ?? SystemIcons.Application,
                Text = "MonitorOff",
                Visible = true,
                ContextMenuStrip = _menu
            };

            _notifyIcon.DoubleClick += (_, __) => _monitors.TurnOnAll();

            BuildMenu();
        }

        public void RefreshMenu()
        {
            BuildMenu();
        }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _menu.Dispose();
        }

        private void BuildMenu()
        {
            _menu.Items.Clear();

            foreach (var session in _monitors.Sessions)
            {
                var item = new ToolStripMenuItem
                {
                    Text = GetMenuText(session),
                    Checked = session.State == MonitorPowerState.Off,
                    CheckOnClick = false
                };

                var captured = session;
                item.Click += (_, __) => captured.Toggle();
                _menu.Items.Add(item);
            }

            _menu.Items.Add(new ToolStripSeparator());

            var turnOnAll = new ToolStripMenuItem("Включить все мониторы");
            turnOnAll.Click += (_, __) => _monitors.TurnOnAll();
            _menu.Items.Add(turnOnAll);

            _menu.Items.Add(new ToolStripSeparator());

            var exit = new ToolStripMenuItem("Выход");
            exit.Click += (_, __) => _onExit();
            _menu.Items.Add(exit);
        }

        private static string GetMenuText(MonitorSession session)
        {
            string status = session.State == MonitorPowerState.Off ? "выкл." : "вкл.";
            return $"{session.Label} — {status}";
        }
    }
}
