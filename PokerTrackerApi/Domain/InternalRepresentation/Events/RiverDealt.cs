namespace PokerTrackerApi.Domain.InternalRepresentation.Events;

public record RiverDealt(PlayingCard Card) : BoardDealt(PokerStreet.River);
