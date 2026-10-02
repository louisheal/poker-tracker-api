namespace PokerTrackerApi.HandImport.HandReaders;

public interface IPokerHandReader
{
    IAsyncEnumerable<HandReadResult> ReadHandsAsync(
        TextReader reader,
        CancellationToken cancellationToken
    );
}
