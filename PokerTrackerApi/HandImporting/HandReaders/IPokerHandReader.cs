namespace PokerTrackerApi.HandImporting.HandReaders;

public interface IPokerHandReader
{
    IAsyncEnumerable<string> ReadHandsAsync(TextReader reader, CancellationToken cancellationToken);
}
