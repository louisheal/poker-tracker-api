namespace PokerTrackerApi.Domain.HandHistories.Events;

public record FlopDealtEvent(
    int Sequence,
    PokerStreet Street,
    PlayingCard First,
    PlayingCard Second,
    PlayingCard Third
) : BoardDealtEvent(Sequence, Street);
