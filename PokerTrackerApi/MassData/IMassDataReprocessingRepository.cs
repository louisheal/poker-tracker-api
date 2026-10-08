using PokerTrackerApi.Domain.PokerHand;

namespace PokerTrackerApi.MassData;

public interface IMassDataReprocessingRepository
{
    Task<MassDataReprocessingJobDto> CreateOrGetActiveJobAsync(CancellationToken cancellationToken);

    Task<MassDataReprocessingJobDto?> GetJobAsync(Guid jobId, CancellationToken cancellationToken);

    Task<ClaimedMassDataReprocessingJob?> ClaimNextJobAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<PokerHand>> GetNextHandBatchAsync(
        string? lastProcessedHandId,
        CancellationToken cancellationToken
    );

    Task<bool> ProcessBatchAsync(
        ClaimedMassDataReprocessingJob job,
        IReadOnlyList<PokerHand> hands,
        CancellationToken cancellationToken
    );

    Task<bool> CompleteJobAsync(
        ClaimedMassDataReprocessingJob job,
        CancellationToken cancellationToken
    );

    Task<bool> FailJobAsync(
        ClaimedMassDataReprocessingJob job,
        string errorMessage,
        CancellationToken cancellationToken
    );
}
