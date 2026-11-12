namespace PokerTrackerApi.Domain.HandHistories.Events;

[GenerateSerializer]
public record FlopDealtEvent(
    int Sequence,
    PokerStreet Street,
    [property: Id(2)] PlayingCard First,
    [property: Id(3)] PlayingCard Second,
    [property: Id(4)] PlayingCard Third
) : BoardDealtEvent(Sequence, Street);
