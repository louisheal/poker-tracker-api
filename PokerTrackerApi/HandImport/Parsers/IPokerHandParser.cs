namespace PokerTrackerApi.HandImport.Parsers;

public interface IPokerHandParser
{
    HandHistoryParseResult ParseHand(string rawHand);
}