namespace AutoAccept.Models.LCU.Chat;

internal class ChatParticipant
{
    public required string Puuid { get; set; }

    // Riot ID
    public required string GameName { get; set; }
    public required string GameTag { get; set; }

    // Old summoner name
    public required string Name { get; set; }
}
