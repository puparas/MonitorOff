using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MonitorOff
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

        }

        private void Form1_Load(object sender, EventArgs e)
        {
                int screenWidth = Screen.PrimaryScreen.Bounds.Size.Width;
                WindowState = FormWindowState.Normal;
                Screen[] monitors = Screen.AllScreens;
                List<Button> buttons = new List<Button>();
                for (int i = 0; i < monitors.Length; i++)
                {
                var m = monitors[i];
                string monitorNum = m.DeviceName.Replace("\\\\.\\DISPLAY", "");
                Button newButton = new Button();
                newButton.Location = new Point(35 * i, 0);
                newButton.Name = m.DeviceName;
                newButton.Size = new Size(35, 35);
                newButton.TabIndex = 0;
                newButton.TabStop = false;
                newButton.Tag = false;
                newButton.Font =  new Font("Microsoft Sans Serif", 10F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(204)));
                newButton.BackColor = SystemColors.ActiveCaption;
                newButton.FlatStyle = FlatStyle.Popup;
                newButton.Text = $"M{monitorNum ?? "7"}";
                newButton.UseVisualStyleBackColor = true;
                newButton.Click += new EventHandler(MonitorBtn_Click);
                newButton.MouseEnter += new EventHandler(MonitorBtn_MouseEnter);
                newButton.MouseLeave += new EventHandler(MonitorBtn_MouseLeave);
                Controls.Add(newButton);
            }
            int formWidth = Width;
            Location = new Point(screenWidth - (formWidth + 20), 10);
        }
        private void MonitorBtn_Click(object sender, EventArgs e)
        {
            
            var bCtrl = (Control)sender;
            string name = bCtrl.Name;
            
            var f = FindFormByName(name);
            if (f != null && f.Opacity == 0.7D)
            {
                f.Opacity = 1D;
                bCtrl.Tag = true;
                bCtrl.BackColor = SystemColors.Info;
            }
            else if(f != null && f.Opacity == 1)
            {
                f.Close();
                bCtrl.BackColor = SystemColors.ActiveCaption;
                bCtrl.Tag = false;
            }
            else
            {
                MakeOverlay(name, true).Show();
                bCtrl.Tag = true;
                TopMost = true;
            }
            
        }
        private void MonitorBtn_MouseEnter(object sender, EventArgs e)
        {
            var bCtrl = (Control)sender;
            Opacity = 1;
            if (!(bool)bCtrl.Tag)
            {
                string name = bCtrl.Name;
                MakeOverlay(name).Show();
                TopMost = true;
            }
        }

        private void MonitorBtn_MouseLeave(object sender, EventArgs e)
        {
            var bCtrl = (Control)sender;
            Opacity = 0.6D;
            if (!(bool)bCtrl.Tag)
            {
                string name = bCtrl.Name;
                var f = FindFormByName(name);
                if (f != null)
                {
                    FindFormByName(name).Close();
                }
            }
        }
        private Form MakeOverlay(string monitorName, bool full = false)
        {
            Form overlay = new Form();
            Screen[] monitors = Screen.AllScreens;
            Rectangle bounds = monitors.First(m => m.DeviceName == monitorName).Bounds;
            overlay.SetBounds(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            overlay.StartPosition = FormStartPosition.Manual;
            overlay.FormBorderStyle = FormBorderStyle.None;
            overlay.BackColor = SystemColors.ActiveCaptionText;
            overlay.Opacity = full ? 1D : 0.7D;
            overlay.Text = monitorName;
            overlay.ActiveControl = null;
            overlay.GotFocus += new EventHandler((i, o)=> { Focus(); });
            overlay.TopMost = true;
            return overlay;
        }
        
        private Form FindFormByName(string MonitorName)
        {
            for (int i = Application.OpenForms.Count - 1; i >= 0; i--)
            {
                if (Application.OpenForms[i].Text == MonitorName)
                    return Application.OpenForms[i];
            }
            return null;
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }
    }
}
