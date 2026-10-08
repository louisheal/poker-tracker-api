namespace PokerTrackerApi.MassData;

public class MassDataReprocessingBackgroundService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MassDataReprocessingBackgroundService> _logger;

    public MassDataReprocessingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<MassDataReprocessingBackgroundService> logger
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
                    scope.ServiceProvider.GetRequiredService<IMassDataReprocessingRepository>();
                var job = await repository.ClaimNextJobAsync(stoppingToken);
                if (job is null)
                {
                    await Task.Delay(PollInterval, stoppingToken);
                    continue;
                }

                var service =
                    scope.ServiceProvider.GetRequiredService<IMassDataReprocessingService>();
                try
                {
                    await service.ProcessAsync(job, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Failed to reprocess postflop betting data for job {JobId}.",
                        job.JobId
                    );
                    await service.FailAsync(job, exception.Message, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "An error occurred in MassData reprocessing.");
                await Task.Delay(PollInterval, stoppingToken);
            }
        }
    }
}
