using System.Text.Json.Serialization;

namespace AutoAccept.Models.LCU.LobbyTeamBuilder;

internal class ChampSelectSession
{
    public List<TeamMember> MyTeam { get; set; } = [];

    public class TeamMember : IPlayerInfo
    {
        public required string Puuid { get; set; }

        [JsonIgnore]
        public int? ChampionId => null;
    }
}
