using AutoAccept.Models.LCU.Chat;
using AutoAccept.Models.LCU.EndOfGame;
using AutoAccept.Models.LCU.Gameflow;
using AutoAccept.Models.LCU.Lobby;
using AutoAccept.Models.LCU.LobbyTeamBuilder;
using System.Net;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;

namespace AutoAccept.Utils;

internal class LCUClient : ClientBase, IDisposable
{
    private const string Username = "riot";
    private const int BufferSize = 1024 * 16;

    private readonly ClientWebSocket _ws = new();
    private Uri _wsUrl = new("wss://127.0.0.1");

    public Action? OnConnected;

    public Action<List<LobbyMember>>? OnLobbyUpdate;
    public Action? OnReadyCheck;
    public Action<ChampSelectSession>? OnChampSelect;
    public Action<Session>? OnGameStart;
    public Action<StatsBlock>? OnEndOfGameStats;
    public Action<ChatParticipant>? OnChatParticipant;

    public LCUClient()
    {
        // Configure WebSocket client
        _ws.Options.RemoteCertificateValidationCallback = (_, _, _, _) => true;
        _ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
        _ws.Options.AddSubProtocol("wamp");
    }

    public new void Dispose()
    {
        _ws.Dispose();
        base.Dispose();
    }

    public void SetEndpoint(int port, string password)
    {
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Username}:{password}")));
        HttpClient.BaseAddress = new Uri($"https://127.0.0.1:{port}/");

        _ws.Options.Credentials = new NetworkCredential(Username, password);
        _wsUrl = new Uri($"wss://127.0.0.1:{port}/");
    }

    public async Task Connect(CancellationToken cancellationToken = default)
    {
        // Open WS connection
        await _ws.ConnectAsync(_wsUrl, cancellationToken);
        OnConnected?.Invoke();

        // Subscribe to API events
        await Subscribe("OnJsonApiEvent");

        // Receive messages
        var buffer = new byte[BufferSize];
        while (_ws.State == WebSocketState.Open)
        {
            var message = new StringBuilder();

            // Read message
            WebSocketReceiveResult result;
            do
            {
                result = await _ws.ReceiveAsync(buffer, cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken);
                }
                else
                {
                    message.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
            } while (!result.EndOfMessage);

            // Finished reading a message
            var msg = message.ToString();

#if DEBUG
            var logLine = $"[LCU][{DateTime.Now:HH:mm:ss.fff}]: {msg}";
            System.Diagnostics.Debug.WriteLine(logLine);
            await File.AppendAllTextAsync("lcu.log", logLine + "\n", CancellationToken.None);
#endif

            // Parse and handle WAMP messages
            HandleMessage(msg);
        }
    }

    public async Task AcceptReadyCheck()
    {
        using var response = await HttpClient.PostAsync("lol-matchmaking/v1/ready-check/accept", null);
        response.EnsureSuccessStatusCode();
    }

    private async Task Subscribe(string topic)
    {
        // Build payload
        var payload = new JsonArray(5, topic);

        await _ws.SendAsync(JsonSerializer.SerializeToUtf8Bytes(payload), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private void HandleMessage(string rawMessage)
    {
        // Parse WAMP message
        using var message = JsonDocument.Parse(rawMessage);
        if (message.RootElement.ValueKind != JsonValueKind.Array || message.RootElement.GetArrayLength() < 1)
        {
            return;
        }

        var messageType = message.RootElement[0].GetInt32();

        // Only handle events
        if (messageType != 8 || message.RootElement.GetArrayLength() != 3) // EVENT
        {
            return;
        }

        // Parse topic
        var topic = message.RootElement[1].GetString();
        if (topic != "OnJsonApiEvent")
        {
            return;
        }

        // Parse payload
        var rawData = message.RootElement[2].GetProperty("data");
        var eventType = message.RootElement[2].GetProperty("eventType").GetString();
        var uri = HttpUtility.UrlDecode(message.RootElement[2].GetProperty("uri").GetString());
        if (string.IsNullOrEmpty(uri))
        {
            return;
        }

        // Detect game state
        if (uri == "/lol-gameflow/v1/gameflow-phase" && eventType == "Update")
        {
            switch (rawData.GetString())
            {
                // case "Lobby":
                // case "Matchmaking":
                //     break;
                case "ReadyCheck":
                    OnReadyCheck?.Invoke();
                    break;
                // case "ChampSelect":
                // case "GameStart":
                // case "InProgress":
                // case "WaitingForStats":
                // case "PreEndOfGame":
                // case "EndOfGame":
                // case "None":
                //     break;
            }
        }
        // Detect lobby member update
        else if (uri == "/lol-lobby/v2/lobby/members" && eventType is "Create" or "Update")
        {
            var lobbyMembers = rawData.Deserialize<List<LobbyMember>>(JsonOptions);
            if (lobbyMembers != null)
            {
                OnLobbyUpdate?.Invoke(lobbyMembers);
            }
        }
        // Detect end of game stats
        else if (uri == "/lol-end-of-game/v1/eog-stats-block" && eventType == "Create")
        {
            var stats = rawData.Deserialize<StatsBlock>(JsonOptions);
            if (stats != null)
            {
                OnEndOfGameStats?.Invoke(stats);
            }
        }
        // Detect champ select
        else if (uri == "/lol-lobby-team-builder/champ-select/v1/session" && eventType == "Create")
        {
            var session = rawData.Deserialize<ChampSelectSession>(JsonOptions);
            if (session != null)
            {
                OnChampSelect?.Invoke(session);
            }
        }
        // Detect game start
        else if (uri == "/lol-gameflow/v1/session" && eventType == "Update")
        {
            var session = rawData.Deserialize<Session>(JsonOptions);
            if (session is { Phase: "GameStart", GameData: { TeamOne.Count: > 0, TeamTwo.Count: > 0 } })
            {
                OnGameStart?.Invoke(session);
            }
        }
        // Detect chat participants
        else if (uri.StartsWith("/lol-chat/v1/conversations/") && /*uri.Contains("@lol-post-game.") &&*/ uri.EndsWith("/participants"))
        {
            var participants = rawData.Deserialize<List<ChatParticipant>>(JsonOptions);
            if (participants != null) // this event is sent multiple times, and will be invoked many times with the same information
            {
                foreach (var participant in participants)
                {
                    OnChatParticipant?.Invoke(participant);
                }
            }
        }
        // Detect champ select lobby
        // else if (uri.StartsWith("/lol-chat/v1/conversations/") && uri.Contains("champ-select.") && uri.EndsWith("/participants"))
        // {
        //     var data = rawData.Deserialize<List<ChatChampSelectParticipant>>(JsonOptions);
        //     if (data is { Count: 5 }) // this event is sent multiple times, wait until we get one with all 5 participants
        //     {
        //         OnChampSelect?.Invoke(data);
        //     }
        // }
    }
}
