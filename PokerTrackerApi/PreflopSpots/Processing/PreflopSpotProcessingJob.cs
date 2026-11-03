namespace PokerTrackerApi.PreflopSpots.Processing;

// TODO : move into shared JobStatus enum
// and create a process jobs namespace with shared types
public enum PreflopSpotProcessingJobStatus
{
    Queued,
    Processing,
    Completed,
    Failed,
}

public class PreflopSpotProcessingJob
{
    public Guid JobId { get; set; }
    public required string HandId { get; set; }
    public PreflopSpotProcessingJobStatus Status { get; set; }
}