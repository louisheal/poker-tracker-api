using PokerTrackerApi.Domain;

namespace PokerTrackerApi.HandHistories;

public class HandHistorySummary
{
    public required string HandId { get; init; }
    public required HoleCards HoleCards { get; set; }
    public required int ButtonSeat { get; init; }
}