namespace PokerTrackerApi.HandParsing;

public interface IHandParsingGrain : IGrainWithStringKey
{
    Task ParseHand(string rawHand, CancellationToken cancellationToken);
}

public class HandParsingGrain : Grain, IHandParsingGrain
{
    public async Task ParseHand(string rawHand, CancellationToken cancellationToken)
    {
        // throw new NotImplementedException();
    }
}
