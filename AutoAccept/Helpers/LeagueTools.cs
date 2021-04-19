using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AutoAccept.Models;

namespace AutoAccept.Helpers
{
    static class LeagueTools
    {
        public static string LocateGame()
        {
            // Locate from running processes
            var path = Process.GetProcessesByName("LeagueClient").Select(p => Path.GetDirectoryName(p.MainModule.FileName)).Where(p => GamePathIsValid(p)).FirstOrDefault();
            if (string.IsNullOrEmpty(path))
            {
                throw new FileNotFoundException();
            }

            return path;
        }

        public static LeagueClientInfo GetClientInfo(string gamePath)
        {
            // Read file
            using var fileStream = File.Open(Path.Combine(gamePath, "lockfile"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fileStream);
            var lockFile = reader.ReadToEnd();

            // Split by :
            var parts = lockFile.Split(':');
            if (parts.Length != 5) throw new Exception("Unknown lockfile format");

            // Parse content
            return new LeagueClientInfo
            {
                Name = parts[0],
                ProcessId = int.Parse(parts[1]),
                AppPort = int.Parse(parts[2]),
                AppPassword = parts[3],
                AppProtocol = parts[4]
            };
        }

        private static bool GamePathIsValid(string path)
        {
            // Null/empty check
            if (string.IsNullOrEmpty(path)) return false;

            // Make sure path exists
            if (!Directory.Exists(path)) return false;

            // Check for common League files
            if (!File.Exists(Path.Combine(path, "LeagueClient.exe"))) return false;

            return true;
        }
    }
}
