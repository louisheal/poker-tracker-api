using PokerTrackerApi.Domain.HandHistories;

namespace PokerTrackerApi.HandParsing;

public interface IHandHistoryParser
{
    HandHistory ParseHandHistory(string rawHand);
}
