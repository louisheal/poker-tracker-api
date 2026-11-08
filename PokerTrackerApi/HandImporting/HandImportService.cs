using PokerTrackerApi.HandFileStorage;

namespace PokerTrackerApi.HandImporting;

public interface IHandImportService
{
    Task ImportFiles(IReadOnlyCollection<IFormFile> files, CancellationToken cancellationToken);
}

public class HandImportService : IHandImportService
{
    private readonly IHandFileStorage _handFileStore;
    private readonly IHandImportPersistenceService _persistenceService;
    private readonly ILogger<HandImportService> _logger;

    public HandImportService(
        IHandFileStorage handFileStore,
        IHandImportPersistenceService persistenceService,
        ILogger<HandImportService> logger
    )
    {
        _handFileStore = handFileStore;
        _persistenceService = persistenceService;
        _logger = logger;
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
        _logger.LogInformation("Started importing hand file {FileId}", fileId);

        await using var stream = file.OpenReadStream();
        await _handFileStore.SaveAsync(stream, fileId, cancellationToken);
        _logger.LogInformation("Stored hand file in object storage {FileId}", fileId);

        await _persistenceService.MarkStoredAndPublishAsync(fileId, cancellationToken);
    }
}
