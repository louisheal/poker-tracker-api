namespace PokerTrackerApi.HandHistories.HandReplays;

public record HandReplayDto(
    string HandId,
    HoleCardsDto HeroCards,
    string HeroPosition,
    IReadOnlyList<HandReplaySpotDto> ActionSequence
);
