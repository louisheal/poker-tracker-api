namespace PokerTrackerApi.HandImport.Readers;

public interface IPokerHandReader
{
    IAsyncEnumerable<HandReadResult> ReadHandsAsync(TextReader reader, CancellationToken cancellationToken);
}