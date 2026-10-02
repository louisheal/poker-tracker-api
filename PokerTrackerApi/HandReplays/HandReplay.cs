using PokerTrackerApi.Domain;

namespace PokerTrackerApi.HandReplays;

public class HandReplay
{
    public required string HandId { get; init; }
    public required HoleCards HeroHoleCards { get; set; }
    public required PokerPosition HeroPosition { get; set; }
}
