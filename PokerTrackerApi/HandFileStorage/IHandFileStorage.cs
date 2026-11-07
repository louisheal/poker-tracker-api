namespace PokerTrackerApi.HandFileStorage;

public interface IHandFileStorage
{
    Task SaveAsync(Stream file, Guid fileId, CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(Guid fileId, CancellationToken cancellationToken);
    Task RemoveAsync(Guid fileId, CancellationToken cancellationToken);
}
