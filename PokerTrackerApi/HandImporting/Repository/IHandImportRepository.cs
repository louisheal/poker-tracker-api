namespace PokerTrackerApi.HandImporting.Repository;

public interface IHandImportRepository
{
    Task<Guid> CreateStartedAsync(string fileName, CancellationToken cancellationToken);
    Task MarkStored(Guid fileId, CancellationToken cancellationToken);
}
