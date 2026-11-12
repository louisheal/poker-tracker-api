namespace PokerTrackerApi.Domain.HandHistories.Events;

[GenerateSerializer]
public record TurnDealtEvent(int Sequence, PokerStreet Street, [property: Id(2)] PlayingCard Card)
    : BoardDealtEvent(Sequence, Street);
