using System.Data;

namespace PokerTrackerApi.HandImporting.Repository;

public interface IHandImportRepository
{
    Task<Guid> CreateStartedAsync(
        string fileName,
        IDbConnection connection,
        CancellationToken cancellationToken
    );
    Task<IReadOnlyList<Guid>> GetIdsCreatedBeforeAsync(
        int limit,
        DateTimeOffset createdBefore,
        IDbConnection connection,
        CancellationToken cancellationToken
    );
    Task<bool> MarkStoredAsync(
        Guid fileId,
        IDbConnection connection,
        IDbTransaction transaction,
        CancellationToken cancellationToken
    );
    Task MarkFailedAsync(
        Guid fileId,
        IDbConnection connection,
        CancellationToken cancellationToken
    );
}
