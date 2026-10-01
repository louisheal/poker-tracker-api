namespace PokerTrackerApi.PreflopSpots;

public record HandActionsDto(
    string HandKey,
    double Fold,
    double Call,
    double Raise);