using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MonitorOff.Core
{
    internal static class AppIcon
    {
        private static Icon _cached;

        public static Icon Get()
        {
            if (_cached != null)
                return _cached;

            string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MonitorOff.ico");
            if (File.Exists(icoPath))
            {
                _cached = new Icon(icoPath);
                return _cached;
            }

            Icon fromExe = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            _cached = fromExe ?? SystemIcons.Application;
            return _cached;
        }

        public static Icon CreateTrayIcon()
        {
            return (Icon)Get().Clone();
        }
    }
}
