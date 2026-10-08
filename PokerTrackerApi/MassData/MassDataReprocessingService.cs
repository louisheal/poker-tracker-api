namespace PokerTrackerApi.MassData;

public interface IMassDataReprocessingService
{
    Task<MassDataReprocessingJobDto> EnqueueAsync(CancellationToken cancellationToken);

    Task<MassDataReprocessingJobDto?> GetJobAsync(Guid jobId, CancellationToken cancellationToken);

    Task ProcessAsync(ClaimedMassDataReprocessingJob job, CancellationToken cancellationToken);

    Task FailAsync(
        ClaimedMassDataReprocessingJob job,
        string errorMessage,
        CancellationToken cancellationToken
    );
}

public class MassDataReprocessingService : IMassDataReprocessingService
{
    private readonly IMassDataReprocessingRepository _repository;

    public MassDataReprocessingService(IMassDataReprocessingRepository repository)
    {
        _repository = repository;
    }

    public Task<MassDataReprocessingJobDto> EnqueueAsync(CancellationToken cancellationToken) =>
        _repository.CreateOrGetActiveJobAsync(cancellationToken);

    public Task<MassDataReprocessingJobDto?> GetJobAsync(
        Guid jobId,
        CancellationToken cancellationToken
    ) => _repository.GetJobAsync(jobId, cancellationToken);

    public async Task ProcessAsync(
        ClaimedMassDataReprocessingJob job,
        CancellationToken cancellationToken
    )
    {
        var lastProcessedHandId = job.LastProcessedHandId;
        while (true)
        {
            var hands = await _repository.GetNextHandBatchAsync(
                lastProcessedHandId,
                cancellationToken
            );
            if (hands.Count == 0)
            {
                break;
            }

            if (!await _repository.ProcessBatchAsync(job, hands, cancellationToken))
            {
                return;
            }
            lastProcessedHandId = hands[^1].HandId;
        }

        await _repository.CompleteJobAsync(job, cancellationToken);
    }

    public async Task FailAsync(
        ClaimedMassDataReprocessingJob job,
        string errorMessage,
        CancellationToken cancellationToken
    )
    {
        await _repository.FailJobAsync(job, errorMessage, cancellationToken);
    }
}
