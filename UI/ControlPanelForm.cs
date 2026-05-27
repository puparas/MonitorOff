using System;
using System.Drawing;
using System.Windows.Forms;
using MonitorOff.Core;

namespace MonitorOff.UI
{
    internal sealed partial class ControlPanelForm : Form
    {
        private readonly Screen _hostScreen;
        private readonly MonitorManager _monitors;
        private readonly Func<MonitorSession, bool> _isSessionHoveredAnywhere;
        private readonly Func<bool> _isAnyButtonHoveredAnywhere;
        private readonly ToolTip _toolTip = new ToolTip();

        public ControlPanelForm(
            Screen hostScreen,
            MonitorManager monitors,
            Func<MonitorSession, bool> isSessionHoveredAnywhere,
            Func<bool> isAnyButtonHoveredAnywhere)
        {
            _hostScreen = hostScreen ?? throw new ArgumentNullException(nameof(hostScreen));
            _monitors = monitors ?? throw new ArgumentNullException(nameof(monitors));
            _isSessionHoveredAnywhere = isSessionHoveredAnywhere ?? throw new ArgumentNullException(nameof(isSessionHoveredAnywhere));
            _isAnyButtonHoveredAnywhere = isAnyButtonHoveredAnywhere ?? throw new ArgumentNullException(nameof(isAnyButtonHoveredAnywhere));

            InitializeComponent();
            KeyPreview = true;
            KeyDown += OnKeyDown;
        }

        private void ControlPanelForm_Load(object sender, EventArgs e)
        {
            BuildMonitorButtons();
            PositionOnHostScreen();
        }

        public void RefreshSession(MonitorSession session)
        {
            if (session == null)
            {
                foreach (Control control in Controls)
                {
                    if (control is Button button && button.Tag is MonitorSession s)
                        ApplyButtonStyle(button, s);
                }

                return;
            }

            foreach (Control control in Controls)
            {
                if (control is Button button && button.Tag is MonitorSession s && s.DeviceName == session.DeviceName)
                    ApplyButtonStyle(button, s);
            }
        }

        public void RefreshAllSessions()
        {
            RefreshSession(null);
        }

        public void KeepOnTop()
        {
            TopMost = true;
            BringToFront();
        }

        private void BuildMonitorButtons()
        {
            Controls.Clear();
            int x = 0;

            foreach (var session in _monitors.Sessions)
            {
                var button = CreateMonitorButton(session, x);
                Controls.Add(button);
                x += AppSettings.ButtonSize + AppSettings.ButtonSpacing;
            }

            int width = Math.Max(AppSettings.ButtonSize, x > 0 ? x - AppSettings.ButtonSpacing : AppSettings.ButtonSize);
            ClientSize = new Size(width, AppSettings.ButtonSize);
        }

        private Button CreateMonitorButton(MonitorSession session, int x)
        {
            var button = new Button
            {
                Location = new Point(x, 0),
                Name = session.DeviceName,
                Size = new Size(AppSettings.ButtonSize, AppSettings.ButtonSize),
                TabStop = false,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Text = session.Label,
                Tag = session,
                UseVisualStyleBackColor = false
            };

            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            button.Click += MonitorButton_Click;
            button.MouseEnter += MonitorButton_MouseEnter;
            button.MouseLeave += MonitorButton_MouseLeave;

            ApplyButtonStyle(button, session);
            _toolTip.SetToolTip(button, session.Tooltip);

            return button;
        }

        private void PositionOnHostScreen()
        {
            var area = _hostScreen.WorkingArea;
            Location = new Point(
                area.Right - Width - AppSettings.PanelMargin,
                area.Top + AppSettings.PanelMargin);
        }

        private void MonitorButton_Click(object sender, EventArgs e)
        {
            var button = (Button)sender;
            var session = (MonitorSession)button.Tag;
            session.Toggle();
            KeepOnTop();
        }

        private void MonitorButton_MouseEnter(object sender, EventArgs e)
        {
            var button = (Button)sender;
            var session = (MonitorSession)button.Tag;

            Opacity = AppSettings.PanelActiveOpacity;

            if (session.State != MonitorPowerState.Off)
                session.ShowPreview();

            ApplyButtonStyle(button, session);
            KeepOnTop();
        }

        private void MonitorButton_MouseLeave(object sender, EventArgs e)
        {
            var button = (Button)sender;
            var session = (MonitorSession)button.Tag;

            BeginInvoke(new Action(() =>
            {
                if (IsDisposed || session == null)
                    return;

                if (!_isSessionHoveredAnywhere(session))
                    session.HidePreview();

                if (!_isAnyButtonHoveredAnywhere())
                    Opacity = AppSettings.PanelIdleOpacity;

                if (button.Tag is MonitorSession s)
                    ApplyButtonStyle(button, s);
            }));
        }

        internal bool IsMouseOverButton(MonitorSession session)
        {
            foreach (Control control in Controls)
            {
                if (!(control is Button button) || button.Tag != session)
                    continue;

                var point = button.PointToClient(Cursor.Position);
                if (button.ClientRectangle.Contains(point))
                    return true;
            }

            return false;
        }

        internal bool IsMouseOverAnyButton()
        {
            foreach (Control control in Controls)
            {
                if (!(control is Button button))
                    continue;

                var point = button.PointToClient(Cursor.Position);
                if (button.ClientRectangle.Contains(point))
                    return true;
            }

            return false;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Escape)
                return;

            _monitors.TurnOnAll();
            e.Handled = true;
        }

        private static void ApplyButtonStyle(Button button, MonitorSession session)
        {
            switch (session.State)
            {
                case MonitorPowerState.Off:
                    button.BackColor = Color.FromArgb(180, 60, 60);
                    button.ForeColor = Color.White;
                    break;
                case MonitorPowerState.Preview:
                    button.BackColor = Color.FromArgb(90, 90, 100);
                    button.ForeColor = Color.White;
                    break;
                default:
                    button.BackColor = session.SupportsHardwareBrightness
                        ? Color.FromArgb(55, 95, 145)
                        : Color.FromArgb(70, 70, 75);
                    button.ForeColor = Color.White;
                    break;
            }
        }
    }
}
