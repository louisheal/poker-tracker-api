namespace PokerTrackerApi.Domain.PokerHand.Events;

public class PokerHandPlayerBetEvent : PokerHandPlayerActionEvent
{
    public required decimal BetAmountBB { get; init; }
}
