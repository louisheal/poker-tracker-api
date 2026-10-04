namespace PokerTrackerApi.HandImports;

public record HandImportSummary(
    int FilesReceived,
    int HandsSaved,
    int DuplicateHands,
    int InvalidHands
);
