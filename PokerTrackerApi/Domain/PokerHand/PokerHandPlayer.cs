namespace PokerTrackerApi.Domain.PokerHand;

public class PokerHandPlayer
{
    public required string PlayerId { get; init; }
    public required PokerPosition Position { get; init; }
    public required decimal StartingStackBB { get; init; }
}
