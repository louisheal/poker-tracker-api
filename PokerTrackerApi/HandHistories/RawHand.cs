namespace PokerTrackerApi.HandHistories;

public sealed class RawHand
{
    public required string HandId { get; init; }

    public required string RawText { get; init; }
}