namespace PokerTrackerApi.HandImport.HandReaders;

public interface IPokerHandReader
{
    IAsyncEnumerable<string> ReadHandsAsync(TextReader reader, CancellationToken cancellationToken);
}
