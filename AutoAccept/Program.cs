using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AutoAccept.Helpers;
using AutoAccept.Models;

namespace AutoAccept
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting auto-accept...");
            Console.WriteLine("Attempting to detect game...");

            // Resolve game path and info
            LeagueClientInfo info = null;
            while (info == null)
            {
                try
                {
                    var gamePath = LeagueTools.LocateGame();
                    info = LeagueTools.GetClientInfo(gamePath);

                    Console.WriteLine($"Found game at {gamePath}");
                    Console.WriteLine($"Detected LeagueClient running as {info.ProcessId}.");
                }
                catch (FileNotFoundException)
                {
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to locate game: " + ex.Message);
                    return;
                }

                Thread.Sleep(1000);
            }

            // Create LCUClient
            var client = new LCUClient(info.AppPort, info.AppPassword);
            client.OnConnected += () => Console.WriteLine("Connected to LCU!");

            // Register ready check callback
            client.OnReadyCheck += async () =>
            {
                // Wait for a short while before accepting
                await Task.Delay(100);
                await client.AcceptReadyCheck();
            };

            client.OnDisconnected += () => Console.WriteLine("Disconnected!?");

            // Connect to websocket
            await client.Connect();
        }
    }
}
