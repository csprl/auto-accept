using System;
using System.Diagnostics;
using System.IO;
using AutoAccept.Models;
using Microsoft.Win32;

namespace AutoAccept.Helpers
{
    static class LeagueTools
    {
        public static string LocateGame()
        {
            // Attempt to locate by running process
            foreach (var p in Process.GetProcessesByName("LeagueClient"))
            {
                var path = Path.GetDirectoryName(p.MainModule.FileName);
                if (GamePathIsValid(path)) return path;
            }

            // Attempt to locate by registry key
            /*using var softwareKey = Registry.LocalMachine.OpenSubKey("SOFTWARE");
            using var lolKey = softwareKey.OpenSubKey("WOW6432Node\\Riot Games\\League of Legends");
            var gamePath = lolKey.GetValue("Path").ToString();
            if (GamePathIsValid(gamePath)) return gamePath;*/

            throw new FileNotFoundException();
        }

        public static LeagueClientInfo GetClientInfo(string gamePath)
        {
            // Read file
            using var fileStream = File.Open(Path.Join(gamePath, "lockfile"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fileStream);
            var lockFile = reader.ReadToEnd();

            // Split by :
            var parts = lockFile.Split(":");
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
            if (!File.Exists(Path.Join(path, "LeagueClient.exe"))) return false;

            return true;
        }
    }
}
