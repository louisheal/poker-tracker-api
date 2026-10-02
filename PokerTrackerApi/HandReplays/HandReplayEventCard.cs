using PokerTrackerApi.Domain;

namespace PokerTrackerApi.HandReplays;

public class HandReplayEventCard
{
    public required string HandId { get; init; }
    public required int Sequence { get; init; }
    public required PlayingCard Card { get; init; }
}
