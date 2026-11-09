namespace PokerTrackerApi.Domain.PokerHand.Events;

public class PokerHandUncalledBetReturnedEvent : PokerHandEvent
{
    public required string PlayerId { get; init; }
    public required decimal AmountBB { get; init; }
}
