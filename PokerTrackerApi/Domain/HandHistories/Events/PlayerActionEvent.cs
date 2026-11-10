namespace PokerTrackerApi.Domain.HandHistories.Events;
public abstract record PlayerActionEvent(int Sequence, string PlayerId)
    : HandHistoryEvent(Sequence);
