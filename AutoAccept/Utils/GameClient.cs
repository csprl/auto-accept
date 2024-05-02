using AutoAccept.Models.Game;
using System.Net.Http.Json;

namespace AutoAccept.Utils;

internal class GameClient : ClientBase
{
    public GameClient()
    {
        // Set base address
        HttpClient.BaseAddress = new Uri("https://127.0.0.1:2999/");
    }

    public async Task<List<PlayerInfo>> GetPlayerList() =>
        await HttpClient.GetFromJsonAsync<List<PlayerInfo>>("liveclientdata/playerlist", JsonOptions) ?? throw new HttpRequestException("Failed to deserialize response.");
}
