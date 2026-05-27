using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;
using MonitorOff.Core;
using MonitorOff.UI;

namespace MonitorOff.App
{
    internal sealed class MonitorOffAppContext : ApplicationContext
    {
        private readonly MonitorManager _monitors = new MonitorManager();
        private readonly List<ControlPanelForm> _panels = new List<ControlPanelForm>();
        private TrayIconService _tray;

        public MonitorOffAppContext()
        {
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
            _monitors.SessionStateChanged += OnSessionStateChanged;
            Initialize();
        }

        private void Initialize()
        {
            _monitors.LoadMonitors();
            CreatePanels();
            _tray = new TrayIconService(_monitors, SystemIcons.Application, ExitApplication);
        }

        private void CreatePanels()
        {
            DisposePanels();

            foreach (var screen in Screen.AllScreens.OrderBy(s => s.Bounds.X).ThenBy(s => s.Bounds.Y))
            {
                var panel = new ControlPanelForm(screen, _monitors, IsSessionHoveredAnywhere, IsAnyButtonHoveredAnywhere);
                panel.Show();
                _panels.Add(panel);
            }
        }

        private bool IsSessionHoveredAnywhere(MonitorSession session)
        {
            foreach (var panel in _panels)
            {
                if (panel.IsMouseOverButton(session))
                    return true;
            }

            return false;
        }

        private bool IsAnyButtonHoveredAnywhere()
        {
            foreach (var panel in _panels)
            {
                if (panel.IsMouseOverAnyButton())
                    return true;
            }

            return false;
        }

        private void OnSessionStateChanged(object sender, MonitorSession session)
        {
            SyncUi(session);
        }

        private void SyncUi(MonitorSession session)
        {
            foreach (var panel in _panels)
            {
                if (session == null)
                    panel.RefreshAllSessions();
                else
                    panel.RefreshSession(session);

                panel.KeepOnTop();
            }

            _tray?.RefreshMenu();
        }

        private void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            if (_panels.Count > 0 && _panels[0].InvokeRequired)
            {
                _panels[0].BeginInvoke(new Action(ApplyDisplaySettingsChange));
                return;
            }

            ApplyDisplaySettingsChange();
        }

        private void ApplyDisplaySettingsChange()
        {
            _monitors.LoadMonitors();
            CreatePanels();
            SyncUi(null);
        }

        private void DisposePanels()
        {
            foreach (var panel in _panels)
            {
                panel.Close();
                panel.Dispose();
            }

            _panels.Clear();
        }

        private void ExitApplication()
        {
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            _monitors.SessionStateChanged -= OnSessionStateChanged;

            _tray?.Dispose();
            _tray = null;

            DisposePanels();
            _monitors.Dispose();

            ExitThread();
        }
    }
}
