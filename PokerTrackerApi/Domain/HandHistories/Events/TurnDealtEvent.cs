namespace PokerTrackerApi.Domain.HandHistories.Events;

public record TurnDealtEvent(int Sequence, PokerStreet Street, PlayingCard Card)
    : BoardDealtEvent(Sequence, Street);
