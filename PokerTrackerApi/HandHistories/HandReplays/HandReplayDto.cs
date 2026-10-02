namespace PokerTrackerApi.HandHistories.HandReplays;

public record HandReplayDto(
    string HandId,
    HoleCardsDto HeroCards,
    string HeroPosition,
    IReadOnlyDictionary<string, decimal> StartingStacksBB,
    IReadOnlyList<HandReplaySpotDto> ActionSequence
);
