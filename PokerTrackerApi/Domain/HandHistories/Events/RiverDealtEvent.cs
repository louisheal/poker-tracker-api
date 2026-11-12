namespace PokerTrackerApi.Domain.HandHistories.Events;

[GenerateSerializer]
public record RiverDealtEvent(int Sequence, PokerStreet Street, [property: Id(2)] PlayingCard Card)
    : BoardDealtEvent(Sequence, Street);
