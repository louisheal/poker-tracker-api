using PokerTrackerApi.Contract;

namespace PokerTrackerApi.HandReplays;

public record HandReplay(
    string HandId,
    HoleCardsDto HeroCards,
    string HeroPosition,
    IReadOnlyDictionary<string, decimal> StartingStacksBB,
    IReadOnlyList<HandReplaySpot> ActionSequence
);
