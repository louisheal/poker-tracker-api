using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Domain.PokerHand;
using PokerTrackerApi.Persistence;

namespace PokerTrackerApi.MassData;

public class MassDataReprocessingRepository : IMassDataReprocessingRepository
{
    private static readonly TimeSpan JobLeaseDuration = TimeSpan.FromMinutes(10);
    private static readonly SemaphoreSlim JobCreationGate = new(1, 1);
    private const int BatchSize = 100;

    private readonly PokerTrackerDbContext _dbContext;

    public MassDataReprocessingRepository(PokerTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MassDataReprocessingJobDto> CreateOrGetActiveJobAsync(
        CancellationToken cancellationToken
    )
    {
        await JobCreationGate.WaitAsync(cancellationToken);
        try
        {
            var activeJob = await _dbContext
                .MassDataReprocessingJobs.AsNoTracking()
                .Where(job =>
                    job.Status == MassDataReprocessingJobStatus.Queued
                    || job.Status == MassDataReprocessingJobStatus.Processing
                )
                .OrderBy(job => job.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (activeJob is not null)
            {
                return ToDto(activeJob);
            }

            var job = new MassDataReprocessingJob
            {
                JobId = Guid.NewGuid(),
                Status = MassDataReprocessingJobStatus.Queued,
                CreatedAt = DateTimeOffset.UtcNow,
                TotalHands = await _dbContext.PokerHands.CountAsync(cancellationToken),
            };
            _dbContext.MassDataReprocessingJobs.Add(job);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return ToDto(job);
        }
        finally
        {
            JobCreationGate.Release();
        }
    }

    public async Task<MassDataReprocessingJobDto?> GetJobAsync(
        Guid jobId,
        CancellationToken cancellationToken
    )
    {
        var job = await _dbContext
            .MassDataReprocessingJobs.AsNoTracking()
            .SingleOrDefaultAsync(job => job.JobId == jobId, cancellationToken);
        return job is null ? null : ToDto(job);
    }

    public async Task<ClaimedMassDataReprocessingJob?> ClaimNextJobAsync(
        CancellationToken cancellationToken
    )
    {
        while (true)
        {
            var now = DateTimeOffset.UtcNow;
            var candidateJobId = await _dbContext
                .MassDataReprocessingJobs.AsNoTracking()
                .Where(job =>
                    job.Status == MassDataReprocessingJobStatus.Queued
                    || (
                        job.Status == MassDataReprocessingJobStatus.Processing
                        && job.LeaseExpiresAt <= now
                    )
                )
                .OrderBy(job => job.CreatedAt)
                .Select(job => (Guid?)job.JobId)
                .FirstOrDefaultAsync(cancellationToken);
            if (candidateJobId is null)
            {
                return null;
            }

            var leaseExpiresAt = now + JobLeaseDuration;
            var claimed = await _dbContext
                .MassDataReprocessingJobs.Where(job =>
                    job.JobId == candidateJobId
                    && (
                        job.Status == MassDataReprocessingJobStatus.Queued
                        || (
                            job.Status == MassDataReprocessingJobStatus.Processing
                            && job.LeaseExpiresAt <= now
                        )
                    )
                )
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(
                                job => job.Status,
                                MassDataReprocessingJobStatus.Processing
                            )
                            .SetProperty(job => job.AttemptCount, job => job.AttemptCount + 1)
                            .SetProperty(job => job.StartedAt, job => job.StartedAt ?? now)
                            .SetProperty(job => job.LeaseExpiresAt, leaseExpiresAt),
                    cancellationToken
                );
            if (claimed == 0)
            {
                continue;
            }

            var job = await _dbContext
                .MassDataReprocessingJobs.AsNoTracking()
                .SingleAsync(job => job.JobId == candidateJobId, cancellationToken);
            return new ClaimedMassDataReprocessingJob(
                job.JobId,
                job.AttemptCount,
                job.LastProcessedHandId
            );
        }
    }

    public async Task<IReadOnlyList<PokerHand>> GetNextHandBatchAsync(
        string? lastProcessedHandId,
        CancellationToken cancellationToken
    )
    {
        var hands = _dbContext
            .PokerHands.AsNoTracking()
            .AsSplitQuery()
            .Include(hand => hand.Players)
            .Include(hand => hand.Events)
            .AsQueryable();
        if (lastProcessedHandId is not null)
        {
            hands = hands.Where(hand => hand.HandId.CompareTo(lastProcessedHandId) > 0);
        }

        return await hands
            .OrderBy(hand => hand.HandId)
            .Take(BatchSize)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<bool> ProcessBatchAsync(
        ClaimedMassDataReprocessingJob job,
        IReadOnlyList<PokerHand> hands,
        CancellationToken cancellationToken
    )
    {
        if (hands.Count == 0)
        {
            return true;
        }

        var handIds = hands.Select(hand => hand.HandId).ToList();
        var spots = hands.SelectMany(PostflopBettingSpotExtractor.Extract).ToArray();
        var lastProcessedHandId = hands[^1].HandId;
        var leaseExpiresAt = DateTimeOffset.UtcNow + JobLeaseDuration;

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            cancellationToken
        );
        try
        {
            await _dbContext
                .PostflopBettingSpots.Where(spot => handIds.Contains(spot.HandId))
                .ExecuteDeleteAsync(cancellationToken);
            _dbContext.PostflopBettingSpots.AddRange(spots);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var updated = await _dbContext
                .MassDataReprocessingJobs.Where(candidate =>
                    candidate.JobId == job.JobId
                    && candidate.Status == MassDataReprocessingJobStatus.Processing
                    && candidate.AttemptCount == job.AttemptCount
                )
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(
                                candidate => candidate.ProcessedHands,
                                candidate => candidate.ProcessedHands + hands.Count
                            )
                            .SetProperty(
                                candidate => candidate.LastProcessedHandId,
                                lastProcessedHandId
                            )
                            .SetProperty(candidate => candidate.LeaseExpiresAt, leaseExpiresAt),
                    cancellationToken
                );
            if (updated != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        finally
        {
            _dbContext.ChangeTracker.Clear();
        }
    }

    public async Task<bool> CompleteJobAsync(
        ClaimedMassDataReprocessingJob job,
        CancellationToken cancellationToken
    )
    {
        var completedAt = DateTimeOffset.UtcNow;
        return await _dbContext
                .MassDataReprocessingJobs.Where(candidate =>
                    candidate.JobId == job.JobId
                    && candidate.Status == MassDataReprocessingJobStatus.Processing
                    && candidate.AttemptCount == job.AttemptCount
                )
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(
                                candidate => candidate.Status,
                                MassDataReprocessingJobStatus.Completed
                            )
                            .SetProperty(candidate => candidate.CompletedAt, completedAt)
                            .SetProperty(
                                candidate => candidate.LeaseExpiresAt,
                                (DateTimeOffset?)null
                            ),
                    cancellationToken
                ) == 1;
    }

    public async Task<bool> FailJobAsync(
        ClaimedMassDataReprocessingJob job,
        string errorMessage,
        CancellationToken cancellationToken
    )
    {
        var storedErrorMessage = errorMessage.Length > 4000 ? errorMessage[..4000] : errorMessage;
        var completedAt = DateTimeOffset.UtcNow;
        return await _dbContext
                .MassDataReprocessingJobs.Where(candidate =>
                    candidate.JobId == job.JobId
                    && candidate.Status == MassDataReprocessingJobStatus.Processing
                    && candidate.AttemptCount == job.AttemptCount
                )
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(
                                candidate => candidate.Status,
                                MassDataReprocessingJobStatus.Failed
                            )
                            .SetProperty(candidate => candidate.CompletedAt, completedAt)
                            .SetProperty(
                                candidate => candidate.LeaseExpiresAt,
                                (DateTimeOffset?)null
                            )
                            .SetProperty(candidate => candidate.ErrorMessage, storedErrorMessage),
                    cancellationToken
                ) == 1;
    }

    private static MassDataReprocessingJobDto ToDto(MassDataReprocessingJob job) =>
        new(
            job.JobId,
            job.Status.ToString().ToLowerInvariant(),
            job.CreatedAt,
            job.StartedAt,
            job.CompletedAt,
            job.TotalHands,
            job.ProcessedHands,
            job.ErrorMessage
        );
}
