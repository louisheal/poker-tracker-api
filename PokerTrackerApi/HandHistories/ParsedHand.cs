using PokerTrackerApi.Domain;

namespace PokerTrackerApi.HandHistories;

public class ParsedHand
{
    public required string HandId { get; init; }
    public required HoleCards HoleCards { get; init; }
}