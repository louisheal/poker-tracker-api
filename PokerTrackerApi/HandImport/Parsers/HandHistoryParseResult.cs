namespace PokerTrackerApi.HandImport.Parsers;

public record HandHistoryParseResult(ParsedHand? Hand, string? Error)
{
    public static HandHistoryParseResult Parsed(ParsedHand hand) => new(hand, null);

    public static HandHistoryParseResult Failed(string error) => new(null, error);
}