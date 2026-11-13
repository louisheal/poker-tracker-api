namespace PokerTrackerApi.HandParsing.Repository;

public class HandHistoryDao
{
    public string HandId { get; set; } = string.Empty;
    public string HeroPlayerId { get; set; } = string.Empty;
    public string HeroHoleCardsJson { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public decimal TotalPotBB { get; set; }
    public decimal RakeBB { get; set; }
}
