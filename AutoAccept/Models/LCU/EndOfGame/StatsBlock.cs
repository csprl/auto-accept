namespace AutoAccept.Models.LCU.EndOfGame;

internal class StatsBlock
{
    public ulong GameId { get; set; }
    public List<Team> Teams { get; set; } = [];

    public class Team
    {
        public bool IsPlayerTeam { get; set; }
        public List<Player> Players { get; set; } = [];

        public class Player
        {
            public bool IsLocalPlayer { get; set; }

            public required string Puuid { get; set; }

            public required string ChampionName { get; set; }
            public int ChampionId { get; set; }

            // Old summoner name
            public required string SummonerName { get; set; }
        }
    }
}
