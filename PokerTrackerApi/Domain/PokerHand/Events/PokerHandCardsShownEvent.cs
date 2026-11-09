using PokerTrackerApi.Domain;

namespace PokerTrackerApi.Domain.PokerHand.Events;

public class PokerHandCardsShownEvent : PokerHandEvent
{
    public required string PlayerId { get; init; }
    public required HoleCards HoleCards { get; init; }
}
