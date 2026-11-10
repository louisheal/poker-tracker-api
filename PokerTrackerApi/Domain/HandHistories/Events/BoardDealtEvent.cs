namespace PokerTrackerApi.Domain.HandHistories.Events;

public abstract record BoardDealtEvent(int Sequence, PokerStreet Street)
    : HandHistoryEvent(Sequence);
