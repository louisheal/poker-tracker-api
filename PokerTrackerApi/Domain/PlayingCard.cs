namespace PokerTrackerApi.Domain;

[GenerateSerializer]
public record PlayingCard([property: Id(0)] Rank Rank, [property: Id(1)] Suit Suit);
