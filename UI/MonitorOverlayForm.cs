using System;
using System.Drawing;
using System.Windows.Forms;
using MonitorOff.Core;

namespace MonitorOff.UI
{
    internal sealed class MonitorOverlayForm : Form
    {
        private readonly Action _ensurePanelsOnTop;
        private bool _clickThrough;

        public MonitorOverlayForm(Screen screen, string deviceName, Action ensurePanelsOnTop)
        {
            DeviceName = deviceName;
            _ensurePanelsOnTop = ensurePanelsOnTop;
            var bounds = screen.Bounds;

            StartPosition = FormStartPosition.Manual;
            Bounds = bounds;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            BackColor = Color.Black;
            Opacity = AppSettings.OverlayPreviewOpacity;
            TopMost = true;
            Text = deviceName;

            MouseDown += OnOverlayInteracted;
            GotFocus += OnOverlayInteracted;
        }

        public string DeviceName { get; }

        public void Configure(double opacity, bool clickThrough)
        {
            Opacity = opacity;
            _clickThrough = clickThrough;

            if (IsHandleCreated)
            {
                NativeMethods.SetClickThrough(Handle, clickThrough);
                RaisePanels();
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= NativeMethods.WsExNoActivate;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            NativeMethods.SetClickThrough(Handle, _clickThrough);
            RaisePanels();
        }

        private void OnOverlayInteracted(object sender, EventArgs e)
        {
            RaisePanels();
        }

        private void RaisePanels()
        {
            _ensurePanelsOnTop?.Invoke();
        }
    }
}
