namespace PokerTrackerApi.Domain.HandHistories.Events;

[GenerateSerializer]
public record PlayerCheckEvent(int Sequence, string PlayerId)
    : PlayerActionEvent(Sequence, PlayerId);
