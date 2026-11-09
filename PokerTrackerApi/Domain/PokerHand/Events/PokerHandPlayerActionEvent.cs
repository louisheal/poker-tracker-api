namespace PokerTrackerApi.Domain.PokerHand.Events;

public abstract class PokerHandPlayerActionEvent : PokerHandEvent
{
    public required string PlayerId { get; init; }
}
