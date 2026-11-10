namespace PokerTrackerApi.Domain.HandHistories.Events;

public record PlayerCallEvent(int Sequence, string PlayerId, decimal CallAmountBB)
    : PlayerActionEvent(Sequence, PlayerId);
