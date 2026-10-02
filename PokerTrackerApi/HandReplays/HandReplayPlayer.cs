namespace PokerTrackerApi.HandReplays;

public class HandReplayPlayer
{
    public required string HandId { get; init; }
    public required string PlayerId { get; init; }
    public required int Seat { get; init; }
    public required decimal StartingStackBB { get; init; }
}
