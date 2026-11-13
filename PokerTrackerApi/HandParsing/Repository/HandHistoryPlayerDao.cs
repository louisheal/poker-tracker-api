using PokerTrackerApi.Domain;

namespace PokerTrackerApi.HandParsing.Repository;

public class HandHistoryPlayerDao
{
    public string HandId { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public PokerPosition Position { get; set; }
    public decimal StartingStackBB { get; set; }
}
