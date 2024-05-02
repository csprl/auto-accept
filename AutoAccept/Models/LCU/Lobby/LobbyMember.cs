namespace AutoAccept.Models.LCU.Lobby;

internal class LobbyMember
{
    public required string Puuid { get; set; }

    // Old summoner name
    public required string SummonerName { get; set; }
    public required string SummonerInternalName { get; set; }
}
