namespace PokerTrackerApi.Domain.InternalRepresentation.Events;

public record TurnDealt(PlayingCard Card) : BoardDealt(PokerStreet.Turn);
