namespace PokerTrackerApi.Winrate;

public class WinrateDao
{
    public string HandId { get; set; } = string.Empty;
    public decimal NetWonBB { get; set; }
    public decimal WonWithShowdownBB { get; set; }
    public decimal WonWithoutShowdownBB { get; set; }
    public decimal NetWonBeforeRakeBB { get; set; }
    public decimal WonWithShowdownBeforeRakeBB { get; set; }
    public decimal WonWithoutShowdownBeforeRakeBB { get; set; }
}
