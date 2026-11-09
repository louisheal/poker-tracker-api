namespace PokerTrackerApi.HandParsing;

public interface IPokerHandParser
{
    HandHistoryParseResult ParseHand(string rawHand);
}
