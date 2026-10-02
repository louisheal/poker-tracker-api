namespace PokerTrackerApi.Domain.InternalRepresentation.Events;

public abstract record PlayerActionEvent(string PlayerId) : ParsedHandEvent;
