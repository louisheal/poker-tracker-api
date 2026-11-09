namespace PokerTrackerApi.Domain.PokerHand.Events;

public class PokerHandCashDropEvent : PokerHandEvent
{
    public required decimal AmountBB { get; init; }
}
