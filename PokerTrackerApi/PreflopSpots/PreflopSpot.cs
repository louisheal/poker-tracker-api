using PokerTrackerApi.Domain;

namespace PokerTrackerApi.PreflopSpots;

public class PreflopSpot
{
    public required string HandId { get; init; }

    public required string SpotKey { get; init; }

    public required string HandKey { get; init; }

    public required PokerAction Action { get; init; }
}
