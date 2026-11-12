namespace PokerTrackerApi.Domain.HandHistories.Events;

[GenerateSerializer]
public abstract record BoardDealtEvent(int Sequence, [property: Id(1)] PokerStreet Street)
    : HandHistoryEvent(Sequence);
