using DotNetCore.CAP;
using PokerTrackerApi.Database;
using PokerTrackerApi.HandImporting.Repository;

namespace PokerTrackerApi.HandImporting.Persistence;

public class HandImportPersistenceService : IHandImportPersistenceService
{
    // TODO : move and rename
    private readonly string TOPIC_NAME = "HandFileUpdates";

    private readonly IHandImportRepository _repository;
    private readonly IPokerDatabaseConnectionFactory _connectionFactory;
    private readonly ICapPublisher _capPublisher;

    public HandImportPersistenceService(
        IHandImportRepository repository,
        IPokerDatabaseConnectionFactory connectionFactory,
        ICapPublisher capPublisher
    )
    {
        _repository = repository;
        _connectionFactory = connectionFactory;
        _capPublisher = capPublisher;
    }

    public async Task<Guid> CreateStartedAsync(
        string fileName,
        CancellationToken cancellationToken
    )
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        return await _repository.CreateStartedAsync(fileName, connection, cancellationToken);
    }

    public async Task MarkStoredAndPublishAsync(
        Guid fileId,
        CancellationToken cancellationToken
    )
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction(_capPublisher, autoCommit: false);

        if (transaction.DbTransaction is not System.Data.IDbTransaction dbTransaction)
        {
            throw new InvalidOperationException("CAP did not provide a compatible database transaction.");
        }

        var markedStored = await _repository.MarkStoredAsync(
            fileId,
            connection,
            dbTransaction,
            cancellationToken
        );

        if (!markedStored)
        {
            throw new InvalidOperationException(
                $"Hand file {fileId} could not be marked Stored."
            );
        }

        await _capPublisher.PublishAsync(
            TOPIC_NAME,
            new HandFileStored(fileId),
            cancellationToken: cancellationToken
        );

        transaction.Commit();
    }
}
