using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Persistence;

namespace PokerTrackerApi.HandImporting.Jobs;

public record ClaimedHandImportFile(
    Guid FileId,
    Guid JobId,
    int AttemptCount,
    string FileName,
    string StorageKey
);

public interface IHandImportJobRepository
{
    Task<HandImportJobDto> CreateAsync(
        IReadOnlyList<StagedHandImportFile> files,
        CancellationToken cancellationToken
    );

    Task<HandImportJobDto?> GetAsync(Guid jobId, CancellationToken cancellationToken);

    Task<HandImportJobDto[]> GetActiveAsync(CancellationToken cancellationToken);

    Task<ClaimedHandImportFile?> ClaimNextFileAsync(CancellationToken cancellationToken);

    Task<bool> RenewLeaseAsync(ClaimedHandImportFile file, CancellationToken cancellationToken);

    Task<bool> CompleteFileAsync(
        ClaimedHandImportFile file,
        HandImportFileSummary summary,
        CancellationToken cancellationToken
    );

    Task<bool> FailFileAsync(
        ClaimedHandImportFile file,
        string errorMessage,
        CancellationToken cancellationToken
    );
}

public class HandImportJobRepository : IHandImportJobRepository
{
    private static readonly TimeSpan FileLeaseDuration = TimeSpan.FromMinutes(3);

    private readonly PokerTrackerDbContext _dbContext;

