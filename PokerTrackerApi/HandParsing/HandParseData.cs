using PokerTrackerApi.Domain;
using PokerTrackerApi.PreflopSpots;

namespace PokerTrackerApi.HandParsing;

public record HandParseData(
    HoleCards HoleCards,
    IReadOnlyList<PreflopSpotObservation> PreflopSpots
);
