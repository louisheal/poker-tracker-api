namespace PokerTrackerApi.Domain.HandHistories.Events;

public record UncalledBetReturnedEvent(int Sequence, string PlayerId, decimal AmountBB)
    : HandHistoryEvent(Sequence);
