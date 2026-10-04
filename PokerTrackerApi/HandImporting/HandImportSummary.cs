namespace PokerTrackerApi.HandImporting;

public record HandImportSummary(
    int FilesReceived,
    int HandsSaved,
    int DuplicateHands,
    int InvalidHands
);
