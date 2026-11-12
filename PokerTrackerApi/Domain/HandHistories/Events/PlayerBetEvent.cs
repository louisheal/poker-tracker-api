namespace PokerTrackerApi.Domain.HandHistories.Events;

[GenerateSerializer]
public record PlayerBetEvent(int Sequence, string PlayerId, [property: Id(2)] decimal BetAmountBB)
    : PlayerActionEvent(Sequence, PlayerId);
