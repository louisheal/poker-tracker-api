namespace PokerTrackerApi.HandImporting.HandParsers;

public interface IPokerHandParser
{
    HandHistoryParseResult ParseHand(string rawHand);
}
