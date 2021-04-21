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

        private LCUClient _client;
        private bool _enabled = true;

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
            _trayIcon.DoubleClick += ToggleState;

            // Create worker thread
            new Thread(new ThreadStart(Worker)).Start();
        }

        private void ToggleState(object sender, EventArgs e)
        {
            if (_client?.IsConnected ?? false)
            {
                _enabled = !_enabled;
                UpdateIcon();
            }
        }

        private void UpdateIcon()
        {
            _trayIcon.Icon = _enabled ? Properties.Resources.accept : Properties.Resources.accept_yellow;
            _trayIcon.Text = _enabled ? "Ready" : "Paused";
        }

        private async void Worker()
        {
            while (true)
            {
                _trayIcon.Icon = Properties.Resources.accept_red;
                _trayIcon.Text = "Looking for League...";

                // Resolve game path and info
                LeagueClientInfo info;
                try
                {
                    var gamePath = LeagueTools.LocateGame();
                    info = LeagueTools.GetClientInfo(gamePath);
                }
                catch (FileNotFoundException)
                {
                    await Task.Delay(5000);
                    continue;
                }
                catch
                {
                    // TODO: handle this
                    return;
                }

                _trayIcon.Text = $"Found LeagueClient ({info.ProcessId})";

                // Create LCUClient
                _client = new LCUClient(info.AppPort, info.AppPassword);
                _client.OnConnected += () =>
                {
                    UpdateIcon();
                };

                // Register ready check callback
                _client.OnReadyCheck += async () =>
                {
                    if (!_enabled)
                    {
                        return;
                    }

                    // Wait for a short while before accepting
                    await Task.Delay(100);

                    // Retry 3 times
                    for (var i = 0; i < 3; i++)
                    {
                        try
                        {
                            await _client.AcceptReadyCheck();
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
                    await _client.Connect();
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
