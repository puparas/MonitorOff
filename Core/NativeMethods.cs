using System;
using System.Runtime.InteropServices;

namespace MonitorOff.Core
{
    internal static class NativeMethods
    {
        public const uint MonitorDefaultToNearest = 2;
        public const uint McCapsBrightness = 0x00000002;

        [StructLayout(LayoutKind.Sequential)]
        public struct Point
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct PhysicalMonitor
        {
            public IntPtr Handle;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string Description;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromPoint(Point pt, uint dwFlags);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(
            IntPtr hMonitor,
            ref uint numberOfPhysicalMonitors);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool GetPhysicalMonitorsFromHMONITOR(
            IntPtr hMonitor,
            uint physicalMonitorArraySize,
            [Out] PhysicalMonitor[] physicalMonitorArray);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool DestroyPhysicalMonitor(IntPtr hPhysicalMonitor);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool GetMonitorCapabilities(
            IntPtr hPhysicalMonitor,
            ref uint monitorCapabilities,
            ref uint supportedColorTemperatures);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool GetMonitorBrightness(
            IntPtr hPhysicalMonitor,
            ref uint minimumBrightness,
            ref uint currentBrightness,
            ref uint maximumBrightness);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool SetMonitorBrightness(IntPtr hPhysicalMonitor, uint newBrightness);

        public const int GwlExstyle = -20;
        public const int WsExTransparent = 0x00000020;
        public const int WsExNoActivate = 0x08000000;

        public static readonly IntPtr HwndTop = new IntPtr(0);

        public const uint SwpNomove = 0x0002;
        public const uint SwpNosize = 0x0001;
        public const uint SwpNoActivate = 0x0010;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int x,
            int y,
            int cx,
            int cy,
            uint uFlags);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        public static void SetWindowAbove(IntPtr window, IntPtr belowWindow)
        {
            if (window == IntPtr.Zero || belowWindow == IntPtr.Zero)
                return;

            SetWindowPos(
                window,
                belowWindow,
                0,
                0,
                0,
                0,
                SwpNomove | SwpNosize | SwpNoActivate);
        }

        public static void SetClickThrough(IntPtr hwnd, bool enabled)
        {
            if (hwnd == IntPtr.Zero)
                return;

            int exStyle = IntPtr.Size == 8
                ? (int)GetWindowLongPtr64(hwnd, GwlExstyle)
                : GetWindowLong32(hwnd, GwlExstyle);

            if (enabled)
                exStyle |= WsExTransparent;
            else
                exStyle &= ~WsExTransparent;

            if (IntPtr.Size == 8)
                SetWindowLongPtr64(hwnd, GwlExstyle, new IntPtr(exStyle));
            else
                SetWindowLong32(hwnd, GwlExstyle, exStyle);
        }
    }
}
