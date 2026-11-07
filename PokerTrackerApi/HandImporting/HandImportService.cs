using PokerTrackerApi.HandImporting.Repository;
using PokerTrackerApi.HandImporting.Storage;

namespace PokerTrackerApi.HandImporting;

public interface IHandImportService
{
    Task ImportFiles(IReadOnlyCollection<IFormFile> files, CancellationToken cancellationToken);
}

public class HandImportService : IHandImportService
{
    private readonly IHandFileStore _handFileStore;
    private readonly IHandImportRepository _repository;
    private readonly ILogger<HandImportService> _logger;

    public HandImportService(
        IHandFileStore handFileStore,
        IHandImportRepository repository,
        ILogger<HandImportService> logger
    )
    {
        _handFileStore = handFileStore;
        _repository = repository;
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
        var fileId = await _repository.CreateStartedAsync(file.FileName, cancellationToken);

        await using var stream = file.OpenReadStream();
        await _handFileStore.SaveAsync(stream, fileId, cancellationToken);

        // await _repository.MarkStored(fileId, cancellationToken);

        // TODO : write publish to outbox
    }
}
