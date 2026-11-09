namespace PokerTrackerApi.Domain.PokerHand.Events;

public class PokerHandPlayerCallEvent : PokerHandPlayerActionEvent
{
    public required decimal CallAmountBB { get; init; }
}
