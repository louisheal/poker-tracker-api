namespace PokerTrackerApi.Domain.PokerHand.Events;

public abstract class PokerHandEvent
{
    public required int Sequence { get; init; }
}
