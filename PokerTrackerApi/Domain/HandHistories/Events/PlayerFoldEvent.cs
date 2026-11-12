namespace PokerTrackerApi.Domain.HandHistories.Events;

[GenerateSerializer]
public record PlayerFoldEvent(int Sequence, string PlayerId)
    : PlayerActionEvent(Sequence, PlayerId);
