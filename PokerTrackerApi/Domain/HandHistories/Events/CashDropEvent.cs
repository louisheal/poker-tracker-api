namespace PokerTrackerApi.Domain.HandHistories.Events;

public record CashDropEvent(int Sequence, decimal AmountBB) : HandHistoryEvent(Sequence);
