using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace AutoAccept
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            // Prevent double-starts
            var currentProcess = Process.GetCurrentProcess();
            if (Process.GetProcessesByName(currentProcess.ProcessName).Any(p => p.Id != currentProcess.Id))
            {
                MessageBox.Show("AutoAccept already seems to be running.\nCheck the notification area in your taskbar.", "Error");
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new App());
        }
    }
}
