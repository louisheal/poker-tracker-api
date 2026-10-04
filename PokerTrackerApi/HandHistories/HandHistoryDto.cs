using PokerTrackerApi.Contract;

namespace PokerTrackerApi.HandHistories;

public record HandHistoryDto(
    string HandId,
    HoleCardsDto HoleCards,
    HandLabelDto[] Labels,
    string Note
);

public record HandLabelDto(string Street, string Label);
