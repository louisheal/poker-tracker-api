using PokerTrackerApi.Domain;

namespace PokerTrackerApi.HandParsing;

// TODO : needs to be promoted to a domain-level concept
public record PreflopSpotObservation(string SpotKey, string HandKey, PokerAction Action);
