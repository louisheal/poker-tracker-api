namespace PokerTrackerApi.HandParsing;

public record HandHistoryParseResult(HandParseData? Hand, string? Error)
{
    public static HandHistoryParseResult Parsed(HandParseData hand) => new(hand, null);

    public static HandHistoryParseResult Failed(string error) => new(null, error);
}