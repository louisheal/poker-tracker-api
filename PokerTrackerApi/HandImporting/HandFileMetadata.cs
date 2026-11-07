namespace PokerTrackerApi.HandImporting;

public record HandFileMetadata(
    Guid FileId,
    string FileName,
    HandFileStatus Status,
    DateTimeOffset CreatedAt
);
