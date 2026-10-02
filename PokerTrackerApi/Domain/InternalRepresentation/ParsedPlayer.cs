namespace PokerTrackerApi.Domain.InternalRepresentation;

public record ParsedPlayer
{
    public ParsedPlayer(PokerPosition position, decimal startingStackBB)
    {
        Position = position;
        StartingStackBB = startingStackBB;
    }

    public PokerPosition Position { get; init; }

    public decimal StartingStackBB { get; init; }
}
