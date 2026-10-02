namespace PokerTrackerApi.Domain.InternalRepresentation.Events;

public record CardsShown(string PlayerId, HoleCards HoleCards) : ParsedHandEvent();
