using PokerTrackerApi.Domain;

namespace PokerTrackerApi.Domain.PokerHand.Events;

public class PokerHandFlopDealtEvent : PokerHandBoardDealtEvent
{
    public required PlayingCard First { get; init; }
    public required PlayingCard Second { get; init; }
    public required PlayingCard Third { get; init; }
}
