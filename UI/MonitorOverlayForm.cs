using System;
using System.Drawing;
using System.Windows.Forms;
using MonitorOff.Core;

namespace MonitorOff.UI
{
    internal sealed class MonitorOverlayForm : Form
    {
        private bool _clickThrough;

        public MonitorOverlayForm(Screen screen, string deviceName)
        {
            DeviceName = deviceName;
            var bounds = screen.Bounds;

            StartPosition = FormStartPosition.Manual;
            Bounds = bounds;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            BackColor = Color.Black;
            Opacity = AppSettings.OverlayPreviewOpacity;
            TopMost = true;
            Text = deviceName;
        }

        public string DeviceName { get; }

        public void Configure(double opacity, bool clickThrough)
        {
            Opacity = opacity;
            _clickThrough = clickThrough;

            if (IsHandleCreated)
                NativeMethods.SetClickThrough(Handle, clickThrough);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            NativeMethods.SetClickThrough(Handle, _clickThrough);
        }
    }
}
