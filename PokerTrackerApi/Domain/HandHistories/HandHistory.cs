using PokerTrackerApi.Domain.HandHistories.Events;

namespace PokerTrackerApi.Domain.HandHistories;

[GenerateSerializer]
public record HandHistory(
    [property: Id(0)] string HandId,
    [property: Id(1)] string HeroPlayerId,
    [property: Id(2)] HoleCards HeroHoleCards,
    [property: Id(3)] IReadOnlyList<HandHistoryPlayer> Players,
    [property: Id(4)] IReadOnlyList<HandHistoryEvent> Events,
    [property: Id(5)] DateTimeOffset Timestamp
);
