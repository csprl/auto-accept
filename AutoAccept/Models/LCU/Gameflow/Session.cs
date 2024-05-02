namespace AutoAccept.Models.LCU.Gameflow;

internal class Session
{
    public required string Phase { get; set; }
    public required Data GameData { get; set; }

    public class Data
    {
        public List<TeamMember> TeamOne { get; set; } = [];
        public List<TeamMember> TeamTwo { get; set; } = [];

        public class TeamMember : IPlayerInfo
        {
            public required string Puuid { get; set; }
            public int? ChampionId { get; set; }
        }
    }
}
