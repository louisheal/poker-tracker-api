namespace PokerTrackerApi.HandReplays;

using PokerTrackerApi.Domain;

public class HandReplayPlayer
{
    public required string HandId { get; init; }
    public required string PlayerId { get; init; }
    public required PokerPosition Position { get; init; }
    public required decimal StartingStackBB { get; init; }
}
