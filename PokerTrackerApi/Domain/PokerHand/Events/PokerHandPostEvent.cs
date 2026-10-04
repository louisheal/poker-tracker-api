using PokerTrackerApi.Domain;

namespace PokerTrackerApi.Domain.PokerHand.Events;

public abstract class PokerHandPostEvent : PokerHandEvent
{
    public required string PlayerId { get; init; }
    public required PostType PostType { get; init; }
    public required decimal AmountBB { get; init; }
}
