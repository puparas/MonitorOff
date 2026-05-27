using System;
using System.Windows.Forms;
using MonitorOff.App;

namespace MonitorOff
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MonitorOffAppContext());
        }
    }
}
