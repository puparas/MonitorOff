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
        private readonly PanelLayoutStore _layoutStore;
        private readonly Func<MonitorSession, bool> _isSessionHoveredAnywhere;
        private readonly Func<bool> _isAnyButtonHoveredAnywhere;
        private readonly ToolTip _toolTip = new ToolTip();
        private int _buttonSize = AppSettings.DefaultButtonSize;
        private bool _isDragging;
        private bool _dragMoved;
        private Point _dragStartCursor;
        private Point _dragStartWindow;
        private readonly ContextMenuStrip _sizeMenu = new ContextMenuStrip();

        public ControlPanelForm(
            Screen hostScreen,
            MonitorManager monitors,
            PanelLayoutStore layoutStore,
            Func<MonitorSession, bool> isSessionHoveredAnywhere,
            Func<bool> isAnyButtonHoveredAnywhere)
        {
            _hostScreen = hostScreen ?? throw new ArgumentNullException(nameof(hostScreen));
            _monitors = monitors ?? throw new ArgumentNullException(nameof(monitors));
            _layoutStore = layoutStore ?? throw new ArgumentNullException(nameof(layoutStore));
            _isSessionHoveredAnywhere = isSessionHoveredAnywhere ?? throw new ArgumentNullException(nameof(isSessionHoveredAnywhere));
            _isAnyButtonHoveredAnywhere = isAnyButtonHoveredAnywhere ?? throw new ArgumentNullException(nameof(isAnyButtonHoveredAnywhere));

            InitializeComponent();
            Icon = AppIcon.CreateTrayIcon();
            KeyPreview = true;
            KeyDown += OnKeyDown;
            BuildSizeMenu();
            ContextMenuStrip = _sizeMenu;
        }

        private void ControlPanelForm_Load(object sender, EventArgs e)
        {
            ApplySavedLayout();
            BuildMonitorButtons();
            AttachDragHandlers(this);
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
                x += _buttonSize + AppSettings.ButtonSpacing;
            }

            int width = Math.Max(_buttonSize, x > 0 ? x - AppSettings.ButtonSpacing : _buttonSize);
            ApplyPanelSize(width, _buttonSize);
            ClampToHostScreen();
            SaveLayout();
        }

        private Button CreateMonitorButton(MonitorSession session, int x)
        {
            var button = new Button
            {
                Location = new Point(x, 0),
                Name = SanitizeControlName(session.DeviceName),
                Size = new Size(_buttonSize, _buttonSize),
                TabStop = false,
                FlatStyle = FlatStyle.Flat,
                Font = CreateButtonFont(session.Label),
                Text = session.Label,
                Tag = session,
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false,
                UseCompatibleTextRendering = true,
                Padding = Padding.Empty
            };

            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            button.Click += MonitorButton_Click;
            button.MouseEnter += MonitorButton_MouseEnter;
            button.MouseLeave += MonitorButton_MouseLeave;
            AttachDragHandlers(button);

            ApplyButtonStyle(button, session);
            _toolTip.SetToolTip(button, session.Tooltip);

            return button;
        }

        private void MonitorButton_Click(object sender, EventArgs e)
        {
            if (_dragMoved)
            {
                _dragMoved = false;
                return;
            }

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

        private void AttachDragHandlers(Control control)
        {
            control.MouseDown += Drag_MouseDown;
            control.MouseMove += Drag_MouseMove;
            control.MouseUp += Drag_MouseUp;
        }

        private void Drag_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            var source = sender as Control;
            if (source == null)
                return;

            _dragMoved = false;
            _isDragging = true;
            _dragStartCursor = source.PointToScreen(e.Location);
            _dragStartWindow = Location;
            source.Capture = true;
        }

        private void Drag_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging)
                return;

            var source = sender as Control;
            if (source == null)
                return;

            Point cursor = source.PointToScreen(e.Location);
            int dx = cursor.X - _dragStartCursor.X;
            int dy = cursor.Y - _dragStartCursor.Y;

            if (Math.Abs(dx) > 1 || Math.Abs(dy) > 1)
                _dragMoved = true;

            Location = new Point(_dragStartWindow.X + dx, _dragStartWindow.Y + dy);
            ClampToHostScreen();
        }

        private void Drag_MouseUp(object sender, MouseEventArgs e)
        {
            if (!_isDragging)
                return;

            _isDragging = false;
            if (sender is Control source)
                source.Capture = false;

            ClampToHostScreen();
            SaveLayout();
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

        private void BuildSizeMenu()
        {
            var decrease = new ToolStripMenuItem("Уменьшить кнопки");
            decrease.Click += (_, __) => ChangeButtonSize(-2);

            var increase = new ToolStripMenuItem("Увеличить кнопки");
            increase.Click += (_, __) => ChangeButtonSize(2);

            _sizeMenu.Items.Add(decrease);
            _sizeMenu.Items.Add(increase);
        }

        private void ChangeButtonSize(int delta)
        {
            int newSize = Math.Max(AppSettings.MinButtonSize, Math.Min(AppSettings.MaxButtonSize, _buttonSize + delta));
            if (newSize == _buttonSize)
                return;

            _buttonSize = newSize;
            BuildMonitorButtons();
            RefreshAllSessions();
            KeepOnTop();
        }

        private void ApplySavedLayout()
        {
            PanelLayout layout = _layoutStore.GetLayout(
                _hostScreen.DeviceName,
                _hostScreen.WorkingArea,
                AppSettings.DefaultButtonSize,
                CalculatePanelWidth(AppSettings.DefaultButtonSize));

            _buttonSize = layout.ButtonSize;
            Location = layout.Location;
        }

        private int CalculatePanelWidth(int buttonSize)
        {
            int count = Math.Max(1, _monitors.Sessions.Count);
            int width = count * buttonSize + (count - 1) * AppSettings.ButtonSpacing;
            return Math.Max(buttonSize, width);
        }

        private void ClampToHostScreen()
        {
            Rectangle area = _hostScreen.WorkingArea;
            int maxX = Math.Max(area.Left, area.Right - Width);
            int maxY = Math.Max(area.Top, area.Bottom - Height);

            int x = Math.Max(area.Left, Math.Min(maxX, Location.X));
            int y = Math.Max(area.Top, Math.Min(maxY, Location.Y));
            Location = new Point(x, y);
        }

        private void SaveLayout()
        {
            _layoutStore.SaveLayout(_hostScreen.DeviceName, _hostScreen.WorkingArea, Location, Width, _buttonSize);
        }

        private void ApplyPanelSize(int width, int height)
        {
            AutoSize = false;
            var size = new Size(width, height);
            MinimumSize = size;
            MaximumSize = size;
            ClientSize = size;
        }

        private Font CreateButtonFont(string text)
        {
            float size = Math.Max(6f, _buttonSize * 0.38f);
            var font = new Font("Segoe UI", size, FontStyle.Bold, GraphicsUnit.Pixel);
            using (var bitmap = new Bitmap(1, 1))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                while (size > 6f)
                {
                    font.Dispose();
                    font = new Font("Segoe UI", size, FontStyle.Bold, GraphicsUnit.Pixel);
                    SizeF measured = graphics.MeasureString(text, font);
                    if (measured.Width <= _buttonSize - 4 && measured.Height <= _buttonSize - 4)
                        break;

                    size -= 0.5f;
                }
            }

            return font;
        }

        private static string SanitizeControlName(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName))
                return "Monitor";

            var chars = deviceName
                .Replace(@"\", "_")
                .Replace(".", "_")
                .Replace(":", "_")
                .ToCharArray();

            return new string(chars);
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

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _toolTip.Dispose();
            _sizeMenu.Dispose();
            base.OnFormClosed(e);
        }
    }
}
