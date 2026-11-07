namespace PokerTrackerApi.HandImporting.Persistence;

public interface IHandImportPersistenceService
{
    Task<Guid> CreateStartedAsync(string fileName, CancellationToken cancellationToken);

    Task MarkStoredAndPublishAsync(Guid fileId, CancellationToken cancellationToken);
}
