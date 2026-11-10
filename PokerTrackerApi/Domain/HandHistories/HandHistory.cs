using PokerTrackerApi.Domain.HandHistories.Events;

namespace PokerTrackerApi.Domain.HandHistories;

public record HandHistory(
    string HandId,
    string HeroPlayerId,
    HoleCards HeroHoleCards,
    IReadOnlyList<HandHistoryPlayer> Players,
    IReadOnlyList<HandHistoryEvent> Events,
    DateTimeOffset Timestamp
);
