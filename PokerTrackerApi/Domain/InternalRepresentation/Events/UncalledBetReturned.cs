namespace PokerTrackerApi.Domain.InternalRepresentation.Events;

public record UncalledBetReturned(string PlayerId, decimal AmountBB) : ParsedHandEvent();
