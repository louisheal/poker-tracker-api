using PokerTrackerApi.Domain;

namespace PokerTrackerApi.HandImport.Parsers;

public record PreflopSpot(string SpotKey, string HandKey, PokerAction Action);