using PokerTrackerApi.Contract;

namespace PokerTrackerApi.HandHistories;

public record HandHistoryDto(string HandId, HoleCardsDto HoleCards);
