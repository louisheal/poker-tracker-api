namespace PokerTrackerApi.HandParsing.Repository;

public class HandHistoryEventDao
{
    public string HandId { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
}
