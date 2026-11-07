namespace PokerTrackerApi.HandImporting.Storage;

public interface IHandFileStore
{
    Task SaveAsync(Stream file, Guid fileId, CancellationToken cancellationToken);
    Task RemoveFileAsync(Guid fileId, CancellationToken cancellationToken);
}
