namespace PokerTrackerApi.Domain.HandHistories.Events;

public record PotAwardedEvent(int Sequence, string PlayerId, decimal AmountBB)
    : HandHistoryEvent(Sequence);
