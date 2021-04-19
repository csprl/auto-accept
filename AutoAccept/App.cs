using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AutoAccept.Helpers;
using AutoAccept.Models;

namespace AutoAccept
{
    class App : ApplicationContext
    {
        private readonly NotifyIcon _trayIcon;

        public App()
        {
            // Create taskbar notification tray icon
            _trayIcon = new NotifyIcon()
            {
                ContextMenu = new ContextMenu(new MenuItem[]
                {
                    new MenuItem("Exit", Exit)
                }),
                Visible = true
            };

            // Create worker thread
            new Thread(new ThreadStart(Worker)).Start();
        }

        private async void Worker()
        {
            while (true)
            {
                _trayIcon.Icon = Properties.Resources.accept_red;
                _trayIcon.Text = "Waiting for game...";

                // Resolve game path and info
                LeagueClientInfo info;
                try
                {
                    var gamePath = LeagueTools.LocateGame();
                    info = LeagueTools.GetClientInfo(gamePath);
                }
                catch (FileNotFoundException)
                {
                    Thread.Sleep(5000);
                    continue;
                }
                catch
                {
                    // TODO: handle this
                    return;
                }

                _trayIcon.Text = $"Found LeagueClient ({info.ProcessId})";

                // Create LCUClient
                var client = new LCUClient(info.AppPort, info.AppPassword);
                client.OnConnected += () =>
                {
                    _trayIcon.Icon = Properties.Resources.accept;
                    _trayIcon.Text = "Ready";
                };

                // Register ready check callback
                client.OnReadyCheck += async () =>
                {
                    // Wait for a short while before accepting
                    await Task.Delay(100);

                    // Retry 3 times
                    for (var i = 0; i < 3; i++)
                    {
                        try
                        {
                            await client.AcceptReadyCheck();
                            break;
                        }
                        catch
                        {
                            // TODO: handle this
                        }
                    }
                };

                // Connect to websocket
                try
                {
                    await client.Connect();
                }
                catch
                {
                    // TODO: handle this
                }
            }
        }

        private void Exit(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;

            Environment.Exit(0);
        }
    }
}
