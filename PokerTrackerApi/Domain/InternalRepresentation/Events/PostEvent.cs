namespace PokerTrackerApi.Domain.InternalRepresentation.Events;

public abstract record PostEvent(string PlayerId, PostType PostType, decimal AmountBB)
    : ParsedHandEvent;
