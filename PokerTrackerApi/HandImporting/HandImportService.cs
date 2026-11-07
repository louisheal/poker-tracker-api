using PokerTrackerApi.HandImporting.Persistence;
using PokerTrackerApi.HandImporting.Storage;

namespace PokerTrackerApi.HandImporting;

public interface IHandImportService
{
    Task ImportFiles(IReadOnlyCollection<IFormFile> files, CancellationToken cancellationToken);
}

public class HandImportService : IHandImportService
{
    private readonly IHandFileStore _handFileStore;
    private readonly IHandImportPersistenceService _persistenceService;

    public HandImportService(
        IHandFileStore handFileStore,
        IHandImportPersistenceService persistenceService
    )
    {
        _handFileStore = handFileStore;
        _persistenceService = persistenceService;
    }

    public async Task ImportFiles(
        IReadOnlyCollection<IFormFile> files,
        CancellationToken cancellationToken
    )
    {
        foreach (var file in files)
        {
            await ImportFile(file, cancellationToken);
        }
    }

    private async Task ImportFile(IFormFile file, CancellationToken cancellationToken)
    {
        var fileId = await _persistenceService.CreateStartedAsync(file.FileName, cancellationToken);

        await using var stream = file.OpenReadStream();
        await _handFileStore.SaveAsync(stream, fileId, cancellationToken);

        await _persistenceService.MarkStoredAndPublishAsync(fileId, cancellationToken);
    }
}
