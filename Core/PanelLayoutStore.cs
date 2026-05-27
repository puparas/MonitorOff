using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;

namespace MonitorOff.Core
{
    internal sealed class PanelLayoutStore
    {
        private const char Separator = '|';
        private readonly string _filePath;
        private readonly Dictionary<string, PanelLayout> _layouts = new Dictionary<string, PanelLayout>(StringComparer.OrdinalIgnoreCase);

        public PanelLayoutStore()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dir = Path.Combine(appData, "MonitorOff");
            Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, "panel-layouts.txt");
            Load();
        }

        public PanelLayout GetLayout(string monitorDeviceName, Rectangle workingArea, int defaultButtonSize, int panelWidth)
        {
            if (monitorDeviceName == null)
                throw new ArgumentNullException(nameof(monitorDeviceName));

            if (_layouts.TryGetValue(monitorDeviceName, out PanelLayout stored))
            {
                return new PanelLayout
                {
                    ButtonSize = ClampButtonSize(stored.ButtonSize),
                    Location = ClampLocation(stored.Location, workingArea, panelWidth, ClampButtonSize(stored.ButtonSize))
                };
            }

            int buttonSize = ClampButtonSize(defaultButtonSize);
            return new PanelLayout
            {
                ButtonSize = buttonSize,
                Location = new Point(
                    workingArea.Right - panelWidth - AppSettings.PanelMargin,
                    workingArea.Top + AppSettings.PanelMargin)
            };
        }

        public void SaveLayout(string monitorDeviceName, Rectangle workingArea, Point location, int panelWidth, int buttonSize)
        {
            if (monitorDeviceName == null)
                throw new ArgumentNullException(nameof(monitorDeviceName));

            int clampedSize = ClampButtonSize(buttonSize);
            Point clampedLocation = ClampLocation(location, workingArea, panelWidth, clampedSize);
            _layouts[monitorDeviceName] = new PanelLayout
            {
                ButtonSize = clampedSize,
                Location = clampedLocation
            };

            Persist();
        }

        private void Load()
        {
            _layouts.Clear();
            if (!File.Exists(_filePath))
                return;

            foreach (string line in File.ReadAllLines(_filePath))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] parts = line.Split(Separator);
                if (parts.Length != 4)
                    continue;

                if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int x))
                    continue;
                if (!int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int y))
                    continue;
                if (!int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int size))
                    continue;

                _layouts[parts[0]] = new PanelLayout
                {
                    Location = new Point(x, y),
                    ButtonSize = ClampButtonSize(size)
                };
            }
        }

        private void Persist()
        {
            var lines = new List<string>(_layouts.Count);
            foreach (var pair in _layouts)
            {
                lines.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}{4}{1}{4}{2}{4}{3}",
                    pair.Key,
                    pair.Value.Location.X,
                    pair.Value.Location.Y,
                    pair.Value.ButtonSize,
                    Separator));
            }

            File.WriteAllLines(_filePath, lines);
        }

        private static int ClampButtonSize(int value)
        {
            return Math.Max(AppSettings.MinButtonSize, Math.Min(AppSettings.MaxButtonSize, value));
        }

        private static Point ClampLocation(Point location, Rectangle area, int panelWidth, int panelHeight)
        {
            int minX = area.Left;
            int maxX = Math.Max(area.Left, area.Right - panelWidth);
            int minY = area.Top;
            int maxY = Math.Max(area.Top, area.Bottom - panelHeight);

            int x = Math.Max(minX, Math.Min(maxX, location.X));
            int y = Math.Max(minY, Math.Min(maxY, location.Y));
            return new Point(x, y);
        }
    }
}
