using System.Diagnostics;

namespace AutoAccept;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    public static void Main()
    {
        // Prevent double-starts
        var currentProcess = Process.GetCurrentProcess();
        if (Process.GetProcessesByName(currentProcess.ProcessName).Any(p => p.Id != currentProcess.Id))
        {
            MessageBox.Show("AutoAccept already seems to be running.\nCheck the notification area in your taskbar.", "Error");
            return;
        }

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();

        Application.Run(new App());
    }
}
