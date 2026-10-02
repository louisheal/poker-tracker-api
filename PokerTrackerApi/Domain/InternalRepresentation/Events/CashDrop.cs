namespace PokerTrackerApi.Domain.InternalRepresentation.Events;

public record CashDrop(decimal AmountBB) : ParsedHandEvent();
