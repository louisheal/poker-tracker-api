using PokerTrackerApi.Domain;
using PokerTrackerApi.HandNotes;

namespace PokerTrackerApi.HandHistories;

public record HandHistory(
    string HandId,
    HoleCards HeroHoleCards,
    HandLabelAssignment[] Labels,
    string Note
);
