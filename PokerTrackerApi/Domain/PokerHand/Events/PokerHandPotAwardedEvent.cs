namespace PokerTrackerApi.Domain.PokerHand.Events;

public class PokerHandPotAwardedEvent : PokerHandEvent
{
    public required string PlayerId { get; init; }
    public required decimal AmountBB { get; init; }
}
