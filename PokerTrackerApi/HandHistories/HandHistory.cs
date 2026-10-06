using PokerTrackerApi.Domain;
using PokerTrackerApi.HandAnnotations;

namespace PokerTrackerApi.HandHistories;

public record HandHistory(
    string HandId,
    HoleCards HeroHoleCards,
    HandLabelAssignment[] Labels,
    string Note,
    bool Flagged
);
