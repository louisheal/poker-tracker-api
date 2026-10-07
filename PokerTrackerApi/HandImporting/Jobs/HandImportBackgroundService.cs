namespace PokerTrackerApi.HandImporting.Jobs;

public class HandImportBackgroundService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan LeaseRenewalInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HandImportBackgroundService> _logger;

    public HandImportBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<HandImportBackgroundService> logger
    )
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository =
                    scope.ServiceProvider.GetRequiredService<IHandImportJobRepository>();
                var file = await repository.ClaimNextFileAsync(stoppingToken);
                if (file is null)
                {
                    await Task.Delay(PollInterval, stoppingToken);
                    continue;
                }

                await ProcessFileAsync(file, scope.ServiceProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "An error occurred while processing hand import jobs.");
                await Task.Delay(PollInterval, stoppingToken);
            }
        }
    }

    private async Task ProcessFileAsync(
        ClaimedHandImportFile file,
        IServiceProvider services,
        CancellationToken stoppingToken
    )
    {
        var repository = services.GetRequiredService<IHandImportJobRepository>();
        var fileStore = services.GetRequiredService<IHandImportFileStore>();
        var importService = services.GetRequiredService<IHandImportService>();

        try
        {
            var summary = await ImportWithLeaseRenewalAsync(
                file,
                fileStore,
                importService,
                stoppingToken
            );
            if (summary is null)
            {
                return;
            }

            if (await repository.CompleteFileAsync(file, summary, stoppingToken))
            {
                fileStore.Delete(file.StorageKey);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to import file {FileName} for job {JobId}.",
                file.FileName,
                file.JobId
            );
            await repository.FailFileAsync(file, exception.Message, stoppingToken);
        }
    }

    private async Task<HandImportFileSummary?> ImportWithLeaseRenewalAsync(
        ClaimedHandImportFile file,
        IHandImportFileStore fileStore,
        IHandImportService importService,
        CancellationToken stoppingToken
    )
    {
        using var processingTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            stoppingToken
        );
        var importTask = importService.ImportFileAsync(
            fileStore.OpenRead(file.StorageKey),
            processingTokenSource.Token
        );

        try
        {
            while (!importTask.IsCompleted)
            {
                var delay = Task.Delay(LeaseRenewalInterval, stoppingToken);
                if (await Task.WhenAny(importTask, delay) == importTask)
                {
                    break;
                }

                await delay;
                if (!await RenewLeaseAsync(file, stoppingToken))
                {
                    processingTokenSource.Cancel();
                    try
                    {
                        await importTask;
                    }
                    catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                    {
                        return null;
                    }

                    return null;
                }
            }

            return await importTask;
        }
        catch
        {
            processingTokenSource.Cancel();
            try
            {
                await importTask;
            }
            catch
            {
                // Preserve the original failure after the scoped import operation has stopped.
            }

            throw;
        }
    }

    private async Task<bool> RenewLeaseAsync(
        ClaimedHandImportFile file,
        CancellationToken cancellationToken
    )
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IHandImportJobRepository>();
        return await repository.RenewLeaseAsync(file, cancellationToken);
    }
}
