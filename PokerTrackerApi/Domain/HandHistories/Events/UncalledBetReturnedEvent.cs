namespace PokerTrackerApi.Domain.HandHistories.Events;

[GenerateSerializer]
public record UncalledBetReturnedEvent(int Sequence, [property: Id(1)] string PlayerId, [property: Id(2)] decimal AmountBB)
    : HandHistoryEvent(Sequence);
