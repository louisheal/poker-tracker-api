namespace PokerTrackerApi.HandFileSplitting;

public interface IHandFileSplitter
{
    IAsyncEnumerable<RawHand> SplitHandsAsync(Stream handFile, CancellationToken cancellationToken);
}
