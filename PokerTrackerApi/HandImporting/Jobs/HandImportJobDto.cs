namespace PokerTrackerApi.HandImporting.Jobs;

public record HandImportJobDto(
    Guid JobId,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    int FilesReceived,
    int HandsSaved,
    int DuplicateHands,
    int InvalidHands,
    IReadOnlyList<HandImportJobFileDto> Files
);

public record HandImportJobFileDto(
    Guid FileId,
    string FileName,
    string Status,
    int HandsSaved,
    int DuplicateHands,
    int InvalidHands,
    string? ErrorMessage
);

