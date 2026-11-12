namespace PokerTrackerApi.Domain.HandHistories.Events;

[GenerateSerializer]
public abstract record PostEvent(
    int Sequence,
    [property: Id(1)] string PlayerId,
    [property: Id(2)] PostType PostType,
    [property: Id(3)] decimal AmountBB
) : HandHistoryEvent(Sequence);