    public HandImportJobRepository(PokerTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HandImportJobDto> CreateAsync(
        IReadOnlyList<StagedHandImportFile> files,
        CancellationToken cancellationToken
    )
    {
        var job = new HandImportJob
        {
            JobId = Guid.NewGuid(),
            Status = HandImportJobStatus.Queued,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        foreach (var file in files)
        {
            job.Files.Add(
                new HandImportJobFile
                {
                    FileId = file.FileId,
                    JobId = job.JobId,
                    Sequence = file.Sequence,
                    FileName = file.FileName,
                    StorageKey = file.StorageKey,
                    Status = HandImportJobFileStatus.Queued,
                }
            );
        }

        _dbContext.HandImportJobs.Add(job);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(job);
    }

    public async Task<HandImportJobDto?> GetAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await _dbContext
            .HandImportJobs.AsNoTracking()
            .Include(importJob => importJob.Files)
            .SingleOrDefaultAsync(importJob => importJob.JobId == jobId, cancellationToken);

        return job is null ? null : ToDto(job);
    }

    public async Task<HandImportJobDto[]> GetActiveAsync(CancellationToken cancellationToken)
    {
        var jobs = await _dbContext
            .HandImportJobs.AsNoTracking()
            .Include(job => job.Files)
            .Where(job =>
                job.Status == HandImportJobStatus.Queued
                || job.Status == HandImportJobStatus.Processing
            )
            .OrderBy(job => job.CreatedAt)
            .ToArrayAsync(cancellationToken);

        return jobs.Select(ToDto).ToArray();
    }

    public async Task<ClaimedHandImportFile?> ClaimNextFileAsync(
        CancellationToken cancellationToken
    )
    {
        while (true)
        {
            var now = DateTimeOffset.UtcNow;
            var candidateFileId = await (
                from candidate in _dbContext.HandImportJobFiles.AsNoTracking()
                join job in _dbContext.HandImportJobs.AsNoTracking()
                    on candidate.JobId equals job.JobId
                where
                    candidate.Status == HandImportJobFileStatus.Queued
                    || (
                        candidate.Status == HandImportJobFileStatus.Processing
                        && candidate.LeaseExpiresAt <= now
                    )
                orderby job.CreatedAt, candidate.Sequence
                select (Guid?)candidate.FileId
            ).FirstOrDefaultAsync(cancellationToken);

            if (candidateFileId is null)
            {
                return null;
            }

            var claimed = await _dbContext
                .HandImportJobFiles.Where(file =>
                    file.FileId == candidateFileId
                    && (
                        file.Status == HandImportJobFileStatus.Queued
                        || (
                            file.Status == HandImportJobFileStatus.Processing
                            && file.LeaseExpiresAt <= now
                        )
                    )
                )
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(file => file.Status, HandImportJobFileStatus.Processing)
                            .SetProperty(file => file.AttemptCount, file => file.AttemptCount + 1)
                            .SetProperty(file => file.LeaseExpiresAt, now + FileLeaseDuration),
                    cancellationToken
                );

            if (claimed == 0)
            {
                continue;
            }

            var file = await _dbContext
                .HandImportJobFiles.AsNoTracking()
                .SingleAsync(file => file.FileId == candidateFileId, cancellationToken);
            await _dbContext
                .HandImportJobs.Where(job => job.JobId == file.JobId)
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(job => job.Status, HandImportJobStatus.Processing)
                            .SetProperty(job => job.StartedAt, job => job.StartedAt ?? now),
                    cancellationToken
                );

            return new ClaimedHandImportFile(
                file.FileId,
                file.JobId,
                file.AttemptCount,
                file.FileName,
                file.StorageKey
            );
        }
    }

    public async Task<bool> RenewLeaseAsync(
        ClaimedHandImportFile file,
        CancellationToken cancellationToken
    )
    {
        var leaseExpiresAt = DateTimeOffset.UtcNow + FileLeaseDuration;
        return await _dbContext
                .HandImportJobFiles.Where(jobFile =>
                    jobFile.FileId == file.FileId
                    && jobFile.Status == HandImportJobFileStatus.Processing
                    && jobFile.AttemptCount == file.AttemptCount
                )
                .ExecuteUpdateAsync(
                    setters =>
                        setters.SetProperty(jobFile => jobFile.LeaseExpiresAt, leaseExpiresAt),
                    cancellationToken
                ) == 1;
    }

    public async Task<bool> CompleteFileAsync(
        ClaimedHandImportFile file,
        HandImportFileSummary summary,
        CancellationToken cancellationToken
    )
    {
        var updated = await _dbContext
            .HandImportJobFiles.Where(jobFile =>
                jobFile.FileId == file.FileId
                && jobFile.Status == HandImportJobFileStatus.Processing
                && jobFile.AttemptCount == file.AttemptCount
            )
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(jobFile => jobFile.Status, HandImportJobFileStatus.Completed)
                        .SetProperty(jobFile => jobFile.LeaseExpiresAt, (DateTimeOffset?)null)
                        .SetProperty(jobFile => jobFile.HandsSaved, summary.HandsSaved)
                        .SetProperty(jobFile => jobFile.DuplicateHands, summary.DuplicateHands)
                        .SetProperty(jobFile => jobFile.InvalidHands, summary.InvalidHands)
                        .SetProperty(jobFile => jobFile.ErrorMessage, (string?)null),
                cancellationToken
            );

        if (updated == 1)
        {
            await UpdateJobIfFinishedAsync(file.JobId, cancellationToken);
        }

        return updated == 1;
    }

    public async Task<bool> FailFileAsync(
        ClaimedHandImportFile file,
        string errorMessage,
        CancellationToken cancellationToken
    )
    {
        var storedErrorMessage = errorMessage.Length > 4000 ? errorMessage[..4000] : errorMessage;
        var updated = await _dbContext
            .HandImportJobFiles.Where(jobFile =>
                jobFile.FileId == file.FileId
                && jobFile.Status == HandImportJobFileStatus.Processing
                && jobFile.AttemptCount == file.AttemptCount
            )
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(jobFile => jobFile.Status, HandImportJobFileStatus.Failed)
                        .SetProperty(jobFile => jobFile.LeaseExpiresAt, (DateTimeOffset?)null)
                        .SetProperty(jobFile => jobFile.ErrorMessage, storedErrorMessage),
                cancellationToken
            );

        if (updated == 1)
        {
            await UpdateJobIfFinishedAsync(file.JobId, cancellationToken);
        }

        return updated == 1;
    }

    private async Task UpdateJobIfFinishedAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var hasPendingFiles = await _dbContext.HandImportJobFiles.AnyAsync(
            file =>
                file.JobId == jobId
                && (
                    file.Status == HandImportJobFileStatus.Queued
                    || file.Status == HandImportJobFileStatus.Processing
                ),
            cancellationToken
        );
        if (hasPendingFiles)
        {
            return;
        }

        var hasFailedFiles = await _dbContext.HandImportJobFiles.AnyAsync(
            file => file.JobId == jobId && file.Status == HandImportJobFileStatus.Failed,
            cancellationToken
        );
        await _dbContext
            .HandImportJobs.Where(job => job.JobId == jobId)
            .ExecuteUpdateAsync(
                setters =>
                    setters
                        .SetProperty(
                            job => job.Status,
                            hasFailedFiles
                                ? HandImportJobStatus.Failed
                                : HandImportJobStatus.Completed
                        )
                        .SetProperty(job => job.CompletedAt, DateTimeOffset.UtcNow),
                cancellationToken
            );
    }

    private static HandImportJobDto ToDto(HandImportJob job)
    {
        var files = job
            .Files.OrderBy(file => file.Sequence)
            .Select(file => new HandImportJobFileDto(
                file.FileId,
                file.FileName,
                file.Status.ToString().ToLowerInvariant(),
                file.HandsSaved,
                file.DuplicateHands,
                file.InvalidHands,
                file.ErrorMessage
            ))
            .ToArray();

        return new HandImportJobDto(
            job.JobId,
            job.Status.ToString().ToLowerInvariant(),
            job.CreatedAt,
            job.StartedAt,
            job.CompletedAt,
            files.Length,
            files.Sum(file => file.HandsSaved),
            files.Sum(file => file.DuplicateHands),
            files.Sum(file => file.InvalidHands),
            files
        );
    }
}
