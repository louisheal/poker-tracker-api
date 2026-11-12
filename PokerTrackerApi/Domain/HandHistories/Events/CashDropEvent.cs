namespace PokerTrackerApi.Domain.HandHistories.Events;

[GenerateSerializer]
public record CashDropEvent(int Sequence, [property: Id(1)] decimal AmountBB)
    : HandHistoryEvent(Sequence);
