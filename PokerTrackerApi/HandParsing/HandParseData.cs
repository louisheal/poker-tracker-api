using PokerTrackerApi.Domain;
using PokerTrackerApi.PreflopSpots;

namespace PokerTrackerApi.HandParsing;

public record HandParseData(
    HoleCards HoleCards,
    PokerPosition HeroPosition,
    IReadOnlyList<PreflopSpotObservation> PreflopSpots
);
