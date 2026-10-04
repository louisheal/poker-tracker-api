using PokerTrackerApi.Domain;

namespace PokerTrackerApi.Domain.PokerHand.Events;

public abstract class PokerHandBoardDealtEvent : PokerHandEvent
{
    public required PokerStreet Street { get; init; }
}
