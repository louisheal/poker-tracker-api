namespace PokerTrackerApi.Domain.HandHistories.Events;

[GenerateSerializer]
public record PlayerCallEvent(int Sequence, string PlayerId, [property: Id(2)] decimal CallAmountBB)
    : PlayerActionEvent(Sequence, PlayerId);
