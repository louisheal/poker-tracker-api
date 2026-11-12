namespace PokerTrackerApi.Domain.HandHistories.Events;
[GenerateSerializer]
public abstract record PlayerActionEvent(int Sequence, [property: Id(1)] string PlayerId)
    : HandHistoryEvent(Sequence);
