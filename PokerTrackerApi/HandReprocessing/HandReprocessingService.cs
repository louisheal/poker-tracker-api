using PokerTrackerApi.HandParsing;

namespace PokerTrackerApi.HandReprocessing;

public interface IHandReprocessingService
{
    Task ReprocessRawHands();
}

public class HandReprocessingService : IHandReprocessingService
{
    private readonly IPokerHandParser _parser;

    public HandReprocessingService(IPokerHandParser parser)
    {
        _parser = parser;
    }

    public Task ReprocessRawHands()
    {
        throw new NotImplementedException();
    }
}