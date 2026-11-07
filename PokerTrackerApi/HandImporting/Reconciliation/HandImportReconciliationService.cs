using Microsoft.Extensions.Options;
using PokerTrackerApi.Database;
using PokerTrackerApi.HandImporting.Repository;
using PokerTrackerApi.HandFileStorage;

namespace PokerTrackerApi.HandImporting.Reconciliation;

public class HandImportReconciliationService : BackgroundService
{
    private readonly IHandFileStorage _store;
    private readonly IHandImportRepository _repository;
    private readonly IPokerDatabaseConnectionFactory _connectionFactory;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _pollInterval;
    private readonly TimeSpan _staleAfter;
    private readonly int _batchSize;

    public HandImportReconciliationService(
        IHandFileStorage store,
        IHandImportRepository repository,
        IPokerDatabaseConnectionFactory connectionFactory,
        IOptions<HandImportReconciliationOptions> options,
        TimeProvider timeProvider
    )
    {
        _store = store;
        _repository = repository;
        _connectionFactory = connectionFactory;
        _timeProvider = timeProvider;
        _pollInterval = options.Value.PollInterval;
        _staleAfter = options.Value.StaleAfter;
        _batchSize = options.Value.BatchSize;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await ReconcileHandFiles(cancellationToken);
            await Task.Delay(_pollInterval, _timeProvider, cancellationToken);
        }
    }

    private async Task ReconcileHandFiles(CancellationToken cancellationToken)
    {
        var staleBefore = _timeProvider.GetUtcNow() - _staleAfter;

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        var staleHandFiles = await _repository.GetIdsCreatedBeforeAsync(
            _batchSize,
            staleBefore,
            connection,
            cancellationToken
        );

        foreach (var fileId in staleHandFiles)
        {
            await ReconcileHandFile(fileId, cancellationToken);
        }
    }

    private async Task ReconcileHandFile(Guid fileId, CancellationToken cancellationToken)
    {
        await _store.RemoveAsync(fileId, cancellationToken);

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await _repository.MarkFailedAsync(fileId, connection, cancellationToken);
    }
}
