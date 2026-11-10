namespace PokerTrackerApi.Domain.HandHistories.Events;

public record PlayerRaiseEvent(
    int Sequence,
    string PlayerId,
    decimal RaiseAmountBB,
    decimal RaiseToAmountBB,
    bool IsAllIn
) : PlayerActionEvent(Sequence, PlayerId);
