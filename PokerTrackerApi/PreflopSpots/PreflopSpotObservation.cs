using PokerTrackerApi.Domain;

namespace PokerTrackerApi.PreflopSpots;

public record PreflopSpotObservation(string SpotKey, string HandKey, PokerAction Action);