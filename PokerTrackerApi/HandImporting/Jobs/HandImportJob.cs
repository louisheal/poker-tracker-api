namespace PokerTrackerApi.HandImporting.Jobs;

public enum HandImportJobStatus
{
    Queued,
    Processing,
    Completed,
    Failed,
}

public enum HandImportJobFileStatus
{
    Queued,
    Processing,
    Completed,
    Failed,
}

public class HandImportJob
{
    public Guid JobId { get; set; }

    public HandImportJobStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public ICollection<HandImportJobFile> Files { get; } = [];
}

public class HandImportJobFile
{
    public Guid FileId { get; set; }

    public Guid JobId { get; set; }

    public int Sequence { get; set; }

    public required string FileName { get; set; }

    public required string StorageKey { get; set; }

    public HandImportJobFileStatus Status { get; set; }

    public int AttemptCount { get; set; }

    public DateTimeOffset? LeaseExpiresAt { get; set; }

    public int HandsSaved { get; set; }

    public int DuplicateHands { get; set; }

    public int InvalidHands { get; set; }

    public string? ErrorMessage { get; set; }
}
