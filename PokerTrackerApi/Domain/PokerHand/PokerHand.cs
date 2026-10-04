using PokerTrackerApi.Domain.PokerHand.Events;

namespace PokerTrackerApi.Domain.PokerHand;

public class PokerHand
{
    public required string HandId { get; init; }
    public required string HeroPlayerId { get; init; }
    public required HoleCards HeroHoleCards { get; init; }
    public ICollection<PokerHandPlayer> Players { get; set; } = new List<PokerHandPlayer>();
    public ICollection<PokerHandEvent> Events { get; init; } = new List<PokerHandEvent>();
}
