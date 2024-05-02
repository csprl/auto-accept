namespace AutoAccept.Models;

internal class LeagueClientInfo
{
    public required string Name { get; init; }
    public int ProcessId { get; init; }
    public int Port { get; init; }
    public required string Password { get; init; }
    public required string Protocol { get; init; }
}
