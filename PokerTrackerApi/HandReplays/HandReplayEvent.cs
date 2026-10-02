namespace PokerTrackerApi.HandReplays;

public class HandReplayEvent
{
    public required string HandId { get; init; }
    public required int Sequence { get; init; }
    public required string Street { get; init; }
    public required string EventType { get; init; }
    public string? PlayerId { get; init; }
    public decimal? AmountBB { get; init; }
    public decimal? RaiseToAmountBB { get; init; }
    public ICollection<HandReplayEventCard> Cards { get; set; } = new List<HandReplayEventCard>();
}
