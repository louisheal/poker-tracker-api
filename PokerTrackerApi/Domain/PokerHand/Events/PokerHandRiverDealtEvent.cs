using PokerTrackerApi.Domain;

namespace PokerTrackerApi.Domain.PokerHand.Events;

public class PokerHandRiverDealtEvent : PokerHandBoardDealtEvent
{
    public required PlayingCard Card { get; init; }
}
