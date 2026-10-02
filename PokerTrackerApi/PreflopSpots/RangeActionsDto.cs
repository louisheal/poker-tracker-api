namespace PokerTrackerApi.PreflopSpots;

public record RangeActionsDto(string SpotKey, IReadOnlyList<HandActionsDto> Hands);
