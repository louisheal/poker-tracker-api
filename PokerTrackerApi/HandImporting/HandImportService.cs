using DotNetCore.CAP;
using PokerTrackerApi.Database;
using PokerTrackerApi.Domain;
using PokerTrackerApi.HandFileMetadata;
using PokerTrackerApi.HandFileStorage;

namespace PokerTrackerApi.HandImporting;

public interface IHandImportService
{
    Task ImportFiles(IReadOnlyCollection<IFormFile> files, CancellationToken cancellationToken);
}

public class HandImportService : IHandImportService
{
    private readonly string TOPIC_NAME = "HandFileUpdates";

    private readonly IHandFileStorage _handFileStore;
    private readonly IHandFileMetadataRepository _repository;
    private readonly IPokerDatabaseConnectionFactory _connectionFactory;
    private readonly ICapPublisher _capPublisher;

    public HandImportService(
        IHandFileStorage handFileStore,
        IHandFileMetadataRepository repository,
        IPokerDatabaseConnectionFactory connectionFactory,
        ICapPublisher capPublisher
    )
    {
        _handFileStore = handFileStore;
        _repository = repository;
        _connectionFactory = connectionFactory;
        _capPublisher = capPublisher;
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
        await using var connection = await _connectionFactory.OpenConnectionAsync(
            cancellationToken
        );

        var fileId = await _repository.CreateMetadataAsync(file.FileName, cancellationToken);

        await using var stream = file.OpenReadStream();
        await _handFileStore.SaveAsync(stream, fileId, cancellationToken);

        using var transaction = connection.BeginTransaction(_capPublisher, autoCommit: false);
        if (transaction.DbTransaction is not System.Data.IDbTransaction dbTransaction)
        {
            throw new InvalidOperationException(
                "CAP did not provide a compatible database transaction."
            );
        }

        var markedStored = await _repository.MarkStoredAsync(
            fileId,
            cancellationToken,
            transaction: dbTransaction
        );

        if (!markedStored)
        {
            throw new InvalidOperationException($"Hand file {fileId} could not be marked Stored.");
        }

        await _capPublisher.PublishAsync(
            TOPIC_NAME,
            new HandFileStored(fileId),
            cancellationToken: cancellationToken
        );

        transaction.Commit();
    }
}
