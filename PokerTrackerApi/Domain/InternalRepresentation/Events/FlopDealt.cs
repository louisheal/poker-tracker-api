namespace PokerTrackerApi.Domain.InternalRepresentation.Events;

public record FlopDealt(PlayingCard First, PlayingCard Second, PlayingCard Third)
    : BoardDealt(PokerStreet.Flop);
