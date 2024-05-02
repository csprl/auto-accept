namespace AutoAccept.Models.Game;

internal class PlayerInfo
{
    // Riot ID game name
    public required string SummonerName { get; set; }

    public required string ChampionName { get; set; }
    public required string Team { get; set; }
}
