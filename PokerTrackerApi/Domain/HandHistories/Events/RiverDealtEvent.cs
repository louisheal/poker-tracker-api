namespace PokerTrackerApi.Domain.HandHistories.Events;

public record RiverDealtEvent(int Sequence, PokerStreet Street, PlayingCard Card)
    : BoardDealtEvent(Sequence, Street);
