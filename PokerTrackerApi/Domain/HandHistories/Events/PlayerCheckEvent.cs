namespace PokerTrackerApi.Domain.HandHistories.Events;

public record PlayerCheckEvent(int Sequence, string PlayerId)
    : PlayerActionEvent(Sequence, PlayerId);
