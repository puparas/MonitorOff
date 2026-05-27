using System;
using System.Management;
using System.Windows.Forms;

namespace MonitorOff.Core
{
    /// <summary>
    /// Hardware brightness via DDC/CI (dxva2) with WMI fallback for internal panels.
    /// </summary>
    internal sealed class BrightnessControl : IDisposable
    {
        private IntPtr _physicalMonitorHandle = IntPtr.Zero;
        private bool _disposed;

        public bool IsSupported { get; private set; }
        public bool UsesWmi { get; private set; }

        public BrightnessControl(Screen screen)
        {
            if (screen == null)
                throw new ArgumentNullException(nameof(screen));

            TryInitializeDdc(screen);
            if (!IsSupported)
                TryInitializeWmi(screen);

            if (IsSupported && !VerifyBrightnessWorks())
                IsSupported = false;
        }

        public bool TryGetBrightness(out uint minimum, out uint current, out uint maximum)
        {
            minimum = current = maximum = 0;

            if (!IsSupported)
                return false;

            if (UsesWmi)
                return TryGetWmiBrightness(out minimum, out current, out maximum);

            uint min = 0, cur = 0, max = 0;
            if (!NativeMethods.GetMonitorBrightness(_physicalMonitorHandle, ref min, ref cur, ref max))
                return false;

            minimum = min;
            current = cur;
            maximum = max;
            return true;
        }

        public bool TrySetBrightness(uint value)
        {
            if (!IsSupported)
                return false;

            if (UsesWmi)
                return TrySetWmiBrightness(value);

            return NativeMethods.SetMonitorBrightness(_physicalMonitorHandle, value);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            if (_physicalMonitorHandle != IntPtr.Zero)
            {
                NativeMethods.DestroyPhysicalMonitor(_physicalMonitorHandle);
                _physicalMonitorHandle = IntPtr.Zero;
            }

            _disposed = true;
        }

        private void TryInitializeDdc(Screen screen)
        {
            var bounds = screen.Bounds;
            var center = new NativeMethods.Point
            {
                X = bounds.Left + bounds.Width / 2,
                Y = bounds.Top + bounds.Height / 2
            };

            IntPtr hMonitor = NativeMethods.MonitorFromPoint(center, NativeMethods.MonitorDefaultToNearest);
            if (hMonitor == IntPtr.Zero)
                return;

            uint count = 0;
            if (!NativeMethods.GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, ref count) || count == 0)
                return;

            var monitors = new NativeMethods.PhysicalMonitor[count];
            if (!NativeMethods.GetPhysicalMonitorsFromHMONITOR(hMonitor, count, monitors))
                return;

            _physicalMonitorHandle = monitors[0].Handle;
            for (int i = 1; i < monitors.Length; i++)
                NativeMethods.DestroyPhysicalMonitor(monitors[i].Handle);

            uint capabilities = 0;
            uint colorTemps = 0;
            if (!NativeMethods.GetMonitorCapabilities(_physicalMonitorHandle, ref capabilities, ref colorTemps))
            {
                ReleaseHandle();
                return;
            }

            if ((capabilities & NativeMethods.McCapsBrightness) == 0)
            {
                ReleaseHandle();
                return;
            }

            uint min = 0, cur = 0, max = 0;
            if (!NativeMethods.GetMonitorBrightness(_physicalMonitorHandle, ref min, ref cur, ref max))
            {
                ReleaseHandle();
                return;
            }

            IsSupported = true;
        }

        private void TryInitializeWmi(Screen screen)
        {
            if (!screen.Primary)
                return;

            try
            {
                uint min, cur, max;
                if (QueryWmiBrightness(out min, out cur, out max))
                {
                    IsSupported = true;
                    UsesWmi = true;
                }
            }
            catch (ManagementException)
            {
                // WMI unavailable or access denied.
            }
        }

        private static bool QueryWmiBrightness(out uint minimum, out uint current, out uint maximum)
        {
            minimum = current = maximum = 0;

            using (var searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM WmiMonitorBrightness"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    if (!(bool)obj["Active"])
                        continue;

                    current = (uint)(byte)obj["CurrentBrightness"];
                    minimum = 0;
                    maximum = 100;
                    return true;
                }
            }

            return false;
        }

        private bool TryGetWmiBrightness(out uint minimum, out uint current, out uint maximum)
        {
            try
            {
                return QueryWmiBrightness(out minimum, out current, out maximum);
            }
            catch (ManagementException)
            {
                minimum = current = maximum = 0;
                return false;
            }
        }

        private bool TrySetWmiBrightness(uint value)
        {
            byte level = (byte)Math.Max(0, Math.Min(100, value));

            try
            {
                using (var searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM WmiMonitorBrightnessMethods"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var inParams = obj.GetMethodParameters("WmiSetBrightness");
                        inParams["Timeout"] = (byte)5;
                        inParams["Brightness"] = level;
                        obj.InvokeMethod("WmiSetBrightness", inParams, null);
                        return true;
                    }
                }
            }
            catch (ManagementException)
            {
                return false;
            }

            return false;
        }

        private bool VerifyBrightnessWorks()
        {
            try
            {
                if (!TryGetBrightness(out _, out uint current, out _))
                    return false;

                return TrySetBrightness(current);
            }
            catch
            {
                return false;
            }
        }

        private void ReleaseHandle()
        {
            if (_physicalMonitorHandle != IntPtr.Zero)
            {
                NativeMethods.DestroyPhysicalMonitor(_physicalMonitorHandle);
                _physicalMonitorHandle = IntPtr.Zero;
            }
        }
    }
}
