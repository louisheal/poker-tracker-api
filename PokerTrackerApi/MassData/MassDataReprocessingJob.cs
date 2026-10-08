namespace PokerTrackerApi.MassData;

public enum MassDataReprocessingJobStatus
{
    Queued,
    Processing,
    Completed,
    Failed,
}

public class MassDataReprocessingJob
{
    public Guid JobId { get; set; }

    public MassDataReprocessingJobStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset? LeaseExpiresAt { get; set; }

    public int AttemptCount { get; set; }

    public int TotalHands { get; set; }

    public int ProcessedHands { get; set; }

    public string? LastProcessedHandId { get; set; }

    public string? ErrorMessage { get; set; }
}

public record MassDataReprocessingJobDto(
    Guid JobId,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    int TotalHands,
    int ProcessedHands,
    string? ErrorMessage
);

public record ClaimedMassDataReprocessingJob(
    Guid JobId,
    int AttemptCount,
    string? LastProcessedHandId
);
