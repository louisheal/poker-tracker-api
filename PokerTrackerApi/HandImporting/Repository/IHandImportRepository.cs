namespace PokerTrackerApi.HandImporting.Repository;

public interface IHandImportRepository
{
    Task<Guid> CreateStartedAsync(string fileName, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> GetIdsCreatedBeforeAsync(
        int limit,
        DateTimeOffset createdBefore,
        CancellationToken cancellationToken
    );
    Task MarkStoredAsync(Guid fileId, CancellationToken cancellationToken);
    Task MarkFailedAsync(Guid fileId, CancellationToken cancellationToken);
}
