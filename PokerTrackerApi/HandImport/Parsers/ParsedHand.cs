using PokerTrackerApi.Domain;

namespace PokerTrackerApi.HandImport.Parsers;

public record ParsedHand(HoleCards HoleCards, IReadOnlyList<PreflopSpot> PreflopSpots);