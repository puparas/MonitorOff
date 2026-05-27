using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace MonitorOff.Core
{
    internal sealed class MonitorManager : IDisposable
    {
        private readonly List<MonitorSession> _sessions = new List<MonitorSession>();

        public event EventHandler<MonitorSession> SessionStateChanged;

        public IReadOnlyList<MonitorSession> Sessions => _sessions;

        public void LoadMonitors()
        {
            UnsubscribeAll();
            _sessions.Clear();

            foreach (var screen in Screen.AllScreens.OrderBy(s => s.Bounds.X).ThenBy(s => s.Bounds.Y))
            {
                var session = new MonitorSession(screen);
                session.StateChanged += OnSessionStateChanged;
                _sessions.Add(session);
            }
        }

        public MonitorSession Find(string deviceName)
        {
            return _sessions.FirstOrDefault(s => s.DeviceName == deviceName);
        }

        public void TurnOnAll()
        {
            foreach (var session in _sessions)
                session.TurnOn();
        }

        public void Dispose()
        {
            UnsubscribeAll();

            foreach (var session in _sessions)
                session.Dispose();

            _sessions.Clear();
        }

        private void OnSessionStateChanged(object sender, EventArgs e)
        {
            SessionStateChanged?.Invoke(this, (MonitorSession)sender);
        }

        private void UnsubscribeAll()
        {
            foreach (var session in _sessions)
                session.StateChanged -= OnSessionStateChanged;
        }
    }
}
