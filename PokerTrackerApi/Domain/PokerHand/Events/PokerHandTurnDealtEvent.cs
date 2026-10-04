using PokerTrackerApi.Domain;

namespace PokerTrackerApi.Domain.PokerHand.Events;

public class PokerHandTurnDealtEvent : PokerHandBoardDealtEvent
{
    public required PlayingCard Card { get; init; }
}
