namespace PokerTrackerApi.Domain.HandHistories.Events;

public record PlayerBetEvent(int Sequence, string PlayerId, decimal BetAmountBB)
    : PlayerActionEvent(Sequence, PlayerId);
