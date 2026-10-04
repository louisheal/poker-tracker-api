using PokerTrackerApi.Domain;

namespace PokerTrackerApi.HandHistories;

public record HandHistory(string HandId, HoleCards HeroHoleCards);
