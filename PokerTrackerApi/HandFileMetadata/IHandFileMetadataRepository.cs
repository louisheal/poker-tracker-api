using System.Data;

namespace PokerTrackerApi.HandFileMetadata;

public interface IHandFileMetadataRepository
{
    Task<Guid> CreateMetadataAsync(
        string fileName,
        CancellationToken cancellationToken
    );
    Task<IReadOnlyList<Guid>> GetIdsCreatedBeforeAsync(
        int limit,
        DateTimeOffset createdBefore,
        CancellationToken cancellationToken
    );
    Task<bool> MarkStoredAsync(
        Guid fileId,
        CancellationToken cancellationToken,
        IDbTransaction? transaction = null
    );
    Task MarkFailedAsync(
        Guid fileId,
        CancellationToken cancellationToken
    );
    Task MarkCompletedAsync(
        Guid fileId,
        CancellationToken cancellationToken
    );
}
