namespace PokerTrackerApi.Domain.HandHistories.Events;

[GenerateSerializer]
public record PlayerRaiseEvent(
    int Sequence,
    string PlayerId,
    [property: Id(2)] decimal RaiseAmountBB,
    [property: Id(3)] decimal RaiseToAmountBB,
    [property: Id(4)] bool IsAllIn
) : PlayerActionEvent(Sequence, PlayerId);
