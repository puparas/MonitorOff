using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using MonitorOff.UI;

namespace MonitorOff.Core
{
    internal enum MonitorPowerState
    {
        On,
        Preview,
        Off
    }

    internal sealed class MonitorSession : IDisposable
    {
        private readonly BrightnessControl _brightness;
        private uint? _savedBrightness;
        private MonitorOverlayForm _overlay;

        public MonitorSession(Screen screen)
        {
            Screen = screen ?? throw new ArgumentNullException(nameof(screen));
            DeviceName = screen.DeviceName;
            Label = BuildLabel(screen.DeviceName);
            _brightness = new BrightnessControl(screen);
            SupportsHardwareBrightness = _brightness.IsSupported;
        }

        public Screen Screen { get; }
        public string DeviceName { get; }
        public string Label { get; }
        public bool SupportsHardwareBrightness { get; }
        public MonitorPowerState State { get; private set; } = MonitorPowerState.On;

        public event EventHandler StateChanged;

        public string Tooltip =>
            SupportsHardwareBrightness
                ? $"{Label}: яркость + затемнение"
                : $"{Label}: только затемнение (DDC/CI недоступен)";

        public void ShowPreview()
        {
            if (State == MonitorPowerState.Off)
                return;

            EnsureOverlay(AppSettings.OverlayPreviewOpacity, clickThrough: true);
            SetState(MonitorPowerState.Preview);
        }

        public void HidePreview()
        {
            if (State != MonitorPowerState.Preview)
                return;

            CloseOverlay();
            SetState(MonitorPowerState.On);
        }

        public void TurnOff()
        {
            if (State == MonitorPowerState.Off)
                return;

            CloseOverlay();

            // Оверлей всегда — даже если DDC/WMI не сработали.
            EnsureOverlay(AppSettings.OverlayFullOpacity, clickThrough: false);
            SetState(MonitorPowerState.Off);

            TryDimHardware();
        }

        public void TurnOn()
        {
            if (State == MonitorPowerState.On)
                return;

            TryRestoreHardware();
            CloseOverlay();
            SetState(MonitorPowerState.On);
        }

        public void Toggle()
        {
            if (State == MonitorPowerState.Off)
                TurnOn();
            else
                TurnOff();
        }

        public void Dispose()
        {
            TurnOn();
            _brightness?.Dispose();
        }

        private void TryDimHardware()
        {
            if (!SupportsHardwareBrightness)
                return;

            try
            {
                if (_brightness.TryGetBrightness(out _, out uint current, out _))
                    _savedBrightness = current;

                if (_brightness.TryGetBrightness(out uint minimum, out _, out _))
                    _brightness.TrySetBrightness(minimum);
            }
            catch
            {
                // Не блокируем оверлей при ошибке аппаратной яркости.
            }
        }

        private void TryRestoreHardware()
        {
            if (!_savedBrightness.HasValue)
                return;

            try
            {
                _brightness.TrySetBrightness(_savedBrightness.Value);
            }
            catch
            {
                // ignore
            }
            finally
            {
                _savedBrightness = null;
            }
        }

        private static string BuildLabel(string deviceName)
        {
            var match = Regex.Match(deviceName ?? "", @"DISPLAY(\d+)", RegexOptions.IgnoreCase);
            return match.Success ? $"M{match.Groups[1].Value}" : "M?";
        }

        private void EnsureOverlay(double opacity, bool clickThrough)
        {
            if (_overlay == null || _overlay.IsDisposed)
            {
                _overlay = new MonitorOverlayForm(Screen, DeviceName);
                _overlay.Show();
            }

            _overlay.Configure(opacity, clickThrough);
        }

        private void CloseOverlay()
        {
            if (_overlay == null || _overlay.IsDisposed)
                return;

            _overlay.Close();
            _overlay.Dispose();
            _overlay = null;
        }

        private void SetState(MonitorPowerState state)
        {
            State = state;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
