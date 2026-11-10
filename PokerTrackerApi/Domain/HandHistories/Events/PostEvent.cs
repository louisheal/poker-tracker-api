namespace PokerTrackerApi.Domain.HandHistories.Events;

public abstract record PostEvent(
    int Sequence,
    string PlayerId,
    PostType PostType,
    decimal AmountBB
) : HandHistoryEvent(Sequence);
