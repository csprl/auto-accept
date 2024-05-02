namespace AutoAccept.Models;

internal class TrackedPlayer
{
    public required string Id { get; set; }
    public bool SameTeam { get; set; }
    public required string Champion { get; set; }

    public int GameNumber { get; set; }
}
