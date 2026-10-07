namespace PokerTrackerApi.HandImporting.Jobs;

public interface IHandImportJobService
{
    Task<HandImportJobDto> EnqueueAsync(
        IReadOnlyList<IFormFile> files,
        CancellationToken cancellationToken
    );

    Task<HandImportJobDto?> GetAsync(Guid jobId, CancellationToken cancellationToken);

    Task<HandImportJobDto[]> GetActiveAsync(CancellationToken cancellationToken);
}

public class HandImportJobService : IHandImportJobService
{
    private readonly IHandImportFileStore _fileStore;
    private readonly IHandImportJobRepository _repository;

    public HandImportJobService(IHandImportFileStore fileStore, IHandImportJobRepository repository)
    {
        _fileStore = fileStore;
        _repository = repository;
    }

    public async Task<HandImportJobDto> EnqueueAsync(
        IReadOnlyList<IFormFile> files,
        CancellationToken cancellationToken
    )
    {
        var stagedFiles = await _fileStore.StageAsync(files, cancellationToken);
        try
        {
            return await _repository.CreateAsync(stagedFiles, cancellationToken);
        }
        catch
        {
            foreach (var file in stagedFiles)
            {
                _fileStore.Delete(file.StorageKey);
            }

            throw;
        }
    }

    public Task<HandImportJobDto?> GetAsync(Guid jobId, CancellationToken cancellationToken) =>
        _repository.GetAsync(jobId, cancellationToken);

    public Task<HandImportJobDto[]> GetActiveAsync(CancellationToken cancellationToken) =>
        _repository.GetActiveAsync(cancellationToken);
}
