namespace PokerTrackerApi.Domain.InternalRepresentation.Events;

public abstract record BoardDealt(PokerStreet Street) : ParsedHandEvent();
