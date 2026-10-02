namespace PokerTrackerApi.Domain.InternalRepresentation;

using PokerTrackerApi.Domain.InternalRepresentation.Events;

public record ParsedHand(
    string HandId,
    string HeroPlayerId,
    HoleCards HeroHoleCards,
    IReadOnlyDictionary<string, ParsedPlayer> Players,
    IReadOnlyList<ParsedHandEvent> Events
);
