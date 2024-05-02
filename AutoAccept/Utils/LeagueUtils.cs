using AutoAccept.Models;
using System.Diagnostics;
using System.Text;

namespace AutoAccept.Utils;

internal static class LeagueUtils
{
    public static string GetGamePath()
    {
        foreach (var p in Process.GetProcessesByName("LeagueClient"))
        {
            using var process = p;

            // Open handle with limited access (Vanguard)
            var processHandle = Interop.OpenProcess(Interop.ProcessAccessFlags.QueryLimitedInformation, false, (uint)process.Id);
            if (processHandle == IntPtr.Zero)
            {
                continue;
            }

            try
            {
                // Get full process name
                var capacity = 1024u;
                var processName = new StringBuilder((int)capacity);
                if (!Interop.QueryFullProcessImageName(processHandle, 0, processName, ref capacity))
                {
                    continue;
                }

                // Check for common League files
                var gamePath = Path.GetDirectoryName(processName.ToString());
                if (!string.IsNullOrEmpty(gamePath) && File.Exists(Path.Combine(gamePath, "LeagueClient.exe")))
                {
                    return gamePath;
                }
            }
            finally
            {
                // Close handle
                Interop.CloseHandle(processHandle);
            }
        }

        throw new FileNotFoundException();
    }

    public static async Task<LeagueClientInfo> GetClientInfo(string gamePath)
    {
        // Read file
        await using var fileStream = File.Open(Path.Combine(gamePath, "lockfile"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(fileStream);
        var lockFile = await reader.ReadToEndAsync();

        // Split by :
        var parts = lockFile.Split(':');
        if (parts.Length != 5)
        {
            throw new FileFormatException("Unknown lockfile format.");
        }

        // Parse content
        return new LeagueClientInfo
        {
            Name = parts[0],
            ProcessId = int.Parse(parts[1]),
            Port = int.Parse(parts[2]),
            Password = parts[3],
            Protocol = parts[4]
        };
    }
}
