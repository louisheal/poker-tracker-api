namespace PokerTrackerApi.Domain.PokerHand.Events;

public class PokerHandPlayerRaiseEvent : PokerHandPlayerActionEvent
{
    public required decimal RaiseAmountBB { get; init; }
    public required decimal RaiseToAmountBB { get; init; }
    public bool IsAllIn { get; init; }
}
