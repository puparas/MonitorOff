using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace MonitorOff.Core
{
    internal sealed class MonitorManager : IDisposable
    {
        private readonly List<MonitorSession> _sessions = new List<MonitorSession>();
        private Action _ensurePanelsOnTop;

        public event EventHandler<MonitorSession> SessionStateChanged;

        public IReadOnlyList<MonitorSession> Sessions => _sessions;

        public void SetEnsurePanelsOnTopCallback(Action callback)
        {
            _ensurePanelsOnTop = callback;
        }

        public void LoadMonitors()
        {
            UnsubscribeAll();
            _sessions.Clear();

            var screens = Screen.AllScreens.OrderBy(s => s.Bounds.X).ThenBy(s => s.Bounds.Y).ToList();
            for (int i = 0; i < screens.Count; i++)
            {
                var session = new MonitorSession(screens[i], i + 1, _ensurePanelsOnTop);
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

        public void TurnOffAll()
        {
            foreach (var session in _sessions)
                session.TurnOff();
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
