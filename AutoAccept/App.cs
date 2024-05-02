using AutoAccept.Models;
using AutoAccept.Models.LCU.Chat;
using AutoAccept.Models.LCU.EndOfGame;
using AutoAccept.Models.LCU.Gameflow;
using AutoAccept.Models.LCU.Lobby;
using AutoAccept.Models.LCU.LobbyTeamBuilder;
using AutoAccept.Utils;
using Microsoft.Toolkit.Uwp.Notifications;

namespace AutoAccept;

internal class App : ApplicationContext, IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly LCUClient _lcuClient = new();

    private readonly Dictionary<string, string> _gameNameMap = new();
    private readonly HashSet<string> _lobbyMembers = [];
    private readonly List<TrackedPlayer> _trackedPlayers = [];

    private int _gameNumber = 1;

    public App()
    {
        // Create taskbar menu
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Exit", null, Exit);

        // Create taskbar notification icon
        _notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = contextMenu,
            Text = "AutoAccept",
            Icon = Properties.Resources.accept_red,
            Visible = true
        };

        // Register actions
        _lcuClient.OnConnected += OnConnected;
        _lcuClient.OnReadyCheck += OnReadyCheck;
        _lcuClient.OnLobbyUpdate += OnLobbyUpdate;
        _lcuClient.OnChampSelect += OnChampSelect;
        _lcuClient.OnGameStart += OnGameStart;
        _lcuClient.OnEndOfGameStats += OnEndOfGameStats;
        _lcuClient.OnChatParticipant += OnChatParticipant;

        // Create worker thread
        new Thread(Worker).Start();
    }

    public new void Dispose()
    {
        _lcuClient.Dispose();
        base.Dispose();
    }

    private void OnConnected()
    {
        SetState("Ready", Properties.Resources.accept);
    }

    private async void OnReadyCheck()
    {
        // Wait for a short while before accepting
        await Task.Delay(100);

        // Retry 3 times
        for (var i = 0; i < 3; i++)
        {
            try
            {
                await _lcuClient.AcceptReadyCheck();
                break;
            }
            catch
            {
                // TODO: handle this
            }
        }
    }

    private void OnLobbyUpdate(List<LobbyMember> lobbyMembers)
    {
        _lobbyMembers.Clear();
        lobbyMembers.ForEach(m => _lobbyMembers.Add(m.Puuid));
    }

    private void OnChampSelect(ChampSelectSession session)
    {
        // Lookup players on same team
        HandlePlayers(session.MyTeam.Where(m => !_lobbyMembers.Contains(m.Puuid)));
    }

    private void OnGameStart(Session session)
    {
        // Lookup players on enemy team
        if (session.GameData.TeamOne.Any(m => _lobbyMembers.Contains(m.Puuid)))
        {
            HandlePlayers(session.GameData.TeamTwo);
        }
        else if (session.GameData.TeamTwo.Any(m => _lobbyMembers.Contains(m.Puuid)))
        {
            HandlePlayers(session.GameData.TeamOne);
        }
    }

    private void OnEndOfGameStats(StatsBlock stats)
    {
        // Increment game number
        var gameNumber = _gameNumber++;

        // Make sure we had lobby status before starting
        if (_lobbyMembers.Count == 0)
        {
            return;
        }

        // Store players
        foreach (var team in stats.Teams)
        {
            foreach (var player in team.Players.Where(p => !p.IsLocalPlayer && !_lobbyMembers.Contains(p.Puuid)))
            {
                _trackedPlayers.Add(new TrackedPlayer
                {
                    Id = player.Puuid,
                    SameTeam = team.IsPlayerTeam,
                    Champion = player.ChampionName,
                    GameNumber = gameNumber
                });
            }
        }
    }

    private void OnChatParticipant(ChatParticipant participant)
    {
        // Store game names
        _gameNameMap[participant.Puuid] = participant.GameName;
    }

    private async void Worker()
    {
        while (true)
        {
            SetState("Looking for League...", Properties.Resources.accept_red);

            // Resolve client path and info
            LeagueClientInfo clientInfo;
            try
            {
                clientInfo = await LeagueUtils.GetClientInfo(LeagueUtils.GetGamePath());
            }
            catch (IOException)
            {
                await Task.Delay(5000);
                continue;
            }
            catch
            {
                // TODO: handle this
                return;
            }

            SetState($"Found LeagueClient ({clientInfo.ProcessId})", Properties.Resources.accept_yellow);

            // Set LCU endpoint
            _lcuClient.SetEndpoint(clientInfo.Port, clientInfo.Password);

            // Connect to LCU
            try
            {
                await _lcuClient.Connect();
            }
            catch
            {
                // TODO: handle this
            }
        }
    }

    private void HandlePlayers(IEnumerable<IPlayerInfo> players)
    {
        var builder = new ToastContentBuilder();
        var show = false;

        foreach (var player in players)
        {
            var info = _trackedPlayers.LastOrDefault(s => s.Id == player.Puuid);
            if (info != null && _gameNameMap.TryGetValue(player.Puuid, out var gameName))
            {
                var gamesAgo = _gameNumber - info.GameNumber;
                var championString = player.ChampionId != null && DDragon.ChampionNames.TryGetValue(player.ChampionId.Value, out var championName) ? $" ({championName})" : "";

                builder.AddText($"{gameName}{championString} played {info.Champion} on {(info.SameTeam ? "your" : "enemy")} team {gamesAgo} {(gamesAgo > 1 ? "games" : "game")} ago");
                show = true;
            }
        }

        if (show)
        {
            builder.Show(toast => toast.ExpirationTime = DateTime.Now.AddMinutes(5));
        }
    }

    private void SetState(string state, Icon? icon = null)
    {
        _notifyIcon.Text = state;

        if (icon != null)
        {
            _notifyIcon.Icon = icon;
        }
    }

    private void Exit(object? sender, EventArgs e)
    {
        // Hide before exit to prevent icon from sticking until mouseover
        _notifyIcon.Visible = false;

        Application.Exit();
    }
}
