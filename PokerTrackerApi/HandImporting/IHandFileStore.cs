namespace PokerTrackerApi.HandImporting;

public interface IHandFileStore
{
    Task SaveAsync(Stream file, Guid fileId, CancellationToken cancellationToken);
}
