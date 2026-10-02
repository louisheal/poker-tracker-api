using PokerTrackerApi.Domain;

namespace PokerTrackerApi.HandParsing;

public record HandParseData(
    HoleCards HoleCards,
    IReadOnlyList<PreflopSpotObservation> PreflopSpots
);
