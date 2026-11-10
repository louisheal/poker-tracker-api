namespace PokerTrackerApi.Domain.HandHistories.Events;

public record PlayerFoldEvent(int Sequence, string PlayerId)
    : PlayerActionEvent(Sequence, PlayerId);
